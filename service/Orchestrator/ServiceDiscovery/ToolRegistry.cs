// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Orchestrator.Models;
using StackExchange.Redis;

namespace Orchestrator.ServiceDiscovery;

internal sealed class ToolRegistry
{
    // private readonly IConnectionMultiplexer _redisConn;
    private readonly IDatabase _db;

    // Redis set containing the list of registered functions
    private const string FunctionsRedisSetName = "functions";

    public ToolRegistry(IDatabase db)
    {
        this._db = db;
    }

    public async Task<List<FunctionDescription>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        RedisValue[] functions = await this._db.SetMembersAsync(FunctionsRedisSetName).ConfigureAwait(false);

        var result = new List<FunctionDescription>();
        foreach (var key in functions)
        {
            RedisValue redisData = await this._db.StringGetAsync(key.ToString()).ConfigureAwait(false);
            FunctionDescription? info = JsonSerializer.Deserialize<FunctionDescription>(redisData.ToString());
            if (info == null) { continue; }

            result.Add(info);
        }

        return result;
    }

    public async Task<ToolInfo[]> FetchToolsAsync(
        Dictionary<string, string> tools,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        // For each tool check swagger availability in parallel
        var toolTasks = tools.OrderBy(x => x.Key)
            .Select(async x => new ToolInfo
            {
                Name = x.Key,
                Endpoint = x.Value,
                SwaggerUrl = await GetSwaggerUrlAsync(httpClient, x.Value, cancellationToken).ConfigureAwait(false)
            });

        return await Task.WhenAll(toolTasks).ConfigureAwait(false);
    }

    // Check if Swagger is available for each tool
    private static async Task<string> GetSwaggerUrlAsync(
        HttpClient httpClient, string baseUrl, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031
        string[] endpoints = ["/docs", "/swagger/index.html"];
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var response = await httpClient.GetAsync(new Uri(baseUrl + endpoint), cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return baseUrl + endpoint;
                }
            }
            catch
            {
                // Ignore failures and try next endpoint
            }
        }

        return string.Empty;
#pragma warning restore CA1031
    }
}
