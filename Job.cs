namespace Joby;

/// <summary>
/// Represents a background job that runs periodically.
/// </summary>
public abstract class Job
{
    private static List<Job> _allJobs = new List<Job>();
    private AutoResetEvent _stopEvent = new AutoResetEvent(false);
    private Thread? _jobThread;
    private bool _isRunning = false;

    /// <summary>
    /// Gets or sets the job execution interval.
    /// </summary>
    public abstract TimeSpan Interval { get; set; }

    /// <summary>
    /// Represents the function that is called every time the job is called.
    /// </summary>
    public abstract void Run();

    /// <summary>
    /// Gets or sets the delay to run the job after starting the job.
    /// </summary>
    public virtual TimeSpan Delay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets what happens to the job when an exception is thrown.
    /// </summary>
    public virtual JobExceptionHandler ExceptionHandler { get; set; } = JobExceptionHandler.Continue;

    /// <summary>
    /// Represents the function that is executed immediately before starting the job.
    /// </summary>
    public virtual void Setup() { }
    
    /// <summary>
    /// Represents the function that runs immediately after the job is stopped.
    /// </summary>
    public virtual void Terminate() { }

    /// <summary>
    /// Represents the function that configures the job main <see cref="Thread"/>.
    /// </summary>
    /// <param name="jobThread">The main job <see cref="Thread"/>.</param>
    public virtual void ConfigureJobThread(Thread jobThread)
    {
        jobThread.IsBackground = true;
    }

    /// <summary>
    /// Represents the function that runs when a job encounters an error.
    /// </summary>
    /// <param name="ex">Represents the object of the thrown exception.</param>
    /// <param name="context">Represents the context from where the exception was thrown.</param>
    public virtual void OnException(Exception ex, JobEventContext context)
    {
        Console.WriteLine($"Exception caught on job {this.GetType().FullName}, on {context}, at {DateTime.Now:g}:");
        Console.WriteLine(ex);
    }

    /// <summary>
    /// Represents the function that is called to get the interval time to wait for the next job call.
    /// </summary>
    /// <returns>By default, this function returns <see cref="Interval"/>.</returns>
    public virtual TimeSpan GetNextInterval()
    {
        return Interval;
    }

    /// <summary>
    /// Gets whether this job is running or not.
    /// </summary>
    public bool IsRunning { get => _isRunning; }

    /// <summary>
    /// Gets or sets whether this job can be started or not. This property does not start or stop the job.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Restarts the job, waiting for the current iteration to finish and starts the job again. This function reconfigures the
    /// job with <see cref="Setup"/>.
    /// </summary>
    public void Restart()
    {
        Stop();
        _stopEvent.WaitOne();
        Start();
    }

    /// <summary>
    /// Stops the execution of the job. Current job iteration will still run until finished.
    /// </summary>
    /// <param name="blocking">Specifies if the current thread should wait for the job to finish.</param>
    public void Stop(bool blocking = false)
    {
        if (!_isRunning) return;
        _isRunning = false;

        if (blocking)
        {
            _stopEvent.WaitOne();
        }
    }

    /// <summary>
    /// Setups and starts the job.
    /// </summary>
    public void Start()
    {
        if (!Enabled)
        {
            throw new InvalidOperationException("This job is not enabled to run.");
        }
        if (_isRunning)
        {
            return;
        }
        _isRunning = true;
        var threadStart = new ThreadStart(ThreadHandler);
        _jobThread = new Thread(threadStart);
        ConfigureJobThread(_jobThread);

        SafeCall(Setup, JobEventContext.OnSetup);
        _stopEvent.Reset();
        _jobThread.Start();
    }

    /// <summary>
    /// Gets all started jobs which implements <typeparamref name="TJob"/> and was
    /// started by <see cref="Start{TJob}()"/>.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    public static IEnumerable<Job> GetRunningJobs<TJob>() where TJob : Job
    {
        foreach (Job j in _allJobs)
        {
            if (j is TJob)
            {
                yield return j;
            }
        }
    }

    /// <summary>
    /// Gets the first started job which <typeparamref name="TJob"/> and was
    /// started by <see cref="Start{TJob}()"/>.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <returns>The <see cref="Job"/>, or null if it was not defined.</returns>
    public static Job? GetRunningJob<TJob>() where TJob : Job
    {
        foreach (Job j in _allJobs)
        {
            if (j is TJob)
            {
                return j;
            }
        }
        return null;
    }

    /// <summary>
    /// Forces all jobs which implements <typeparamref name="TJob"/> to run once. This function does not interfere with the state it is in, that is, it does not start the work.
    /// </summary>
    /// <typeparam name="TJob">An type which implements <see cref="Job"/>.</typeparam>
    public static void ForceRun<TJob>() where TJob : Job
    {
        foreach (Job j in _allJobs)
        {
            if (j.GetType() == typeof(TJob))
            {
                j.Run();
            }
        }
    }

    /// <summary>
    /// Creates an new instance of the specified job object and starts it.
    /// </summary>
    /// <typeparam name="TJob">An type which implements <see cref="Job"/>.</typeparam>
    /// <returns>The new instance of the job.</returns>
    public static TJob Start<TJob>() where TJob : Job, new()
    {
        TJob job = new TJob();
        job.Start();

        _allJobs.Add(job);

        return job;
    }

    /// <summary>
    /// Defines the specified job object and starts it.
    /// </summary>
    /// <param name="job">The job to start.</param>
    /// <returns>The provided job.</returns>
    public static Job Start(Job job)
    {
        job.Start();

        _allJobs.Add(job);

        return job;
    }

    /// <summary>
    /// Stop all jobs started with <see cref="Start{TJob}()"/>.
    /// </summary>
    /// <param name="blocking">Specifies if the current thread should wait for the jobs to finish.</param>
    public static void StopAll(bool blocking = false)
    {
        foreach (Job j in _allJobs)
            j.Stop(blocking);
    }

    private void SafeCall(Action function, JobEventContext context)
    {
        try
        {
            function();
        }
        catch (Exception ex)
        {
            OnException(ex, context);
        }
    }

    private void ThreadHandler()
    {
        // this entire method is running inside an dedicated thread for the job
        Thread.Sleep(Delay);
        while (_isRunning)
        {
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                OnException(ex, JobEventContext.OnRun);

                if (ExceptionHandler == JobExceptionHandler.Stop)
                {
                    Stop();
                }
            }
            finally
            {
                TimeSpan interval = GetNextInterval();
                Thread.Sleep(interval);
            }
        }
        SafeCall(Terminate, JobEventContext.OnTerminate);
        _isRunning = false;
        _stopEvent.Set();
    }
}