#if !MODERN
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace LlmTornado.Code
{
    internal sealed class HttpUtility
    {
        private sealed class HttpQSCollection : NameValueCollection
        {
            internal HttpQSCollection()
                : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            public override string ToString()
            {
                int count = Count;
                if (count == 0)
                {
                    return "";
                }

                StringBuilder sb = new StringBuilder();
                string[] keys = AllKeys;
                for (int i = 0; i < count; i++)
                {
                    string key = keys[i];
                    string[] values = GetValues(key);
                    if (values != null)
                    {
                        foreach (string value in values)
                        {
                            if (!string.IsNullOrEmpty(key))
                            {
                                sb.Append(UrlEncode(key)).Append('=');
                            }
                            sb.Append(UrlEncode(value)).Append('&');
                        }
                    }
                }

                return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "";
            }
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            HttpQSCollection result = new HttpQSCollection();
            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            int queryLength = query.Length;
            int namePos = query.StartsWith("?") ? 1 : 0;
            if (queryLength == namePos)
            {
                return result;
            }

            while (namePos <= queryLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (int q = namePos; q < queryLength; q++)
                {
                    if (valuePos == -1 && query[q] == '=')
                    {
                        valuePos = q + 1;
                    }
                    else if (query[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }
                }

                string name;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UrlDecode(query.Substring(namePos, valuePos - namePos - 1), encoding);
                }

                if (valueEnd < 0)
                {
                    valueEnd = query.Length;
                }

                namePos = valueEnd + 1;
                string value = UrlDecode(query.Substring(valuePos, valueEnd - valuePos), encoding);
                result.Add(name, value);
            }

            return result;
        }

        public static string HtmlDecode(string s)
        {
            return HttpEncoder.HtmlDecode(s);
        }

        public static void HtmlDecode(string s, TextWriter output)
        {
            HttpEncoder.HtmlDecode(s, output);
        }

        public static string HtmlEncode(string s)
        {
            return HttpEncoder.HtmlEncode(s);
        }

        public static void HtmlEncode(string s, TextWriter output)
        {
            HttpEncoder.HtmlEncode(s, output);
        }

        public static string HtmlAttributeEncode(string s)
        {
            return HttpEncoder.HtmlAttributeEncode(s);
        }

        public static void HtmlAttributeEncode(string s, TextWriter output)
        {
            HttpEncoder.HtmlAttributeEncode(s, output);
        }

        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8);
        }

        public static string UrlPathEncode(string str)
        {
            return HttpEncoder.UrlPathEncode(str);
        }

        public static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        }

        public static byte[] UrlEncodeToBytes(string str)
        {
            return UrlEncodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
        public static byte[] UrlEncodeUnicodeToBytes(string str)
        {
            if (str == null)
                return null;
            return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));
        }

        public static string UrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8);
        }

        public static string UrlDecode(byte[] bytes, Encoding e)
        {
            if (bytes == null)
                return null;
            return UrlDecode(bytes, 0, bytes.Length, e);
        }

        public static byte[] UrlDecodeToBytes(string str)
        {
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlDecodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeToBytes(e.GetBytes(str));
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return HttpEncoder.UrlDecode(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
                return null;
            return HttpEncoder.UrlEncode(str, e);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            return HttpEncoder.UrlEncode(bytes, offset, count);
        }

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
        public static string UrlEncodeUnicode(string str)
        {
            return HttpEncoder.UrlEncodeUnicode(str);
        }

        public static string UrlDecode(string str, Encoding e)
        {
            return HttpEncoder.UrlDecode(str, e);
        }

        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
        {
            return HttpEncoder.UrlDecode(bytes, offset, count, e);
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            return HttpEncoder.UrlDecode(bytes, offset, count);
        }

        public static string JavaScriptStringEncode(string value)
        {
            return HttpEncoder.JavaScriptStringEncode(value, false);
        }

        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            return HttpEncoder.JavaScriptStringEncode(value, addDoubleQuotes);
        }
    }

    internal static class HttpEncoder
    {
        // Set of safe chars, from RFC 1738.4 minus '+'
        private static readonly bool[] s_urlSafe = new bool[128];
        private static readonly char[] s_invalidJavaScriptChars;
        private static readonly char[] s_htmlAttributeEncodingChars = new[] { '<', '"', '\'', '&' };

        static HttpEncoder()
        {
            const string SafeChars = "!()*-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
            foreach (char c in SafeChars)
            {
                s_urlSafe[c] = true;
            }

            const string InvalidJsChars = "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F" +
                                        "\"&'<>\\" +
                                        "\u0085\u2028\u2029";
            s_invalidJavaScriptChars = InvalidJsChars.ToCharArray();
        }

        internal static string HtmlAttributeEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int pos = value.IndexOfAny(s_htmlAttributeEncodingChars);
            if (pos < 0)
            {
                return value;
            }

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            HtmlAttributeEncodeInternal(value, pos, writer);
            return writer.ToString();
        }

        internal static void HtmlAttributeEncode(string value, TextWriter output)
        {
            if (value == null)
            {
                return;
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            int pos = value.IndexOfAny(s_htmlAttributeEncodingChars);
            if (pos < 0)
            {
                output.Write(value);
                return;
            }

            HtmlAttributeEncodeInternal(value, pos, output);
        }

        private static void HtmlAttributeEncodeInternal(string s, int index, TextWriter output)
        {
            output.Write(s.Substring(0, index));

            for (int i = index; i < s.Length; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '<':
                        output.Write("<");
                        break;
                    case '"':
                        output.Write("\"");
                        break;
                    case '\'':
                        output.Write("'");
                        break;
                    case '&':
                        output.Write("&");
                        break;
                    default:
                        output.Write(ch);
                        break;
                }
            }
        }

        internal static string HtmlDecode(string value)
        {
            return string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(value);
        }

        internal static void HtmlDecode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.Write(WebUtility.HtmlDecode(value));
        }

        internal static string HtmlEncode(string value)
        {
            return string.IsNullOrEmpty(value) ? value : WebUtility.HtmlEncode(value);
        }

        internal static void HtmlEncode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.Write(WebUtility.HtmlEncode(value));
        }

        internal static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            if (string.IsNullOrEmpty(value))
            {
                return addDoubleQuotes ? "\"\"" : string.Empty;
            }

            int i = value.IndexOfAny(s_invalidJavaScriptChars);
            if (i < 0)
            {
                return addDoubleQuotes ? "\"" + value + "\"" : value;
            }

            return EncodeJsStringCore(value, i, addDoubleQuotes);
        }

        private static string EncodeJsStringCore(string value, int firstInvalidChar, bool addDoubleQuotes)
        {
            StringBuilder sb = new StringBuilder(value.Length + 5);
            if (addDoubleQuotes)
            {
                sb.Append('"');
            }

            int lastIndex = 0;
            int currentIndex = firstInvalidChar;

            while (currentIndex != -1)
            {
                sb.Append(value, lastIndex, currentIndex - lastIndex);
                char c = value[currentIndex];
                switch (c)
                {
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        break;
                }
                lastIndex = currentIndex + 1;
                currentIndex = value.IndexOfAny(s_invalidJavaScriptChars, lastIndex);
            }

            sb.Append(value, lastIndex, value.Length - lastIndex);

            if (addDoubleQuotes)
            {
                sb.Append('"');
            }

            return sb.ToString();
        }

        internal static byte[] UrlDecode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            int decodedBytesCount = 0;
            byte[] decodedBytes = new byte[count];

            for (int i = 0; i < count; i++)
            {
                int pos = offset + i;
                byte b = bytes[pos];

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    int h1 = HexConverter.FromChar((char)bytes[pos + 1]);
                    int h2 = HexConverter.FromChar((char)bytes[pos + 2]);

                    if (h1 != -1 && h2 != -1)
                    {
                        b = (byte)((h1 << 4) | h2);
                        i += 2;
                    }
                }
                decodedBytes[decodedBytesCount++] = b;
            }

            if (decodedBytesCount < decodedBytes.Length)
            {
                byte[] result = new byte[decodedBytesCount];
                Array.Copy(decodedBytes, 0, result, 0, decodedBytesCount);
                return result;
            }
            return decodedBytes;
        }

        internal static string UrlDecode(byte[] bytes, int offset, int count, Encoding encoding)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            UrlDecoder helper = new UrlDecoder(count, encoding);

            for (int i = 0; i < count; i++)
            {
                int pos = offset + i;
                byte b = bytes[pos];

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    if (bytes[pos + 1] == 'u' && i < count - 5)
                    {
                        int h1 = HexConverter.FromChar((char)bytes[pos + 2]);
                        int h2 = HexConverter.FromChar((char)bytes[pos + 3]);
                        int h3 = HexConverter.FromChar((char)bytes[pos + 4]);
                        int h4 = HexConverter.FromChar((char)bytes[pos + 5]);

                        if (h1 != -1 && h2 != -1 && h3 != -1 && h4 != -1)
                        {
                            char ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            i += 5;
                            helper.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexConverter.FromChar((char)bytes[pos + 1]);
                        int h2 = HexConverter.FromChar((char)bytes[pos + 2]);

                        if (h1 != -1 && h2 != -1)
                        {
                            b = (byte)((h1 << 4) | h2);
                            i += 2;
                        }
                    }
                }
                helper.AddByte(b);
            }
            return helper.GetString();
        }

        internal static string UrlDecode(string value, Encoding encoding)
        {
            if (value == null)
            {
                return null;
            }

            int count = value.Length;
            UrlDecoder helper = new UrlDecoder(count, encoding);

            for (int pos = 0; pos < count; pos++)
            {
                char ch = value[pos];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    if (value[pos + 1] == 'u' && pos < count - 5)
                    {
                        int h1 = HexConverter.FromChar(value[pos + 2]);
                        int h2 = HexConverter.FromChar(value[pos + 3]);
                        int h3 = HexConverter.FromChar(value[pos + 4]);
                        int h4 = HexConverter.FromChar(value[pos + 5]);

                        if (h1 != -1 && h2 != -1 && h3 != -1 && h4 != -1)
                        {
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;
                            helper.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexConverter.FromChar(value[pos + 1]);
                        int h2 = HexConverter.FromChar(value[pos + 2]);

                        if (h1 != -1 && h2 != -1)
                        {
                            byte b = (byte)((h1 << 4) | h2);
                            pos += 2;
                            helper.AddByte(b);
                            continue;
                        }
                    }
                }

                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch);
                else
                    helper.AddChar(ch);
            }
            return helper.GetString();
        }

        internal static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }
            return UrlEncode(bytes, offset, count, false);
        }

        private static bool NeedsEncoding(byte[] bytes, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                if ((b < '0' || b > '9') && (b < 'a' || b > 'z') && (b < 'A' || b > 'Z') && b != '-' && b != '_' && b != '.')
                {
                    return true;
                }
            }
            return false;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            int cUnsafe = 0;
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                if (b == ' ')
                    cUnsafe++;
                else if (!(b < 128 && s_urlSafe[b]))
                    cUnsafe += 2;
            }

            if (!alwaysCreateNewReturnValue && cUnsafe == 0)
            {
                if (offset == 0 && count == bytes.Length)
                    return bytes;

                byte[] sub = new byte[count];
                Buffer.BlockCopy(bytes, offset, sub, 0, count);
                return sub;
            }

            byte[] expandedBytes = new byte[count + cUnsafe];
            int pos = 0;
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                if (b < 128 && s_urlSafe[b])
                {
                    expandedBytes[pos++] = b;
                }
                else if (b == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b);
                }
            }
            return expandedBytes;
        }

        internal static byte[] UrlEncode(string str, Encoding e)
        {
            if (str == null)
                return null;
            byte[] bytes = e.GetBytes(str);
            return UrlEncode(bytes, 0, bytes.Length, false);
        }

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(*).")]
        internal static string UrlEncodeUnicode(string value)
        {
            if (value == null)
            {
                return null;
            }

            int l = value.Length;
            StringBuilder sb = new StringBuilder(l);

            for (int i = 0; i < l; i++)
            {
                char ch = value[i];

                if ((ch & 0xff80) == 0)
                {
                    if (ch < 128 && s_urlSafe[ch])
                    {
                        sb.Append(ch);
                    }
                    else if (ch == ' ')
                    {
                        sb.Append('+');
                    }
                    else
                    {
                        sb.Append('%');
                        sb.Append(HexConverter.ToCharLower(ch >> 4));
                        sb.Append(HexConverter.ToCharLower(ch));
                    }
                }
                else
                {
                    sb.Append("%u");
                    sb.Append(HexConverter.ToCharLower(ch >> 12));
                    sb.Append(HexConverter.ToCharLower(ch >> 8));
                    sb.Append(HexConverter.ToCharLower(ch >> 4));
                    sb.Append(HexConverter.ToCharLower(ch));
                }
            }
            return sb.ToString();
        }

        internal static string UrlPathEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string schemeAndAuthority, path, queryAndFragment;
            if (!UriUtil.TrySplitUriForPathEncode(value, out schemeAndAuthority, out path, out queryAndFragment))
            {
                return UrlPathEncodeImpl(value);
            }
            return string.Concat(schemeAndAuthority, UrlPathEncodeImpl(path), queryAndFragment);
        }

        private static string UrlPathEncodeImpl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int i = IndexOfPathEncodingChars(value);
            if (i < 0)
            {
                return value;
            }

            int indexOfQuery = value.IndexOf('?');
            string toEncode = (indexOfQuery >= 0 && indexOfQuery < i) ? value :
                              (indexOfQuery >= 0) ? value.Substring(0, indexOfQuery) : value;

            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < toEncode.Length; j++)
            {
                char ch = toEncode[j];
                if (ch < 33 || ch > 126)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(new[] { ch });
                    foreach (byte b in bytes)
                    {
                        sb.Append('%');
                        sb.Append(HexConverter.ToCharLower(b >> 4));
                        sb.Append(HexConverter.ToCharLower(b));
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return (indexOfQuery >= 0) ? sb.ToString() + value.Substring(indexOfQuery) : sb.ToString();
        }

        private static int IndexOfPathEncodingChars(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < 33 || s[i] > 126)
                    return i;
            }
            return -1;
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if (bytes == null && count == 0)
                return false;
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (offset < 0 || offset > bytes.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > bytes.Length - offset)
                throw new ArgumentOutOfRangeException("count");
            return true;
        }

        private class UrlDecoder
        {
            private int _numChars;
            private readonly char[] _charBuffer;
            private int _numBytes;
            private readonly byte[] _byteBuffer;
            private readonly Encoding _encoding;

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _encoding = encoding;
                _charBuffer = new char[bufferSize];
                _byteBuffer = new byte[bufferSize];
            }

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                _byteBuffer[_numBytes++] = b;
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                return new string(_charBuffer, 0, _numChars);
            }
        }

        private static class HexConverter
        {
            internal static int FromChar(char c)
            {
                if (c >= '0' && c <= '9')
                    return c - '0';
                if (c >= 'a' && c <= 'f')
                    return c - 'a' + 10;
                if (c >= 'A' && c <= 'F')
                    return c - 'A' + 10;
                return -1;
            }

            internal static char ToCharLower(int value)
            {
                value &= 0xF;
                return (char)(value > 9 ? value - 10 + 'a' : value + '0');
            }
        }

        private static class UriUtil
        {
            internal static bool TrySplitUriForPathEncode(string input, out string schemeAndAuthority, out string path, out string queryAndFragment)
            {
                schemeAndAuthority = null;
                path = null;
                queryAndFragment = string.Empty;

                int schemeEnd = input.IndexOf("://");
                if (schemeEnd < 0)
                {
                    return false;
                }

                int authorityEnd = input.IndexOf('/', schemeEnd + 3);
                if (authorityEnd < 0)
                {
                    int queryStartNoPath = input.IndexOfAny(new[] { '?', '#' }, schemeEnd + 3);
                    if (queryStartNoPath > 0)
                    {
                        schemeAndAuthority = input.Substring(0, queryStartNoPath);
                        path = string.Empty;
                        queryAndFragment = input.Substring(queryStartNoPath);
                    }
                    else
                    {
                        schemeAndAuthority = input;
                        path = string.Empty;
                    }
                    return true;
                }

                schemeAndAuthority = input.Substring(0, authorityEnd);
                string rest = input.Substring(authorityEnd);

                int queryStart = rest.IndexOfAny(new[] { '?', '#' });
                if (queryStart >= 0)
                {
                    path = rest.Substring(0, queryStart);
                    queryAndFragment = rest.Substring(queryStart);
                }
                else
                {
                    path = rest;
                }
                return true;
            }
        }
    }
}

