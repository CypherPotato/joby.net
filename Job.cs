using System.Timers;
using Timer = System.Timers.Timer;

namespace Joby;

/// <summary>
/// Represents a background job that runs periodically.
/// </summary>
public abstract class Job : IJob, IPeriodicJob {
    private static List<IJob> _allJobs = new List<IJob> ();
    private AutoResetEvent _jobEvent = new AutoResetEvent ( false );
    private Timer itimer = new Timer ();
    private bool _isInitialized = false;
    private bool _isRunning = false;
    private bool disposedValue;

    /// <summary>
    /// Represents the function that is called to get the interval time to wait for the next job call.
    /// </summary>
    public abstract TimeSpan GetNextInterval ();

    /// <summary>
    /// Represents the function that is called every time the job is called.
    /// </summary>
    public abstract void Run ();

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
    public virtual void Setup () { }

    /// <summary>
    /// Represents the function that runs immediately after the job is stopped.
    /// </summary>
    public virtual void Terminate () { }

    /// <summary>
    /// Represents the function that runs when a job encounters an error.
    /// </summary>
    /// <param name="ex">Represents the object of the thrown exception.</param>
    /// <param name="context">Represents the context from where the exception was thrown.</param>
    public virtual void OnException ( Exception ex, JobEventContext context ) {
        Console.WriteLine ( $"Exception caught on job {GetType ().FullName}, on {context}, at {DateTime.Now:g}:" );
        Console.WriteLine ( ex );
    }

    /// <summary>
    /// Gets whether this job is running or not.
    /// </summary>
    public bool IsRunning { get => _isRunning; }

    /// <summary>
    /// Gets whether this job is disposed or not.
    /// </summary>
    public bool IsDisposed { get => disposedValue; }

    /// <summary>
    /// Gets or sets whether this job can be started or not. This property does not start or stop the job.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Restarts the job, waiting for the current iteration to finish and starts the job again. This function reconfigures the
    /// job with <see cref="Setup"/>.
    /// </summary>
    public void Restart () {
        Stop ();
        _jobEvent.WaitOne ();
        Start ();
    }

    /// <summary>
    /// Stops the execution of the job. Current job iteration will still run until finished.
    /// </summary>
    public void Stop () {
        Stop ( false );
    }

    /// <summary>
    /// Stops the execution of the job. Current job iteration will still run until finished.
    /// </summary>
    /// <param name="blocking">Specifies if the current thread should wait for the job to finish.</param>
    public void Stop ( bool blocking ) {
        if (!_isRunning)
            return;

        SafeCall ( Terminate, JobEventContext.OnTerminate );
        itimer.Stop ();
        _isRunning = false;

        if (blocking) {
            _jobEvent.WaitOne ();
        }
    }

    /// <summary>
    /// Setups and starts the job.
    /// </summary>
    public void Start () {
        if (disposedValue)
            throw new ObjectDisposedException ( nameof ( Job ) );
        if (!Enabled)
            throw new InvalidOperationException ( "This job is not enabled." );
        if (_isRunning)
            return;

        if (!SafeCall ( Setup, JobEventContext.OnSetup ))
            return;

        if (!_isInitialized) {
            itimer.AutoReset = false;
            itimer.Elapsed += Tick;
            itimer.Interval = Math.Max ( Delay.TotalMilliseconds, 0.1 );

            _isInitialized = true;
        }

        _isRunning = true;

        itimer.Start ();
        _jobEvent.Reset ();
    }

