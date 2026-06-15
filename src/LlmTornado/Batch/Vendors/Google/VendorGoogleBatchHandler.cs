using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Files;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Batch.Vendors.Google;

/// <summary>
/// Handles all batch operations for Google/Gemini.
/// </summary>
internal static class VendorGoogleBatchHandler
{
    public static async Task<HttpCallResult<BatchItem>> Create(BatchRequest request, IEndpointProvider provider, EndpointBase endpoint, CancellationToken cancellationToken)
    {
        TornadoApi api = provider.Api!;
        string? model = request.Requests.FirstOrDefault()?.Params.Model?.Name;
        if (string.IsNullOrEmpty(model))
        {
            return new HttpCallResult<BatchItem>(System.Net.HttpStatusCode.BadRequest, null, null, false, null)
            {
                Exception = new ArgumentException("Model must be specified in batch request items for Google/Gemini")
            };
        }

        byte[] jsonlBytes = VendorGoogleBatchRequest.SerializeToJsonlBytes(request, provider);

        HttpCallResult<TornadoFile> uploadResult = await api.Files.Upload(new FileUploadRequest
        {
            Bytes = jsonlBytes,
            Name = $"batch_{Guid.NewGuid():N}.jsonl",
            MimeType = "application/jsonl"
        }, provider.Provider).ConfigureAwait(false);

        if (!uploadResult.Ok || string.IsNullOrEmpty(uploadResult.Data?.Id))
        {
            return new HttpCallResult<BatchItem>(
                uploadResult.Code,
                uploadResult.Response,
                null,
                false,
                uploadResult.Request
            )
            {
                Exception = uploadResult.Exception ?? new Exception("Failed to upload Gemini batch input file")
            };
        }

        VendorGoogleBatchRequest googleRequest = new VendorGoogleBatchRequest(request, uploadResult.Data.Id);
        string body = googleRequest.Serialize();

        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, $"models/{model}:batchGenerateContent");

        return await endpoint.HttpPost<BatchItem>(provider, CapabilityEndpoints.Batch, url, body, ct: cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<HttpCallResult<BatchItem>> Get(string batchId, IEndpointProvider provider, EndpointBase endpoint, CancellationToken cancellationToken)
    {
        string batchPath = batchId.StartsWith("batches/") ? batchId : $"batches/{batchId}";
        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, batchPath);
        
        return await endpoint.HttpGet<BatchItem>(provider, CapabilityEndpoints.Batch, url, ct: cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<HttpCallResult<ListResponse<BatchItem>>> List(IEndpointProvider provider, EndpointBase endpoint, ListQuery? query, CancellationToken cancellationToken)
    {
        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, "batches");
        
        HttpCallResult<VendorGoogleBatchListResponse> response = await endpoint.HttpGet<VendorGoogleBatchListResponse>(provider, CapabilityEndpoints.Batch, url, query?.ToQueryParams(provider.Provider), ct: cancellationToken).ConfigureAwait(false);
        
        if (!response.Ok || response.Data is null)
        {
            return new HttpCallResult<ListResponse<BatchItem>>(response.Code, response.Response, null, false, response.Request)
            {
                Exception = response.Exception
            };
        }
        
        List<BatchItem> items = [];
        
        if (response.Data.Operations is not null)
        {
            foreach (JToken operation in response.Data.Operations)
            {
                BatchItem? item = VendorGoogleBatchItem.Deserialize(operation.ToString(Formatting.None));
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }
        
        ListResponse<BatchItem> result = new ListResponse<BatchItem>(items, !string.IsNullOrEmpty(response.Data.NextPageToken), nextPageToken: response.Data.NextPageToken);
        
        return new HttpCallResult<ListResponse<BatchItem>>(response.Code, response.Response, result, true, response.Request);
    }
    
    public static async Task<HttpCallResult<BatchItem>> Cancel(string batchId, IEndpointProvider provider, EndpointBase endpoint, CancellationToken cancellationToken)
    {
        string batchPath = batchId.StartsWith("batches/") ? batchId : $"batches/{batchId}";
        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, $"{batchPath}:cancel");
        
        return await endpoint.HttpPost<BatchItem>(provider, CapabilityEndpoints.Batch, url, ct: cancellationToken).ConfigureAwait(false);
    }
    
    public static async Task<HttpCallResult<bool>> Delete(string batchId, IEndpointProvider provider, EndpointBase endpoint, CancellationToken cancellationToken)
    {
        string batchPath = batchId.StartsWith("batches/") ? batchId : $"batches/{batchId}";
        string url = provider.ApiUrl(CapabilityEndpoints.BaseUrl, batchPath);

        HttpCallResult<object> response = await endpoint.HttpDelete<object>(provider, CapabilityEndpoints.Batch, url, ct: cancellationToken).ConfigureAwait(false);
        
        return new HttpCallResult<bool>(response.Code, response.Response, response.Ok, response.Ok, response.Request)
        {
            Exception = response.Exception
        };
    }
    
    public static async IAsyncEnumerable<BatchResult> StreamResults(BatchItem batch, IEndpointProvider provider, EndpointBase endpoint, Func<string, LLmProviders?, CancellationToken, Task<HttpCallResult<BatchItem>>> getBatch, CancellationToken cancellationToken)
    {
        BatchItem workingBatch = batch;
        
        // If no output data, fetch full batch details first (List doesn't return output)
        if (batch.GoogleInlinedResponses is null && string.IsNullOrEmpty(batch.OutputFileId) && !string.IsNullOrEmpty(batch.Id))
        {
            HttpCallResult<BatchItem> fullBatch = await getBatch(batch.Id, provider.Provider, cancellationToken).ConfigureAwait(false);
            if (fullBatch.Data is not null)
            {
                workingBatch = fullBatch.Data;
            }
        }
        
        // Check for inlined responses
        if (workingBatch.GoogleInlinedResponses is not null)
        {
            foreach (JToken inlinedResponse in workingBatch.GoogleInlinedResponses)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                
                BatchResult? result = VendorGoogleBatchResult.DeserializeInlined(inlinedResponse);
                if (result is not null)
                {
                    yield return result;
                }
            }
            yield break;
        }
        
        // If no inlined responses, try to download from output file
        if (!string.IsNullOrEmpty(workingBatch.OutputFileId))
        {
            string fileUrl = provider.ApiUrl(CapabilityEndpoints.BaseUrlStripped, $"download/v1beta/{workingBatch.OutputFileId}:download?alt=media");
            
            StreamResponse? response = await endpoint.HttpGetStream(provider, CapabilityEndpoints.Batch, fileUrl, ct: cancellationToken).ConfigureAwait(false);
            
            if (response?.Stream is null)
            {
                yield break;
            }

            try
            {
                using StreamReader reader = new StreamReader(response.Stream);
                
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
                    
                    BatchResult? result = VendorGoogleBatchResult.Deserialize(line);
                    if (result is not null)
                    {
                        yield return result;
                    }
                }
            }
            finally
            {
                response.Response?.Dispose();
            }
        }
    }
}
