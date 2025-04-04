// Copyright (c) Microsoft. All rights reserved.

namespace FunctionalTests;

public sealed class OrchestratorTests : BaseTestCase
{
    public OrchestratorTests(ITestOutputHelper console) : base(console)
    {
    }

    [Fact]
    public async Task PostEmptyJob()
    {
        // Arrange
        var payload = new { _workflow = new { }, foo = "bar" };

        // Act
        var response = await this.Client.PostAsJsonAsync("/api/jobs", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("foo")
            .Which.Should().BeOfKind(JsonValueKind.String)
            .And.Be("bar");
    }

    [Fact]
    public async Task GetEnv()
    {
        // Act
        var response = await this.Client.GetAsync("/env").ConfigureAwait(false);
        var jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("workspaceDir");
        json.Should().HaveProperty("environment");
    }

    [Fact]
    public async Task GeTools()
    {
        // Act
        var response = await this.Client.GetAsync("/tools").ConfigureAwait(false);
        var jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("tools");
        json.Should().HaveProperty("functions");
    }
}
