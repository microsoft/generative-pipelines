// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CommonDotNet.ServiceDiscovery;

public sealed class ToolRegistry : IDisposable, IAsyncDisposable
{
    // Set by Aspire
    public const string ToolNameEnvVar = "TOOL_NAME";

    // Redis set containing the list of registered functions
    private const string FunctionsRedisSetName = "functions";

    private readonly bool _isDisabled = false;
    private readonly IConnectionMultiplexer? _redisConn = null;
    private readonly IDatabase? _db = null;
    private readonly ILogger<ToolRegistry> _log;

    public ToolRegistry(IServiceProvider sp, ILoggerFactory? lf = null)
    {
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<ToolRegistry>();

        // When working with GenerativePipelines, the registry can be disabled setting
        // the "GenerativePipelines:ToolsRegistryEnabled" configuration value to false.
        // or GenerativePipelines__ToolsRegistryEnabled=false env var.
        // See DependencyInjection.AddRedisToolsRegistry() for more details.
        if (sp == null)
        {
            this._isDisabled = true;
            return;
        }

        try
        {
            this._redisConn = sp.GetService<IConnectionMultiplexer>();
        }
        catch (InvalidOperationException e) when (e.Message.Contains("No endpoints specified"))
        {
            this._log.LogError("Redis connection string not available");
        }
#pragma warning disable CA1031
        catch (Exception e)
        {
            this._log.LogError(e, "Redis connection failed");
        }
#pragma warning restore CA1031

        if (this._redisConn != null)
        {
            this._db = this._redisConn.GetDatabase();
            this._log.LogInformation("Redis connection not established, tools registration enabled");
        }
        else
        {
            this._log.LogWarning("Redis connection not established, tools registration disabled");
        }
    }

    public void RegisterPostFunction(string url, string description)
    {
        this.RegisterJsonFunction(HttpMethod.Post, url, description);
    }

    public void RegisterPostMultipartFunction(string url, string description)
    {
        this.RegisterMultipartFunction(HttpMethod.Post, url, description);
    }

    public void RegisterJsonFunction(HttpMethod method, string url, string description)
    {
        this.RegisterFunction(method, url, description, isJson: true);
    }

    public void RegisterMultipartFunction(HttpMethod method, string url, string description)
    {
        this.RegisterFunction(method, url, description, isJson: false);
    }

    /// <summary>
    /// Register a function in Redis.
    /// Note: a tool can provide multiple functions, using different HTTP paths.
    /// </summary>
    /// <param name="method">HTTP method used to invoke the function</param>
    /// <param name="url">The path to the function</param>
    /// <param name="description">Function description</param>
    /// <param name="isJson">Whether the HTTP endpoint expects JSON input (true) or Multipart Form data (false)</param>
    private void RegisterFunction(
        HttpMethod method,
        string url,
        string description,
        bool isJson = true)
    {
        if (this._isDisabled || this._db == null) { return; }

        // The tool name should be set using an env var, e.g. injected by the hosting
        // environment and match exactly the host name used for HTTP requests.
        var toolName = Environment.GetEnvironmentVariable(ToolNameEnvVar) ?? "";

        var data = new FunctionDescription
        {
            Id = $"{toolName}{url}",
            Tool = toolName,
            Url = url,
            Method = method.ToString(),
            InputType = isJson ? FunctionDescription.ContentType.Json : FunctionDescription.ContentType.Multipart,
            OutputType = FunctionDescription.ContentType.Json,
            Description = description,
        };

        /* Store a unique ID into a "functions" Redis set, used to index key-values.
         * The ID points to a Redis key where the entire function description is stored.
         * This approach allows to modify function details without causing
         * duplicate entries in the Redis set. */

        // Data stored in Redis KV
        var redisDataKey = $"FunctionDetails:{toolName}:{url}";
        var redisDataValue = JsonSerializer.Serialize(data);
        this._db.StringSet(redisDataKey, redisDataValue, TimeSpan.MaxValue);

        // Pointer stored in Redis Set
        this._db.SetAdd(FunctionsRedisSetName, redisDataKey);
    }

    public void Dispose()
    {
        if (this._isDisabled) { return; }

        this._redisConn?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (this._isDisabled) { return; }

        if (this._redisConn == null) { return; }

        await this._redisConn.DisposeAsync().ConfigureAwait(false);
    }

    // private static string HashThis(string value)
    // {
    //     return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToUpperInvariant();
    // }
}
