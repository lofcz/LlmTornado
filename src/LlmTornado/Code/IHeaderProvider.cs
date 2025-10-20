using System.Collections.Generic;

namespace LlmTornado.Code;

/// <summary>
/// Internal interface for requests that need to provide additional headers based on the target provider.
/// </summary>
internal interface IHeaderProvider
{
    /// <summary>
    /// Returns additional headers required by this request for the specified provider.
    /// </summary>
    /// <param name="provider">The provider this request is being serialized for</param>
    IEnumerable<string> GetHeaders(LLmProviders provider);
}

