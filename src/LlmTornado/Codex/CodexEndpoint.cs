using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Codex;

/// <summary>
/// Connects to ChatGPT subscription authentication and Codex text turns through the official app-server or direct OAuth.
/// </summary>
public sealed class CodexEndpoint
{
    internal CodexEndpoint()
    {
    }

    /// <summary>
    /// Starts and initializes a Codex app-server session.
    /// </summary>
    public Task<CodexSession> ConnectAsync(
        CodexAppServerOptions? options = null,
        CancellationToken cancellationToken = default)
        => CodexSession.ConnectAsync(options ?? new CodexAppServerOptions(), cancellationToken);

    /// <summary>
    /// Creates a standalone Codex session that authenticates directly with OpenAI through browser OAuth.
    /// This path does not require a local Codex installation.
    /// </summary>
    public Task<CodexOAuthSession> ConnectOAuthAsync(
        CodexOAuthOptions? options = null,
        CancellationToken cancellationToken = default)
        => CodexOAuthSession.ConnectAsync(options ?? new CodexOAuthOptions(), cancellationToken);
}
