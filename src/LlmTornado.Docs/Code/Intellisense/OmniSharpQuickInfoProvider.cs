/* 
This code is adapted from https://github.com/OmniSharp/omnisharp-vscode

MIT License

Copyright (c) .NET Foundation and Contributors
All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 
*/

using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Options;
using OmniSharp.Roslyn.CSharp.Helpers;

#nullable enable

namespace LlmTornado.Docs.Code.Intellisense;

public class OmniSharpQuickInfoProvider
{
    // Based on https://github.com/dotnet/roslyn/blob/7dc32a952e77c96c31cae6a2ba6d253a558fc7ff/src/Features/LanguageServer/Protocol/Handler/Hover/HoverHandler.cs

    // These are internal tag values taken from https://github.com/dotnet/roslyn/blob/master/src/Features/Core/Portable/Common/TextTags.cs
    // They're copied here so that we can ensure we render blocks correctly in the markdown
    // https://github.com/dotnet/roslyn/issues/46254 tracks making these public

    /// <summary>
    /// Section kind for nullability analysis.
    /// </summary>
    internal const string NullabilityAnalysis = nameof(NullabilityAnalysis);

    private readonly AdhocWorkspace _workspace;
    private readonly FormattingOptions _formattingOptions;
    private readonly ILogger<OmniSharpQuickInfoProvider>? _logger;


    public OmniSharpQuickInfoProvider(AdhocWorkspace workspace, FormattingOptions formattingOptions, ILoggerFactory? loggerFactory)
    {
        _workspace = workspace;
        _formattingOptions = formattingOptions;
        _logger = loggerFactory?.CreateLogger<OmniSharpQuickInfoProvider>();
    }

    public async Task<QuickInfoResponse> Handle(QuickInfoRequest request, Document document)
    {

        QuickInfoResponse response = new QuickInfoResponse();
        response.Markdown = "HAHAHAHAHA";

        if (document is null)
        {
            return response;
        }

        QuickInfoService? quickInfoService = QuickInfoService.GetService(document);
        if (quickInfoService is null)
        {
            _logger?.LogWarning($"QuickInfo service was null for {document.FilePath}");
            return response;
        }

        SourceText sourceText = await document.GetTextAsync();
        int position = sourceText.GetTextPosition(request);

        QuickInfoItem? quickInfo = await quickInfoService.GetQuickInfoAsync(document, position);
        if (quickInfo is null)
        {
            _logger?.LogTrace($"No QuickInfo found for {document.FilePath}:{request.Line},{request.Column}");
            return response;
        }

        StringBuilder finalTextBuilder = new StringBuilder();

        bool lastSectionHadLineBreak = true;
        QuickInfoSection? description = quickInfo.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
        if (description is object)
        {
            appendSection(description, MarkdownFormat.AllTextAsCSharp);
        }

        QuickInfoSection? summary = quickInfo.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.DocumentationComments);
        if (summary is object)
        {
            appendSection(summary, MarkdownFormat.Default);
        }

        foreach (QuickInfoSection section in quickInfo.Sections)
        {
            switch (section.Kind)
            {
                case QuickInfoSectionKinds.Description:
                case QuickInfoSectionKinds.DocumentationComments:
                    continue;

                case QuickInfoSectionKinds.TypeParameters:
                    appendSection(section, MarkdownFormat.AllTextAsCSharp);
                    break;

                case QuickInfoSectionKinds.AnonymousTypes:
                    // The first line is "Anonymous Types:"
                    // Then we want all anonymous types to be C# highlighted
                    appendSection(section, MarkdownFormat.FirstLineDefaultRestCSharp);
                    break;

                case NullabilityAnalysis:
                    // Italicize the nullable analysis for emphasis.
                    appendSection(section, MarkdownFormat.Italicize);
                    break;

                default:
                    appendSection(section, MarkdownFormat.Default);
                    break;
            }
        }

        response.Markdown = finalTextBuilder.ToString().Trim();

        return response;

        void appendSection(QuickInfoSection section, MarkdownFormat format)
        {
            if (!lastSectionHadLineBreak && !section.TaggedParts.StartsWithNewline())
            {
                finalTextBuilder.Append(_formattingOptions.NewLine);
                finalTextBuilder.Append(_formattingOptions.NewLine);
            }
            MarkdownHelpers.TaggedTextToMarkdown(section.TaggedParts, finalTextBuilder, _formattingOptions, format, out lastSectionHadLineBreak);
        }
    }
}
