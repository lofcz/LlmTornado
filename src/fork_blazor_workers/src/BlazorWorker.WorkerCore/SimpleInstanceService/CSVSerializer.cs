using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
{
    public class CSVSerializer
    {
        public const char EscapeChar = '\\';
        public const char Separator = '|';

        public static string EscapeString(string s)
        {
            return s?.Replace(EscapeChar, EscapeChar)
                .Replace(Separator.ToString(), new string(new[] { EscapeChar, Separator }));
        }

        public static string Serialize(string prefix, params object[] fields)
        {
            return string.Join(Separator.ToString(), new[] { prefix }.Concat(fields));
        }

        public static void Deserialize(string prefix, string message, Queue<Action<string>> fieldParserQueue)
        {
            if (!message.StartsWith(prefix))
            {
                throw new FormatException($"Unexpected start of message, expected {prefix}");
            }
            var body = message.Substring(prefix.Length+1);
            var sb = new StringBuilder(body.Length);
            var lastChar = ' ';
            var pos = -1;

            void nextParser() {
                var fieldValue = sb.ToString();
                try
                {
                    fieldParserQueue.Dequeue()(fieldValue);
                }
                catch (Exception e)
                {
                    throw new FormatException($"Error when parsing field value '{fieldValue}' message prefixed {prefix}. body '{body}' buffer left '{body.Substring(pos)}", e);
                }
            }

            foreach (var chr in body)
            {
                pos++;
                if (lastChar == EscapeChar && chr == EscapeChar)
                {
                    continue;
                }
                else if (lastChar != EscapeChar && chr == Separator)
                {
                    nextParser();

                    if (fieldParserQueue.Count == 0)
                    {
                        return;
                    }

                    sb.Clear();
                }
                else
                {
                    sb.Append(chr);
                    lastChar = chr;
                }
                
            }

            if(fieldParserQueue.Count > 1)
            {
                throw new FormatException($"Unexpected end of message prefixed {prefix}");
            }

            nextParser();
        }
    }
}
