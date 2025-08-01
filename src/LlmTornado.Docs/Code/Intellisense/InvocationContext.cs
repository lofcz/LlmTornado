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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LlmTornado.Docs.Code.Intellisense;

internal class InvocationContext
{
    public SemanticModel SemanticModel { get; }
    public int Position { get; }
    public SyntaxNode Receiver { get; }
    public IEnumerable<TypeInfo> ArgumentTypes { get; }
    public IEnumerable<SyntaxToken> Separators { get; }
    public bool IsInStaticContext { get; }

    public InvocationContext(SemanticModel semModel, int position, SyntaxNode receiver, ArgumentListSyntax argList, bool isStatic)
    {
        SemanticModel = semModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argList.Arguments.Select(argument => semModel.GetTypeInfo(argument.Expression));
        Separators = argList.Arguments.GetSeparators();
        IsInStaticContext = isStatic;
    }

    public InvocationContext(SemanticModel semModel, int position, SyntaxNode receiver, AttributeArgumentListSyntax argList, bool isStatic)
    {
        SemanticModel = semModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argList.Arguments.Select(argument => semModel.GetTypeInfo(argument.Expression));
        Separators = argList.Arguments.GetSeparators();
        IsInStaticContext = isStatic;
    }
}
