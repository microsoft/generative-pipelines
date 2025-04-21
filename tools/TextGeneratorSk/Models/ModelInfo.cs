// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextGeneratorSk.Models;

internal sealed class ModelInfo : IValidatableObject
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum ModelProviders
    {
        AzureAI,
        OpenAI,
        Ollama,
    }

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public ModelProviders Provider { get; set; } = ModelProviders.AzureAI;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Endpoint { get; set; }

    [JsonPropertyName("contextWindow")]
    public long ContextWindow { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public long MaxOutputTokens { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Provider != ModelProviders.AzureAI && this.Provider != ModelProviders.OpenAI)
        {
            yield return new ValidationResult("The model provider is required, the value is invalid", [nameof(this.Provider)]);
        }

        if (string.IsNullOrWhiteSpace(this.ModelId))
        {
            yield return new ValidationResult("The model ID is required, the value is empty", [nameof(this.ModelId)]);
        }

        if (string.IsNullOrWhiteSpace(this.Model))
        {
            yield return new ValidationResult("The model name is required, the value is empty", [nameof(this.Model)]);
        }

        if (this.ContextWindow < 1)
        {
            yield return new ValidationResult("The context window cannot be less than 1", [nameof(this.ContextWindow)]);
        }

        if (this.MaxOutputTokens < 1)
        {
            yield return new ValidationResult("The max output tokens cannot be less than 1", [nameof(this.MaxOutputTokens)]);
        }
    }
}
