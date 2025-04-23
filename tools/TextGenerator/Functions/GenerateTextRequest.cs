// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextGenerator.Functions;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum PromptTypes
{
    PlainText = 1,
    Handlebars = 2,
}

internal sealed class GenerateTextRequest : IValidatableObject
{
    [JsonPropertyName("modelId")]
    [JsonPropertyOrder(1)]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prompt { get; set; }

    [JsonPropertyName("systemPrompt")]
    [JsonPropertyOrder(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("promptType")]
    public PromptTypes PromptType { get; set; } = PromptTypes.PlainText;

    [JsonPropertyName("systemPromptType")]
    public PromptTypes SystemPromptType { get; set; } = PromptTypes.PlainText;

    [JsonPropertyName("promptTemplateData")]
    public object? PromptTemplateData { get; set; }

    /// <summary>
    /// The maximum number of tokens to generate in the completion.
    /// </summary>
    [JsonPropertyName("maxTokens")]
    [JsonPropertyOrder(99)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature controls the randomness of the completion.
    /// The higher the temperature, the more random the completion.
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonPropertyOrder(100)]
    public float Temperature { get; set; } = 0;

    /// <summary>
    /// Nucleus sampling, also known as TopP, controls the diversity of the completion.
    /// The lower the value, the less diverse and more focused the completion.
    /// A higher value allows for more diverse completions by considering a larger set of possible tokens.
    /// </summary>
    [JsonPropertyName("topP")]
    [JsonPropertyOrder(110)]
    public float NucleusSampling { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of most probable tokens that the model considers when generating
    /// the next part of the text. This property reduces the probability of generating nonsense.
    /// A higher value gives more diverse answers, while a lower value is more conservative.
    /// </summary>
    [JsonPropertyName("topK")]
    [JsonPropertyOrder(110)]
    public int? TruncatedSampling { get; set; } = 0;

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens
    /// based on whether they appear in the text so far, increasing the
    /// model's likelihood to talk about new topics.
    /// </summary>
    [JsonPropertyName("presencePenalty")]
    [JsonPropertyOrder(120)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PresencePenalty { get; set; }

    /// <summary>
    /// Sets the random number seed to use for generation.
    /// Setting this to a specific number will make the model generate the same
    /// text for the same prompt. (Default: 0)
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens
    /// based on their existing frequency in the text so far, decreasing
    /// the model's likelihood to repeat the same line verbatim.
    /// </summary>
    [JsonPropertyName("frequencyPenalty")]
    [JsonPropertyOrder(130)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? FrequencyPenalty { get; set; }

    public GenerateTextRequest FixState()
    {
        if (string.IsNullOrWhiteSpace(this.ModelId)) { this.ModelId = string.Empty; }

        if (string.IsNullOrWhiteSpace(this.SystemPrompt)) { this.SystemPrompt = string.Empty; }

        this.ModelId = this.ModelId.Trim();

        return this;
    }

    /// <inherit />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(this.ModelId))
        {
            yield return new ValidationResult("The modelId is required, the value is empty", [nameof(this.ModelId)]);
        }

        if (string.IsNullOrWhiteSpace(this.Prompt))
        {
            yield return new ValidationResult("A prompt is required, the value is empty", [nameof(this.Prompt)]);
        }
    }
}
