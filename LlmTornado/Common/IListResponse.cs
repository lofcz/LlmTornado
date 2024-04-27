using System.Collections.Generic;

namespace LlmTornado.Common;

public interface IListResponse<out TObject>
{
    IReadOnlyList<TObject> Items { get; }
}