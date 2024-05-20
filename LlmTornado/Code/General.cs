namespace LlmTornado.Code;

public class General
{
    public static string IIID()
    {
        return $"_{Nanoid.Generate("0123456789abcdefghijklmnopqrstuvwxzyABCDEFGHCIJKLMNOPQRSTUVWXYZ", 23)}";
    }
}