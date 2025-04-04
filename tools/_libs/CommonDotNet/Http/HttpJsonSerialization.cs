// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CommonDotNet.Http;

public static class HttpJsonSerialization
{
    // ASP.NET env var
    private const string AspNetCoreEnvVar = "ASPNETCORE_ENVIRONMENT";

    // .NET env var
    private const string DotNetEnvVar = "DOTNET_ENVIRONMENT";

    public static void ConfigureSerializationOptions(this IServiceCollection s)
    {
        var env = Environment.GetEnvironmentVariable(AspNetCoreEnvVar) ?? Environment.GetEnvironmentVariable(DotNetEnvVar) ?? string.Empty;

        s.Configure<JsonSerializerOptions>(options => ConfigureJsonOptions(options, env));
        s.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => ConfigureJsonOptions(options.SerializerOptions, env));
        s.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => ConfigureJsonOptions(options.JsonSerializerOptions, env));
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

        // Breaks YAML to JSON deserialization
        // options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    }
}
