// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Aspire.AppHost.Internals;

public class AppSettings
{
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

    /// <summary>
    /// The Docker image to use for Ollama
    /// </summary>
    [JsonPropertyName("OllamaContainerImage")]
    public string OllamaContainerImage { get; set; } = "ollama/ollama";

    /// <summary>
    /// Tag for the Docker image to use for Ollama
    /// </summary>
    [JsonPropertyName("OllamaContainerTag")]
    public string OllamaContainerTag { get; set; } = "latest";

    /// <summary>
    /// The Docker image to use for Ollama UI
    /// </summary>
    [JsonPropertyName("OllamaWebUiContainerImage")]
    public string OllamaWebUiContainerImage { get; set; } = "open-webui/open-webui";

    /// <summary>
    /// Tag for the Docker image to use for Ollama UI
    /// </summary>
    [JsonPropertyName("OllamaWebUiContainerTag")]
    public string OllamaWebUiContainerTag { get; set; } = "0.5.20";
}
