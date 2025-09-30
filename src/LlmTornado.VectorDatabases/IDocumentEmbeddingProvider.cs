using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

public interface IDocumentEmbeddingProvider
{
    Task<float[]> Invoke(string content);
    Task<float[][]> Invoke(string[] contents);
}