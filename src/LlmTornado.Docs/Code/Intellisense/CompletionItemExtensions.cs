/* MIT License
 * 
 * Copyright (c) .NET Foundation and Contributors All Rights Reserved
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using CompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace LlmTornado.Docs.Code.Intellisense;

internal static class CompletionItemExtensions
{
    private const string GetSymbolsAsync = nameof(GetSymbolsAsync);
    private const string InsertionText = nameof(InsertionText);
    private const string ObjectCreationCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.ObjectCreationCompletionProvider";
    private const string NamedParameterCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.NamedParameterCompletionProvider";
    internal const string OverrideCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.OverrideCompletionProvider";
    internal const string PartialMethodCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.PartialMethodCompletionProvider";
    internal const string InternalsVisibleToCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.InternalsVisibleToCompletionProvider";
    internal const string XmlDocCommentCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.XmlDocCommentCompletionProvider";
    internal const string TypeImportCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.TypeImportCompletionProvider";
    internal const string ExtensionMethodImportCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.ExtensionMethodImportCompletionProvider";
    internal const string EmeddedLanguageCompletionProvider = "Microsoft.CodeAnalysis.CSharp.Completion.Providers.EmbeddedLanguageCompletionProvider";
    private const string ProviderName = nameof(ProviderName);
    private const string SymbolCompletionItem = "Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem";
    private const string SymbolKind = nameof(SymbolKind);
    private const string SymbolName = nameof(SymbolName);
    private const string Symbols = nameof(Symbols);
    private static readonly Type _symbolCompletionItemType;
    private static readonly MethodInfo _getSymbolsAsync;
    private static readonly PropertyInfo _getProviderName;
    private static readonly MethodInfo _getCompletionsInternalAsync;
    private static readonly MethodInfo _getChangeAsync;
    internal static readonly PerLanguageOption<bool?> ShowItemsFromUnimportedNamespaces = new PerLanguageOption<bool?>("CompletionOptions", "ShowItemsFromUnimportedNamespaces", defaultValue: null);

    static CompletionItemExtensions()
    {
        _symbolCompletionItemType = typeof(CompletionItem).GetTypeInfo().Assembly.GetType(SymbolCompletionItem);
        _getSymbolsAsync = _symbolCompletionItemType.GetMethod(GetSymbolsAsync, BindingFlags.Public | BindingFlags.Static);

        _getProviderName = typeof(CompletionItem).GetProperty(ProviderName, BindingFlags.NonPublic | BindingFlags.Instance);

        _getCompletionsInternalAsync = typeof(Microsoft.CodeAnalysis.Completion.CompletionService).GetMethod(nameof(GetCompletionsInternalAsync), BindingFlags.NonPublic | BindingFlags.Instance);
        _getChangeAsync = typeof(Microsoft.CodeAnalysis.Completion.CompletionService).GetMethod(nameof(GetChangeAsync), BindingFlags.NonPublic | BindingFlags.Instance);
    }

    internal static string GetProviderName(this CompletionItem item) => (string)_getProviderName.GetValue(item);

    public static bool IsObjectCreationCompletionItem(this CompletionItem item) => GetProviderName(item) == ObjectCreationCompletionProvider;

    public static Task<(CompletionList completionList, bool expandItemsAvailable)> GetCompletionsInternalAsync(
        this Microsoft.CodeAnalysis.Completion.CompletionService completionService,
        Document document,
        int caretPosition,
        CompletionTrigger trigger = default,
        ImmutableHashSet<string> roles = null,
        OptionSet options = null,
        CancellationToken cancellationToken = default)
        => (Task<(CompletionList completionList, bool expandItemsAvailable)>)_getCompletionsInternalAsync.Invoke(completionService, new object[] { document, caretPosition, trigger, roles, options, cancellationToken });

    internal static Task<CompletionChange> GetChangeAsync(
        this Microsoft.CodeAnalysis.Completion.CompletionService completionService,
        Document document,
        CompletionItem item,
        TextSpan completionListSpan,
        char? commitCharacter = null,
        bool disallowAddingImports = false,
        CancellationToken cancellationToken = default)
        => (Task<CompletionChange>)_getChangeAsync.Invoke(completionService, new object[] { document, item, completionListSpan, commitCharacter, disallowAddingImports, cancellationToken });

    public static bool TryGetInsertionText(this CompletionItem completionItem, out string insertionText) => completionItem.Properties.TryGetValue(InsertionText, out insertionText);
}
