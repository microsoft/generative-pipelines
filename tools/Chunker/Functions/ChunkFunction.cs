// Copyright (c) Microsoft. All rights reserved.

using Chunker.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;

namespace Chunker.Functions;

internal sealed class ChunkFunction
{
    private readonly ILogger<ChunkFunction> _log;

    public ChunkFunction(ILoggerFactory? lf = null)
    {
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<ChunkFunction>();
    }

    public IResult Invoke(ChunkRequest req)
    {
        ITextTokenizer tokenizer;
        switch (req.Tokenizer.ToLowerInvariant())
        {
            case "gpt4" or "cl100k" or "cl100k_base":
                tokenizer = new CL100KTokenizer();
                break;
            case "gpt4o" or "o200k" or "o200k_base":
                tokenizer = new O200KTokenizer();
                break;
            case "gpt3" or "p50k" or "p50k_base":
                tokenizer = new P50KTokenizer();
                break;
            case "":
            case "char":
                tokenizer = new OneCharTokenizer();
                break;
            default:
                return Results.BadRequest("Unsupported tokenizer, try 'cl100k_base' or 'char'");
        }

        var chunker = new PlainTextChunker(tokenizer);
        List<string> chunks = chunker.Split(req.Text, new PlainTextChunkerOptions
        {
            MaxTokensPerChunk = req.MaxTokensPerChunk,
            Overlap = req.Overlap,
            ChunkHeader = req.ChunkHeader,
        });

        return Results.Ok(new ChunkResponse { Chunks = chunks });
    }
}
