using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace LlmTornado.Uploads;

/// <summary>
///     Endpoint for interacting with the /uploads API.
/// </summary>
public class UploadsEndpoint : EndpointBase
{
    internal UploadsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Uploads;

    // Default values live in UploadOptions

    /// <summary>
    ///     Creates a new upload.
    /// </summary>
    public async Task<Upload> CreateUpload(CreateUploadRequest request, CancellationToken token = default)
    {
        HttpCallResult<Upload> result = await CreateUploadSafe(request, token).ConfigureAwait(false);
        if (!result.Ok)
        {
            throw result.Exception;
        }
        return result.Data!;
    }

    /// <summary>
    ///     Creates a new upload.
    /// </summary>
    public Task<HttpCallResult<Upload>> CreateUploadSafe(CreateUploadRequest request, CancellationToken token = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpPost<Upload>(provider, Endpoint, postData: request, ct: token);
    }

    /// <summary>
    ///     Uploads a file by creating an upload, chunking the data, uploading all parts and completing the upload.
    /// </summary>
    public async Task<Upload> CreateUploadAutoChunk(CreateUploadRequest request, byte[] data, UploadOptions? options = null, CancellationToken token = default)
    {
        HttpCallResult<Upload> res = await CreateUploadAutomaticSafe(request, data, options, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception ?? new Exception("Upload failed");
        }
        return res.Data!;
    }

    /// <summary>
    ///     Uploads a file by creating an upload, chunking the data, uploading all parts and completing the upload.
    /// </summary>
    public Task<HttpCallResult<Upload>> CreateUploadAutomaticSafe(CreateUploadRequest request, byte[] data, UploadOptions? options = null, CancellationToken token = default)
    {
        return CreateUploadAutomaticSafeInternal(request, data, null, options ?? new UploadOptions(), token);
    }

    /// <summary>
    ///     Uploads a file by creating an upload, chunking the data from a stream, uploading all parts and completing the upload.
    /// </summary>
    public async Task<Upload> CreateUploadAutoChunk(CreateUploadRequest request, Stream data, UploadOptions? options = null, CancellationToken token = default)
    {
        HttpCallResult<Upload> res = await CreateUploadAutomaticSafe(request, data, options, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception ?? new Exception("Upload failed");
        }
        return res.Data!;
    }

    /// <summary>
    ///     Uploads a file by creating an upload, chunking the data from a stream, uploading all parts and completing the upload.
    /// </summary>
    public Task<HttpCallResult<Upload>> CreateUploadAutomaticSafe(CreateUploadRequest request, Stream data, UploadOptions? options = null, CancellationToken token = default)
    {
        return CreateUploadAutomaticSafeInternal(request, null, data, options ?? new UploadOptions(), token);
    }

    /// <summary>
    ///     Adds a part to an existing upload. The data can be no longer than 64 MB.
    /// </summary>
    public async Task<UploadPart> AddUploadPart(string uploadId, byte[] data, CancellationToken token = default)
    {
        HttpCallResult<UploadPart> res = await AddUploadPartSafe(uploadId, data, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception;
        }
        return res.Data!;
    }

    /// <summary>
    ///     Adds a part to an existing upload. The data can be no longer than 64 MB.
    /// </summary>
    public Task<HttpCallResult<UploadPart>> AddUploadPartSafe(string uploadId, byte[] data, CancellationToken token = default)
    {
        ByteArrayContent bc = new ByteArrayContent(data);
        return AddUploadPartInternal(uploadId, bc, token);
    }

    /// <summary>
    ///     Adds a part to an existing upload using a stream. The data can be no longer than 64 MB.
    /// </summary>
    public async Task<UploadPart> AddUploadPart(string uploadId, Stream data, CancellationToken token = default)
    {
        HttpCallResult<UploadPart> res = await AddUploadPartSafe(uploadId, data, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception;
        }
        return res.Data!;
    }

    /// <summary>
    ///     Adds a part to an existing upload using a stream. The data can be no longer than 64 MB.
    /// </summary>
    public Task<HttpCallResult<UploadPart>> AddUploadPartSafe(string uploadId, Stream data, CancellationToken token = default)
    {
        StreamContent sc = new StreamContent(data);
        return AddUploadPartInternal(uploadId, sc, token);
    }

    /// <summary>
    ///     Completes an upload.
    /// </summary>
    public async Task<Upload> CompleteUpload(string uploadId, CompleteUploadRequest request, CancellationToken token = default)
    {
        HttpCallResult<Upload> res = await CompleteUploadSafe(uploadId, request, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception;
        }
        return res.Data!;
    }

    /// <summary>
    ///     Completes an upload.
    /// </summary>
    public Task<HttpCallResult<Upload>> CompleteUploadSafe(string uploadId, CompleteUploadRequest request, CancellationToken token = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        string url = GetUrl(provider, $"/{uploadId}/complete");
        return HttpPost<Upload>(provider, Endpoint, url: url, postData: request, ct: token);
    }

