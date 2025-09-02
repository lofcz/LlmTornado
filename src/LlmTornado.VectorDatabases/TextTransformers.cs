using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/***
 def convert_texts_to_DocChunks(texts:List[str], collection_name:str, user:str) -> List[DocChunk]:
    #Add document id to each child document to later retrieve parent
    chunks = []
    for i, content in enumerate(texts): #Add Id to each child document
        chunks.append(DocChunk(str(uuid.uuid4()), content, str({"index": str(i)}), collection_name, user)) #Create a list of all the child documents to later child chunk
    return chunks
    
def convert_texts_to_DBChunks(texts:List[str], index:str, collection_name:str) -> List[DBChunk]:
    #Add document id to each child document to later retrieve parent
    chunks = []
    for i, text in enumerate(texts): #Add Id to each child document
        chunks.append(DBChunk(str(uuid.uuid4()), text, {"index": index, "sub_index":str(i), "source":collection_name})) 
    return chunks
    
def create_parent_chunks(text:List[str], chunk_size:int, chunk_overlap:int, collection_name:str, user:str) -> List[DocChunk]:
    texts = RecursiveCharacterTextSplitter(text, chunk_size, chunk_overlap)
    chunks = convert_texts_to_DocChunks(texts, collection_name, user)
    return chunks

def create_child_chunks(parent_chunk:List[DocChunk], chunk_size:int, chunk_overlap:int, collection_name:str) -> List[DBChunk]:
    chunks = []
    for chunk in parent_chunk:
        sub_texts = RecursiveCharacterTextSplitter(chunk.content, chunk_size, chunk_overlap)
        chunks.extend(convert_texts_to_DBChunks(sub_texts, chunk.id, collection_name))
    return chunks

def RecursiveCharacterTextSplitter(texts:str, chunk_size:int, chunk_overlap:int) -> List[str]:
    raw_chunks = texts.split("\n")
    text_chunks = []
    chunk_text = ""
    for chunk in raw_chunks:
        # Add the current line to the chunk text
        chunk_text += f"{chunk}\n"
        if len(chunk_text) >= chunk_size:
            # If the chunk text reaches or exceeds the chunk size, append it to the list
            text_chunks.append(chunk_text)
            # Then start the next chunk with the overlap from the current chunk
            chunk_text = chunk_text[-chunk_overlap:]
    # After looping through all lines, add any remaining text as the last chunk
    if chunk_text.strip():  # This checks if there's any non-whitespace character left
        text_chunks.append(chunk_text)
    return text_chunks
***/

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
        public List<string> RecursiveCharacterTextSplitter(string texts, int chunk_size=256, int chunk_overlap=64)
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
