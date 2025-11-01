using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

public interface IDocumentRetriever
{
    Task<IEnumerable<Document>> SearchAsync(float[] queryEmbedding, TornadoWhereOperator? where = null, int topK = 5, bool includeSource = false);
    IEnumerable<Document> Search(float[] queryEmbedding, TornadoWhereOperator? where = null, int topK = 5, bool includeSource = false);
}
