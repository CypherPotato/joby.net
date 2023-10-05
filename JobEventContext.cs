namespace Joby;

/// <summary>
/// Represents the context from which a job event was raised.
/// </summary>
public enum JobEventContext
{
    /// <summary>
    /// This event was raised from the job <see cref="Job.Run"/> method.
    /// </summary>
    OnRun,

    /// <summary>
    /// This event was raised from the job <see cref="Job.Setup"/> method.
    /// </summary>
    OnSetup,

    /// <summary>
    /// This event was raised from the job <see cref="Job.Terminate"/> method.
    /// </summary>
    OnTerminate
}