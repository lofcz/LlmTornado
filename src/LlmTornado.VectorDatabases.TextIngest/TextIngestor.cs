using UglyToad.PdfPig;

namespace LlmTornado.VectorDatabases.TextIngest;

public class TextIngestor
{
    public List<Document> IngestPdf(string filePath)
    {
        string extractedText = string.Empty;
        List<string> pageText = new List<string>();
        List<Document> documents = new List<Document>();

        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                pageText.Add(string.Join(" ", page.GetWords().Select(w => w.Text)));
            }
        }

        int pageIndex = 1;
        int chunkIndex = 0;

        foreach (var text in pageText)
        {
            List<string> chunks = TextTransformers.RecursiveCharacterTextSplitter(text, 1000, 200);
            foreach (var chunk in chunks)
            {
                documents.Add(new Document(Guid.NewGuid().ToString(), chunk, new Dictionary<string, object>() { { "fileName", Path.GetFileName(filePath) }, { "page", pageIndex }, { "chunk", chunkIndex } }));
                chunkIndex++;
            }
            pageIndex++;
        }

        return documents;
    }
}
