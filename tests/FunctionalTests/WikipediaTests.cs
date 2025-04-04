// Copyright (c) Microsoft. All rights reserved.

namespace FunctionalTests;

public sealed class WikipediaTests : BaseTestCase
{
    public WikipediaTests(ITestOutputHelper console) : base(console)
    {
    }

    [Fact]
    public async Task FetchPage()
    {
        // Arrange
        var payload = new
        {
            _workflow = new
            {
                steps = new object[]
                {
                    new
                    {
                        function = "wikipedia/en"
                    }
                },
            },
            title = "Trope"
        };

        // Act
        var response = await this.Client.PostAsJsonAsync("/api/jobs", payload).ConfigureAwait(false);
        string jsonResponse = await this.LogResponseAsync(response).ConfigureAwait(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        jsonResponse.Should().BeCorrectJson();
        var json = jsonResponse.AsJson();
        json.Should().HaveProperty("title")
            .Which.Should().BeOfKind(JsonValueKind.String)
            .And.Be("Trope");
        json.Should().HaveProperty("content")
            .Which.Should().BeOfKind(JsonValueKind.String)
            .And.BeStringMatching(x => x.Contains("cantillation"));
    }
}
