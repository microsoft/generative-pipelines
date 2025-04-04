// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Chunker.Models;

internal sealed class ChunkRequest
{
    [JsonPropertyName("text")]
    [JsonPropertyOrder(0)]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("maxTokensPerChunk")]
    [JsonPropertyOrder(1)]
    public int MaxTokensPerChunk { get; set; } = 1000;

    [JsonPropertyName("overlap")]
    [JsonPropertyOrder(2)]
    public int Overlap { get; set; } = 0;

    [JsonPropertyName("chunkHeader")]
    [JsonPropertyOrder(3)]
    public string ChunkHeader { get; set; } = string.Empty;

    [JsonPropertyName("tokenizer")]
    [JsonPropertyOrder(10)]
    public string Tokenizer { get; set; } = "cl100k_base";
}
