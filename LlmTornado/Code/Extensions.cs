using System.Diagnostics.CodeAnalysis;

namespace LlmTornado.Code;

internal static class Extensions
{
    internal static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
}