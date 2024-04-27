namespace LlmTornado.Code;

internal static class Extensions
{
    internal static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
}