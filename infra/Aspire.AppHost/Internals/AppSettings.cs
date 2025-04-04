// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Aspire.AppHost.Internals;

public class AppSettings
{
    [JsonPropertyName("UseAzureAiSearch")]
    public bool UseAzureAiSearch { get; set; } = false;

    [JsonPropertyName("UsePostgres")]
    public bool UsePostgres { get; set; } = false;

    [JsonPropertyName("UsePostgresOnAzure")]
    public bool UsePostgresOnAzure { get; set; } = false;

    [JsonPropertyName("UseQdrant")]
    public bool UseQdrant { get; set; } = false;

    [JsonPropertyName("UseRedis")]
    public bool UseRedis { get; set; } = false;

    [JsonPropertyName("UseRedisTools")]
    public bool UseRedisTools { get; set; } = false;

    [JsonPropertyName("PostgresContainerImage")]
    public string PostgresContainerImage { get; set; } = string.Empty;

    [JsonPropertyName("PostgresContainerImageTag")]
    public string PostgresContainerImageTag { get; set; } = string.Empty;
}
