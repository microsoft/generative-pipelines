// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Models;

internal sealed class FunctionDetails
{
    public enum FunctionTypes
    {
        None,
        Internal,
        Http,
    }

    public FunctionTypes Type { get; set; }
    public string Tool { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;

    public static FunctionDetails Parse(string? functionId)
    {
        if (string.IsNullOrWhiteSpace(functionId))
        {
            return new FunctionDetails { Type = FunctionTypes.None };
        }

        switch (functionId.ToLowerInvariant())
        {
            // case "goto":
            // case "stop-if":
            case "break":
            case "stop":
            case "exit":
                return new FunctionDetails { Function = "stop", Type = FunctionTypes.Internal };
        }

        var parts = functionId.Split('/', 2);
        return new FunctionDetails
        {
            Type = FunctionTypes.Http,
            Tool = parts[0],
            Function = parts.Length == 2 ? $"/{parts[1].Trim('/')}/" : "/",
        };
    }
}
