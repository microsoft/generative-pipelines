// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Config;

internal sealed class ModelInfo : IValidatableObject
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum ModelProviders
    {
        AzureAI,
        OpenAI,
    }

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public ModelProviders Provider { get; set; } = ModelProviders.AzureAI;

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("deployment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Deployment { get; set; }

    [JsonPropertyName("endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Endpoint { get; set; }

    [JsonPropertyName("maxDimensions")]
    public int MaxDimensions { get; set; }

    [JsonPropertyName("supportsCustomDimensions")]
    public bool SupportsCustomDimensions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Provider != ModelProviders.AzureAI && this.Provider != ModelProviders.OpenAI)
        {
            yield return new ValidationResult("The model provider is required, the value is invalid", [nameof(this.Provider)]);
        }

        if (this.Provider == ModelProviders.AzureAI && string.IsNullOrWhiteSpace(this.Deployment))
        {
            yield return new ValidationResult("The Azure deployment name is required, the value is empty", [nameof(this.Deployment)]);
        }

        if (this.Provider == ModelProviders.OpenAI && string.IsNullOrWhiteSpace(this.Model))
        {
            yield return new ValidationResult("The OpenAI model name is required, the value is empty", [nameof(this.Model)]);
        }

        if (string.IsNullOrWhiteSpace(this.ModelId))
        {
            yield return new ValidationResult("The model ID is required, the value is empty", [nameof(this.ModelId)]);
        }

        if (this.MaxDimensions < 1)
        {
            yield return new ValidationResult("The max dimensions cannot be less than 1", [nameof(this.MaxDimensions)]);
        }
    }
}
