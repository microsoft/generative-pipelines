// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Diagnostics;
using Orchestrator.Models;

namespace Orchestrator.FunctionAdapters;

internal sealed class HttpAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpAdapter> _log;

    public HttpAdapter(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory? loggerFactory = null)
    {
        this._httpClientFactory = httpClientFactory;
        this._log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<HttpAdapter>();
    }

    public async Task<(bool success, IResult? error)> ExecuteAsync(
        Workflow workflow,
        Step step,
        FunctionDetails functionDetails,
        JobContext jobContext,
        dynamic errorDetails,
        Activity? activity,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Job {JobId}: Looking up function '{Function}'", workflow.JobId, step.Function);
        var client = this.GetHttpClient(functionDetails, workflow.JobId);
        if (client == null)
        {
            this._log.LogError("Job {JobId}: Unable to create HTTP client for function '{Function}'", workflow.JobId, step.Function);
            activity?.SetStatus(ActivityStatusCode.Error, $"Function {step.Function} not found, HTTP client not available");
            errorDetails.Message = $"Function {step.Function}not found, HTTP client not available";
            errorDetails.Description = $"There is no HTTP client for '{step.Function}', the name could be wrong or the tool is not registered";
            return (false, Results.NotFound(errorDetails));
        }

        // TODO: allow configurations, overrides, paths, query strings, headers, auth, etc.
        this._log.LogDebug("Job {JobId}: Preparing HTTP request", workflow.JobId);
        string path = functionDetails.Function;
        HttpMethod method = HttpMethod.Post;
        using HttpRequestMessage request = new(method, path) { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionOrLower };
        this._log.LogDebug("Job {JobId}: Serializing request content", workflow.JobId);
        request.Content = JsonContent.Create(jobContext.State);
        // ================================================================

        this._log.LogDebug("Job {JobId}: Invoking function '{Function}': {Method} {Url}",
            workflow.JobId, step.Function, request.Method, $"{client.BaseAddress?.AbsoluteUri}{request.RequestUri}");
        HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            errorDetails.Response = Logging.RemovePiiFromMessage(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            errorDetails.Description = $"Call to '{request.RequestUri}' returned {response.StatusCode}";

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    errorDetails.Message = "Function not found";
                    activity?.SetStatus(ActivityStatusCode.Error, "Function not found");
                    return (false, Results.NotFound(errorDetails));
                case HttpStatusCode.BadRequest:
                    errorDetails.Message = "Invalid call to function";
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid call to function");
                    return (false, Results.BadRequest(errorDetails));
                default:
                    errorDetails.Message = "Function error";
                    activity?.SetStatus(ActivityStatusCode.Error, "Function error");
                    return (false, Results.InternalServerError(errorDetails));
            }
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        jobContext.State = JsonSerializer.Deserialize<object>(json);

        return (true, null);
    }

    private HttpClient? GetHttpClient(FunctionDetails functionDetails, string jobId)
    {
        this._log.LogDebug("Job {JobId}: Searching HTTP client for tool '{Tool}', function '{Function}'",
            jobId, functionDetails.Tool, functionDetails.Function);

        HttpClient client = this._httpClientFactory.CreateClient(functionDetails.Tool);
        if (client.BaseAddress?.AbsoluteUri == null)
        {
            this._log.LogError("Job {JobId}: HTTP client for '{Tool}' is missing a base address", jobId, functionDetails.Tool);
            return null;
        }

        this._log.LogDebug("Job {JobId}: HTTP client base address for {Tool}: {BaseAddress}",
            jobId, functionDetails.Tool, client.BaseAddress.AbsoluteUri);
        return client;
    }
}
