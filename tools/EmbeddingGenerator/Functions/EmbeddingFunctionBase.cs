// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using OpenAI.Embeddings;

namespace EmbeddingGenerator.Functions;

internal static class EmbeddingFunctionBase
{
    public static async Task<IResult> InvokeAsync(
        EmbeddingClient client,
        string? input,
        List<string>? inputs,
        bool supportsCustomDimensions,
        int? dimensions,
        CancellationToken cancellationToken)
    {
        var result = new EmbeddingResponse();
        EmbeddingGenerationOptions options = new();
        if (supportsCustomDimensions && dimensions is > 0)
        {
            options.Dimensions = dimensions;
        }

        var strings = (input != null) ? [input] : inputs;

        ClientResult<OpenAIEmbeddingCollection>? embeddings = await client
            .GenerateEmbeddingsAsync(strings, options, cancellationToken).ConfigureAwait(false);
        if (embeddings == null)
        {
            return Results.BadRequest("The embedding generation failed");
        }

        var status = embeddings.GetRawResponse().Status;
        if (status is < 200 or > 299)
        {
            return Results.BadRequest($"The embedding generation failed with status code {status}");
        }

        if (input != null)
        {
            result.Embedding = embeddings.Value[0].ToFloats().ToArray();
        }
        else
        {
            result.Embeddings = [];
            foreach (var e in embeddings.Value)
            {
                result.Embeddings.Add(e.ToFloats().ToArray());
            }
        }

        result.InputTokenCount = embeddings.Value.Usage.InputTokenCount;
        result.TotalTokenCount = embeddings.Value.Usage.TotalTokenCount;

        return Results.Ok(result);
    }
}
