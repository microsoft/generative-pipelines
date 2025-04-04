// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class UpsertRecordResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