    /// <summary>
    ///     Cancels an upload.
    /// </summary>
    public async Task<Upload> CancelUpload(string uploadId, CancellationToken token = default)
    {
        HttpCallResult<Upload> res = await CancelUploadSafe(uploadId, token).ConfigureAwait(false);
        if (!res.Ok)
        {
            throw res.Exception;
        }
        return res.Data!;
    }
    
    /// <summary>
    ///     Cancels an upload.
    /// </summary>
    public Task<HttpCallResult<Upload>> CancelUploadSafe(string uploadId, CancellationToken token = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        string url = GetUrl(provider, $"/{uploadId}/cancel");
        return HttpPost<Upload>(provider, Endpoint, url: url, postData: null, ct: token);
    }

    private Task<HttpCallResult<UploadPart>> AddUploadPartInternal(string uploadId, HttpContent contentPart, CancellationToken token)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(contentPart, "data", "chunk");
        string url = GetUrl(provider, $"/{uploadId}/parts");
        return HttpPost<UploadPart>(provider, Endpoint, url: url, postData: content, ct: token);
    }

    private async Task<HttpCallResult<Upload>> CreateUploadAutomaticSafeInternal(CreateUploadRequest request, byte[]? dataBytes, Stream? dataStream, UploadOptions options, CancellationToken token)
    {
        long totalSize;
        
        if (dataBytes is not null)
        {
            totalSize = dataBytes.LongLength;
        }
        else if (dataStream is not null && dataStream.CanSeek)
        {
            totalSize = dataStream.Length;
        }
        else
        {
            totalSize = request.Bytes;
        }

        request.Bytes = totalSize;

        HttpCallResult<Upload> createRes = await CreateUploadSafe(request, token).ConfigureAwait(false);
        if (!createRes.Ok)
        {
            return createRes;
        }

        string uploadId = createRes.Data!.Id;
        DateTime startTime = DateTime.UtcNow;

        // Prepare chunks
        if (dataBytes is not null)
        {
            return await UploadChunksFromBytes(uploadId, dataBytes, options, startTime, totalSize, token).ConfigureAwait(false);
        }

        if (dataStream is not null)
        {
            return await UploadChunksFromStream(uploadId, dataStream, totalSize, options, startTime, token).ConfigureAwait(false);
        }

        Exception ex = new ArgumentException("Either dataBytes or dataStream must be provided");
        return new HttpCallResult<Upload>(HttpStatusCode.BadRequest, null, null, false, new RestDataOrException<HttpResponseData>(ex))
        {
            Exception = ex
        };
    }

    private async Task<HttpCallResult<Upload>> UploadChunksFromBytes(string uploadId, byte[] data, UploadOptions options, DateTime startTime, long totalSize, CancellationToken token)
    {
        int chunkSize = options.ChunkSize;
        int totalChunks = (int)Math.Ceiling((double)data.Length / chunkSize);
        ConcurrentDictionary<int, string> partIdMap = new ConcurrentDictionary<int, string>();
        int completed = 0;

        // Report initial progress (0%)
        options.Progress?.Report(new UploadProgress
        {
            Progress = 0.0,
            TotalFileSize = totalSize,
            ElapsedTime = TimeSpan.Zero,
            TotalChunkCount = totalChunks,
            CompletedChunks = 0
        });

#if MODERN
        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.DegreeOfParallelism,
            CancellationToken = token
        };

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, totalChunks), parallelOptions, async (index, ct) =>
            {
                int start = index * chunkSize;
                int length = Math.Min(chunkSize, data.Length - start);
                byte[] chunk = new byte[length];
                Buffer.BlockCopy(data, start, chunk, 0, length);

                int attempts = 0;
                while (true)
                {
                    HttpCallResult<UploadPart> partRes = await AddUploadPartSafe(uploadId, chunk, ct).ConfigureAwait(false);
                    if (partRes.Ok)
                    {
                        partIdMap[index] = partRes.Data!.Id;
                        break;
                    }

                    attempts++;

                    if (attempts >= 3)
                    {
                        return;
                    }
                }

                int done = Interlocked.Increment(ref completed);
                options.Progress?.Report(new UploadProgress
                {
                    Progress = (double)done / totalChunks,
                    TotalFileSize = totalSize,
                    ElapsedTime = DateTime.UtcNow - startTime,
                    TotalChunkCount = totalChunks,
                    CompletedChunks = done
                });
            });
        }
        catch (Exception e)
        {
            return new HttpCallResult<Upload>(HttpStatusCode.ServiceUnavailable, null, null, false, new RestDataOrException<HttpResponseData>(e))
            {
                Exception = e
            };
        }
