using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace LlmTornado.Docs.Webcil;

internal static class WebcilConverterUtil
{
    private static readonly byte[] SectionHeaderText = { 0x2E, 0x74, 0x65, 0x78, 0x74, 0x00, 0x00, 0x00 }; // .text
    private static readonly byte[] SectionHeaderRsRc = { 0x2E, 0x72, 0x73, 0x72, 0x63, 0x00, 0x00, 0x00 }; // .rsrc
    private static readonly byte[] SectionHeaderReloc = { 0x2E, 0x72, 0x65, 0x6C, 0x6F, 0x63, 0x00, 0x00 }; // .reloc
    private static readonly byte[] MSDOS =
    {
        0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
        0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72, 0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
        0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E, 0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
        0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };
    private static readonly ushort[] DOSReservedWords1 = { 0, 0, 0, 0 };
    private static readonly ushort[] DOSReservedWords2 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly DateTime Epoch = new(1970, 1, 1);
    private static readonly int SizeofDOSHeader = Marshal.SizeOf<IMAGE_DOS_HEADER>(); // 64
    private static readonly int SizeofFileHeader = Marshal.SizeOf<IMAGE_FILE_HEADER>();
    private static readonly int SizeofMSDOS = MSDOS.Length; // 64
    private static readonly int SizeofNTHeaders = Marshal.SizeOf<IMAGE_NT_HEADERS32>(); // 248
    private static readonly int SizeofOptionalHeader = Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>();
    private static readonly int SizeofSectionHeader = Marshal.SizeOf<IMAGE_SECTION_HEADER>(); // 40

    private const uint FileAlignment = 0x0200;
    private const uint SectionAlignment = 0x2000;

    /// <summary>
    /// Convert a Portable Executable file into a Webcil file.
    /// </summary>
    /// <param name="inputPath">The input path for the PE file.</param>
    /// <param name="outputPath">The output path for the Webcil file.</param>
    /// <param name="wrapInWebAssembly">The Webcil should be wrapped in Wasm [default value is <c>true</c>].</param>
    public static void ConvertToWebcil(string inputPath, string outputPath, bool wrapInWebAssembly = true)
    {
        var webcilConverter = WebcilConverter.FromPortableExecutable(inputPath, outputPath);
        webcilConverter.WrapInWebAssembly = wrapInWebAssembly;

        webcilConverter.ConvertToWebcil();
    }

