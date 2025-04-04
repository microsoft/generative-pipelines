// Copyright (c) Microsoft. All rights reserved.

using Extractor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.KernelMemory.Pipeline;

namespace Extractor.Functions;

internal sealed class ExtractMultipartFunction
{
    private readonly MimeTypesDetection _mimeTypeDetection;
    private readonly Extractor _extractor;
    private readonly ILogger<ExtractMultipartFunction> _log;

    public ExtractMultipartFunction(MimeTypesDetection mimeTypeDetection, Extractor extractor, ILoggerFactory? lf = null)
    {
        this._mimeTypeDetection = mimeTypeDetection;
        this._extractor = extractor;
        this._log = (lf ?? NullLoggerFactory.Instance).CreateLogger<ExtractMultipartFunction>();
    }

    public async Task<IResult> InvokeAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        HttpRequest request = httpContext.Request;

        if (!request.HasFormContentType)
        {
            throw new ArgumentException("Invalid content, multipart form data not found");
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);

        // There must be one file
        if (form.Files.Count != 1)
        {
            return Results.BadRequest(form.Files.Count == 0
                ? "No file was uploaded"
                : "Only one file can be uploaded");
        }

        // Read file content
        Stream s = form.Files[0].OpenReadStream();
        BinaryData data;
        await using (s.ConfigureAwait(false))
        {
            data = new BinaryData(ReadAllBytes(s));
        }

        // Prepare file to process
        var file = new FileToProcess
        {
            Content = data,
            MimeType = this._mimeTypeDetection.GetFileType(form.Files[0].FileName),
            Size = form.Files[0].Length
        };

        return Results.Ok(await this._extractor.ExtractAsync(file, cancellationToken).ConfigureAwait(false));
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream s1)
        {
            return s1.ToArray();
        }

        using (var s2 = new MemoryStream())
        {
            stream.CopyTo(s2);
            return s2.ToArray();
        }
    }
}