#else
        try
        {
            for (int index = 0; index < totalChunks; index++)
            {
                int start = index * chunkSize;
                int length = Math.Min(chunkSize, data.Length - start);
                byte[] chunk = new byte[length];
                Buffer.BlockCopy(data, start, chunk, 0, length);

                int attempts = 0;
                while (true)
                {
                    HttpCallResult<UploadPart> partRes = await AddUploadPartSafe(uploadId, chunk, token).ConfigureAwait(false);
                    if (partRes.Ok)
                    {
                        partIdMap[index] = partRes.Data!.Id;
                        break;
                    }

                    attempts++;
                    
                    if (attempts >= 3)
                    {
                        Exception ex = new Exception($"Failed to upload part {index} after 3 attempts: {partRes.Exception?.Message ?? "Unknown error"}");
                        return new HttpCallResult<Upload>(HttpStatusCode.ServiceUnavailable, null, null, false, new RestDataOrException<HttpResponseData>(ex))
                        {
                            Exception = ex
                        };
                    }
                }

                int done = Interlocked.Increment(ref completed);
                options.Progress?.Report(new UploadProgress
                {
                    Progress = (double)done / totalChunks,
                    TotalFileSize = totalSize,
                    ElapsedTime = DateTime.UtcNow - startTime,
                    TotalChunkCount = totalChunks,
                    CompletedChunks = done
                });
            }
        }
        catch (Exception e)
        {
            return new HttpCallResult<Upload>(HttpStatusCode.ServiceUnavailable, null, null, false, new RestDataOrException<HttpResponseData>(e))
            {
                Exception = e
            };
        }
#endif

        // Verify all parts uploaded
        if (partIdMap.Count != totalChunks)
        {
            Exception ex = new Exception("Failed to upload all parts");
            return new HttpCallResult<Upload>(HttpStatusCode.ServiceUnavailable, null, null, false, new RestDataOrException<HttpResponseData>(ex))
            {
                Exception = ex
            };
        }

        List<string> orderedPartIds = Enumerable.Range(0, totalChunks).Select(i => partIdMap[i]).ToList();
        HttpCallResult<Upload> completeRes = await CompleteUploadSafe(uploadId, new CompleteUploadRequest { PartIds = orderedPartIds }, token).ConfigureAwait(false);
        if (completeRes.Ok)
        {
            options.Progress?.Report(new UploadProgress
            {
                Progress = 1.0,
                TotalFileSize = totalSize,
                ElapsedTime = DateTime.UtcNow - startTime,
                TotalChunkCount = totalChunks,
                CompletedChunks = totalChunks
            });
        }
        return completeRes;
    }

    private async Task<HttpCallResult<Upload>> UploadChunksFromStream(string uploadId, Stream stream, long totalSize, UploadOptions options, DateTime startTime, CancellationToken token)
    {
        List<string> partIds = [];
        byte[] buffer = new byte[options.ChunkSize];
        long uploaded = 0;
        int totalChunks = (int)Math.Ceiling((double)totalSize / options.ChunkSize);

        // Report initial progress (0%)
        options.Progress?.Report(new UploadProgress
        {
            Progress = 0.0,
            TotalFileSize = totalSize,
            ElapsedTime = TimeSpan.Zero,
            TotalChunkCount = totalChunks,
            CompletedChunks = 0
        });
        
        while (true)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, options.ChunkSize), token).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }
            byte[] chunkData = new byte[read];
            Buffer.BlockCopy(buffer, 0, chunkData, 0, read);

            int attempts = 0;
            while (true)
            {
                HttpCallResult<UploadPart> partRes = await AddUploadPartSafe(uploadId, chunkData, token).ConfigureAwait(false);
                if (partRes.Ok)
                {
                    partIds.Add(partRes.Data!.Id);
                    break;
                }

                attempts++;
                if (attempts >= 3)
                {
                    return new HttpCallResult<Upload>(partRes.Code, partRes.Response, null, false, partRes.Request)
                    {
                        Exception = partRes.Exception
                    };
                }
            }

            uploaded += read;
            if (totalSize > 0)
            {
                options.Progress?.Report(new UploadProgress
                {
                    Progress = (double)uploaded / totalSize,
                    TotalFileSize = totalSize,
                    ElapsedTime = DateTime.UtcNow - startTime,
                    TotalChunkCount = totalChunks,
                    CompletedChunks = partIds.Count
                });
            }
        }

        HttpCallResult<Upload> completeRes = await CompleteUploadSafe(uploadId, new CompleteUploadRequest { PartIds = partIds }, token).ConfigureAwait(false);
        if (completeRes.Ok)
        {
            options.Progress?.Report(new UploadProgress
            {
                Progress = 1.0,
                TotalFileSize = totalSize,
                ElapsedTime = DateTime.UtcNow - startTime,
                TotalChunkCount = partIds.Count,
                CompletedChunks = partIds.Count
            });
        }
        return completeRes;
    }
} 