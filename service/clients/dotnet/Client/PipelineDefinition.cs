// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.GenerativePipelines;

public class PipelineDefinition
{
    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Input { get; set; }

    [JsonPropertyName("steps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PipelineStep> Steps { get; set; } = new();

    public PipelineDefinition AddStep(string? function = null, string? id = null, string? xin = null, string? xout = null)
    {
        this.Steps.Add(new PipelineStep
        {
            Id = id,
            Function = function,
            Xin = xin,
            Xout = xout
        });
        return this;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(new PipelineRequest
        {
            Input = this.Input,
            Workflow = new PipelineRequest.WorkflowBlock
            {
                Steps = this.Steps
            }
        });
    }
}
