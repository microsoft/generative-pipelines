// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace EmbeddingGenerator.Functions;

public class EmbeddingResponse
{
    [JsonPropertyName("promptTokens")]
    [JsonPropertyOrder(1)]
    public int InputTokenCount { get; set; } = 0;

    [JsonPropertyName("totalTokens")]
    [JsonPropertyOrder(2)]
    public int TotalTokenCount { get; set; } = 0;

    [JsonPropertyName("embedding")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Embedding { get; set; }

    [JsonPropertyName("embeddings")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<float[]>? Embeddings { get; set; }
}