    /// <summary>
    /// Convert a Webcil stream into a Portable Executable which can be used to create a valid <see cref="MetadataReference"/>.
    /// </summary>
    /// <param name="inputStream">The input sStream.</param>
    /// <param name="wrappedInWebAssembly">The Webcil is wrapped in Wasm [default value is <c>true</c>].</param>
    /// <returns>A byte[] Portable Executable</returns>
    public static byte[] ConvertFromWebcil(Stream inputStream, bool wrappedInWebAssembly = true)
    {
        Stream webcilStream;
        if (wrappedInWebAssembly)
        {
            using var unwrapper = new WasmWebcilUnwrapper(inputStream);
            webcilStream = new MemoryStream();
            unwrapper.WriteUnwrapped(webcilStream);

            webcilStream.Flush();
            webcilStream.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            webcilStream = inputStream;
        }

        // These are Webcil variables
        var webcilHeader = ReadHeader(webcilStream);
        var webcilSectionHeaders = ReadSectionHeaders(webcilStream, webcilHeader.coff_sections);
        var webcilSectionHeadersCount = webcilSectionHeaders.Length;
        var webcilSectionHeadersSizeOfRawData = (uint)webcilSectionHeaders.Sum(x => x.SizeOfRawData);

        // These are PE (Portable Executable) variables
        int sectionStart = SizeofDOSHeader + SizeofMSDOS + SizeofNTHeaders + webcilSectionHeadersCount * SizeofSectionHeader; // 496
        int sectionStartRounded = sectionStart.RoundToNearest();
        var extraBytesAfterSections = new byte[sectionStartRounded - sectionStart];
        var pointerToRawDataFirstSectionHeader = webcilSectionHeaders[0].PointerToRawData;
        var pointerToRawDataOffsetBetweenWebcilAndPE = sectionStartRounded - pointerToRawDataFirstSectionHeader;

        using var peStream = new MemoryStream();

        var DOSHeader = new IMAGE_DOS_HEADER
        {
            MagicNumber = 0x5A4D,
            BytesOnLastPageOfFile = 0x90,
            PagesInFile = 3,
            Relocations = 0,
            SizeOfHeaderInParagraphs = 4,
            MinimumExtraParagraphs = 0,
            MaximumExtraParagraphs = 0xFFFF,
            InitialSS = 0,
            InitialSP = 0xB8,
            Checksum = 0,
            InitialIP = 0,
            InitialCS = 0,
            AddressOfRelocationTable = 0x40,
            OverlayNumber = 0,
            ReservedWords1 = DOSReservedWords1,
            OEMIdentifier = 0,
            OEMInformation = 0,
            ReservedWords2 = DOSReservedWords2,
            FileAddressOfNewExeHeader = 0x80
        };
        peStream.WriteStruct(DOSHeader);

        peStream.Write(MSDOS);

        var IMAGE_NT_HEADERS32 = new IMAGE_NT_HEADERS32
        {
            Signature = 0x4550, // 'PE'
            FileHeader = new IMAGE_FILE_HEADER
            {
                Machine = Constants.IMAGE_FILE_MACHINE_I386,
                NumberOfSections = 3,
                TimeDateStamp = GetImageTimestamp(),
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = 0x00E0,
                Characteristics = 0x0022
            },
            OptionalHeader = new IMAGE_OPTIONAL_HEADER32
            {
                Magic = 0x010B, // Signature/Magic - Represents PE32 for 32-bit (0x10b) and PE32+ for 64-bit (0x20B) 
                MajorLinkerVersion = 0x30,
                MinorLinkerVersion = 0,
                SizeOfCode = (uint)webcilSectionHeaders[0].SizeOfRawData,
                SizeOfInitializedData = (uint)(webcilSectionHeaders[1].SizeOfRawData + webcilSectionHeaders[2].SizeOfRawData),
                SizeOfUninitializedData = 0,
                AddressOfEntryPoint = 0, // This can be set to 0
                BaseOfCode = 0x2000,
                BaseOfData = 0xA000,
                ImageBase = 0x400000, // The default value for applications is 0x00400000
                SectionAlignment = SectionAlignment,
                FileAlignment = FileAlignment,
                MajorOperatingSystemVersion = 4,
                MinorOperatingSystemVersion = 0,
                MajorImageVersion = 0,
                MinorImageVersion = 0,
                MajorSubsystemVersion = 4,
                MinorSubsystemVersion = 0,
                Win32VersionValue = 0,
                SizeOfImage = webcilSectionHeadersSizeOfRawData.RoundToNearest(SectionAlignment),
                SizeOfHeaders = GetSizeOfHeaders(DOSHeader, webcilSectionHeadersCount),
                CheckSum = 0,
                Subsystem = 3, // IMAGE_SUBSYSTEM_WINDOWS_CUI
                DllCharacteristics = 0x8560,
                SizeOfStackReserve = 0x100000,
                SizeOfStackCommit = 0x1000,
                SizeOfHeapReserve = 0x100000,
                SizeOfHeapCommit = 0x1000,
                LoaderFlags = 0,
                NumberOfRvaAndSizes = 0x10,
                DataDirectory = new IMAGE_DATA_DIRECTORY[]
                {
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_EXPORT
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_IMPORT (can be 0)
                    new() { Size = (uint) webcilSectionHeaders[1].VirtualSize, VirtualAddress = (uint) webcilSectionHeaders[1].VirtualAddress }, // IMAGE_DIRECTORY_ENTRY_RESOURCE
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_EXCEPTION
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_SECURITY
                    new() { Size = (uint) webcilSectionHeaders[2].VirtualSize, VirtualAddress = (uint) webcilSectionHeaders[2].VirtualAddress }, // IMAGE_DIRECTORY_ENTRY_BASERELOC
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_DEBUG (can be 0)
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_ARCHITECTURE
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_GLOBALPTR
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_TLS
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT
                    new() { Size = 0x0008, VirtualAddress = (uint) webcilSectionHeaders[0].VirtualAddress }, // IMAGE_DIRECTORY_ENTRY_IAT
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }, // IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT
                    new() { Size = 0x0048, VirtualAddress = (uint) webcilSectionHeaders[0].VirtualAddress + 8 }, // TODO ??? IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR
                    new() { Size = 0x0000, VirtualAddress = 0x0000 }  // ?
                }
            }
        };
        peStream.WriteStruct(IMAGE_NT_HEADERS32);

