// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Orchestrator.ServiceDiscovery;

namespace Orchestrator.Models;

internal sealed class OrchestratorStatus
{
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(1)]
    public Dictionary<string, ToolInfo>? Tools { get; set; }

    [JsonPropertyName("functions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(1)]
    public Dictionary<string, FunctionDescription>? Functions { get; set; }

    [JsonPropertyName("workspaceDir")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(2)]
    public string? WorkspaceDir { get; set; }

    [JsonPropertyName("environment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(3)]
    public object? Environment { get; set; }
}

internal sealed class ToolInfo
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    [JsonPropertyOrder(2)]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("swaggerUrl")]
    [JsonPropertyOrder(3)]
    public string SwaggerUrl { get; set; } = string.Empty;
}
