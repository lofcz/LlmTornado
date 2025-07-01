using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LlmTornado.Code;

internal class General
{
    public static string IIID()
    {
        return $"_{Nanoid.Generate("0123456789abcdefghijklmnopqrstuvwxzyABCDEFGHCIJKLMNOPQRSTUVWXYZ", 23)}";
    }
}

internal enum HttpVerbs
{
    Get,
    Head,
    Post,
    Put,
    Delete,
    Connect,
    Options,
    Trace,
    Patch
}

internal static class HttpVerbsCls
{
    public static HttpMethod Get = HttpMethod.Get;
    public static HttpMethod Head = HttpMethod.Head;
    public static HttpMethod Post = HttpMethod.Post;
    public static HttpMethod Put = HttpMethod.Put;
    public static HttpMethod Delete = HttpMethod.Delete;
    public static HttpMethod Connect = new HttpMethod("CONNECT");
    public static HttpMethod Options = HttpMethod.Options;
    public static HttpMethod Trace = HttpMethod.Trace;
    public static HttpMethod Patch = new HttpMethod("PATCH");
}

internal sealed class PrefixChecker
{
    public static PrefixChecker JsonStart = new PrefixChecker([ "{", "[", "\"", "-", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "t", "f", "n"]);
    public static PrefixChecker JsonStartObjectArrayString = new PrefixChecker([ "{", "[", "\""]);
    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = [];
        public bool IsEndOfPrefix { get; set; }
    }

    private readonly TrieNode _root = new TrieNode();
    
    public PrefixChecker(IEnumerable<string>? prefixes)
    {
        if (prefixes is null)
        {
            return;
        }

        foreach (string prefix in prefixes)
        {
            if (prefix != null)
            {
                Insert(prefix);
            }
        }
    }

    private void Insert(string prefix)
    {
        TrieNode? currentNode = _root;
        
        foreach (char c in prefix)
        {
            if (!currentNode.Children.TryGetValue(c, out TrieNode? childNode))
            {
                childNode = new TrieNode();
                currentNode.Children[c] = childNode;
            }
            
            currentNode = childNode;
        }
        
        currentNode.IsEndOfPrefix = true;
    }
    
    public bool StartsWithAny(string? input)
    {
        if (input is null)
        {
            return false;
        }

        TrieNode? currentNode = _root;
        
        foreach (char c in input)
        {
            if (currentNode.IsEndOfPrefix)
            {
                return true;
            }

            if (!currentNode.Children.TryGetValue(c, out TrieNode? childNode))
            {
                return false;
            }
            
            currentNode = childNode;
        }
        
        return currentNode.IsEndOfPrefix;
    }
}