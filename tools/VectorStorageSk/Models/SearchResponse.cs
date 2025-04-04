// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class SearchResponse
{
    internal sealed class SearchResult
    {
        [JsonPropertyName("value")]
        public object Record { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; } = 0;
    }

    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();
}
