namespace LlmTornado.Responses;

/// <summary>
/// Options specific to a <c>response.create</c> event sent over a Responses WebSocket connection.
/// </summary>
public class ResponseWebSocketCreateOptions
{
    /// <summary>
    /// When <c>false</c>, prepares request state (tools, instructions, etc.) without generating model output.
    /// Useful for warming up a connection before the first real turn. Returns a response ID that can be
    /// chained via <see cref="ResponseRequest.PreviousResponseId"/>.
    /// </summary>
    public bool? Generate { get; set; }
}
