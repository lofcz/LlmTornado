using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Common;

namespace LlmTornado.Threads;

/// <summary>
///     Create threads that assistants can interact with.<br />
///     <see href="https://platform.openai.com/docs/api-reference/threads" />
/// </summary>
public interface IThreadsEndpoint
{
    /// <summary>
    ///     Create a thread.
    /// </summary>
    /// <param name="request"><see cref="CreateThreadRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="ThreadResponse" />.</returns>
    public Task<HttpCallResult<ThreadResponse>> CreateThreadAsync(CreateThreadRequest? request = null, CancellationToken? cancellationToken = default);
    
    /// <summary>
    /// Retrieves a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="ThreadResponse"/> to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ThreadResponse"/>.</returns>
    public Task<HttpCallResult<ThreadResponse>> RetrieveThreadAsync(string threadId, CancellationToken? cancellationToken = default);
    
    /// <summary>
    /// Modifies a thread.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="ThreadResponse.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId">The id of the <see cref="ThreadResponse"/> to modify.</param>
    /// <param name="metadata">Set of up to 16 key-value pairs that can be attached to an object.
    /// This can be useful for storing additional information about the object in a structured format.
    /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ThreadResponse"/>.</returns>
    public Task<HttpCallResult<ThreadResponse>> ModifyThreadAsync(string threadId, IDictionary<string, string> metadata, CancellationToken? cancellationToken = default);
    
    /// <summary>
    /// Delete a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="ThreadResponse"/> to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns>True, if was successfully deleted.</returns>
    public Task<HttpCallResult<bool>> DeleteThreadAsync(string threadId, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Create a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to create a message for.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="MessageResponse"/>.</returns>
    public Task<HttpCallResult<MessageResponse>> CreateMessageAsync(string threadId, CreateMessageRequest request, CancellationToken? cancellationToken = default);
}