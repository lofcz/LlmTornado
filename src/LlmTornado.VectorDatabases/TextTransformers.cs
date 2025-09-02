using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LlmTornado.VectorDatabases
{
    public class TextTransformers
    {
        /// <summary>
        /// Splits the input text into chunks of approximately chunkSize characters, preserving line boundaries,
        /// and keeping the last chunkOverlap characters as the start of the next chunk (overlap).
        /// </summary>
        /// <param name="texts">Full text to split (required, not null).</param>
        /// <param name="chunk_size">Target maximum size (in chars) for a chunk (must be > 0).</param>
        /// <param name="chunk_overlap">Number of tail characters from a completed chunk to prepend to the next (0 <= overlap < chunk_size).</param>
        public static List<string> RecursiveCharacterTextSplitter(string texts, int chunk_size=256, int chunk_overlap=64)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (chunk_size <= 0) throw new ArgumentOutOfRangeException(nameof(chunk_size), "chunk_size must be > 0.");
            if (chunk_overlap < 0) throw new ArgumentOutOfRangeException(nameof(chunk_overlap), "chunk_overlap must be >= 0.");
            if (chunk_overlap >= chunk_size) throw new ArgumentException("chunk_overlap must be < chunk_size.", nameof(chunk_overlap));

            // Normalize line endings to avoid Windows \r issues splitting only on '\n'
            var normalized = texts.Replace("\r\n", "\n");
            var rawLines = normalized.Split('\n'); // StringSplitOptions.None to preserve empty lines

            var chunks = new List<string>();
            var sb = new StringBuilder();

            foreach (var line in rawLines)
            {
                sb.Append(line);
                sb.Append('\n');

                if (sb.Length >= chunk_size)
                {
                    // Emit current chunk
                    chunks.Add(sb.ToString());

                    if (chunk_overlap > 0)
                    {
                        // Keep only the overlap tail for the next chunk
                        var current = sb;
                        var keepCount = chunk_overlap;
                        // Safe because chunk_overlap < chunk_size and we only reach here when sb.Length >= chunk_size
                        var start = current.Length - keepCount;
                        var tail = current.ToString(start, keepCount);
                        sb.Clear();
                        sb.Append(tail);
                    }
                    else
                    {
                        sb.Clear();
                    }
                }
            }

            //Add any remaining non-whitespace content
            if (sb.Length > 0 && sb.ToString().Trim().Length > 0)
            {
                chunks.Add(sb.ToString());
            }

            return chunks;
        }
    }
}
