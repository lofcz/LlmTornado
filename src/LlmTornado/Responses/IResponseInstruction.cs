using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Interface for instruction types that can be either a string or an array of input items.
/// </summary>
[JsonConverter(typeof(InstructionJsonConverter))]
public interface IResponseInstruction
{
}

/// <summary>
/// String-based instruction implementation.
/// </summary>
public class StringResponseInstruction : IResponseInstruction
{
    /// <summary>
    /// The instruction text.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Creates a new string instruction.
    /// </summary>
    /// <param name="value">The instruction text</param>
    public StringResponseInstruction(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an empty string instruction.
    /// </summary>
    public StringResponseInstruction()
    {
        Value = string.Empty;
    }

    /// <summary>
    /// Implicit conversion from string to StringInstruction.
    /// </summary>
    /// <param name="value">The string value</param>
    /// <returns>A new StringInstruction</returns>
    public static implicit operator StringResponseInstruction(string value)
    {
        return new StringResponseInstruction(value);
    }

    /// <summary>
    /// Implicit conversion from StringInstruction to string.
    /// </summary>
    /// <param name="responseInstruction">The StringInstruction</param>
    /// <returns>The string value</returns>
    public static implicit operator string(StringResponseInstruction responseInstruction)
    {
        return responseInstruction.Value;
    }
}

/// <summary>
/// Array-based instruction implementation containing multiple input items.
/// </summary>
public class ArrayResponseInstruction : IResponseInstruction
{
    /// <summary>
    /// The array of input items.
    /// </summary>
    public List<ResponseInputItem> Items { get; set; }

    /// <summary>
    /// Creates a new array instruction.
    /// </summary>
    /// <param name="items">The input items</param>
    public ArrayResponseInstruction(List<ResponseInputItem> items)
    {
        Items = items;
    }

    /// <summary>
    /// Creates an empty array instruction.
    /// </summary>
    public ArrayResponseInstruction()
    {
        Items = [];
    }

    /// <summary>
    /// Implicit conversion from List&lt;ResponseInputItem&gt; to ArrayInstruction.
    /// </summary>
    /// <param name="items">The list of input items</param>
    /// <returns>A new ArrayInstruction</returns>
    public static implicit operator ArrayResponseInstruction(List<ResponseInputItem> items)
    {
        return new ArrayResponseInstruction(items);
    }

    /// <summary>
    /// Implicit conversion from ArrayInstruction to List&lt;ResponseInputItem&gt;.
    /// </summary>
    /// <param name="responseInstruction">The ArrayInstruction</param>
    /// <returns>The list of input items</returns>
    public static implicit operator List<ResponseInputItem>(ArrayResponseInstruction responseInstruction)
    {
        return responseInstruction.Items;
    }
} 