// This is a conversion of the modern .NET System.HashCode struct to be compatible
// with .NET Framework 4.6.2. It replaces modern APIs like BitOperations.RotateLeft
// and ReadOnlySpan<T> with framework-compatible equivalents.
public struct HashCode
{
    private static readonly uint s_seed = GenerateGlobalSeed();

    private const uint Prime1 = 2654435761U;
    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    private uint _v1, _v2, _v3, _v4;
    private uint _queue1, _queue2, _queue3;
    private uint _length;

    private static uint GenerateGlobalSeed()
    {
        byte[] randomBytes = new byte[sizeof(uint)];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        return BitConverter.ToUInt32(randomBytes, 0);
    }

    public static int Combine<T1>(T1 value1)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);

        uint hash = MixEmptyState();
        hash += 4;

        hash = QueueRound(hash, hc1);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);

        uint hash = MixEmptyState();
        hash += 8;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);

        uint hash = MixEmptyState();
        hash += 12;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = QueueRound(hash, hc3);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);
        uint hc4 = (uint)(value4 != null ? value4.GetHashCode() : 0);

        uint v1, v2, v3, v4;
        Initialize(out v1, out v2, out v3, out v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 16;

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);
        uint hc4 = (uint)(value4 != null ? value4.GetHashCode() : 0);
        uint hc5 = (uint)(value5 != null ? value5.GetHashCode() : 0);

        uint v1, v2, v3, v4;
        Initialize(out v1, out v2, out v3, out v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 20;

        hash = QueueRound(hash, hc5);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);
        uint hc4 = (uint)(value4 != null ? value4.GetHashCode() : 0);
        uint hc5 = (uint)(value5 != null ? value5.GetHashCode() : 0);
        uint hc6 = (uint)(value6 != null ? value6.GetHashCode() : 0);

        uint v1, v2, v3, v4;
        Initialize(out v1, out v2, out v3, out v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 24;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);
        uint hc4 = (uint)(value4 != null ? value4.GetHashCode() : 0);
        uint hc5 = (uint)(value5 != null ? value5.GetHashCode() : 0);
        uint hc6 = (uint)(value6 != null ? value6.GetHashCode() : 0);
        uint hc7 = (uint)(value7 != null ? value7.GetHashCode() : 0);

        uint v1, v2, v3, v4;
        Initialize(out v1, out v2, out v3, out v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 28;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = QueueRound(hash, hc7);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
    {
        uint hc1 = (uint)(value1 != null ? value1.GetHashCode() : 0);
        uint hc2 = (uint)(value2 != null ? value2.GetHashCode() : 0);
        uint hc3 = (uint)(value3 != null ? value3.GetHashCode() : 0);
        uint hc4 = (uint)(value4 != null ? value4.GetHashCode() : 0);
        uint hc5 = (uint)(value5 != null ? value5.GetHashCode() : 0);
        uint hc6 = (uint)(value6 != null ? value6.GetHashCode() : 0);
        uint hc7 = (uint)(value7 != null ? value7.GetHashCode() : 0);
        uint hc8 = (uint)(value8 != null ? value8.GetHashCode() : 0);

        uint v1, v2, v3, v4;
        Initialize(out v1, out v2, out v3, out v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        v1 = Round(v1, hc5);
        v2 = Round(v2, hc6);
        v3 = Round(v3, hc7);
        v4 = Round(v4, hc8);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 32;

        hash = MixFinal(hash);
        return (int)hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = s_seed + Prime1 + Prime2;
        v2 = s_seed + Prime2;
        v3 = s_seed;
        v4 = s_seed - Prime1;
    }

    // .NET Framework 4.6.2 does not have BitOperations.RotateLeft, so we provide our own.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int offset)
    {
        return (value << offset) | (value >> (32 - offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round(uint hash, uint input)
    {
        return RotateLeft(hash + input * Prime2, 13) * Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QueueRound(uint hash, uint queuedValue)
    {
        return RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixState(uint v1, uint v2, uint v3, uint v4)
    {
        return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
    }

    private static uint MixEmptyState()
    {
        return s_seed + Prime5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    public void Add<T>(T value)
    {
        Add(value != null ? value.GetHashCode() : 0);
    }

    public void Add<T>(T value, IEqualityComparer<T> comparer)
    {
        Add(value == null ? 0 : (comparer != null ? comparer.GetHashCode(value) : value.GetHashCode()));
    }

    /// <summary>Adds a span of bytes to the hash code.</summary>
    /// <param name="value">The byte array.</param>
    public void AddBytes(byte[] value)
    {
        if (value == null)
        {
            return;
        }
        AddBytes(value, 0, value.Length);
    }

    /// <summary>Adds a span of bytes to the hash code.</summary>
    /// <param name="value">The byte array.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <remarks>
    /// This method was converted from using ReadOnlySpan to byte[] for .NET Framework compatibility.
    /// </remarks>
    public unsafe void AddBytes(byte[] value, int offset, int count)
    {
        if (value == null || count == 0)
        {
            return;
        }

        // Use a fixed block to get a pointer to the start of the array segment.
        fixed (byte* pValue = &value[offset])
        {
            byte* pos = pValue;
            byte* end = pos + count;

            if (count < (sizeof(int) * 4))
            {
                goto Small;
            }

            if (_length == 0)
            {
                uint v1, v2, v3, v4;
                Initialize(out v1, out v2, out v3, out v4);
                _v1 = v1; _v2 = v2; _v3 = v3; _v4 = v4;
            }
            else
            {
                switch (_length % 4)
                {
                    case 1:
                        Debug.Assert((end - pos) >= sizeof(int));
                        Add(*(int*)pos);
                        pos += sizeof(int);
                        goto case 2;
                    case 2:
                        Debug.Assert((end - pos) >= sizeof(int));
                        Add(*(int*)pos);
                        pos += sizeof(int);
                        goto case 3;
                    case 3:
                        Debug.Assert((end - pos) >= sizeof(int));
                        Add(*(int*)pos);
                        pos += sizeof(int);
                        break;
                }
            }

            long remaining = end - pos;
            byte* blockEnd = end - (remaining % (sizeof(int) * 4));
            while (pos < blockEnd)
            {
                Debug.Assert((blockEnd - pos) >= (sizeof(int) * 4));
                _v1 = Round(_v1, *(uint*)pos);
                _v2 = Round(_v2, *(uint*)(pos + sizeof(int) * 1));
                _v3 = Round(_v3, *(uint*)(pos + sizeof(int) * 2));
                _v4 = Round(_v4, *(uint*)(pos + sizeof(int) * 3));

                _length += 4;
                pos += sizeof(int) * 4;
            }

            Small:
            while ((end - pos) >= sizeof(int))
            {
                Add(*(int*)pos);
                pos += sizeof(int);
            }

            while (pos < end)
            {
                Add((int)*pos);
                pos++;
            }
        }
    }

    private void Add(int value)
    {
        uint val = (uint)value;
        uint previousLength = _length++;
        uint position = previousLength % 4;

        if (position == 0)
            _queue1 = val;
        else if (position == 1)
            _queue2 = val;
        else if (position == 2)
            _queue3 = val;
        else // position == 3
        {
            if (previousLength == 3)
            {
                uint v1, v2, v3, v4;
                Initialize(out v1, out v2, out v3, out v4);
                _v1 = v1; _v2 = v2; _v3 = v3; _v4 = v4;
            }

            _v1 = Round(_v1, _queue1);
            _v2 = Round(_v2, _queue2);
            _v3 = Round(_v3, _queue3);
            _v4 = Round(_v4, val);
        }
    }

    public int ToHashCode()
    {
        uint length = _length;
        uint position = length % 4;
        uint hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

        hash += length * 4;

        if (position > 0)
        {
            hash = QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, _queue2);
                if (position > 2)
                    hash = QueueRound(hash, _queue3);
            }
        }

        hash = MixFinal(hash);
        return (int)hash;
    }
}
#endif