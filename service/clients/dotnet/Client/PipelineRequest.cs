// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.GenerativePipelines;

internal sealed class PipelineRequest
{
    internal sealed class WorkflowBlock
    {
        [JsonPropertyName("steps")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PipelineStep> Steps { get; set; } = new();
    }

    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Input { get; set; }

    [JsonPropertyName("_workflow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkflowBlock Workflow { get; set; } = new();
}
