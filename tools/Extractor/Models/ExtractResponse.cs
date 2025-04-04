// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Extractor.Models;

internal sealed class ExtractResponse
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("sections")]
    public List<ExtractedSection> Sections { get; set; } = [];

    [JsonPropertyOrder(1)]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Complete content extracted from the file
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("fullText")]
    public string FullText { get; set; } = string.Empty;

    [JsonPropertyOrder(3)]
    [JsonPropertyName("size")]
    public long Size { get; set; } = 0;
}
