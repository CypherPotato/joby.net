namespace Joby;

/// <summary>
/// Represents an job interface.
/// </summary>
public interface IJob : IDisposable {
    /// <summary>
    /// Starts the job.
    /// </summary>
    public void Start ();

    /// <summary>
    /// Stops the job.
    /// </summary>
    public void Stop ();

    /// <summary>
    /// Gets an boolean indicating if the job is running or not.
    /// </summary>
    public bool IsRunning { get; }
}
