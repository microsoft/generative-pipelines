// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Extractor.Models;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.Pipeline;

namespace Extractor;

internal sealed class Extractor
{
    private readonly List<IContentDecoder> _decoders;

    public Extractor()
    {
        this._decoders = new List<IContentDecoder>();

        this._decoders.Add(new HtmlDecoder());
        this._decoders.Add(new MarkDownDecoder());
        this._decoders.Add(new MsExcelDecoder());
        this._decoders.Add(new MsPowerPointDecoder());
        this._decoders.Add(new MsWordDecoder());
        this._decoders.Add(new PdfDecoder());
    }

    public async Task<ExtractResponse> ExtractAsync(
        FileToProcess file,
        CancellationToken cancellationToken = default)
    {
        var result = new ExtractResponse
        {
            MimeType = file.MimeType,
            Sections = [],
            FullText = string.Empty,
            Size = file.Size
        };

        if (string.IsNullOrWhiteSpace(file.MimeType)) { return result; }

        switch (file.MimeType)
        {
            case MimeTypes.PlainText:
            case MimeTypes.Json:
                result.Sections = [new ExtractedSection { Content = file.Content.ToString() }];
                result.FullText = file.Content.ToString();
                return result;

            default:
                var decoder = this._decoders.LastOrDefault(d => d.SupportsMimeType(file.MimeType));
                if (decoder is null)
                {
                    throw new ArgumentException("Mime type not supported", nameof(file.MimeType));
                }

                var fullText = new StringBuilder();
                FileContent fileContent = await decoder.DecodeAsync(file.Content, cancellationToken).ConfigureAwait(false);

                foreach (Chunk chunk in fileContent.Sections)
                {
                    var section = new ExtractedSection
                    {
                        Content = chunk.Content,
                        Metadata = { ["PageNumber"] = chunk.PageNumber }
                    };
                    foreach (var meta in chunk.Metadata)
                    {
                        section.Metadata[meta.Key] = meta.Value;
                    }

                    fullText.Append(chunk.Content);
                    result.Sections.Add(section);
                }

                result.FullText = fullText.ToString();
                return result;
        }
    }
}
