using System.Runtime.InteropServices;

namespace LlmTornado.Docs.Webcil;

internal static class StreamExtensions
{
    internal static void WriteStruct<T>(this Stream stream, T structData) where T : struct
    {
        var bytes = StructToBytes(structData);
        stream.Write(bytes);
    }

    private static byte[] StructToBytes<T>(T structData) where T : struct
    {
        int size = Marshal.SizeOf(structData);
        byte[] byteArray = new byte[size];
        nint ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(structData, ptr, false);
            Marshal.Copy(ptr, byteArray, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return byteArray;
    }
}