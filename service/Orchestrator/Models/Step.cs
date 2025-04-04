// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.Models;

internal sealed class Step
{
    /// <summary>
    /// Unique id for the step, allowing to reference its content from other steps.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional JMESPath expression to transform the input to the function.
    /// </summary>
    [JsonPropertyName("xin")]
    [JsonPropertyOrder(10)]
    public string InputTransformation { get; set; } = string.Empty;

    [JsonPropertyName("function")]
    [JsonPropertyOrder(20)]
    public string Function { get; set; } = string.Empty;

    /// <summary>
    /// Optional JMESPath expression to transform the output of the function.
    /// </summary>
    [JsonPropertyName("xout")]
    [JsonPropertyOrder(30)]
    public string OutputTransformation { get; set; } = string.Empty;
}
