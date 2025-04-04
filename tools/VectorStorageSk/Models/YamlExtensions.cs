// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace VectorStorageSk.Models;

internal static class YamlExtensions
{
    public static bool? AsBoolean(JsonElement boolAsString)
    {
        return AsBoolean(boolAsString.Deserialize<string>());
    }

    public static bool? AsBoolean(string? boolAsString)
    {
        if (string.Equals(boolAsString, "true", StringComparison.OrdinalIgnoreCase)) { return true; }

        if (string.Equals(boolAsString, "false", StringComparison.OrdinalIgnoreCase)) { return false; }

        return null;
    }
}
