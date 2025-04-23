// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CommonDotNet.Models;

public static class ValidationExtensions
{
    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the object is null.
    /// </summary>
    public static T EnsureValid<T>(this T? obj) where T : IValidatableObject
    {
        obj.AssertValidity(typeName: typeof(T).FullName);
        return obj;
    }

    /// <summary>
    /// Determines whether the object's state is valid.
    /// </summary>
    /// <returns><c>true</c> if the object is in a valid state; otherwise, <c>false</c>.</returns>
    public static bool IsValid(this IValidatableObject obj, out string errMsg)
    {
        if (obj == null)
        {
            errMsg = "The object is null";
            return false;
        }

        errMsg = string.Empty;
        ValidationResult? firstError = obj.Validate(new ValidationContext(obj)).FirstOrDefault();
        if (firstError == null) { return true; }

        errMsg = firstError.ErrorMessage ?? "The object state is not valid";
        return false;
    }

    /// <summary>
    /// Ensures the object's state is valid. Throws an <see cref="ValidationException"/> if invalid.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when the object is in an invalid state.</exception>
    public static void AssertValidity([NotNull] this IValidatableObject? obj, string? typeName = null)
    {
        if (obj == null)
        {
            if (string.IsNullOrWhiteSpace(typeName)) { throw new ValidationException("The object is null"); }

            throw new ValidationException($"The '{typeName}' instance is null");
        }

        if (obj.IsValid(out string errMsg)) { return; }

        throw new ValidationException(errMsg);
    }
}
