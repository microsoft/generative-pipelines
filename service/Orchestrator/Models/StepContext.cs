// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.Models;

public class StepContext
{
    [JsonPropertyName("in")]
    public object? In { get; set; }

    [JsonPropertyName("out")]
    public object? Out { get; set; }
}
