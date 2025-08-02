using System.Reflection;

namespace LlmTornado.Docs.Webcil;

public class WasmWebcilWrapper
{
    private static readonly FieldInfo FieldInfoPrefix = typeof(WebcilWasmWrapper).GetField("s_wasmWrapperPrefix", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField)!;

    public static byte[] GetPrefix()
    {
#if NET7_0_OR_GREATER
        return GetPrefixValue<ReadOnlyMemory<byte>>().ToArray();
#else
        return GetPrefixValue<byte[]>();
#endif
    }

    private static T GetPrefixValue<T>()
    {
        return (T)FieldInfoPrefix.GetValue(null)!;
    }
}