        var textSectionHeader = new IMAGE_SECTION_HEADER
        {
            Name = SectionHeaderText,
            Misc = new IMAGE_SECTION_HEADER.UnionType { VirtualSize = (uint)webcilSectionHeaders[0].VirtualSize },
            VirtualAddress = (uint)webcilSectionHeaders[0].VirtualAddress,
            SizeOfRawData = (uint)webcilSectionHeaders[0].SizeOfRawData,
            PointerToRawData = webcilSectionHeaders[0].GetCorrectedPointerToRawData(pointerToRawDataOffsetBetweenWebcilAndPE),
            Characteristics = 0x60000020
        };
        peStream.WriteStruct(textSectionHeader);

        var rsrcSectionHeader = new IMAGE_SECTION_HEADER
        {
            Name = SectionHeaderRsRc,
            Misc = new IMAGE_SECTION_HEADER.UnionType { VirtualSize = (uint)webcilSectionHeaders[1].VirtualSize },
            VirtualAddress = (uint)webcilSectionHeaders[1].VirtualAddress,
            SizeOfRawData = (uint)webcilSectionHeaders[1].SizeOfRawData,
            PointerToRawData = webcilSectionHeaders[1].GetCorrectedPointerToRawData(pointerToRawDataOffsetBetweenWebcilAndPE),
            Characteristics = 0x40000040
        };
        peStream.WriteStruct(rsrcSectionHeader);

        var relocSectionHeader = new IMAGE_SECTION_HEADER
        {
            Name = SectionHeaderReloc,
            Misc = new IMAGE_SECTION_HEADER.UnionType { VirtualSize = (uint)webcilSectionHeaders[2].VirtualSize },
            VirtualAddress = (uint)webcilSectionHeaders[2].VirtualAddress,
            SizeOfRawData = (uint)webcilSectionHeaders[2].SizeOfRawData,
            PointerToRawData = webcilSectionHeaders[2].GetCorrectedPointerToRawData(pointerToRawDataOffsetBetweenWebcilAndPE),
            Characteristics = 0x42000040
        };
        peStream.WriteStruct(relocSectionHeader);

        if (extraBytesAfterSections.Length > 0)
        {
            peStream.Write(extraBytesAfterSections);
        }

        // Just copy all data
        foreach (var webcilSectionHeader in webcilSectionHeaders)
        {
            var buffer = new byte[webcilSectionHeader.SizeOfRawData];
            webcilStream.Seek(webcilSectionHeader.PointerToRawData, SeekOrigin.Begin);
            ReadExactly(webcilStream, buffer);

            peStream.Write(buffer, 0, buffer.Length);
        }

        peStream.Flush();
        peStream.Seek(0, SeekOrigin.Begin);

