using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
///     Chat features supported only by Cohere.
/// </summary>
public class ChatResponseVendorCohereExtensions
{
    private static readonly Regex CitationRegex = new Regex("\\.?<\\/?co:[ ]*(\\d+,?)+>", RegexOptions.Compiled);

    /// <summary>
    ///     Inline citations for the generated reply.
    /// </summary>
    public List<VendorCohereChatCitation>? Citations { get; set; }
    
    /// <summary>
    ///     Documents seen by the model when generating the reply.
    /// </summary>
    public List<VendorCohereChatDocument>? Documents { get; set; }
    
    /// <summary>
    ///     Documents retrieved from each of the conducted searches.
    /// </summary>
    public List<VendorCohereChatSearchResult>? SearchResults { get; set; }
    
    /// <summary>
    ///     Generated search queries, meant to be used as part of the RAG flow.
    /// </summary>
    public List<VendorCohereChatSearchQuery>? SearchQueries { get; set; }

    /// <summary>
    ///     Empty extensions.
    /// </summary>
    public ChatResponseVendorCohereExtensions()
    {
        
    }

    /// <summary>
    ///     Returns a list of blocks where each block is either uncited or cited by one or more document.
    ///     Also clears citation markers which are not intended for the end users.
    ///     This overload can only be used in non-streaming scenarios.
    /// </summary>
    /// <returns></returns>
    public List<VendorCohereCitationBlock> ParseCitations()
    {
        return []; // ParseCitations(Response.Message ?? string.Empty);
    }

    /// <summary>
    ///     Returns a list of blocks where each block is either uncited or cited by one or more document.
    ///     Also clears citation markers which are not intended for the end users.
    /// </summary>
    /// <returns></returns>
    public List<VendorCohereCitationBlock> ParseCitations(string text)
    {
        List<VendorCohereCitationBlock> blocks = [];
        int pos = 0;

        if (Citations is not null && Citations.Count > 0)
        {
            foreach (VendorCohereChatCitation citation in Citations.OrderBy(x => x.Start))
            {
                if (text.Length > 0 && citation.Start > pos)
                {
                    string beforeSnippet = ClearSnippet(text.Substring(pos, citation.Start.Value - pos));

                    if (beforeSnippet.Length > 0)
                    {
                        blocks.Add(new VendorCohereCitationBlock
                        {
                            Text = beforeSnippet
                        });   
                    }

                    pos += citation.Start.Value - pos;
                }
                
                string snippet = ClearSnippet(citation.Text);

                if (snippet.Length > 0)
                {
                    blocks.Add(new VendorCohereCitationBlock
                    {
                        Text = snippet,
                        Citation = citation
                    });   
                }

                pos += citation.Text.Length;
            }

            if (pos < text.Length)
            {
                string snippet = ClearSnippet(text[pos..]);

                if (snippet.Length > 0)
                {
                    blocks.Add(new VendorCohereCitationBlock
                    {
                        Text = text[pos..]
                    });   
                }
            }
        }
        else
        {
            blocks.Add(new VendorCohereCitationBlock
            {
                Text = text
            });
        }

        return blocks;

        string ClearSnippet(string txt)
        {
            return CitationRegex.Replace(txt, string.Empty);
        }
    }
    
    internal VendorCohereChatResult Response { get; set; }

    internal ChatResponseVendorCohereExtensions(VendorCohereChatResult response)
    {
        /*Citations = response.Citations;
        Documents = response.Documents;
        SearchQueries = response.SearchQueries;
        SearchResults = response.SearchResults;
        Response = response;*/
    }
}