// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EmbeddingGenerator.Models;

namespace EmbeddingGenerator.Functions;

internal sealed class CustomEmbeddingRequest : IValidatableObject
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModelProviders
    {
        AzureAI,
        OpenAI,
    }

    [JsonPropertyName("provider")]
    [JsonPropertyOrder(1)]
    public ModelProviders Provider { get; set; } = ModelProviders.AzureAI;

    [JsonPropertyName("endpoint")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Endpoint { get; set; }

    [JsonPropertyName("auth")]
    [JsonPropertyOrder(3)]
    public AuthTypes Auth { get; set; } = AuthTypes.DefaultAzureCredential;

    [JsonPropertyName("apiKey")]
    [JsonPropertyOrder(3)]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("deployment")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Deployment { get; set; }

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

    [JsonPropertyName("maxDimensions")]
    public int MaxDimensions { get; set; } = 1536;

    [JsonPropertyName("supportsCustomDimensions")]
    public bool SupportsCustomDimensions { get; set; } = false;

    public CustomEmbeddingRequest FixState()
    {
        if (string.IsNullOrWhiteSpace(this.ApiKey)) { this.ApiKey = string.Empty; }

        this.ApiKey = this.ApiKey.Trim();

        if (string.IsNullOrWhiteSpace(this.Endpoint)) { this.Endpoint = string.Empty; }

        this.Endpoint = this.Endpoint.Trim();

        return this;
    }

    /// <inherit />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        this.FixState();

        if ((this.Inputs == null || this.Inputs.Count == 0) && string.IsNullOrEmpty(this.Input))
        {
            yield return new ValidationResult($"Both {nameof(this.Input)} and {nameof(this.Inputs)} are empty", [nameof(this.Input), nameof(this.Inputs)]);
        }

        if (this.Inputs?.Count > 0 && !string.IsNullOrEmpty(this.Input))
        {
            yield return new ValidationResult($"Both {nameof(this.Input)} and {nameof(this.Inputs)} are provided, only one is allowed, specifying either a single value or a list of values", [nameof(this.Input), nameof(this.Inputs)]);
        }

        if (!Enum.IsDefined(typeof(AuthTypes), this.Auth))
        {
            yield return new ValidationResult($"The auth type '{this.Auth}' is not valid", [nameof(this.Auth)]);
        }

        if (this.Auth == AuthTypes.ApiKey && string.IsNullOrWhiteSpace(this.ApiKey))
        {
            yield return new ValidationResult("The API key is required, the value is empty", [nameof(this.ApiKey)]);
        }

        if (this.Provider == ModelProviders.OpenAI && string.IsNullOrWhiteSpace(this.Model))
        {
            yield return new ValidationResult("The OpenAI model name is required, the value is empty", [nameof(this.Model)]);
        }

        if (this.Provider == ModelProviders.AzureAI && string.IsNullOrWhiteSpace(this.Deployment))
        {
            yield return new ValidationResult("The Azure deployment name is required, the value is empty", [nameof(this.Deployment)]);
        }

        if (this.Provider == ModelProviders.AzureAI && !Uri.TryCreate(this.Endpoint, UriKind.Absolute, out _))
        {
            yield return new ValidationResult($"The endpoint '{this.Endpoint}' is not valid", [nameof(this.Endpoint)]);
        }
    }
}
