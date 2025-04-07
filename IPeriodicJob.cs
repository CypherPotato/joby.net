namespace Joby;

/// <summary>
/// Represents an job which runs on intervals.
/// </summary>
public interface IPeriodicJob {
    /// <summary>
    /// Gets or sets an boolean indicating if this job can be started or not.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Represents the function that is called to get the interval time to wait for the next job call.
    /// </summary>
    /// <returns>Returns the time to wait before the next run is called.</returns>
    public TimeSpan GetNextInterval ();
}
