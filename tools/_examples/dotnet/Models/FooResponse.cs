// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace dotnetTemplate.Models;

internal sealed class FooResponse
{
    [JsonPropertyName("message")]
    [JsonPropertyOrder(0)]
    public string Message { get; set; } = string.Empty;
}
