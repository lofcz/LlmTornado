using System.Runtime.InteropServices;

namespace LlmTornado.Docs.Webcil;

public class Webcil
{
    /// <summary>
    /// The header of a WebCIL file.
    /// </summary>
    ///
    /// <remarks>
    /// The header is a subset of the PE, COFF and CLI headers that are needed by the mono runtime to load managed assemblies.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct WebcilHeader
    {
        public fixed byte id[4]; // 'W' 'b' 'I' 'L'
        // 4 bytes
        public ushort version_major; // 0
        public ushort version_minor; // 0
        // 8 bytes

        public ushort coff_sections;
        public ushort reserved0; // 0
        // 12 bytes
        public uint pe_cli_header_rva;
        public uint pe_cli_header_size;
        // 20 bytes
        public uint pe_debug_rva;
        public uint pe_debug_size;
        // 28 bytes
    }
    
    /// <summary>
    /// This is the Webcil analog of System.Reflection.PortableExecutable.SectionHeader, but with fewer fields
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct WebcilSectionHeader
    {
        public readonly int VirtualSize;
        public readonly int VirtualAddress;
        public readonly int SizeOfRawData;
        public readonly int PointerToRawData;

        public WebcilSectionHeader(int virtualSize, int virtualAddress, int sizeOfRawData, int pointerToRawData)
        {
            VirtualSize = virtualSize;
            VirtualAddress = virtualAddress;
            SizeOfRawData = sizeOfRawData;
            PointerToRawData = pointerToRawData;
        }
    }
}