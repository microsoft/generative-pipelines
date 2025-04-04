// Copyright (c) Microsoft. All rights reserved.

namespace CommonDotNet.Models;

public interface IValidatable<T>
{
    /// <summary>
    /// Attempts to automatically fix any issues in the object's state.
    /// </summary>
    /// <returns>The instance after attempting to fix its state.</returns>
    T FixState();

    /// <summary>
    /// Determines whether the object's state is valid.
    /// </summary>
    /// <returns><c>true</c> if the object is in a valid state; otherwise, <c>false</c>.</returns>
    bool IsValid(out string errMsg);

    /// <summary>
    /// Ensures the object's state is valid. Throws an <see cref="ValidationException"/> if invalid.
    /// </summary>
    /// <returns>The validated instance.</returns>
    /// <exception cref="ValidationException">Thrown when the object is in an invalid state.</exception>
    T Validate();
}
