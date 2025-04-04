// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class SearchRequest : CollectionRequest, IValidatableObject
{
    // Temporary workaround for the lack of support for VectorStoreGenericDataModel in MEVD
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    // To be used once MEVD supports search on VectorStoreGenericDataModel
    [JsonPropertyName("fields")]
    public List<FieldDefinition> Fields { get; set; } = new();

    // Query string with filter expression, AND, OR, NOT, etc.
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filter { get; set; }

    [JsonPropertyName("vector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Vector { get; set; }

    // Full text search
    [JsonPropertyName("keywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Keywords { get; set; }

    [JsonPropertyName("top")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Top { get; set; }

    [JsonPropertyName("skip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Skip { get; set; }

    [JsonIgnore]
    public bool IsHybridSearch
    {
        get
        {
            return (!string.IsNullOrWhiteSpace(this.Filter));
        }
    }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (this.Fields.Count > 0)
        {
            foreach (var result in this.Fields.SelectMany(f => f.Validate(validationContext)))
            {
                yield return result;
            }
        }

        var vectorField = this.Fields.FirstOrDefault(f => f.Type == FieldTypes.Embedding);
        if (vectorField == null)
        {
            // SK VectorStore library limitation
            yield return new ValidationResult("Search requires information about the vector field", [nameof(this.Fields)]);
        }

        if (vectorField != null && this.Vector != null && this.Vector.Length != vectorField.VectorSize)
        {
            yield return new ValidationResult($"The size of the vector must be {vectorField.VectorSize}", [nameof(this.Vector)]);
        }

        // TODO: check fields

        if (this.Top is < 1)
        {
            yield return new ValidationResult("The value of Top cannot be zero or negative", [nameof(this.Top)]);
        }

        if (this.Skip is < 0)
        {
            yield return new ValidationResult("The value of Skip cannot be negative", [nameof(this.Skip)]);
        }
    }
}
