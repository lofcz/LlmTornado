using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Common;
using LlmTornado.Files;

namespace LlmTornado.Assistants;

/// <summary>
///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
///     interpreter and file based retrieval.
/// </summary>
public interface IAssistantsEndpoint
{
    /// <summary>
    ///     Get list of assistants.
    /// </summary>
    /// <param name="query">(optional) <see cref="ListQuery" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>
    ///     <see cref="ListResponse{Assistant}" />
    /// </returns>
    public Task<HttpCallResult<ListResponse<AssistantResponse>>> ListAssistantsAsync(ListQuery? query = null, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Create an assistant.
    /// </summary>
    /// <param name="request"><see cref="CreateAssistantRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantResponse" />.</returns>
    public Task<HttpCallResult<AssistantResponse>> CreateAssistantAsync(CreateAssistantRequest request, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Retrieves an assistant.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantResponse" />.</returns>
    public Task<HttpCallResult<AssistantResponse>> RetrieveAssistantAsync(string assistantId, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Modifies an assistant. All fields in the existing assistant are replaced with the fields from
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="CreateAssistantRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantResponse" />.</returns>
    public Task<HttpCallResult<AssistantResponse>> ModifyAssistantAsync(string assistantId, CreateAssistantRequest request, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Delete an assistant.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>True, if the assistant was deleted.</returns>
    public Task<HttpCallResult<bool>> DeleteAssistantAsync(string assistantId, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Returns a list of assistant files.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant the file belongs to.</param>
    /// <param name="query"><see cref="ListQuery" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="ListResponse{AssistantFile}" />.</returns>
    public Task<HttpCallResult<ListResponse<AssistantFileResponse>>> ListFilesAsync(string assistantId, ListQuery? query = null, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Attach a file to an assistant.
    /// </summary>
    /// <param name="assistantId"> The ID of the assistant for which to attach a file. </param>
    /// <param name="file">
    ///     A <see cref="File" /> (with purpose="assistants") that the assistant should use.
    ///     Useful for tools like retrieval and code_interpreter that can access files.
    /// </param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantFileResponse" />.</returns>
    public Task<HttpCallResult<AssistantFileResponse>> AttachFileAsync(string assistantId, File file, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Attach a file to an assistant.
    /// </summary>
    /// <param name="assistantId"> The ID of the assistant for which to attach a file. </param>
    /// <param name="fileId">
    ///     A file ID obtained by creating / retrieving a file.
    /// </param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantFileResponse" />.</returns>
    public Task<HttpCallResult<AssistantFileResponse>> AttachFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Retrieves an AssistantFile.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant who the file belongs to.</param>
    /// <param name="fileId">The ID of the file we're getting.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="AssistantFileResponse" />.</returns>
    public Task<HttpCallResult<AssistantFileResponse>> RetrieveFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Remove an assistant file.
    /// </summary>
    /// <remarks>
    ///     Note that removing an AssistantFile does not delete the original File object,
    ///     it simply removes the association between that File and the Assistant.
    ///     To delete a File, use the File delete endpoint instead.
    /// </remarks>
    /// <param name="assistantId">The ID of the assistant that the file belongs to.</param>
    /// <param name="fileId">The ID of the file to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>True, if file was removed.</returns>
    public Task<HttpCallResult<bool>> RemoveFileAsync(string assistantId, string fileId, CancellationToken? cancellationToken = default);

    /// <summary>
    ///     Remove an assistant file.
    /// </summary>
    /// <remarks>
    ///     Note that removing an AssistantFile does not delete the original File object,
    ///     it simply removes the association between that File and the Assistant.
    ///     To delete a File, use the File delete endpoint instead.
    /// </remarks>
    /// <param name="assistantId">The ID of the assistant that the file belongs to.</param>
    /// <param name="file">The file to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>True, if file was removed.</returns>
    public Task<HttpCallResult<bool>> RemoveFileAsync(string assistantId, File file, CancellationToken? cancellationToken = default);
}