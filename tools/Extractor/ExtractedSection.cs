// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Extractor;

internal sealed class ExtractedSection
{
    /// <summary>
    /// Page text content
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata attached to the section.
    /// Values are JSON strings to be serialized/deserialized.
    /// Examples:
    /// - sentences are complete y/n
    /// - page number
    /// </summary>
    [JsonPropertyOrder(10)]
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
