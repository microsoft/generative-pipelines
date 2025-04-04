// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal abstract class CollectionRequest : StorageRequest, IValidatableObject
{
    [JsonPropertyName("collection")]
    public string CollectionName { get; set; } = string.Empty;

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (string.IsNullOrWhiteSpace(this.CollectionName))
        {
            yield return new ValidationResult("Collection name not defined", [nameof(this.CollectionName)]);
        }
    }
}
