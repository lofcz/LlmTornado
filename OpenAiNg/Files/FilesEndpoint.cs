using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAiNg.Code;
using OpenAiNg.Common;

namespace OpenAiNg.Files;

/// <summary>
///     The API endpoint for operations List, Upload, Delete, Retrieve files
/// </summary>
public class FilesEndpoint : EndpointBase, IFilesEndpoint
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="OpenAiApi" /> as <see cref="OpenAiApi.Files" />.
	/// </summary>
	/// <param name="api"></param>
	internal FilesEndpoint(OpenAiApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "files".
	/// </summary>
	protected override string Endpoint => "files";
    
	/// <summary>
	/// 
	/// </summary>
	protected override CapabilityEndpoints CapabilityEndpoint => CapabilityEndpoints.Files;
	

	/// <summary>
	///     Get the list of all files
	/// </summary>
	/// <returns></returns>
	/// <exception cref="HttpRequestException"></exception>
	public async Task<List<File>?> GetFilesAsync()
    {
        return (await HttpGet<FilesData>(Api.EndpointProvider, CapabilityEndpoint).ConfigureAwait(ConfigureAwaitOptions.None))?.Data;
    }

	/// <summary>
	///     Returns information about a specific file
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <returns></returns>
	public Task<File?> GetFileAsync(string fileId)
    {
        return HttpGet<File>(Api.EndpointProvider, CapabilityEndpoint, $"{Url}/{fileId}");
    }

	/// <summary>
	///     Returns the contents of the specific file as string
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <returns></returns>
	public Task<string> GetFileContentAsStringAsync(string fileId)
    {
        return HttpGetContent(Api.EndpointProvider, CapabilityEndpoint, $"{Url}/{fileId}/content");
    }

	/// <summary>
	///     Delete a file
	/// </summary>
	/// <param name="fileId">The ID of the file to use for this request</param>
	/// <returns></returns>
	public Task<File?> DeleteFileAsync(string fileId)
    {
        return HttpDelete<File>(Api.EndpointProvider, CapabilityEndpoint,$"{Url}/{fileId}");
    }


	/// <summary>
	///     Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all
	///     the files uploaded by one organization can be up to 1 GB. Please contact OpenAI if you need to increase the storage
	///     limit
	/// </summary>
	/// <param name="filePath">The name of the file to use for this request</param>
	/// <param name="purpose">
	///     The intendend purpose of the uploaded documents. Use "fine-tune" for Fine-tuning. This allows us
	///     to validate the format of the uploaded file.
	/// </param>
	public async Task<HttpCallResult<File>> UploadFileAsync(string filePath, FilePurpose purpose = FilePurpose.Finetune)
    {
        MultipartFormDataContent content = new()
        {
            { new StringContent(purpose is FilePurpose.Finetune ? "fine-tune" : "assistants"), "purpose" },
            { new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath).ConfigureAwait(ConfigureAwaitOptions.None)), "file", Path.GetFileName(filePath) }
        };

        return await HttpPost<File>(Api.EndpointProvider, Url, content).ConfigureAwait(ConfigureAwaitOptions.None);
    }

	/// <summary>
	///     A helper class to deserialize the JSON API responses. This should not be used directly.
	/// </summary>
	private class FilesData : ApiResultBase
    {
        [JsonProperty("data")] public List<File> Data { get; set; }

        [JsonProperty("object")] public string Obj { get; set; }
    }
}