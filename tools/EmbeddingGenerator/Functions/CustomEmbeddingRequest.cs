// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EmbeddingGenerator.Models;

namespace EmbeddingGenerator.Functions;

internal sealed class CustomEmbeddingRequest : EmbeddingRequest, IValidatableObject
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModelProviders
    {
        AzureAI,
        OpenAI,
    }

    [JsonPropertyName("provider")]
    public ModelProviders Provider { get; set; } = ModelProviders.AzureAI;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("auth")]
    public AuthTypes Auth { get; set; } = AuthTypes.DefaultAzureCredential;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("maxDimensions")]
    public int MaxDimensions { get; set; } = 1536;

    [JsonPropertyName("supportsCustomDimensions")]
    public bool SupportsCustomDimensions { get; set; } = false;

    public new CustomEmbeddingRequest FixState()
    {
        base.FixState();
        if (string.IsNullOrWhiteSpace(this.ApiKey)) { this.ApiKey = string.Empty; }

        this.ApiKey = this.ApiKey.Trim();

        if (string.IsNullOrWhiteSpace(this.Endpoint)) { this.Endpoint = string.Empty; }

        this.Endpoint = this.Endpoint.Trim();

        return this;
    }

    /// <inherit />
    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        this.FixState();

        foreach (var x in base.Validate(validationContext))
        {
            yield return x;
        }

        if (!Enum.IsDefined(typeof(AuthTypes), this.Auth))
        {
            yield return new ValidationResult($"The auth type '{this.Auth}' is not valid", [nameof(this.Auth)]);
        }

        if (this.Provider == ModelProviders.AzureAI && !Uri.TryCreate(this.Endpoint, UriKind.Absolute, out _))
        {
            yield return new ValidationResult($"The endpoint '{this.Endpoint}' is not valid", [nameof(this.Endpoint)]);
        }

        if (this.Auth == AuthTypes.ApiKey && string.IsNullOrWhiteSpace(this.ApiKey))
        {
            yield return new ValidationResult("The API key is required, the value is empty", [nameof(this.ApiKey)]);
        }
    }
}
