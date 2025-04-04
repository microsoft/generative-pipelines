// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Models;

internal sealed class ModelInfo
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
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Endpoint { get; set; }

    [JsonPropertyName("maxDimensions")]
    public int MaxDimensions { get; set; }

    [JsonPropertyName("supportsCustomDimensions")]
    public bool SupportsCustomDimensions { get; set; }
}
