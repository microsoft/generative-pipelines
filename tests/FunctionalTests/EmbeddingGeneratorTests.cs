// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;

namespace FunctionalTests;

public sealed class EmbeddingGeneratorTests : BaseTestCase
{
    private readonly IConfiguration _config;

    public EmbeddingGeneratorTests(ITestOutputHelper console) : base(console)
    {
        this._config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
    }

    [Fact]
    public async Task OpenAIAda0002Test()
    {
        // Arrange
        var payload = new
        {
            modelId = "openai-text-embedding-ada-002",
            input = "some text",
        };

        // Act
        var response = await this.EmbeddingGeneratorClient.PostAsJsonAsync("/vectorize", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("promptTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x > 0);
        json.Should().HaveProperty("totalTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x > 0);
        json.Should().HaveProperty("embedding")
            .Which.Should().BeOfKind(JsonValueKind.Array);
    }

    [Fact]
    public async Task AzureOpenAIAda0002Test()
    {
        // Arrange
        var payload = new
        {
            modelId = "text-embedding-3-small",
            input = "some text",
        };

        // Act
        var response = await this.EmbeddingGeneratorClient.PostAsJsonAsync("/vectorize", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("promptTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("totalTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("embedding")
            .Which.Should().BeOfKind(JsonValueKind.Array);
    }

    [Fact]
    public async Task CustomAzureVectorizationTest()
    {
        // Arrange
        var payload = new
        {
            provider = "AzureAI",
            auth = $"{this._config["AzureAIEmbeddingAuth"]}",
            endpoint = $"{this._config["AzureAIEndpoint"]}",
            modelId = $"{this._config["AzureAIEmbeddingModel"]}",
            maxDimensions = 1536,
            supportsCustomDimensions = true,
            dimensions = 5,
            input = "some text",
        };

        // Act
        var response = await this.EmbeddingGeneratorClient.PostAsJsonAsync("/vectorize-custom", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("promptTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("totalTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("embedding")
            .Which.Should().BeOfKind(JsonValueKind.Array);
    }

    [Fact]
    public async Task CustomOpenAIVectorizationTest()
    {
        // Arrange
        var payload = new
        {
            provider = "OpenAI",
            apiKey = $"{this._config["OpenAIEmbeddingAuth"]}",
            modelId = $"{this._config["OpenAIEmbeddingModel"]}",
            maxDimensions = 1536,
            supportsCustomDimensions = true,
            dimensions = 5,
            input = "some text",
        };

        // Act
        var response = await this.EmbeddingGeneratorClient.PostAsJsonAsync("/vectorize-custom", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("promptTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("totalTokens")
            .Which.Should().BeOfKind(JsonValueKind.Number)
            .And.BeIntegerMatching(x => x == 2);
        json.Should().HaveProperty("embedding")
            .Which.Should().BeOfKind(JsonValueKind.Array);
    }
}