        return peStream.ToArray();
    }

    private static Webcil.WebcilHeader ReadHeader(Stream webcilStream)
    {
        var webcilHeader = ReadStructure<Webcil.WebcilHeader>(webcilStream);

        if (!BitConverter.IsLittleEndian)
        {
            webcilHeader.version_major = BinaryPrimitives.ReverseEndianness(webcilHeader.version_major);
            webcilHeader.version_minor = BinaryPrimitives.ReverseEndianness(webcilHeader.version_minor);
            webcilHeader.coff_sections = BinaryPrimitives.ReverseEndianness(webcilHeader.coff_sections);
            webcilHeader.pe_cli_header_rva = BinaryPrimitives.ReverseEndianness(webcilHeader.pe_cli_header_rva);
            webcilHeader.pe_cli_header_size = BinaryPrimitives.ReverseEndianness(webcilHeader.pe_cli_header_size);
            webcilHeader.pe_debug_rva = BinaryPrimitives.ReverseEndianness(webcilHeader.pe_debug_rva);
            webcilHeader.pe_debug_size = BinaryPrimitives.ReverseEndianness(webcilHeader.pe_debug_size);
        }

        return webcilHeader;
    }

    private static ImmutableArray<Webcil.WebcilSectionHeader> ReadSectionHeaders(Stream webcilStream, int sectionsHeaders)
    {
        var result = new List<Webcil.WebcilSectionHeader>();
        for (int i = 0; i < sectionsHeaders; i++)
        {
            result.Add(ReadSectionHeader(webcilStream));
        }

        return ImmutableArray.Create(result.ToArray());
    }

    private static Webcil.WebcilSectionHeader ReadSectionHeader(Stream webcilStream)
    {
        var sectionHeader = ReadStructure<Webcil.WebcilSectionHeader>(webcilStream);

        if (!BitConverter.IsLittleEndian)
        {
            sectionHeader = new Webcil.WebcilSectionHeader
            (
                virtualSize: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualSize),
                virtualAddress: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualAddress),
                sizeOfRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.SizeOfRawData),
                pointerToRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.PointerToRawData)
            );
        }

        return sectionHeader;
    }

    private static uint GetSizeOfHeaders(IMAGE_DOS_HEADER IMAGE_DOS_HEADER, int numSectionHeaders)
    {
        var soh = IMAGE_DOS_HEADER.FileAddressOfNewExeHeader + // e_lfanew member of IMAGE_DOS_HEADER
                  sizeof(uint) + // 4 byte signature
                  SizeofFileHeader +
                  SizeofOptionalHeader + // size of optional header
                  numSectionHeaders * SizeofSectionHeader // size of all section headers
        ;

        return (uint)soh.RoundToNearest();
    }

    /// <summary>
    /// The low 32 bits of the time stamp of the image.
    /// This represents the date and time the image was created by the linker.
    /// The value is represented in the number of seconds elapsed since midnight (00:00:00), January 1, 1970, Universal Coordinated Time, according to the system clock.
    /// </summary>
    private static uint GetImageTimestamp()
    {
        // Calculate the total seconds since Unix epoch
        var totalSeconds = (DateTime.UtcNow - Epoch).Ticks / TimeSpan.TicksPerSecond;

        // Convert to uint (low 32 bits)
        return (uint)totalSeconds;
    }
    
    internal static int RoundToNearest(this int number, int nearest = 512)
    {
        int remainder = number % nearest;
        int halfNearest = nearest / 2;
        return remainder >= halfNearest ? number + nearest - remainder : number - remainder;
    }

    internal static uint RoundToNearest(this uint number, uint nearest = 512)
    {
        uint remainder = number % nearest;
        uint halfNearest = nearest / 2;
        return remainder >= halfNearest ? number + nearest - remainder : number - remainder;
    }

#if NETCOREAPP2_1_OR_GREATER
    private static T ReadStructure<T>(Stream s) where T : unmanaged
    {
        T structure = default;
        unsafe
        {
            byte* p = (byte*)&structure;
            Span<byte> buffer = new Span<byte>(p, sizeof(T));
            int read = s.Read(buffer);
            if (read != sizeof(T))
            {
                throw new InvalidOperationException("Couldn't read the full structure from the stream.");
            }
        }

        return structure;
    }
#else
    private static T ReadStructure<T>(Stream s) where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        s.Read(buffer, 0, size);

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
#endif

#if NETCOREAPP2_1_OR_GREATER && !NET6_0
    private static void ReadExactly(Stream s, Span<byte> buffer)
    {
        s.ReadExactly(buffer);
    }
#else
    private static void ReadExactly(Stream s, byte[] buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = s.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            offset += read;
        }
    }
#endif
}