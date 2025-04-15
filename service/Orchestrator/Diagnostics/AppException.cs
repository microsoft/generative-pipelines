// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Diagnostics;

/// <summary>
/// Provides the base exception from which all other exceptions derive.
/// </summary>
public class AppException : Exception
{
    public bool? IsTransient { get; protected init; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppException"/> class with a default message.
    /// </summary>
    /// <param name="isTransient">Optional parameter to indicate if the error is temporary and might disappear by retrying.</param>
    public AppException(bool? isTransient = null)
    {
        this.IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="isTransient">Optional parameter to indicate if the error is temporary and might disappear by retrying.</param>
    public AppException(string? message, bool? isTransient = null) : base(message)
    {
        this.IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppException"/> class with its message set to <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="isTransient">Optional parameter to indicate if the error is temporary and might disappear by retrying.</param>
    public AppException(string? message, Exception? innerException, bool? isTransient = null) : base(message, innerException)
    {
        this.IsTransient = isTransient;
    }
}
