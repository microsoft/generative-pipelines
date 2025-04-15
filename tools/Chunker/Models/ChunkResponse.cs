// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Chunker.Models;

internal sealed class ChunkResponse
{
    [JsonPropertyName("count")]
    [JsonPropertyOrder(0)]
    public long Count => this.Chunks.Count;

    [JsonPropertyName("chunks")]
    [JsonPropertyOrder(1)]
    public List<string> Chunks { get; set; } = [];
}