    /// <summary>
    /// Gets all started jobs which implements <typeparamref name="TJob"/> and was
    /// started by <see cref="Start{TJob}()"/>.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    public static IEnumerable<IJob> GetRunningJobs<TJob> () where TJob : IJob {
        foreach (IJob j in _allJobs) {
            if (j is TJob) {
                yield return j;
            }
        }
    }

    /// <summary>
    /// Gets the first started job which <typeparamref name="TJob"/> and was
    /// started by <see cref="Start{TJob}()"/>.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <returns>The <see cref="IJob"/>, or null if it was not defined.</returns>
    public static IJob? GetRunningJob<TJob> () where TJob : IJob {
        foreach (IJob j in _allJobs) {
            if (j is TJob) {
                return j;
            }
        }
        return null;
    }

    /// <summary>
    /// Creates an new instance of the specified job object and starts it.
    /// </summary>
    /// <typeparam name="TJob">An type which implements <see cref="IJob"/>.</typeparam>
    /// <returns>The new instance of the job.</returns>
    public static TJob Start<TJob> () where TJob : IJob, new() {
        TJob job = new TJob ();
        job.Start ();

        _allJobs.Add ( job );

        return job;
    }

    /// <summary>
    /// Defines the specified job object and starts it.
    /// </summary>
    /// <param name="job">The job to start.</param>
    /// <returns>The provided job.</returns>
    public static IJob Start ( IJob job ) {
        job.Start ();

        _allJobs.Add ( job );

        return job;
    }

    /// <summary>
    /// Starts a new inline job with the specified action and scheduling parameters.
    /// </summary>
    /// <param name="jobAction">The action to execute as the job.</param>
    /// <param name="interval">The interval between job executions.</param>
    /// <param name="delay">The initial delay before the job starts.</param>
    /// <param name="exceptionHandler">The action to handle exceptions that occur during job execution.</param>
    /// <returns>The newly created inline job.</returns>
    public static IJob StartNew ( Action jobAction, TimeSpan? interval, TimeSpan? delay, Action<Exception> exceptionHandler ) {
        return Start ( new InlineJob ( interval ?? TimeSpan.FromMinutes ( 10 ), delay ?? TimeSpan.Zero, exceptionHandler, jobAction ) );
    }

    /// <summary>
    /// Stop all jobs started with <see cref="Start{TJob}()"/>.
    /// </summary>
    public static void StopAll () {
        foreach (IJob j in _allJobs)
            j.Stop ();
    }

    private bool SafeCall ( Action function, JobEventContext context ) {
        try {
            function ();
            return true;
        }
        catch (Exception ex) {
            OnException ( ex, context );
            return false;
        }
    }

    private void Tick ( object? state, ElapsedEventArgs /*DO NOT USE*/_ ) {
        try {
            if (!_isRunning)
                return;

            Run ();
        }
        catch (Exception ex) {
            OnException ( ex, JobEventContext.OnRun );
            if (ExceptionHandler == JobExceptionHandler.Stop) {
                Stop ();
            }
        }
        finally {
            _jobEvent.Set ();

            TimeSpan nextInterval = GetNextInterval ();
            double tm = nextInterval.TotalMilliseconds;

            if (tm > 0 && state is Timer t) {
                t.Interval = tm;
                t.Start ();
            }
        }
    }

    class InlineJob : Job {

        private TimeSpan _interval;
        private Action<Exception> _exceptionHandler;
        private Action _action;

        public InlineJob ( TimeSpan interval, TimeSpan delay, Action<Exception> exceptionHandler, Action action ) {
            _interval = interval;
            _exceptionHandler = exceptionHandler;
            _action = action;

            Delay = delay;
        }

        public override TimeSpan GetNextInterval () {
            return _interval;
        }

        public override void OnException ( Exception ex, JobEventContext context ) {
            _exceptionHandler ( ex );
        }

        public override void Run () {
            _action ();
        }
    }

    /// <summary>
    /// Releases the resources used by the current instance.
    /// </summary>
    /// <param name="disposing">A boolean value indicating whether the method was called directly or by the garbage collector.</param>
    protected virtual void Dispose ( bool disposing ) {
        if (!disposedValue) {
            if (disposing) {
                Stop ( true );
            }

            disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose () {
        Dispose ( disposing: true );
        GC.SuppressFinalize ( this );
    }
}