using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Helper methods for OpenAI Responses API compaction workflows, including server-side
/// <c>context_management</c> and the standalone <c>/responses/compact</c> endpoint.
/// </summary>
public static class ResponseCompactionExtensions
{
    /// <summary>
    /// Converts a compaction output item into an input item for the next request.
    /// </summary>
    public static CompactionInputItem ToInputItem(this ResponseCompactionOutputItem item)
    {
        return new CompactionInputItem(item.EncryptedContent)
        {
            Id = item.Id
        };
    }

    /// <summary>
    /// Returns the most recent compaction item from a response, if any.
    /// </summary>
    public static ResponseCompactionOutputItem? GetLatestCompaction(this ResponseResult result)
    {
        return result.Output?.OfType<ResponseCompactionOutputItem>().LastOrDefault();
    }

    /// <summary>
    /// Returns the most recent compaction item from a compact result, if any.
    /// </summary>
    public static ResponseCompactionOutputItem? GetLatestCompaction(this ResponseCompactResult result)
    {
        return result.Output?.OfType<ResponseCompactionOutputItem>().LastOrDefault();
    }

    /// <summary>
    /// Converts response output items into input items for stateless conversation chaining.
    /// </summary>
    public static List<ResponseInputItem> ToInputItems(this IEnumerable<IResponseOutputItem>? output)
    {
        if (output is null)
        {
            return [];
        }

        List<ResponseInputItem> items = [];
        foreach (IResponseOutputItem item in output)
        {
            ResponseInputItem? inputItem = item.ToInputItem();
            if (inputItem is not null)
            {
                items.Add(inputItem);
            }
        }

        return items;
    }

    /// <summary>
    /// Converts a single response output item into an input item when the wire format is compatible.
    /// </summary>
    public static ResponseInputItem? ToInputItem(this IResponseOutputItem output)
    {
        switch (output)
        {
            case ResponseCompactionOutputItem compaction:
                return compaction.ToInputItem();
            default:
                string json = JsonConvert.SerializeObject(output, EndpointBase.NullSettings);
                JObject jo = JObject.Parse(json);
                InputItemJsonConverter converter = new InputItemJsonConverter();
                return converter.ReadJson(new JTokenReader(jo), typeof(ResponseInputItem), null, false, JsonSerializer.Create(EndpointBase.NullSettings));
        }
    }

    /// <summary>
    /// Appends all output items from a response to an existing input list.
    /// </summary>
    public static void AppendOutput(this List<ResponseInputItem> input, ResponseResult result)
    {
        input.AddRange(result.Output.ToInputItems());
    }

    /// <summary>
    /// Drops input items that precede the most recent compaction item to reduce request size.
    /// The latest compaction item carries the context needed to continue the conversation.
    /// Do not use when chaining via <see cref="ResponseRequest.PreviousResponseId"/>.
    /// </summary>
    public static List<ResponseInputItem> PruneBeforeLatestCompaction(this IEnumerable<ResponseInputItem> input)
    {
        List<ResponseInputItem> items = input.ToList();
        int lastCompactionIndex = items.FindLastIndex(x => x is CompactionInputItem);

        if (lastCompactionIndex <= 0)
        {
            return items;
        }

        return items.Skip(lastCompactionIndex).ToList();
    }

    /// <summary>
    /// Enables server-side compaction on the request with the given token threshold.
    /// </summary>
    public static ResponseRequest WithServerSideCompaction(this ResponseRequest request, int compactThreshold)
    {
        request.ContextManagement = [ResponseContextManagementItem.Compaction(compactThreshold)];
        return request;
    }
}
