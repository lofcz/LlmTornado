#if !MODERN
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Code;

internal static partial class Extensions
{
    public static Dictionary<string, IEnumerable<string>> ConvertHeaders(this HttpRequestHeaders headers)
    {
        Dictionary<string, IEnumerable<string>> result = new Dictionary<string, IEnumerable<string>>();
    
        foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
        {
            result[header.Key] = header.Value;
        }
    
        return result;
    }
    
    public static double Clamp(this double value, double min, double max)
    {
        if (value < min)
            return min;
        return value > max ? max : value;
    }
    
    public static float Clamp(this float value, float min, float max)
    {
        if (value < min)
            return min;
        return value > max ? max : value;
    }
    
    public static int Clamp(this int value, int min, int max)
    {
        if (value < min)
            return min;
        return value > max ? max : value;
    }
    
    public static long Clamp(this long value, long min, long max)
    {
        if (value < min)
            return min;
        return value > max ? max : value;
    }
    
    private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => // 32
    [
        00, 01, 28, 02, 29, 14, 24, 03,
        30, 22, 20, 15, 25, 17, 04, 08,
        31, 27, 13, 23, 21, 19, 16, 07,
        26, 12, 18, 06, 11, 05, 10, 09
    ];

    private static ReadOnlySpan<byte> Log2DeBruijn => // 32
    [
        00, 09, 01, 10, 13, 21, 02, 29,
        11, 14, 16, 18, 22, 25, 03, 30,
        08, 12, 20, 28, 15, 17, 24, 07,
        19, 27, 23, 06, 26, 05, 04, 31
    ];
    
    private static int Log2SoftwareFallback(uint value)
    {
        // No AggressiveInlining due to large method size
        // Has conventional contract 0->0 (Log(0) is undefined)

        // Fill trailing zeros with ones, eg 00010010 becomes 00011111
        value |= value >> 01;
        value |= value >> 02;
        value |= value >> 04;
        value |= value >> 08;
        value |= value >> 16;

        // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
        return Unsafe.AddByteOffset(
            // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
            ref MemoryMarshal.GetReference(Log2DeBruijn),
            // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
            (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CLSCompliant(false)]
    public static int LeadingZeroCount(this uint value)
    {
        return 31 ^ Log2SoftwareFallback(value);
    }
    
    public static int LeadingZeroCount(this int value)
    {
        return LeadingZeroCount((uint)value);
    }
    
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search, StringComparison.InvariantCulture);
        if (pos < 0)
            return text;
    
        StringBuilder sb = new StringBuilder(text.Length - search.Length + replace.Length);
        sb.Append(text, 0, pos);
        sb.Append(replace);
        sb.Append(text, pos + search.Length, text.Length - pos - search.Length);
        return sb.ToString();
    }
}
#endif