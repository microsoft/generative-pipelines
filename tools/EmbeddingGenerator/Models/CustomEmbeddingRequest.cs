// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CommonDotNet.Models;

namespace EmbeddingGenerator.Models;

internal sealed class CustomEmbeddingRequest : EmbeddingRequest, IValidatable<CustomEmbeddingRequest>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthTypes
    {
        AzureIdentity,
        APIKey
    }

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
    public AuthTypes Auth { get; set; } = AuthTypes.AzureIdentity;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("maxDimensions")]
    public int MaxDimensions { get; set; } = 1536;

    [JsonPropertyName("supportsCustomDimensions")]
    public bool SupportsCustomDimensions { get; set; } = false;

    /// <inherit />
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
    public new bool IsValid(out string errMsg)
    {
        if (!base.IsValid(out errMsg)) { return false; }

        if (!Uri.TryCreate(this.Endpoint, UriKind.Absolute, out _))
        {
            errMsg = $"The endpoint '{this.Endpoint}' is not valid";
            return false;
        }

        if (this.Auth == AuthTypes.APIKey && string.IsNullOrWhiteSpace(this.ApiKey))
        {
            errMsg = "The API key is required, the value is empty";
            return false;
        }

        return true;
    }

    /// <inherit />
    public new CustomEmbeddingRequest Validate()
    {
        base.Validate();
        if (!this.FixState().IsValid(out var errMsg)) { throw new ValidationException(errMsg); }

        return this;
    }
}
