// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CommonDotNet.Http;

namespace TextGenerator.Functions;

internal sealed class GenerateTextResponse
{
    /// <summary>
    /// Content of the answer.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(10)]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("streamState")]
    [JsonPropertyOrder(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamStates? StreamState { get; set; } = null;

    [JsonPropertyName("report")]
    [JsonPropertyOrder(100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Report? Report { get; set; } = null;
}
