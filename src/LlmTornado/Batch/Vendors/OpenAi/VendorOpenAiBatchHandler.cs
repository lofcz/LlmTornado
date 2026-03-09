using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Files;

namespace LlmTornado.Batch.Vendors.OpenAi;

/// <summary>
/// Handles all batch operations for OpenAI.
/// </summary>
internal static class VendorOpenAiBatchHandler
{
    public static async Task<HttpCallResult<BatchItem>> Create(BatchRequest request, IEndpointProvider provider, EndpointBase endpoint, CancellationToken cancellationToken)
    {
        TornadoApi api = provider.Api!;
        
        byte[] jsonlBytes = VendorOpenAiBatchRequest.SerializeToJsonlBytes(request, provider);
        
        HttpCallResult<TornadoFile> uploadResult = await api.Files.Upload(
            jsonlBytes, 
            $"batch_{Guid.NewGuid():N}.jsonl",
            FilePurpose.Batch,
            "application/jsonl",
            provider: provider.Provider
        ).ConfigureAwait(false);
        
        if (!uploadResult.Ok || uploadResult.Data?.Id is null)
        {
            return new HttpCallResult<BatchItem>(
                uploadResult.Code, 
                uploadResult.Response, 
                null, 
                false, 
                uploadResult.Request
            )
            {
                Exception = uploadResult.Exception ?? new Exception("Failed to upload batch file")
            };
        }
        
        // Determine the endpoint based on the batch request items
        // All items in a batch must target the same endpoint
        string batchEndpoint = "/v1/chat/completions";
        if (request.Requests.Count > 0)
        {
            batchEndpoint = request.Requests[0].Endpoint switch
            {
                BatchRequestEndpoint.ImageGenerations => "/v1/images/generations",
                BatchRequestEndpoint.ImageEdits => "/v1/images/edits",
                _ => "/v1/chat/completions"
            };
        }
        
        VendorOpenAiBatchCreateRequest createRequest = new VendorOpenAiBatchCreateRequest
        {
            InputFileId = uploadResult.Data.Id,
            Endpoint = batchEndpoint,
            CompletionWindow = request.CompletionWindow.Value,
            Metadata = request.VendorExtensions?.OpenAi?.Metadata
        };
        
        return await endpoint.HttpPost<BatchItem>(provider, CapabilityEndpoints.Batch, postData: createRequest, ct: cancellationToken).ConfigureAwait(false);
    }
    
    public static async IAsyncEnumerable<BatchResult> StreamResults(BatchItem batch, IEndpointProvider provider, CancellationToken cancellationToken)
    {
        if (batch.OutputFileId is null)
        {
            yield break;
        }
        
        TornadoApi api = provider.Api!;
        string content = await api.Files.GetContent(batch.OutputFileId, provider.Provider).ConfigureAwait(false);
        
        using StringReader reader = new StringReader(content);
        
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            
            BatchResult? result = VendorOpenAiBatchResult.Deserialize(line);
            if (result is not null)
            {
                yield return result;
            }
        }
    }
}
