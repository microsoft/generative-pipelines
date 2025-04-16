// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Aspire.AppHost.Internals;

public class AppSettings
{
    /// <summary>
    /// Access key required to access the Orchestrator API.
    /// If empty, Aspire AppHost sets a random key when deploying the Orchestrator to Azure.
    /// </summary>
    [JsonPropertyName("AccessKey1")]
    public string? AccessKey1 { get; set; }

    /// <summary>
    /// Access key required to access the Orchestrator API.
    /// If empty, Aspire AppHost sets a random key when deploying the Orchestrator to Azure.
    /// </summary>
    [JsonPropertyName("AccessKey2")]
    public string? AccessKey2 { get; set; }

    /// <summary>
    /// Whether to deploy Postgres, both locally and on Azure.
    /// </summary>
    [JsonPropertyName("UsePostgres")]
    public bool UsePostgres { get; set; } = false;

    /// <summary>
    /// Whether to deploy Postgres on Azure. UsePostgres must be true too.
    /// </summary>
    [JsonPropertyName("UsePostgresOnAzure")]
    public bool UsePostgresOnAzure { get; set; } = false;

    /// <summary>
    /// Whether to deploy Qdrant, both locally and on Azure.
    /// </summary>
    [JsonPropertyName("UseQdrant")]
    public bool UseQdrant { get; set; } = false;

    /// <summary>
    /// Whether to deploy Redis, both locally and on Azure.
    /// </summary>
    [JsonPropertyName("UseRedis")]
    public bool UseRedis { get; set; } = false;

    /// <summary>
    /// Whether to deploy Redis tools (local only).
    /// </summary>
    [JsonPropertyName("UseRedisTools")]
    public bool UseRedisTools { get; set; } = false;

    /// <summary>
    /// The Docker image to use for Postgres + pgvector.
    /// </summary>
    [JsonPropertyName("PostgresContainerImage")]
    public string PostgresContainerImage { get; set; } = "pgvector/pgvector";

    /// <summary>
    /// Tag for the Docker image to use for Postgres + pgvector.
    /// </summary>
    [JsonPropertyName("PostgresContainerImageTag")]
    public string PostgresContainerImageTag { get; set; } = "pg17";
}
