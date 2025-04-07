namespace Joby;

/// <summary>
/// Represents a background job that runs periodically and which it's <see cref="Run"/> function is async.
/// </summary>
public abstract class AsyncJob : Job {
    /// <summary>
    /// Represents the async function that is called every time the job is called.
    /// </summary>
    public abstract Task RunAsync ();

    /// <summary>
    /// Starts a new asynchronous job with the specified action and scheduling parameters.
    /// </summary>
    /// <param name="jobAction">The asynchronous action to execute as the job.</param>
    /// <param name="interval">The interval between job executions.</param>
    /// <param name="delay">The initial delay before the job starts.</param>
    /// <param name="exceptionHandler">The action to handle exceptions that occur during job execution.</param>
    /// <returns>The newly created inline job.</returns>
    public static IJob StartNewAsync ( Func<Task> jobAction, TimeSpan? interval, TimeSpan? delay, Action<Exception> exceptionHandler ) {
        return Start ( new InlineJob ( interval ?? TimeSpan.FromMinutes ( 10 ), delay ?? TimeSpan.Zero, exceptionHandler, jobAction ) );
    }

    /// <inheritdoc/>
    public override void Run () {
        var task = this.RunAsync ();
        task.GetAwaiter ().GetResult ();
    }

    class InlineJob : AsyncJob {

        private TimeSpan _interval;
        private Action<Exception> _exceptionHandler;
        private Func<Task> _action;

        public InlineJob ( TimeSpan interval, TimeSpan delay, Action<Exception> exceptionHandler, Func<Task> action ) {
            this._interval = interval;
            this._exceptionHandler = exceptionHandler;
            this._action = action;

            this.Delay = delay;
        }

        public override TimeSpan GetNextInterval () {
            return this._interval;
        }

        public override void OnException ( Exception ex, JobEventContext context ) {
            this._exceptionHandler ( ex );
        }

        public override Task RunAsync () {
            return this._action ();
        }
    }
}