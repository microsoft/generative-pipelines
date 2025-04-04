// Copyright (c) Microsoft. All rights reserved.

namespace CommonDotNet.Models;

public class ValidationException : AppException
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ValidationException()
        : this(message: null, innerException: null)
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ValidationException(string? message)
        : this(message, innerException: null)
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="message">A string that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidationException(string? message, Exception? innerException)
        : base(message, innerException, isTransient: false)
    {
    }
}
