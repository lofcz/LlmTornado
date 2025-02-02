using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Files.Vendors;
using Newtonsoft.Json;

namespace LlmTornado.Files;

/// <summary>
///     The API endpoint for operations List, Upload, Delete, Retrieve files.
/// </summary>
public class FilesEndpoint : EndpointBase
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Files" />.
	/// </summary>
	/// <param name="api"></param>
	internal FilesEndpoint(TornadoApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "files".
	/// </summary>
	protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Files;
	
	/// <summary>
	///     Get the list of all files.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="HttpRequestException"></exception>
	public async Task<TornadoPagingList<TornadoFile>?> GetFilesAsync(ListQuery? query = null, LLmProviders? provider = null, CancellationToken token = default)
	{
		IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);

		if (provider is LLmProviders.Google)
		{
			VendorGoogleTornadoFilesList? ilResult = await HttpGet<VendorGoogleTornadoFilesList>(resolvedProvider, Endpoint, queryParams: query?.ToQueryParams(resolvedProvider), ct: token).ConfigureAwait(ConfigureAwaitOptions.None);
			
			return new TornadoPagingList<TornadoFile>
			{
				Items = ilResult?.Files.Select(x => x.ToFile(null)).ToList() ?? [],
				PageToken = ilResult?.PageToken
			};
		}
		
		return resolvedProvider.Provider switch
		{
			LLmProviders.OpenAi => new TornadoPagingList<TornadoFile>
			{
				Items = (await HttpGet<TornadoFiles>(resolvedProvider, Endpoint, queryParams: query?.ToQueryParams(resolvedProvider), ct: token).ConfigureAwait(ConfigureAwaitOptions.None))?.Data ?? []
			},
			_ => null
		};
	}

	/// <summary>
	///     Returns information about a specific file
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <param name="provider">Which provider should be used</param>
	/// <returns></returns>
	public async Task<TornadoFile?> GetFileAsync(string fileId, LLmProviders? provider = null)
	{
		IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);

		// sadly, when creating the file there is an extra wrapper which is not in place when retrieving it, so we have to do this
		if (resolvedProvider.Provider is LLmProviders.Google)
		{
			VendorGoogleTornadoFileContent? result = await HttpGet<VendorGoogleTornadoFileContent>(resolvedProvider, CapabilityEndpoints.BaseUrl, GetUrl(resolvedProvider, CapabilityEndpoints.BaseUrl, fileId)).ConfigureAwait(ConfigureAwaitOptions.None);;

			if (result is not null)
			{
				return result.ToFile(null);
			}
		}
	    
        return await HttpGet<TornadoFile>(resolvedProvider, Endpoint, GetUrl(resolvedProvider, $"/{fileId}")).ConfigureAwait(ConfigureAwaitOptions.None);
    }

	/// <summary>
	///     Returns the contents of the specific file as string. Supported only by OpenAi.
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <returns></returns>
	public Task<string> GetFileContentAsStringAsync(string fileId)
    {
        return HttpGetContent(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{fileId}/content"));
    }

	/// <summary>
	///     Deletes given file.
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <param name="provider">Which provider will be used</param>
	/// <returns></returns>
	public async Task<DeletedTornadoFile?> DeleteFileAsync(string fileId, LLmProviders? provider = null)
    {
	    IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);

	    string url = resolvedProvider.Provider switch
	    {
		    LLmProviders.Google => GetUrl(resolvedProvider, CapabilityEndpoints.BaseUrl, fileId),
		    _ => GetUrl(resolvedProvider, $"/{fileId}")
	    };
	    
	    DeletedTornadoFile? file = await HttpDelete<DeletedTornadoFile>(resolvedProvider, Endpoint, url).ConfigureAwait(ConfigureAwaitOptions.None);

	    if (file is not null)
	    {
		    // this is set automatically only by OpenAi
		    file.Deleted = true;
		    file.Id = fileId;
	    }

	    return file;
    }

	/// <summary>
	///     Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all
	///     the files uploaded by one organization can be up to 1 GB. Please contact OpenAI if you need to increase the storage
	///     limit
	/// </summary>
	/// <param name="filePath">The name of the file to use for this request</param>
	/// <param name="purpose">
	///     The intended purpose of the uploaded documents. Use "fine-tune" for Fine-tuning. This allows us
	///     to validate the format of the uploaded file.
	/// </param>
	/// <param name="fileName">Determined from path if not set</param>
	/// <param name="provider">Which provider will be used</param>
	public async Task<HttpCallResult<TornadoFile>> UploadFileAsync(string filePath, FilePurpose purpose = FilePurpose.Finetune, string? fileName = null, LLmProviders provider = LLmProviders.OpenAi)
    {
	    if (!File.Exists(filePath))
	    {
		    return new HttpCallResult<TornadoFile>(HttpStatusCode.UnprocessableEntity, null, null, false, new RestDataOrException<HttpResponseMessage>(new Exception($"File {filePath} not found")));
	    }

	    byte[] bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(ConfigureAwaitOptions.None);
	    string finalFileName = fileName ?? (provider is LLmProviders.Google ? Guid.NewGuid().ToString() : Path.GetFileName(filePath)); // google requires alphanum + dashes, up to 40 chars

        return await UploadFileAsync(bytes, finalFileName, purpose, provider).ConfigureAwait(ConfigureAwaitOptions.None);
    }
	
	/// <summary>
	///     Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all
	///     the files uploaded by one organization can be up to 1 GB. Please contact OpenAI if you need to increase the storage
	///     limit
	/// </summary>
	/// <param name="stream">Stream to read the file content from</param>
	/// <param name="purpose">
	///     The intended purpose of the uploaded documents. Use "fine-tune" for Fine-tuning. This allows us
	///     to validate the format of the uploaded file.
	/// </param>
	/// <param name="fileName">Determined from path if not set</param>
	/// <param name="provider">Which provider will be used</param>
	public async Task<HttpCallResult<TornadoFile>> UploadFileAsync(Stream stream, string fileName, FilePurpose purpose = FilePurpose.Finetune, LLmProviders? provider = null)
	{
		byte[] bytes = await stream.ToArrayAsync().ConfigureAwait(ConfigureAwaitOptions.None);
		return await UploadFileAsync(bytes, fileName, purpose, provider).ConfigureAwait(ConfigureAwaitOptions.None);
	}
	
	/// <summary>
	///     Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all
	///     the files uploaded by one organization can be up to 1 GB. Please contact OpenAI if you need to increase the storage
	///     limit
	/// </summary>
	/// <param name="fileBytes">Bytes of the file to be uploaded</param>
	/// <param name="purpose">
	///     The intended purpose of the uploaded documents. Use "fine-tune" for Fine-tuning. This allows us
	///     to validate the format of the uploaded file.
	/// </param>
	/// <param name="fileName">Determined from path if not set</param>
	/// <param name="provider">Which provider will be used</param>
	public async Task<HttpCallResult<TornadoFile>> UploadFileAsync(byte[] fileBytes, string fileName, FilePurpose purpose = FilePurpose.Finetune, LLmProviders? provider = null)
	{
		return await UploadFileAsync(new FileUploadRequest
		{
			Bytes = fileBytes,
			Name = fileName,
			Purpose = purpose
		}, provider).ConfigureAwait(ConfigureAwaitOptions.None);
	}

	/// <summary>
	///     Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all
	///     the files uploaded by one organization can be up to 1 GB. Please contact OpenAI if you need to increase the storage
	///     limit
	/// </summary>
	/// <param name="request">The request</param>
	/// <param name="provider">Which provider will be used</param>
	/// <returns></returns>
	public async Task<HttpCallResult<TornadoFile>> UploadFileAsync(FileUploadRequest request, LLmProviders? provider = null)
	{
		IEndpointProvider resolvedProvider = Api.ResolveProvider(provider);
		TornadoRequestContent content = request.Serialize(resolvedProvider);
		
		string url = resolvedProvider.Provider switch
		{
			LLmProviders.Google => resolvedProvider.ApiUrl(CapabilityEndpoints.BaseUrlStripped, "upload/v1beta/files"),
			_ => GetUrl(resolvedProvider)
		};

		if (resolvedProvider.Provider is LLmProviders.OpenAi)
		{
			HttpCallResult<TornadoFile> file = await HttpPost<TornadoFile>(resolvedProvider, CapabilityEndpoints.Files, url, content.Body).ConfigureAwait(ConfigureAwaitOptions.None);

			if (content.Body is IDisposable disposableOaiBody)
			{
				disposableOaiBody.Dispose();
			}

			return file;
		}

		if (resolvedProvider.Provider is not LLmProviders.Google)
		{
			return new HttpCallResult<TornadoFile>(HttpStatusCode.ServiceUnavailable, "Endpoint not available", null, false, new RestDataOrException<HttpResponseMessage>(new Exception("Endpoint not available")));
		}
		
		// for Google, two requests must be made: first one signals the intent to upload a file and receives payload url, the second uploads the file

		// reset internal state
		request.InternalState = FileUploadRequestStates.Unknown;
			
		// T is used as a placeholder here, this request, if successful, returns empty string body
		HttpCallResult<TornadoFile> result = await HttpPost<TornadoFile>(resolvedProvider, CapabilityEndpoints.Files, url, content.Body, headers: new Dictionary<string, object?>
		{
			{ "X-Goog-Upload-Protocol", "resumable" },
			{ "X-Goog-Upload-Command", "start" },
			{ "X-Goog-Upload-Header-Content-Length", request.Bytes.Length },
			{ "X-Goog-Upload-Header-Content-Type", request.MimeType }
		}).ConfigureAwait(ConfigureAwaitOptions.None);

		if (content.Body is IDisposable disposable)
		{
			disposable.Dispose();
		}

		if (!result.Ok || result.Request.Data is null)
		{
			return result;
		}

		if (!result.Request.Data.Headers.TryGetValues("x-goog-upload-url", out IEnumerable<string>? uploadUrls))
		{
			return result;
		}

		// this should have length = 1, make compiler happy
		string? uploadUrl = uploadUrls.FirstOrDefault();

		if (uploadUrl is null)
		{
			return result;
		}

		request.InternalState = FileUploadRequestStates.PayloadUrlObtained;
			
		// serialize the request again, this time we get binary data
		TornadoRequestContent binaryContent = request.Serialize(resolvedProvider);
			
		result = await HttpPost<TornadoFile>(resolvedProvider, CapabilityEndpoints.None, uploadUrl, binaryContent.Body, headers: new Dictionary<string, object?>
		{
			{ "X-Goog-Upload-Offset", "0" },
			{ "X-Goog-Upload-Command", "upload, finalize" }
		}).ConfigureAwait(ConfigureAwaitOptions.None);

		// dispose the ByteArrayContent
		if (binaryContent.Body is IDisposable disposableBody)
		{
			disposableBody.Dispose();
		}

		return result;

	}

	/// <summary>
	///     A helper class to deserialize the JSON API responses. This should not be used directly.
	/// </summary>
	private class TornadoFiles : ApiResultBase
    {
        [JsonProperty("data")] 
        public List<TornadoFile> Data { get; set; }

        [JsonProperty("object")] 
        public string Obj { get; set; }
    }
}