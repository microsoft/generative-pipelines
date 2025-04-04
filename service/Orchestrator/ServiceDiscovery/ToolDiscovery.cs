// Copyright (c) Microsoft. All rights reserved.

using System.Collections;
using System.Text.RegularExpressions;

namespace Orchestrator.ServiceDiscovery;

internal static partial class ToolDiscovery
{
    public static Dictionary<string, string> GetTools(IConfiguration config)
    {
        var cfgServices = GetToolsFromAppConfig(config);
        var envServices = GetToolsFromEnvVars();

        // Merge list, giving precedence to env vars and to https endpoints
        IEnumerable<string> allKeys = cfgServices.Keys.Union(envServices.Keys);
        return allKeys.ToDictionary(
            svcName => svcName,
            svcName =>
            {
                envServices.TryGetValue(svcName, out var envEndpoint);
                cfgServices.TryGetValue(svcName, out var cfgEndpoint);
                // Since the key is in the union, at least one of these is non-null.
                return envEndpoint != null && (cfgEndpoint == null || envEndpoint.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                    ? envEndpoint
                    : cfgEndpoint!;
            });
    }

    /// <summary>
    /// Get list of services from appsettings files (and any source used by the config manager,
    /// which may include env vars and Azure Config)
    /// </summary>
    private static Dictionary<string, string> GetToolsFromAppConfig(IConfiguration config)
    {
        const string SectionName = "App:Tools";
        var section = config.GetSection(SectionName);
        var list = new Dictionary<string, string>();
        if (section == null) { return list; }

        foreach (IConfigurationSection s in section.GetChildren())
        {
            var serviceName = s.Key;
            var endpoint = s.Value;
            if (string.IsNullOrWhiteSpace(endpoint)) { continue; }

            // If the value is new, add it and move to the next
            if (list.TryAdd(serviceName, endpoint)) { continue; }

            // Otherwise use it only if it is HTTPS
            if (endpoint.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            {
                list[serviceName] = endpoint;
            }
        }

        return list;
    }

    /// <summary>
    /// Get list of services from env vars injected by Aspire
    /// </summary>
    private static Dictionary<string, string> GetToolsFromEnvVars()
    {
        Dictionary<string, string> list = [];

        foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            // Env var name example: services__functionName__http__0
            // Env var name example: services__functionName__https__0
            var envVarName = envVar.Key.ToString() ?? string.Empty;
            if (envVarName.StartsWith("services__", StringComparison.Ordinal))
            {
                var serviceName = Regex.Replace(envVarName, @"^services__(.*?)__http.*$", "$1");
                if (list.ContainsKey(serviceName))
                {
                    // Use HTTPS if available
                    if (envVarName.EndsWith("https__0", StringComparison.Ordinal))
                    {
                        list[serviceName] = envVar.Value?.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    list[serviceName] = envVar.Value?.ToString() ?? string.Empty;
                }
            }
        }

        return list;
    }
}
