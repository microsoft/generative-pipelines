// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.Models;

internal sealed class JobContext : Dictionary<string, object?>
{
    private const string StartKey = "start";
    private const string StateKey = "state";

    // Stores the initial input to the workflow
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public object? Start
    {
        get => this[StartKey];
        init => this[StartKey] = value;
    }

    // Stores the current state of the workflow data
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public object? State
    {
        get => this[StateKey];
        set => this[StateKey] = value;
    }

    public JobContext()
    {
        this[StartKey] = new { };
        this[StateKey] = new { };
    }
}
