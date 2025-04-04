// Copyright (c) Microsoft. All rights reserved.

using Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.GenerativePipelines;

namespace Client.Tests;

public class GpClientTest : BaseTestCase
{
    public GpClientTest(ITestOutputHelper console) : base(console)
    {
    }

    [Fact]
    public async Task Test1()
    {
        var provider = new ServiceCollection()
            .UseGenerativePipelines("http://localhost:60000")
            .BuildServiceProvider();

        var client = provider.GetRequiredService<GPClient>();

        var pipeline = client.NewPipeline();
        pipeline.Input = new
        {
            sourceId = "itwikidolomiti",
            page = "Dolomiti"
        };

        pipeline.AddStep(
            function: "wikipedia/it",
            xin: """
                 { title: start.input.page }
                 """
        );

        pipeline.AddStep(
            function: "chunker/chunk",
            xin: """
                 {
                     text:              state.content,
                     maxTokensPerChunk: `400`,
                     overlap:           `0`,
                     tokenizer:         'cl100k_base'
                 }
                 """);

        var result = await client.RunPipelineAsync(pipeline).ConfigureAwait(false);
        Assert.NotNull(result);

        this.Console.WriteLine(result);
        System.Console.WriteLine(result);

        await File.WriteAllTextAsync("/tmp/result.json", result).ConfigureAwait(false);
    }
}
