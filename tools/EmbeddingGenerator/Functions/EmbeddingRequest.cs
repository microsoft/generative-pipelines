// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Functions;

internal class EmbeddingRequest : IValidatableObject
{
    [JsonPropertyName("modelId")]
    [JsonPropertyOrder(1)]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Input { get; set; }

    [JsonPropertyName("inputs")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Inputs { get; set; }

    [JsonPropertyName("dimensions")]
    [JsonPropertyOrder(13)]
    public int? Dimensions { get; set; }

    [JsonPropertyName("cache")]
    [JsonPropertyOrder(30)]
    public bool Cache { get; set; } = true;

    [JsonPropertyName("ignoreCache")]
    [JsonPropertyOrder(31)]
    public bool IgnoreCache { get; set; } = false;

    public EmbeddingRequest FixState()
    {
        if (string.IsNullOrWhiteSpace(this.ModelId)) { this.ModelId = string.Empty; }

        this.ModelId = this.ModelId.Trim();

        if (this.Dimensions < 0) { this.Dimensions = null; }

        return this;
    }

    /// <inherit />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        this.FixState();

        if (string.IsNullOrWhiteSpace(this.ModelId))
        {
            yield return new ValidationResult("The modelId is required, the value is empty", [nameof(this.ModelId)]);
        }

        if ((this.Inputs == null || this.Inputs.Count == 0) && string.IsNullOrEmpty(this.Input))
        {
            yield return new ValidationResult($"Both {nameof(this.Input)} and {nameof(this.Inputs)} are empty", [nameof(this.Input), nameof(this.Inputs)]);
        }

        if (this.Inputs?.Count > 0 && !string.IsNullOrEmpty(this.Input))
        {
            yield return new ValidationResult($"Both {nameof(this.Input)} and {nameof(this.Inputs)} are provided, only one is allowed, specifying either a single value or a list of values", [nameof(this.Input), nameof(this.Inputs)]);
        }
    }
}
