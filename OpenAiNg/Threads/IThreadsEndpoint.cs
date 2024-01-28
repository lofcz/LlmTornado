using System.Threading;
using System.Threading.Tasks;
using OpenAiNg.Common;

namespace OpenAiNg.Threads;

public interface IThreadsEndpoint
{
    public Task<HttpCallResult<ThreadResponse>> CreateThreadAsync(CreateThreadRequest? request = null, CancellationToken? cancellationToken = default);
}