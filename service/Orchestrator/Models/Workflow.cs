// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.Models;

internal sealed class Workflow
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; } = [];

    // TODO
    // [JsonPropertyName("subs")]
    // public Dictionary<string, Workflow> Subroutines { get; set; } = new();
}
