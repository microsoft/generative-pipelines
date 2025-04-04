// Copyright (c) Microsoft. All rights reserved.

namespace FunctionalTests;

public abstract class BaseTestCase
{
    protected readonly HttpClient Client;

    protected readonly ITestOutputHelper Console;

    protected BaseTestCase(ITestOutputHelper console)
    {
        this.Console = console;
        this.Client = new HttpClient { BaseAddress = new Uri("http://localhost:60000") };
    }

    protected async Task<string> LogResponseAsync(HttpResponseMessage response)
    {
        this.Console.WriteLine($"Status code: {response.StatusCode}");
        this.Console.WriteLine("Response Headers:");
        foreach (var header in response.Headers)
        {
            this.Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        string jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        this.Console.WriteLine("\nResponse Body:");
        this.Console.WriteLine(jsonResponse);

        return jsonResponse;
    }
}
