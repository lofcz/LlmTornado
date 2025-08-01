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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Roslyn.CSharp.Services.Documentation;

namespace LlmTornado.Docs.Code.Intellisense;

public class OmniSharpSignatureHelpService
{
    private readonly AdhocWorkspace _workspace;


    public OmniSharpSignatureHelpService(AdhocWorkspace workspace)
    {
        _workspace = workspace;
    }

    public async Task<SignatureHelpResponse> Handle(SignatureHelpRequest request, Document document2)
    {
        List<InvocationContext> invocations = new List<InvocationContext>();
        foreach (Document document in new[] { document2 })
        {
            InvocationContext? invocation = await GetInvocation(document, request);
            if (invocation != null)
            {
                invocations.Add(invocation);
            }
        }

        if (invocations.Count == 0)
        {
            return null;
        }

        SignatureHelpResponse response = new SignatureHelpResponse();

        // define active parameter by position
        foreach (SyntaxToken comma in invocations.First().Separators)
        {
            if (comma.Span.Start > invocations.First().Position)
            {
                break;
            }
            response.ActiveParameter += 1;
        }

        // process all signatures, define active signature by types
        HashSet<SignatureHelpItem> signaturesSet = new HashSet<SignatureHelpItem>();
        int bestScore = int.MinValue;
        SignatureHelpItem bestScoredItem = null;

        foreach (InvocationContext invocation in invocations)
        {
            IEnumerable<TypeInfo> types = invocation.ArgumentTypes;
            ISymbol throughSymbol = null;
            ISymbol throughType = null;
            IEnumerable<IMethodSymbol> methodGroup = invocation.SemanticModel.GetMemberGroup(invocation.Receiver).OfType<IMethodSymbol>();
            if (invocation.Receiver is MemberAccessExpressionSyntax)
            {
                ExpressionSyntax throughExpression = ((MemberAccessExpressionSyntax)invocation.Receiver).Expression;
                throughSymbol = invocation.SemanticModel.GetSpeculativeSymbolInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsExpression).Symbol;
                throughType = invocation.SemanticModel.GetSpeculativeTypeInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                bool includeInstance = (throughSymbol != null && !(throughSymbol is ITypeSymbol)) ||
                                       throughExpression is LiteralExpressionSyntax ||
                                       throughExpression is TypeOfExpressionSyntax;
                bool includeStatic = (throughSymbol is INamedTypeSymbol) || throughType != null;
                methodGroup = methodGroup.Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
            }
            else if (invocation.Receiver is SimpleNameSyntax && invocation.IsInStaticContext)
            {
                methodGroup = methodGroup.Where(m => m.IsStatic || m.MethodKind == MethodKind.LocalFunction);
            }

            foreach (IMethodSymbol methodOverload in methodGroup)
            {
                SignatureHelpItem signature = BuildSignature(methodOverload);
                signaturesSet.Add(signature);

                int score = InvocationScore(methodOverload, types);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoredItem = signature;
                }
            }
        }

        List<SignatureHelpItem> signaturesList = signaturesSet.ToList();
        response.Signatures = signaturesList;
        response.ActiveSignature = signaturesList.IndexOf(bestScoredItem);

        return response;
    }

    private async Task<InvocationContext> GetInvocation(Document document, Request request)
    {
        SourceText sourceText = await document.GetTextAsync();
        int position = sourceText.GetTextPosition(request);
        SyntaxTree? tree = await document.GetSyntaxTreeAsync();
        SyntaxNode root = await tree.GetRootAsync();
        SyntaxNode? node = root.FindToken(position).Parent;

        // Walk up until we find a node that we're interested in.
        while (node != null)
        {
            if (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Span.Contains(position))
            {
                SemanticModel? semanticModel = await document.GetSemanticModelAsync();
                return new InvocationContext(semanticModel, position, invocation.Expression, invocation.ArgumentList, invocation.IsInStaticContext());
            }

            if (node is ObjectCreationExpressionSyntax objectCreation && objectCreation.ArgumentList.Span.Contains(position))
            {
                SemanticModel? semanticModel = await document.GetSemanticModelAsync();
                return new InvocationContext(semanticModel, position, objectCreation, objectCreation.ArgumentList, objectCreation.IsInStaticContext());
            }

            if (node is AttributeSyntax attributeSyntax && attributeSyntax.ArgumentList.Span.Contains(position))
            {
                SemanticModel? semanticModel = await document.GetSemanticModelAsync();
                return new InvocationContext(semanticModel, position, attributeSyntax, attributeSyntax.ArgumentList, attributeSyntax.IsInStaticContext());
            }

            node = node.Parent;
        }

        return null;
    }

    private int InvocationScore(IMethodSymbol symbol, IEnumerable<TypeInfo> types)
    {
        ImmutableArray<IParameterSymbol> parameters = symbol.Parameters;
        if (parameters.Count() < types.Count())
        {
            return int.MinValue;
        }

        int score = 0;
        IEnumerator<TypeInfo> invocationEnum = types.GetEnumerator();
        ImmutableArray<IParameterSymbol>.Enumerator definitionEnum = parameters.GetEnumerator();
        while (invocationEnum.MoveNext() && definitionEnum.MoveNext())
        {
            if (invocationEnum.Current.ConvertedType == null)
            {
                // 1 point for having a parameter
                score += 1;
            }
            else if (SymbolEqualityComparer.Default.Equals(invocationEnum.Current.ConvertedType, definitionEnum.Current.Type))
            {
                // 2 points for having a parameter and being
                // the same type
                score += 2;
            }
        }

        return score;
    }

    private static SignatureHelpItem BuildSignature(IMethodSymbol symbol)
    {
        SignatureHelpItem signature = new SignatureHelpItem();
        signature.Documentation = symbol.GetDocumentationCommentXml();
        signature.Name = symbol.MethodKind == MethodKind.Constructor ? symbol.ContainingType.Name : symbol.Name;
        signature.Label = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        signature.StructuredDocumentation = DocumentationConverter.GetStructuredDocumentation(symbol);

        signature.Parameters = symbol.Parameters.Select(parameter =>
        {
            return new SignatureHelpParameter()
            {
                Name = parameter.Name,
                Label = parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                Documentation = signature.StructuredDocumentation.GetParameterText(parameter.Name)
            };
        });

        return signature;
    }
}
