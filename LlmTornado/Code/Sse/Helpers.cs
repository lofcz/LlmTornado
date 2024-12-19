// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Text;

namespace LlmTornado.Code.Sse
{
    internal static class Helpers
    {
        public static void WriteUtf8Number(this IBufferWriter<byte> writer, long value)
        {
            const int MaxDecimalDigits = 20;
            Span<byte> buffer = writer.GetSpan(MaxDecimalDigits);
            bool success = value.TryFormat(buffer, out int bytesWritten, provider: CultureInfo.InvariantCulture);
            writer.Advance(bytesWritten);
        }

        public static void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            Span<byte> buffer = writer.GetSpan(value.Length);
            value.CopyTo(buffer);
            writer.Advance(value.Length);
        }

        public static void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            Span<byte> buffer = writer.GetSpan(maxByteCount);
            int bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
            writer.Advance(bytesWritten);
        }
    }
}
