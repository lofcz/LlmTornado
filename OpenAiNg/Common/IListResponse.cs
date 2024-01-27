// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
namespace OpenAiNg.Common;

public interface IListResponse<out TObject>
{
    IReadOnlyList<TObject> Items { get; }
}
