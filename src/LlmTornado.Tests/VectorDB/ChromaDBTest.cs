using LlmTornado.Demo;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.ChromaDB.Client;
using LlmTornado.VectorDatabases.Intergrations;
using LlmTornado.VectorDatabases.PgVector.Integrations;
using LlmTornado.VectorDatabases.TextIngest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Tests.VectorDB;

public class ChromaDBTest
{
    public const string HostUri = "http://localhost:8008/api/v2/";

    [Test]
    public async Task DeserializeDataSet()
    {
        var json = File.ReadAllText("Static/Files/golden_data.json");
        var faissData = JsonConvert.DeserializeObject<VectorDbCollection>(json);
        Console.WriteLine($"NextId: {faissData.NextId}");
        Console.WriteLine($"Entries count: {faissData.Entries.Length}");
        foreach (var entry in faissData.Entries)
        {
            Console.WriteLine($"Id: {entry.Id}");
            Console.WriteLine($"Document: {entry.Document.Substring(0, Math.Min(100, entry.Document.Length))}...");
            Console.WriteLine($"Metadata: {JsonConvert.SerializeObject(entry.Metadata)}");
            Console.WriteLine($"Embedding Length: {entry.Embedding.Length}");
            Console.WriteLine($"Distance: {entry.Distance}");
            Console.WriteLine("-----");
        }
    }


    [Test]
    [Flaky("Requires local ChromaDB instance running")]
    public async Task UploadGoldenDataToChroma()
    {
        TornadoChromaDB vectordb = new TornadoChromaDB(HostUri);

        // Initialize a collection
        await vectordb.InitializeCollection("golden_data");

        List<VectorDocument> vectorDocuments = new List<VectorDocument>();

        var json = File.ReadAllText("Static/Files/golden_data.json");
        var faissData = JsonConvert.DeserializeObject<VectorDbCollection>(json);

        foreach (var entry in faissData.Entries)
        {
            vectorDocuments.Add(new VectorDocument(entry.Id, entry.Document)
            {
                Embedding = entry.Embedding,
                Metadata = entry.Metadata
            });
        }

        await vectordb.AddDocumentsAsync(vectorDocuments.ToArray());
    }

    [Test]
    public async Task QueryChromaDb()
    {
        TornadoChromaDB vectordb = new TornadoChromaDB(HostUri);
        await vectordb.InitializeCollection("golden_data");
        string queryText = "Table 3: Comparison of AttendOut, vanilla Dropout and LayerDrop. Model CoLA QNLI MNLI-mm RoBERTa 62.5 92.0 86.6 \u002B Vanilla 61.3 92.2 86.9 \u002B AttendOut 63.8 93.1 87.8 \u002B LayerDrop 62.1 92.6 87.1 \u002B Attn.LayerDrop 64.2 92.7 87.3 Table 4: Comparison of AttendOut and scheduled Bernoulli dropout. Model CoLA QNLI SWAG RoBERTa 62.5 92.0 83.8 \u002B Scheduler 63.3 92.6 83.6 \u002B AttendOut 63.8 93.1 84.1 LayerDrop We also compare with LayerDrop [29], which focuses on skipping the entire encoder blocks, Inspired of it, we design another strategy which randomly skips attention layers via Eq. 4. For fair enough comparison, we set the dropout probabilities to 0.2 for both methods, following the settings in [29]. Intuitively in Table 3, vanilla Dropout with \uFB01xed probability does not produce noticeable gain (1.9% bellow RoBERTa on CoLA). However, AttendOut shows powerful advantage (4.1%, 1.0% and 1.0% over vanilla Dropout on CoLA, QNLI and MNLI), which stresses the necessity of dynamic dropout patterns rather than \uFB01xed static one. On the other hand, both layer-level regularizers are effective, while attention LayerDrop performs stronger and more stable on all the three. Especially on CoLA, it outperforms RoBERTa by 1.7 points, while LayerDrop meets a performance drop, which demonstrates that removing the attention layers act as a more effective regularizer than removing the entire SAN block as for self-attention based models. 7.2 Pattern Approximation Guided by AttendOut, we design a dropout scheduler, in which we utilize piece-wise linearity to approximate the real curves as depicted in Figure 3. Taking QNLI as an example, we initialize the dropout probabilities to 0.6 for all attention layers and set a a speci\uFB01c slope for each of them. Note that here the corresponding mask matrices are randomly-generated and subject to Bernoulli distribution. In AttendOut, however, the distribution are learned dynamically through self-attention of G-Net. As shown in Table 4, RoBERTa with scheduled Bernoulli dropout works surprisingly well on both CoLA and QNLI, which outperforms RoBERTa by 0.8 and 0.6 points respectively, closer to AttendOut, even if the strategy here is random-based and much looser. The guided scheduled dropout helps unfold the correctness of the dynamic dropout patterns learned by AttendOut as well as the self-attention based dropout maker. 8 Conclusion This paper focuses on the co-adaption problem of deep self-attention networks, and presents a novel dropout method onto self-attention empowered pre-trained language models. Extensive experiments on multiple natural language processing tasks demonstrate that our proposed approach is universal and quali\uFB01ed to enable more robust task-speci\uFB01c tuning, which contributes to much stronger state-of- the-arts. We probe into the learned dropout patterns on different tasks, which empirically guide us to the very needed dynamic attention dropout design. 8\n";
        TornadoApi api = new TornadoApi(Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        var embedding = await api.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, queryText);
        var results = await vectordb.QueryByEmbeddingAsync(embedding.Data.First().Embedding, topK: 1);
        foreach (var result in results)
        {
            Console.WriteLine($"Id: {result.Id}");
            Console.WriteLine($"Document: {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            Console.WriteLine($"Metadata: {JsonConvert.SerializeObject(result.Metadata)}");
            Console.WriteLine("-----");
            Assert.That(result.Id.Equals("3d238da5-ee32-4397-941d-0bc904e760d4"));
        }
    }

    [Test]
    public async Task GetChromaDoc()
    {
        TornadoChromaDB vectordb = new TornadoChromaDB(HostUri);
        await vectordb.InitializeCollection("golden_data");
        var docs = await vectordb.GetDocumentsAsync(new[] { "3d238da5-ee32-4397-941d-0bc904e760d4" });
        foreach (var doc in docs)
        {
            Console.WriteLine($"Id: {doc.Id}");
            Console.WriteLine($"Document: {doc.Content.Substring(0, Math.Min(100, doc.Content.Length))}...");
            Console.WriteLine($"Metadata: {JsonConvert.SerializeObject(doc.Metadata)}");
            Assert.That(doc.Id.Equals("3d238da5-ee32-4397-941d-0bc904e760d4"));
        }
    }

    [Test]
    public async Task ChromaWhereTest()
    {
        TornadoChromaDB vectordb = new TornadoChromaDB(HostUri);
        await vectordb.InitializeCollection("golden_data");
        var docs = await vectordb.GetDocumentWhereAsync(TornadoWhereOperator.Equal("page", 8));
        foreach (var doc in docs)
        {
            Console.WriteLine($"Id: {doc.Id}");
            Console.WriteLine($"Document: {doc.Content.Substring(0, Math.Min(100, doc.Content.Length))}...");
            Console.WriteLine($"Metadata: {JsonConvert.SerializeObject(doc.Metadata)}");
        }
    }
}
