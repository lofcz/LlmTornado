// Licensed under the MIT License. See LICENSE in the project root for license information.
using OpenAiNg.Files;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAiNg.Chat;
using OpenAiNg.Common;

namespace OpenAiNg.Assistants
{
    /// <summary>
    /// Assistants are higher-level API than <see cref="ChatEndpoint"/> featuring automatic context management, code interpreter and file based retrieval.
    /// </summary>
    public sealed class AssistantsEndpoint : EndpointBase, IAssistantsEndpoint
    {
        internal AssistantsEndpoint(OpenAiApi api) : base(api) { }
        
        protected override string Endpoint => "assistants";

        /// <summary>
        /// Get list of assistants.
        /// </summary>
        /// <param name="query">(optional) <see cref="ListQuery"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="ListResponse{Assistant}"/></returns>
        public Task<ListResponse<AssistantResponse>?> ListAssistantsAsync(ListQuery? query = null, CancellationToken? cancellationToken = default)
        {
            return HttpGetRaw<ListResponse<AssistantResponse>>(GetUrl(queryParameters: query), cancellationToken);
        }
        
        /// <summary>
        /// Create an assistant.
        /// </summary>
        /// <param name="request"><see cref="CreateAssistantRequest"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantResponse"/>.</returns>
        public Task<AssistantResponse?> CreateAssistantAsync(CreateAssistantRequest request, CancellationToken? cancellationToken = default)
        {
            return HttpPostRaw<AssistantResponse>(Url, request, cancellationToken);
        }

        /// <summary>
        /// Retrieves an assistant.
        /// </summary>
        /// <param name="assistantId">The ID of the assistant to retrieve.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantResponse"/>.</returns>
        public Task<AssistantResponse?> RetrieveAssistantAsync(string assistantId, CancellationToken? cancellationToken = default)
        {
            return HttpGetRaw<AssistantResponse>(GetUrl($"/{assistantId}"), cancellationToken, true);
        }

        /// <summary>
        /// Modifies an assistant. All fields in the existing assistant are replaced with the fields from <see cref="request"/>.
        /// </summary>
        /// <param name="assistantId">The ID of the assistant to modify.</param>
        /// <param name="request"><see cref="CreateAssistantRequest"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantResponse"/>.</returns>
        public Task<AssistantResponse?> ModifyAssistantAsync(string assistantId, CreateAssistantRequest request, CancellationToken? cancellationToken = default)
        {
            return HttpPostRaw<AssistantResponse>(GetUrl($"/{assistantId}"), request, cancellationToken);
        }

        /// <summary>
        /// Delete an assistant.
        /// </summary>
        /// <param name="assistantId">The ID of the assistant to delete.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>True, if the assistant was deleted.</returns>
        public async Task<bool> DeleteAssistantAsync(string assistantId, CancellationToken? cancellationToken = default)
        {
            DeletionStatus? status = await HttpAtomic<DeletionStatus>(HttpMethod.Delete, GetUrl($"/{assistantId}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            return status?.Deleted ?? false;
        }
        
        /// <summary>
        /// Returns a list of assistant files.
        /// </summary>
        /// <param name="assistantId">The ID of the assistant the file belongs to.</param>
        /// <param name="query"><see cref="ListQuery"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="ListResponse{AssistantFile}"/>.</returns>
        public Task<ListResponse<AssistantFileResponse>?> ListFilesAsync(string assistantId, ListQuery? query = null, CancellationToken? cancellationToken = default)
        {
            return HttpGetRaw<ListResponse<AssistantFileResponse>>(GetUrl($"/{assistantId}/files", query), cancellationToken, true);
        }

        /// <summary>
        /// Attach a file to an assistant.
        /// </summary>
        /// <param name="assistantId"> The ID of the assistant for which to attach a file. </param>
        /// <param name="file">
        /// A <see cref="File"/> (with purpose="assistants") that the assistant should use.
        /// Useful for tools like retrieval and code_interpreter that can access files.
        /// </param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantFileResponse"/>.</returns>
        public Task<AssistantFileResponse?> AttachFileAsync(string assistantId, File file, CancellationToken? cancellationToken = default)
        {
            return HttpPostRaw<AssistantFileResponse>(GetUrl($"/{assistantId}/files"), new { file_id = file.Id }, cancellationToken);
        }
        
        /// <summary>
        /// Attach a file to an assistant.
        /// </summary>
        /// <param name="assistantId"> The ID of the assistant for which to attach a file. </param>
        /// <param name="fileId">
        /// A file ID obtained by creating / retrieving a file.
        /// </param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantFileResponse"/>.</returns>
        public Task<AssistantFileResponse?> AttachFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default)
        {
            return HttpPostRaw<AssistantFileResponse>(GetUrl($"/{assistantId}/files"), new { file_id = fileId }, cancellationToken);
        }

        /// <summary>
        /// Retrieves an AssistantFile.
        /// </summary>
        /// <param name="assistantId">The ID of the assistant who the file belongs to.</param>
        /// <param name="fileId">The ID of the file we're getting.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="AssistantFileResponse"/>.</returns>
        public Task<AssistantFileResponse?> RetrieveFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default)
        {
            return HttpGetRaw<AssistantFileResponse>(GetUrl($"/{assistantId}/files/{fileId}"), cancellationToken, true);
        }

        /// <summary>
        /// Remove an assistant file.
        /// </summary>
        /// <remarks>
        /// Note that removing an AssistantFile does not delete the original File object,
        /// it simply removes the association between that File and the Assistant.
        /// To delete a File, use the File delete endpoint instead.
        /// </remarks>
        /// <param name="assistantId">The ID of the assistant that the file belongs to.</param>
        /// <param name="fileId">The ID of the file to delete.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>True, if file was removed.</returns>
        public async Task<bool> RemoveFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default)
        {
            DeletionStatus? status = await HttpAtomic<DeletionStatus>(HttpMethod.Delete, GetUrl($"/{assistantId}/files/{fileId}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            return status?.Deleted ?? false;
        }
        
        /// <summary>
        /// Remove an assistant file.
        /// </summary>
        /// <remarks>
        /// Note that removing an AssistantFile does not delete the original File object,
        /// it simply removes the association between that File and the Assistant.
        /// To delete a File, use the File delete endpoint instead.
        /// </remarks>
        /// <param name="assistantId">The ID of the assistant that the file belongs to.</param>
        /// <param name="file">The file to delete.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>True, if file was removed.</returns>
        public async Task<bool> RemoveFileAsync(string assistantId, File file, CancellationToken? cancellationToken = default)
        {
            DeletionStatus? status = await HttpAtomic<DeletionStatus>(HttpMethod.Delete, GetUrl($"/{assistantId}/files/{file.Id}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
            return status?.Deleted ?? false;
        }
    }
}