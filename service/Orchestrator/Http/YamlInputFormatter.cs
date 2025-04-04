// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Orchestrator.Http;

public class YamlToJsonMiddleware
{
    private static readonly JsonSerializerOptions s_jsOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly RequestDelegate _next;

    public YamlToJsonMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsYamlRequest(context.Request.ContentType))
        {
            Encoding encoding = GetEncoding(context.Request.Headers["Content-Type"]) ?? Encoding.UTF8;
            CancellationToken cancellationToken = context.RequestAborted; // Get cancellation token

            // Read the entire request body as BinaryData, allowing cancellation
            // Note we're using this approach only for YAML requests, assuming they don't need to be streamed
            BinaryData requestData = await BinaryData.FromStreamAsync(context.Request.Body, cancellationToken)
                .ConfigureAwait(false);
            string yaml = requestData.ToString();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            object yamlObject = deserializer.Deserialize<object>(yaml);
            string json = JsonSerializer.Serialize(yamlObject, s_jsOptions);

            byte[] jsonBytes = encoding.GetBytes(json);
            context.Request.Body = new MemoryStream(jsonBytes);
            context.Request.ContentLength = jsonBytes.Length;
            context.Request.ContentType = "application/json";
        }

        await this._next(context).ConfigureAwait(false);
    }

    private static bool IsYamlRequest(string? contentType)
    {
        return !string.IsNullOrEmpty(contentType) &&
               (contentType.StartsWith("application/x-yaml", StringComparison.OrdinalIgnoreCase) ||
                contentType.StartsWith("text/yaml", StringComparison.OrdinalIgnoreCase));
    }

    private static Encoding? GetEncoding(string? contentType)
    {
        if (contentType == null || !contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

#pragma warning disable CA1031
        string charset = contentType.Split("charset=")[1].Trim();
        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch
        {
            return Encoding.UTF8;
        }
#pragma warning restore CA1031
    }
}
