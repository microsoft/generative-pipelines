// Copyright (c) Microsoft. All rights reserved.

using Extractor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.KernelMemory.Pipeline;

namespace Extractor.Functions;

internal sealed class ExtractFunction
{
    private readonly MimeTypesDetection _mimeTypeDetection;
    private readonly Extractor _extractor;
    private readonly ILogger<ExtractFunction> _log;

    public ExtractFunction(MimeTypesDetection mimeTypeDetection, Extractor extractor, ILoggerFactory? lf = null)
    {
        this._mimeTypeDetection = mimeTypeDetection;
        this._extractor = extractor;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<ExtractFunction>();
    }

    public async Task<IResult> InvokeAsync(ExtractRequest req, CancellationToken cancellationToken = default)
    {
        if (req == null)
        {
            return Results.BadRequest("The request is empty");
        }

        if (string.IsNullOrWhiteSpace(req.MimeType))
        {
            req.MimeType = this._mimeTypeDetection.GetFileType(req.FileName);
        }

        // Decode base64 content
        BinaryData data = new(Convert.FromBase64String(req.Content));

        // Prepare file to process
        var file = new FileToProcess
        {
            Content = data,
            MimeType = req.MimeType,
            Size = data.Length
        };

        return Results.Ok(await this._extractor.ExtractAsync(file, cancellationToken).ConfigureAwait(false));
    }
}
