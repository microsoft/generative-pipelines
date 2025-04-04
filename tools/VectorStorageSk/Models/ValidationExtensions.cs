// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace VectorStorageSk.Models;

public static class ValidationExtensions
{
    public static ValidationResult? Validate(this IValidatableObject obj)
    {
        var result = obj.Validate(new ValidationContext(obj)).FirstOrDefault();
        return result?.ErrorMessage is null ? null : result;
    }
}
