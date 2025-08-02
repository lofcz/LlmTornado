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

using System.Collections.Immutable;

namespace LlmTornado.Docs.Code.Intellisense;

internal static class ImmutableArrayExtensions
{
    public static ImmutableArray<TOut> SelectAsArray<TIn, TOut>(this ImmutableArray<TIn> array, Func<TIn, TOut> mapper)
    {
        if (array.IsDefaultOrEmpty)
        {
            return ImmutableArray<TOut>.Empty;
        }

        ImmutableArray<TOut>.Builder builder = ImmutableArray.CreateBuilder<TOut>(array.Length);
        foreach (TIn e in array)
        {
            builder.Add(mapper(e));
        }

        return builder.MoveToImmutable();
    }

    public static ImmutableArray<T> ToImmutableAndClear<T>(this ImmutableArray<T>.Builder builder)
    {
        if (builder.Capacity == builder.Count)
        {
            return builder.MoveToImmutable();
        }
        else
        {
            ImmutableArray<T> result = builder.ToImmutable();
            builder.Clear();
            return result;
        }
    }
}
