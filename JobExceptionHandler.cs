using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joby;

/// <summary>
/// Represents the action that the job should take after encountering an error.
/// </summary>
public enum JobExceptionHandler
{
    /// <summary>
    /// The job should continue and ignore the exception.
    /// </summary>
    Continue,

    /// <summary>
    /// The job should stop.
    /// </summary>
    Stop
}