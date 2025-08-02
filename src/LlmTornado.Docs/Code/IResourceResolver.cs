namespace LlmTornado.Docs.Code;

public interface IResourceResolver
{
    public Task<string> ResolveResource(string resource);
}