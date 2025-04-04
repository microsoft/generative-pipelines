// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace dotnetTemplate.Models;

internal sealed class FooRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
