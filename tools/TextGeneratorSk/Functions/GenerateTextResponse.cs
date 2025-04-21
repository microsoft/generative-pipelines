// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CommonDotNet.Http;

namespace TextGeneratorSk.Functions;

internal sealed class GenerateTextResponse
{
    public StreamStates? StreamState { get; set; } = null;

    /// <summary>
    /// Content of the answer.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(10)]
    public string Text { get; set; } = string.Empty;
}
