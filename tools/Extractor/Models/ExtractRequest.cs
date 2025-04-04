// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Extractor.Models;

internal sealed class ExtractRequest
{
    [JsonPropertyName("content")]
    [JsonPropertyOrder(0)]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    [JsonPropertyOrder(1)]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    [JsonPropertyOrder(2)]
    public string MimeType { get; set; } = string.Empty;
}
