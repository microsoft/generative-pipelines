// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.ServiceDiscovery;

// IMPORTANT: Copy of CommonDotnet.ServiceDiscovery.FunctionDescription
#pragma warning disable CA1056
public sealed class FunctionDescription
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentType
    {
        Json,
        Multipart,
        SSEStream,
    }

    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tool")]
    [JsonPropertyOrder(10)]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    [JsonPropertyOrder(11)]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    [JsonPropertyOrder(20)]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("inputType")]
    [JsonPropertyOrder(21)]
    public ContentType InputType { get; set; } = ContentType.Json;

    [JsonPropertyName("outputType")]
    [JsonPropertyOrder(22)]
    public ContentType OutputType { get; set; } = ContentType.Json;

    [JsonPropertyName("description")]
    [JsonPropertyOrder(30)]
    public string Description { get; set; } = string.Empty;
}
