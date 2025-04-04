# Generative Pipelines in .NET

Example usage:

```csharp
// DI
var services = new ServiceCollection();
services.Configure<ClientOptions>(opts =>
{
    opts.BaseUrl = "http://localhost:60000";
    opts.ApiKey = "";
});
services.AddHttpClient<GPClient>();
var provider = services.BuildServiceProvider();

// Start
var client = provider.GetRequiredService<GPClient>();
var pipeline = client.NewPipeline();

// Dynamic input
pipeline.Input = new
{
    sourceId = "itwikidolomiti",
    page = "Dolomiti"
};

// Steps
pipeline.Steps.Add(new PipelineStep {
    Function = "wikipedia/it",
    Xin = """
          {
              title: start.input.page
          }
          """});

pipeline.Steps.Add(new PipelineStep {
    Function = "chunker/chunk",
    Xin = """
          {
              text:              state.content,
              maxTokensPerChunk: `400`,
              overlap:           `0`,
              tokenizer:         'cl100k_base'
          }
          """});

// Run
var result = await client.RunPipelineAsync(pipeline).ConfigureAwait(false);
Console.WriteLine(result);
```