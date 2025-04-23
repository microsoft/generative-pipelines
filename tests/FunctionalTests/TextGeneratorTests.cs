// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;

namespace FunctionalTests;

public class TextGeneratorTests : BaseTestCase
{
    private readonly IConfiguration _config;

    public TextGeneratorTests(ITestOutputHelper console) : base(console)
    {
        this._config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
    }

    [Fact]
    public async Task OpenAITest()
    {
        // Arrange
        var payload = new
        {
            modelId = "gpt-4.1-nano",
            systemPrompt = "reply with a single Italian sentence, starting with 'Ciao'. Be brief.",
            prompt = "what's the most recent model from OpenAI?",
        };

        // Act
        var response = await this.TextGeneratorClient.PostAsJsonAsync("/generate-text", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("text")
            .Which.Should().BeOfKind(JsonValueKind.String)
            .And.BeStringMatching(x => x.Contains("Ciao", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AzureAITest()
    {
        // Arrange
        var payload = new
        {
            modelId = "gpt-4o",
            systemPrompt = "reply with a single Italian sentence, starting with 'Ciao'. Be brief.",
            prompt = "what's the most recent model from OpenAI?",
        };

        // Act
        var response = await this.TextGeneratorClient.PostAsJsonAsync("/generate-text", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("text")
            .Which.Should().BeOfKind(JsonValueKind.String)
            .And.BeStringMatching(x => x.Contains("Ciao", StringComparison.OrdinalIgnoreCase));
    }
}
