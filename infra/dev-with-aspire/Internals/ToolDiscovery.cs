// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;

namespace Aspire.AppHost.Internals;

internal static class ToolDiscovery
{
    /// <summary>
    /// Scan directory for C# .csproj files (ignoring projects starting with "_")
    /// </summary>
    /// <returns>List of service names and csproj files</returns>
    public static IEnumerable<(string name, string projectFilePath)> FindCSharpTools(string path)
    {
        foreach (string file in Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories))
        {
            string relativePath = file[path.Length..].Trim('\\', '/');
            if (relativePath.Split('\\', '/').Any(p => p.StartsWith('_'))) { continue; }

            yield return (ToSnakeCase(Path.GetFileNameWithoutExtension(relativePath)), relativePath);
        }
    }

    /// <summary>
    /// Scan directory for F# .fsproj files (ignoring projects starting with "_")
    /// </summary>
    /// <returns>List of service names and csproj files</returns>
    public static IEnumerable<(string name, string projectFilePath)> FindFSharpTools(string path)
    {
        foreach (string file in Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories))
        {
            string relativePath = file[path.Length..].Trim('\\', '/');
            if (relativePath.Split('\\', '/').Any(p => p.StartsWith('_'))) { continue; }

            yield return (ToSnakeCase(Path.GetFileNameWithoutExtension(relativePath)), relativePath);
        }
    }

    /// <summary>
    /// Scan directory for Node.js package.json files (ignoring node_modules subdir and projects starting with "_")
    /// </summary>
    /// <returns>List of services and dir names</returns>
    public static IEnumerable<(string name, string dirName)> FindNodeJsTools(string path)
    {
        foreach (string file in Directory.GetFiles(path, "package.json", SearchOption.AllDirectories))
        {
            string relativePath = file[path.Length..].Trim('\\', '/');
            var parts = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0].StartsWith('_') || parts.Contains("node_modules")) { continue; }

            yield return (ToSnakeCase(parts[0]), parts[0]);
        }
    }

    /// <summary>
    /// Scan directory for Poetry pyproject.toml files (ignoring venvs subdir and projects starting with "_")
    /// </summary>
    /// <returns>List of services and dir names</returns>
    public static IEnumerable<(string name, string dirName)> FindPythonTools(string path)
    {
        foreach (string file in Directory.GetFiles(path, "pyproject.toml", SearchOption.AllDirectories))
        {
            string relativePath = file[path.Length..].Trim('\\', '/');
            var parts = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0].StartsWith('_') || parts.Contains(".venv") || parts.Contains("__pycache__")) { continue; }

            yield return (ToSnakeCase(parts[0]), parts[0]);
        }
    }

    private static string ToSnakeCase(string s)
    {
        return string.Concat(s.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower(CultureInfo.InvariantCulture);
    }
}
