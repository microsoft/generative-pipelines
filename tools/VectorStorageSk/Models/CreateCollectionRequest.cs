// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class CreateCollectionRequest : CollectionRequest, IValidatableObject
{
    [JsonPropertyName("fields")]
    public List<FieldDefinition> Fields { get; set; } = new();

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (this.Fields.Count == 0)
        {
            yield return new ValidationResult("The list of fields is missing or empty", [nameof(this.Fields)]);
        }
        else
        {
            foreach (var result in this.Fields.SelectMany(f => f.Validate(validationContext)))
            {
                yield return result;
            }
        }
    }
}
