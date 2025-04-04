// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace VectorStorageSk.Models;

internal sealed class CreateCollectionResponse
{
    [JsonPropertyName("message")]
    [JsonPropertyOrder(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StorageTypes? Type { get; set; }
}
