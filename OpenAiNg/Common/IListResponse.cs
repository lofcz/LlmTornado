using System.Collections.Generic;

namespace OpenAiNg.Common;

public interface IListResponse<out TObject>
{
    IReadOnlyList<TObject> Items { get; }
}