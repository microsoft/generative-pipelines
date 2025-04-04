// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace Microsoft.GenerativePipelines;

public sealed class GPClient
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public static GPClient Build(HttpClient httpClient, string baseUrl, string? apiKey)
        => new(httpClient, Options.Create(new ClientOptions
        {
            BaseUrl = baseUrl,
            ApiKey = apiKey ?? string.Empty
        }));

    public GPClient(HttpClient httpClient, IOptions<ClientOptions> options)
    {
        var baseUrl = options.Value.BaseUrl;
        var apiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("BaseUrl must be provided.", nameof(options));
        }

        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("baseUrl must start with http:// or https://");
        }

        this._baseUrl = baseUrl.TrimEnd('/');
        this._apiKey = apiKey;
        this._httpClient = httpClient;
    }

    public PipelineDefinition NewPipeline()
    {
        return new PipelineDefinition();
    }

    public async Task<string> RunPipelineAsync(PipelineDefinition pipeline, CancellationToken ct = default)
    {
        var url = $"{this._baseUrl}/api/jobs";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new StringContent(pipeline.ToJson(), Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(this._apiKey))
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this._apiKey);
        }

        HttpResponseMessage response = await this._httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        string responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if ((int)response.StatusCode > 299)
        {
            throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase} - {responseJson}");
        }

        return responseJson;
    }
}
