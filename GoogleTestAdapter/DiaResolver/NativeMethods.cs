using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    struct LOADED_IMAGE
    {
        public IntPtr ModuleName;
        public IntPtr hFile;
        public IntPtr MappedAddress;
        public IntPtr FileHeader;
        public IntPtr LastRvaSection;
        public uint NumbOfSections;
        public IntPtr FirstRvaSection;
        public uint charachteristics;
        public ushort systemImage;
        public ushort dosImage;
        public ushort readOnly;
        public ushort version;
        public IntPtr links_1;
        public IntPtr links_2;
        public uint sizeOfImage;
        public IntPtr links_3;
        public IntPtr links_4;
        public IntPtr links_5;
    }

    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_IMPORT_DESCRIPTOR
    {
        [FieldOffset(0)]
        public uint Characteristics;
        [FieldOffset(0)]
        public uint OriginalFirstThunk;

        [FieldOffset(4)]
        public uint TimeDateStamp;
        [FieldOffset(8)]
        public uint ForwarderChain;
        [FieldOffset(12)]
        public uint Name;
        [FieldOffset(16)]
        public uint FirstThunk;
    }

    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_DOS_HEADER
    {   // DOS .EXE header
        [FieldOffset(0)]
        public ushort e_magic;                     // Magic number

        [FieldOffset(2)]
        public ushort e_cblp;                      // public bytes on last page of file

        [FieldOffset(4)]
        public ushort e_cp;                        // Pages in file

        [FieldOffset(6)]
        public ushort e_crlc;                      // Relocations

        [FieldOffset(8)]
        public ushort e_cparhdr;                   // Size of header in paragraphs

        [FieldOffset(10)]
        public ushort e_minalloc;                  // Minimum extra paragraphs needed

        [FieldOffset(12)]
        public ushort e_maxalloc;                  // Maximum extra paragraphs needed

        [FieldOffset(14)]
        public ushort e_ss;                        // Initial (relative) SS value

        [FieldOffset(16)]
        public ushort e_sp;                        // Initial SP value

        [FieldOffset(18)]
        public ushort e_csum;                      // Checksum

        [FieldOffset(20)]
        public ushort e_ip;                        // Initial IP value

        [FieldOffset(22)]
        public ushort e_cs;                        // Initial (relative) CS value

        [FieldOffset(24)]
        public ushort e_lfarlc;                    // File address of relocation table

        [FieldOffset(26)]
        public ushort e_ovno;                      // Overlay number

        [FieldOffset(28)]
        public ushort e_res;                     // 4 Reserved public ushorts

        [FieldOffset(36)]
        public ushort e_oemid;                     // OEM identifier (for e_oeminfo)

        [FieldOffset(38)]
        public ushort e_oeminfo;                   // OEM information; e_oemid specific

        [FieldOffset(40)]
        public ushort e_res2;                    // Reserved public ushorts

        [FieldOffset(60)]
        public int   e_lfanew;                    // File address of new exe header
    }

    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_DATA_DIRECTORY
    {
        [FieldOffset(0)]
        public uint VirtualAddress;

        [FieldOffset(4)]
        public uint Size;
    }


    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_OPTIONAL_HEADER32
    {
        //
        // Standard fields.
        //
        [FieldOffset(0)]
        public ushort Magic;

        [FieldOffset(2)]
        public byte MajorLinkerVersion;

        [FieldOffset(3)]
        public byte MinorLinkerVersion;

        [FieldOffset(4)]
        public uint SizeOfCode;

        [FieldOffset(8)]
        public uint SizeOfInitializedData;

        [FieldOffset(12)]
        public uint SizeOfUninitializedData;

        [FieldOffset(16)]
        public uint AddressOfEntryPoint;

        [FieldOffset(20)]
        public uint BaseOfCode;

        [FieldOffset(24)]
        public uint BaseOfData;

        //
        // NT additional fields.
        //

        [FieldOffset(28)]
        public uint ImageBase;

        [FieldOffset(32)]
        public uint SectionAlignment;

        [FieldOffset(36)]
        public uint FileAlignment;

        [FieldOffset(40)]
        public ushort MajorOperatingSystemVersion;

        [FieldOffset(42)]
        public ushort MinorOperatingSystemVersion;

        [FieldOffset(44)]
        public ushort MajorImageVersion;

        [FieldOffset(46)]
        public ushort MinorImageVersion;

        [FieldOffset(48)]
        public ushort MajorSubsystemVersion;

        [FieldOffset(50)]
        public ushort MinorSubsystemVersion;

        [FieldOffset(52)]
        public uint Win32VersionValue;

        [FieldOffset(56)]
        public uint SizeOfImage;

        [FieldOffset(60)]
        public uint SizeOfHeaders;

        [FieldOffset(64)]
        public uint CheckSum;

        [FieldOffset(68)]
        public ushort Subsystem;

        [FieldOffset(70)]
        public ushort DllCharacteristics;

        [FieldOffset(72)]
        public uint SizeOfStackReserve;

        [FieldOffset(76)]
        public uint SizeOfStackCommit;

        [FieldOffset(80)]
        public uint SizeOfHeapReserve;

        [FieldOffset(84)]
        public uint SizeOfHeapCommit;

        [FieldOffset(88)]
        public uint LoaderFlags;

        [FieldOffset(92)]
        public uint NumberOfRvaAndSizes;

        [FieldOffset(96)]
        public IMAGE_DATA_DIRECTORY DataDirectory;

        //IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
    }

    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0)]
        public ushort Magic;

        [FieldOffset(2)]
        public byte MajorLinkerVersion;

        [FieldOffset(3)]
        public byte MinorLinkerVersion;

        [FieldOffset(4)]
        public uint SizeOfCode;

        [FieldOffset(8)]
        public uint SizeOfInitializedData;

        [FieldOffset(12)]
        public uint SizeOfUninitializedData;

        [FieldOffset(16)]
        public uint AddressOfEntryPoint;

        [FieldOffset(20)]
        public uint BaseOfCode;

        [FieldOffset(24)]
        public UInt64 ImageBase;

        [FieldOffset(32)]
        public uint SectionAlignment;

        [FieldOffset(36)]
        public uint FileAlignment;

        [FieldOffset(40)]
        public ushort MajorOperatingSystemVersion;

        [FieldOffset(42)]
        public ushort MinorOperatingSystemVersion;

        [FieldOffset(44)]
        public ushort MajorImageVersion;

        [FieldOffset(46)]
        public ushort MinorImageVersion;

        [FieldOffset(48)]
        public ushort MajorSubsystemVersion;

        [FieldOffset(50)]
        public ushort MinorSubsystemVersion;

        [FieldOffset(52)]
        public uint Win32VersionValue;

        [FieldOffset(56)]
        public uint SizeOfImage;

        [FieldOffset(60)]
        public uint SizeOfHeaders;

        [FieldOffset(64)]
        public uint CheckSum;

        [FieldOffset(68)]
        public ushort Subsystem;
        
        [FieldOffset(70)]
        public ushort DllCharacteristics;

        [FieldOffset(72)]
        public UInt64 SizeOfStackReserve;

        [FieldOffset(80)]
        public UInt64 SizeOfStackCommit;

        [FieldOffset(88)]
        public UInt64 SizeOfHeapReserve;

        [FieldOffset(96)]
        public UInt64 SizeOfHeapCommit;

        [FieldOffset(104)]
        public uint LoaderFlags;

        [FieldOffset(108)]
        public uint NumberOfRvaAndSizes;

        [FieldOffset(112)]
        public IMAGE_DATA_DIRECTORY DataDirectory;
       // IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
    }


    [StructLayout(LayoutKind.Explicit)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_DEBUG_DIRECTORY
    {
        [FieldOffset(0)]
        public uint Characteristics;

        [FieldOffset(4)]
        public uint TimeDateStamp;

        [FieldOffset(8)]
        public ushort MajorVersion;

        [FieldOffset(10)]
        public ushort MinorVersion;

        [FieldOffset(12)]
        public uint Type;

        [FieldOffset(16)]
        public uint SizeOfData;

        [FieldOffset(20)]
        public uint AddressOfRawData;

        [FieldOffset(24)]
        public uint PointerToRawData;
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(0)]
        public fixed byte Name[8];

        [FieldOffset(8)]
        public uint VirtualSizePhysicalAddress;

        [FieldOffset(12)]
        public uint VirtualAddress;

        [FieldOffset(16)]
        public uint SizeOfRawData;

        [FieldOffset(20)]
        public uint PointerToRawData;

        [FieldOffset(24)]
        public uint PointerToRelocations;

        [FieldOffset(28)]
        public uint PointerToLinenumbers;

        [FieldOffset(32)]
        public ushort NumberOfRelocations;

        [FieldOffset(34)]
        public ushort NumberOfLinenumbers;

        [FieldOffset(36)]
        public uint Characteristics;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PdbInfo
    {
        [FieldOffset(0)]
        public uint Signature;

        [FieldOffset(4)]
        //public fixed byte Guid[16];
        // don't care about making this an array, as
        // we don't need it anyway
        public byte Guid;

        [FieldOffset(20)]
        public uint Age;

        [FieldOffset(24)]
        public byte PdbFileName;
    }

    enum NameSearchOptions : uint
    {
        NsNone = 0x0u,
        NsfCaseSensitive = 0x1u,
        NsfCaseInsensitive = 0x2u,
        NsfFNameExt = 0x4u,
        NsfRegularExpression = 0x8u,
        NsfUndecoratedName = 0x10u
    }

    internal class NativeSourceFileLocation
    {
        /*
        Test methods: Symbol=[<namespace>::]<test_case_name>_<test_name>_Test::TestBody
        Trait methods: Symbol=[<namespace>::]<test_case_name>_<test_name>_Test::<trait name>__GTA__<trait value>_GTA_TRAIT
        */
        internal string Symbol;
        internal uint AddressSection;
        internal uint AddressOffset;
        internal uint Length;

        public override string ToString()
        {
            return Symbol;
        }
    }

    unsafe public static class NativeMethods
    {
        [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern bool MapAndLoad(string imageName, string dllPath, LOADED_IMAGE* loadedImage, bool dotDll, bool readOnly);

        [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern bool UnMapAndLoad(ref LOADED_IMAGE loadedImage);

        [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern IMAGE_IMPORT_DESCRIPTOR* ImageDirectoryEntryToData(IntPtr pBase, byte mappedAsImage, ushort directoryEntry, uint* size);

        [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr ImageRvaToVa(IntPtr pNtHeaders, IntPtr pBase, uint rva, IntPtr pLastRvaSection);

        public class ImportsParser
        {
            private LOADED_IMAGE _loadedImage = new LOADED_IMAGE();

            public List<string> Imports { get; } = new List<string>();

            public ImportsParser(string executable, ILogger logger)
            {
                fixed (LOADED_IMAGE* fixedLoadedImage = &_loadedImage)
                {
                    if (MapAndLoad(executable, null, fixedLoadedImage, true, true))
                    {
                        uint size = 0u;
                        IMAGE_IMPORT_DESCRIPTOR* directoryEntryPtr = ImageDirectoryEntryToData(fixedLoadedImage->MappedAddress, 0, 1, &size);
                        IMAGE_IMPORT_DESCRIPTOR directoryEntry = *directoryEntryPtr;
                        while (directoryEntry.OriginalFirstThunk != 0u)
                        {
                            Imports.Add(GetString(directoryEntry.Name));
                            directoryEntryPtr++;
                            directoryEntry = *directoryEntryPtr;
                        }
                        if (!UnMapAndLoad(ref _loadedImage))
                        {
                            logger.LogError("UnMapAndLoad failed!");
                        }
                    }
                }
            }

            private string GetString(uint name)
            {
                IntPtr stringPtr = ImageRvaToVa(_loadedImage.FileHeader, _loadedImage.MappedAddress, name, IntPtr.Zero);
                return Marshal.PtrToStringAnsi(stringPtr);
            }

        }

        public class PDBPathExtractor
        {
            public string pdbPath = null;
            private LOADED_IMAGE _loadedImage = new LOADED_IMAGE();

            private class SectionHeader
            {
                public long VirtualAddress;
                public long PointerToRawData;
                public long SizeOfRawData;
            }

            private long AddressToOffset(long VirtualAdress, SectionHeader[] sections)
            {
                for (int i = 0; i < sections.Length; i++)
                {
                    if (VirtualAdress > sections[i].VirtualAddress &&
                        VirtualAdress < sections[i].VirtualAddress + sections[i].SizeOfRawData)
                    {
                        return VirtualAdress - sections[i].VirtualAddress + sections[i].PointerToRawData;
                    }
                }

                return 0;
            }

            // Most windows executables contain the path to their PDB
            // in the header. This should be the most stable way to
            // determine the location and the name of the PDB.
            // 
            // This is inspired by 
            // https://deplinenoise.wordpress.com/2013/06/14/getting-your-pdb-name-from-a-running-executable-windows/
            //
            // In contrast to this blog, the virtual addresses must 
            // be converted to an offset in the executable and 32bit and
            // 64bit executables must be treated diffently
            public PDBPathExtractor(string executable, List<String> errorMessages)
            {
                const int sizeof_IMAGE_NT_HEADERS32 = 248;
                const int sizeof_IMAGE_NT_HEADERS64 = 264;
                int sizeof_IMAGE_NT_HEADERS = sizeof_IMAGE_NT_HEADERS32;

                fixed (LOADED_IMAGE* fixedLoadedImage = &_loadedImage)
                {
                    if (MapAndLoad(executable, null, fixedLoadedImage, true, true))
                    {
                        IMAGE_DOS_HEADER* dosHeader = (IMAGE_DOS_HEADER*)fixedLoadedImage->MappedAddress;
                        IntPtr fileHeader = fixedLoadedImage->MappedAddress + dosHeader->e_lfanew + 4;

                        // use the 32 Bit struct to get the magic and decide, whether
                        // the executable is 32 or 64 bit
                        IMAGE_OPTIONAL_HEADER32* optHeader32 = (IMAGE_OPTIONAL_HEADER32*)(fileHeader + 20);

                        IMAGE_DATA_DIRECTORY* directory;
                        if (optHeader32->Magic != 0x20b)
                        {
                            // this is a 32 bit executable
                            // keep using optHeader 32
                            // the debug directory is at index 6
                            directory = &(optHeader32->DataDirectory) + 6;
                        }
                        else
                        {
                            // 64 bit executable, use the appropriate
                            // structds and offset
                            sizeof_IMAGE_NT_HEADERS = sizeof_IMAGE_NT_HEADERS64;
                            IMAGE_OPTIONAL_HEADER64* optHeader64 = (IMAGE_OPTIONAL_HEADER64*)(fileHeader + 20);
                            directory = &(optHeader64->DataDirectory) + 6;
                        }

                        // get information on all sections. This is required to
                        // map addresses to correct offsets
                        uint numberOfSections = fixedLoadedImage->NumbOfSections;
                        SectionHeader[] sections = new SectionHeader[numberOfSections];
                        IMAGE_SECTION_HEADER* secHeader = (IMAGE_SECTION_HEADER*)(fileHeader - 4 + sizeof_IMAGE_NT_HEADERS);

                        for (int i = 0; i < numberOfSections; i++)
                        {
                            sections[i] = new SectionHeader();

                            sections[i].VirtualAddress = secHeader->VirtualAddress;
                            sections[i].SizeOfRawData = secHeader->SizeOfRawData;
                            sections[i].PointerToRawData = secHeader->PointerToRawData;

                            secHeader++;
                        }

                        int offset = (int)AddressToOffset((int)directory->VirtualAddress, sections);

                        IMAGE_DEBUG_DIRECTORY* dbg_dir = (IMAGE_DEBUG_DIRECTORY*)((int)fixedLoadedImage->MappedAddress + (int)offset);

                        offset = (int)AddressToOffset((int)dbg_dir->AddressOfRawData, sections);
                        PdbInfo* pdbInfo = (PdbInfo*)((int)fixedLoadedImage->MappedAddress + (int)offset);

                        byte* path = &pdbInfo->PdbFileName;

                        this.pdbPath = "";
                        while (*path != 0)
                        {
                            this.pdbPath += Convert.ToChar(*path);
                            path++;
                        }

                        if (!UnMapAndLoad(ref _loadedImage))
                        {
                            errorMessages.Add("UnMapAndLoad failed!");
                        }
                    }
                }
            }
        }
    }

}