// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace Orchestrator.Http;

public static class HttpJsonSerialization
{
    // ASP.NET env var
    private const string AspNetCoreEnvVar = "ASPNETCORE_ENVIRONMENT";

    // .NET env var
    private const string DotNetEnvVar = "DOTNET_ENVIRONMENT";

    public static IServiceCollection ConfigureSerializationOptions(this IServiceCollection services)
    {
        var env = Environment.GetEnvironmentVariable(AspNetCoreEnvVar) ?? Environment.GetEnvironmentVariable(DotNetEnvVar) ?? string.Empty;

        services.Configure<JsonSerializerOptions>(options => ConfigureJsonOptions(options, env));
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => ConfigureJsonOptions(options.SerializerOptions, env));
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => ConfigureJsonOptions(options.JsonSerializerOptions, env));

        return services;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options, string env)
    {
        options.AllowTrailingCommas = true;
        options.PropertyNameCaseInsensitive = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.NewLine = "\n";
        options.WriteIndented = env.Equals("development", StringComparison.OrdinalIgnoreCase);
        options.IndentSize = 2;
        options.IndentCharacter = ' ';
    }
}
