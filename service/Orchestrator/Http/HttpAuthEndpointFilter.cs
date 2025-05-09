// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Primitives;
using Orchestrator.Config;

namespace Orchestrator.Http;

internal sealed class HttpAuthEndpointFilter : IEndpointFilter
{
    private readonly WebServiceAuthConfig _config;

    public HttpAuthEndpointFilter(WebServiceAuthConfig config)
    {
        config.Validate();
        this._config = config;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Check if the Authorization header is present and contains a valid access key
        if (this._config.Type == WebServiceAuthConfig.WebServiceAuthType.AccessKey)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(this._config.AuthorizationHeader, out StringValues apiKey))
            {
                return Results.Problem(detail: $"Missing {this._config.AuthorizationHeader} HTTP header", statusCode: 401);
            }

            var key = apiKey.ToString().TrimStart();
            if (key.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the "Bearer " prefix (7 chars) from the key
                key = key.Substring(7);
            }

            key = key.Trim();

            // Don't allow empty keys, regardless of the configuration.
            // This is to prevent accidental deployments with empty access keys.
            if (string.IsNullOrWhiteSpace(key))
            {
                return Results.Problem(detail: "Missing Access Key", statusCode: 403);
            }

            if (!string.Equals(key, this._config.AccessKey1, StringComparison.Ordinal)
                && !string.Equals(key, this._config.AccessKey2, StringComparison.Ordinal))
            {
                return Results.Problem(detail: "Invalid Access Key", statusCode: 403);
            }
        }

        // TODO: support Bearer token authentication

        return await next(context).ConfigureAwait(false);
    }
}
