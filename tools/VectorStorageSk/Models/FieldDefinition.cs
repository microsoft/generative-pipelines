// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class FieldDefinition : IValidatableObject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public FieldTypes Type { get; set; } = FieldTypes.Undefined;

    [JsonPropertyName("isFilterable")]
    public bool IsFilterable { get; set; } = false;

    [JsonPropertyName("isFullTextSearchable")]
    public bool IsFullTextSearchable { get; set; } = false;

    [JsonPropertyName("vectorSize")]
    public int? VectorSize { get; set; }

    [JsonPropertyName("vectorDistance")]
    public VectorDistanceFunctions VectorDistanceFunction { get; set; } = VectorDistanceFunctions.CosineSimilarity;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            yield return new ValidationResult("Field name empty or not defined", [nameof(this.Name)]);
        }

        if (this.Type == FieldTypes.Embedding)
        {
            if (this.VectorSize == null)
            {
                yield return new ValidationResult($"Vector size not defined for field '{this.Name}'", [nameof(this.VectorSize)]);
            }
            else if (this.VectorSize <= 0)
            {
                yield return new ValidationResult($"Invalid vector size '{this.VectorSize}' for field '{this.Name}'", [nameof(this.VectorSize)]);
            }
        }

        if (!Enum.IsDefined(typeof(VectorDistanceFunctions), this.VectorDistanceFunction))
        {
            yield return new ValidationResult($"Unknown distance function '{this.VectorDistanceFunction:G}' for field '{this.Name}'", [nameof(this.Type)]);
        }

        if (!Enum.IsDefined(typeof(FieldTypes), this.Type))
        {
            yield return new ValidationResult($"Unknown type '{this.Type:G}' for field '{this.Name}'", [nameof(this.Type)]);
        }
    }
}
