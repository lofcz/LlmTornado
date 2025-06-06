using System.Collections.Frozen;
using System.Collections.Generic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.Google;

internal class VendorGoogleEmbeddingRequest
{
    /// <summary>
    /// Required. Embed requests for the batch. The model in each of these requests must match the model specified BatchEmbedContentsRequest.model.
    /// </summary>
    [JsonProperty("requests")]
    public List<VendorGoogleEmbeddingRequest>? Requests { get; set; }
    
    /// <summary>
    /// Required. The content to embed. Only the parts.text fields will be counted.
    /// </summary>
    [JsonProperty("content")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessage? Content { get; set; }
    
    /// <summary>
    /// TASK_TYPE_UNSPECIFIED	Unset value, which will default to one of the other enum values.
    /// RETRIEVAL_QUERY	Specifies the given text is a query in a search/retrieval setting.
    /// RETRIEVAL_DOCUMENT	Specifies the given text is a document from the corpus being searched.
    /// SEMANTIC_SIMILARITY	Specifies the given text will be used for STS.
    /// CLASSIFICATION	Specifies that the given text will be classified.
    /// CLUSTERING	Specifies that the embeddings will be used for clustering.
    /// QUESTION_ANSWERING	Specifies that the given text will be used for question answering.
    /// FACT_VERIFICATION	Specifies that the given text will be used for fact verification.
    /// </summary>
    [JsonProperty("taskType")]
    public string? TaskType { get; set; }
    
    /// <summary>
    /// Optional. An optional title for the text. Only applicable when TaskType is RETRIEVAL_DOCUMENT.
    /// Note: Specifying a title for RETRIEVAL_DOCUMENT provides better quality embeddings for retrieval.
    /// </summary>
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    /// <summary>
    /// Required. The model's resource name. This serves as an ID for the Model to use. This name should match a model name returned by the models.list method. Format: models/{model}
    /// </summary>
    [JsonProperty("model")]
    public string? Model { get; set; }

    
    /// <summary>
    /// Optional. Optional reduced dimension for the output embedding. If set, excessive values in the output embedding are truncated from the end. Supported by newer models since 2024 only. You cannot set this value if using the earlier model (models/embedding-001).
    /// </summary>
    [JsonProperty("outputDimensionality")]
    public int? OutputDimensionality { get; set; }

    private static readonly FrozenDictionary<EmbeddingRequestVendorGoogleExtensionsTaskTypes, string> TaskTypesMap = new Dictionary<EmbeddingRequestVendorGoogleExtensionsTaskTypes, string>
    {
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.Unspecified, "TASK_TYPE_UNSPECIFIED" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.RetrievalQuery, "RETRIEVAL_QUERY" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.RetrievalDocument, "RETRIEVAL_DOCUMENT" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.SemanticSimilarity, "SEMANTIC_SIMILARITY" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.Classification, "CLASSIFICATION" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.Clustering, "CLUSTERING" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.QuestionAnswering, "QUESTION_ANSWERING" },
        { EmbeddingRequestVendorGoogleExtensionsTaskTypes.FactVerification, "FACT_VERIFICATION" }
    }.ToFrozenDictionary();
    
    static void Setup(EmbeddingRequest request, string input, VendorGoogleEmbeddingRequest dest)
    {
        dest.Model = $"models/{request.Model}";
        dest.Content = new VendorGoogleChatRequest.VendorGoogleChatRequestMessage
        {
            Parts =
            [
                new VendorGoogleChatRequestMessagePart
                {
                    Text = input
                }
            ]
        };

        if (request.VendorExtensions?.Google is not null)
        {
            EmbeddingRequestVendorGoogleExtensions extensions = request.VendorExtensions.Google;

            if (extensions.OutputDimensionality > 0)
            {
                dest.OutputDimensionality = extensions.OutputDimensionality;
            }

            dest.Title = extensions.Title;

            if (extensions.TaskType is not null && TaskTypesMap.TryGetValue(extensions.TaskType.Value, out string? taskTypeSerialized))
            {
                dest.TaskType = taskTypeSerialized;
            }
        }
    }
    
    public VendorGoogleEmbeddingRequest(EmbeddingRequest request, string input)
    {
        Setup(request, input, this);
    }

    public VendorGoogleEmbeddingRequest(EmbeddingRequest request, IEndpointProvider provider)
    {
        if (request.InputVector?.Count > 0)
        {
            request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.Embeddings, null)}/{request.Model.Name}:batchEmbedContents");

            Requests = [];

            foreach (string rqs in request.InputVector)
            {
                Requests.Add(new VendorGoogleEmbeddingRequest(request, rqs));
            }
        }
        else if (request.InputScalar is not null)
        {
            request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.Embeddings, null)}/{request.Model.Name}:embedContent");
            Setup(request, request.InputScalar, this);
        }
    }
}