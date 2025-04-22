// Copyright (c) Microsoft. All rights reserved.

namespace FunctionalTests;

public abstract class BaseTestCase
{
    protected readonly HttpClient Client;
    protected readonly HttpClient EmbeddingGeneratorClient;

    protected readonly ITestOutputHelper Console;

    protected BaseTestCase(ITestOutputHelper console)
    {
        this.Console = console;
        this.Client = new HttpClient { BaseAddress = new Uri("http://localhost:60000") };
        this.EmbeddingGeneratorClient = new HttpClient { BaseAddress = new Uri("http://localhost:5083") };
    }

    protected async Task<string> LogResponseAsync(HttpResponseMessage response)
    {
        this.WriteLine($"Status code: {response.StatusCode}");
        this.WriteLine("Response Headers:");
        foreach (var header in response.Headers)
        {
            this.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        foreach (var header in response.Content.Headers)
        {
            this.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        this.WriteLine("\nResponse Body:");
        this.WriteLine(body);

        return body;
    }

    private void WriteLine(string message)
    {
        // For CLI
        System.Console.WriteLine(message);
        // For IDE
        this.Console.WriteLine(message);
    }
}
