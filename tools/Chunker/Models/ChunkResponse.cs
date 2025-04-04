// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Chunker.Models;

internal sealed class ChunkResponse
{
    [JsonPropertyName("chunks")]
    [JsonPropertyOrder(0)]
    public List<string> Chunks { get; set; } = [];
}
