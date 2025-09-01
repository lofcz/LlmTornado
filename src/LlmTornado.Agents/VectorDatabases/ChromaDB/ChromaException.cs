namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public class ChromaException : Exception
{
	public ChromaException() { }
	public ChromaException(string? message) : base(message) { }
	public ChromaException(string? message, Exception? inner) : base(message, inner) { }
}