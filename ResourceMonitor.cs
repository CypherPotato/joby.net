using System.Diagnostics;

namespace Joby.Metrics;

/// <summary>
/// Measures resource usage of the current process.
/// </summary>
public sealed class ResourceMeasurer : IDisposable {
    private readonly Process _process;
    private readonly Stopwatch _watch;
    private readonly TimeSpan _cpuStart;
    private readonly long _allocatedStart;
    private readonly long _workingSetStart;
    private readonly long _gcMemoryStart;

    private ResourceMeasurer () {
        _process = Process.GetCurrentProcess ();
        _process.Refresh ();

        _watch = Stopwatch.StartNew ();
        _cpuStart = _process.TotalProcessorTime;
        _allocatedStart = GC.GetAllocatedBytesForCurrentThread ();
        _workingSetStart = _process.WorkingSet64;
        _gcMemoryStart = GC.GetTotalMemory ( false );
    }

    /// <summary>
    /// Creates a new <see cref="ResourceMeasurer"/> instance and starts capturing resource metrics for the current process.
    /// </summary>
    /// <returns>A <see cref="ResourceMeasurer"/> that is already measuring.</returns>
    public static ResourceMeasurer StartCapturingCurrent ()
        => new ();

    /// <summary>
    /// Gets a snapshot of resource usage since the measurement started.
    /// </summary>
    /// <returns>A <see cref="ResourceSnapshot"/> containing the measured values.</returns>
    public ResourceSnapshot Snapshot () {
        _process.Refresh ();

        var elapsed = _watch.Elapsed;
        var cpu = _process.TotalProcessorTime - _cpuStart;
        var allocated = GC.GetAllocatedBytesForCurrentThread () - _allocatedStart;
        var workingSet = _process.WorkingSet64 - _workingSetStart;
        var gcMemory = GC.GetTotalMemory ( false ) - _gcMemoryStart;

        return new ResourceSnapshot (
            elapsed,
            cpu,
            allocated,
            workingSet,
            gcMemory
        );
    }

    /// <summary>
    /// Stops the internal timer and releases the underlying <see cref="Process"/> resources.
    /// </summary>
    public void Dispose () {
        _watch.Stop ();
        _process.Dispose ();
    }
}

/// <summary>
/// Immutable snapshot of resource usage metrics.
/// </summary>
/// <param name="Elapsed">The total elapsed time since measurement began.</param>
/// <param name="CpuTime">The amount of CPU time consumed by the process since measurement began.</param>
/// <param name="AllocatedBytesCurrentThread">
/// The number of bytes allocated on the current thread since measurement began.
/// </param>
/// <param name="WorkingSetDeltaBytes">
/// The change in the process's working set size (in bytes) since measurement began.
/// </param>
/// <param name="ManagedHeapDeltaBytes">
/// The change in the managed heap size (in bytes) since measurement began.
/// </param>
public sealed record ResourceSnapshot (
    TimeSpan Elapsed,
    TimeSpan CpuTime,
    long AllocatedBytesCurrentThread,
    long WorkingSetDeltaBytes,
    long ManagedHeapDeltaBytes
);