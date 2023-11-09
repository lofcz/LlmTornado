using System.IO;
using System.Net.Http;

namespace OpenAiNg.Code;

public class Ref<T>
{
    public T? Ptr { get; set; }
}

public class StreamResponse
{
    public Stream Stream { get; set; }
    public ApiResultBase Headers { get; set; }
    public HttpResponseMessage Response { get; set; }
}