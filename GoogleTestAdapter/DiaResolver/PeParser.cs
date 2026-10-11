using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GoogleTestAdapter.Common;
using Microsoft.Win32.SafeHandles;

namespace GoogleTestAdapter.DiaResolver
{

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    unsafe struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        public fixed ushort e_res1[4];
        public ushort e_oemid;
        public ushort e_oeminfo;
        public fixed ushort e_res2[10];
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    unsafe struct IMAGE_NT_HEADERS
    {
        public uint Signature;
        public fixed byte FileHeader[20];
        // the optional header's size differs between PE32 and PE32+, its start is all we need
        public ushort OptionalHeaderMagic;
    }

    struct LoadedImage
    {
        public IntPtr MappedAddress;
        public IntPtr FileHeader;
        public long Size;
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

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    struct IMAGE_DELAYLOAD_DESCRIPTOR
    {
        public uint Attributes;
        public uint DllNameRVA;
        public uint ModuleHandleRVA;
        public uint ImportAddressTableRVA;
        public uint ImportNameTableRVA;
        public uint BoundImportAddressTableRVA;
        public uint UnloadInformationTableRVA;
        public uint TimeDateStamp;
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

    unsafe public static class PeParser
    {
        private static class NativeMethods
        {
            // ReSharper disable once InconsistentNaming
            public const ushort IMAGE_DIRECTORY_ENTRY_IMPORT = 1;
            // ReSharper disable once InconsistentNaming
            public const ushort IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT = 13;
            // ReSharper disable once InconsistentNaming
            public const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;
            // ReSharper disable once InconsistentNaming
            public const uint DLL_ATTRIBUTE_RVA_BASED = 1;

            public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
            public const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE\0\0

            public const uint GENERIC_READ = 0x80000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint OPEN_EXISTING = 3;
            public const uint PAGE_READONLY = 0x02;
            public const uint FILE_MAP_READ = 0x0004;

            // imageHlp's MapAndLoad only accepts ANSI paths, so files are mapped "by hand" to support any path
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
                IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpFileMappingAttributes,
                uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetFileSizeEx(SafeFileHandle hFile, out long lpFileSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr MapViewOfFile(SafeFileHandle hFileMappingObject, uint dwDesiredAccess,
                uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

            [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
            public static extern void* ImageDirectoryEntryToData(IntPtr pBase, byte mappedAsImage, ushort directoryEntry, uint* size);

            [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
            public static extern IntPtr ImageRvaToVa(IntPtr pNtHeaders, IntPtr pBase, uint rva, IntPtr pLastRvaSection);
        }

        private static bool MapAndLoad(string executable, out LoadedImage image)
        {
            image = new LoadedImage();

            long fileSize;
            IntPtr mappedAddress;
            using (var file = NativeMethods.CreateFile(executable, NativeMethods.GENERIC_READ, NativeMethods.FILE_SHARE_READ,
                IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (file.IsInvalid || !NativeMethods.GetFileSizeEx(file, out fileSize) || fileSize < sizeof(IMAGE_DOS_HEADER))
                    return false;

                using (var mapping = NativeMethods.CreateFileMapping(file, IntPtr.Zero, NativeMethods.PAGE_READONLY, 0, 0, null))
                {
                    if (mapping.IsInvalid)
                        return false;

                    // the view keeps the file mapped after its handles have been closed
                    mappedAddress = NativeMethods.MapViewOfFile(mapping, NativeMethods.FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                    if (mappedAddress == IntPtr.Zero)
                        return false;
                }
            }

            var dosHeader = (IMAGE_DOS_HEADER*)mappedAddress;
            if (dosHeader->e_magic != NativeMethods.IMAGE_DOS_SIGNATURE
                || dosHeader->e_lfanew <= 0
                || fileSize < (long)dosHeader->e_lfanew + sizeof(IMAGE_NT_HEADERS)
                || ((IMAGE_NT_HEADERS*)((byte*)mappedAddress + dosHeader->e_lfanew))->Signature != NativeMethods.IMAGE_NT_SIGNATURE)
            {
                NativeMethods.UnmapViewOfFile(mappedAddress);
                return false;
            }

            image.MappedAddress = mappedAddress;
            image.FileHeader = new IntPtr((byte*)mappedAddress + dosHeader->e_lfanew);
            image.Size = fileSize;
            return true;
        }

        private static void ParsePeFile(string executable, ILogger logger, Action<LoadedImage> action)
        {
            LoadedImage image = new LoadedImage();
            bool loaded = false;
            try
            {
                loaded = MapAndLoad(executable, out image);
                if(loaded)
                    action(image);
            }
            finally
            {
                if (loaded && !NativeMethods.UnmapViewOfFile(image.MappedAddress))
                    logger.LogError("UnMapAndLoad failed!");
            }
        }

        private static void ProcessImports(string executable, ILogger logger, Func<string, bool> predicate)
        {
            ParsePeFile(executable, logger, (image) =>
            {
                bool shouldContinue = true;
                uint size = 0u;

                var directoryEntry = (IMAGE_IMPORT_DESCRIPTOR*)NativeMethods.ImageDirectoryEntryToData(image.MappedAddress, 0, NativeMethods.IMAGE_DIRECTORY_ENTRY_IMPORT, &size);
                if (directoryEntry == null)
                {
                    logger.DebugWarning($"Error while parsing imports of {executable}: {Win32Utils.GetLastWin32Error()}");
                    return;
                }

                while (shouldContinue && directoryEntry->OriginalFirstThunk != 0u)
                {
                    shouldContinue = predicate(GetString(image, directoryEntry->Name));
                    directoryEntry++;
                }
            });
        }

        public static List<string> ParseImports(string executable, ILogger logger)
        {
            var imports = new List<string>();
            ProcessImports(executable, logger, (import) =>
            {
                imports.Add(import);
                return true; // Always continue.
            });
            return imports;
        }

        public static bool FindImport(string executable, string import, StringComparison comparisonType, ILogger logger)
        {
            var found = false;
            ProcessImports(executable, logger, (currentImport) =>
            {
                found = String.Compare(import, currentImport, comparisonType) == 0;
                return !found; // Continue only if not found yet.
            });
            return found;
        }

        private static string GetString(LoadedImage image, uint offset)
        {
            IntPtr stringPtr = NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, offset, IntPtr.Zero);
            return PtrToStringUtf8(stringPtr);
        }

        // limits protect against malformed files, whose tables might not be terminated
        private const int MaxNrOfImportedFunctionsPerDll = 100000;
        private const int MaxFunctionNameLength = 4096;

        /// <summary>
        /// Calls the predicate for the names of the functions the executable imports from DLLs (including delay-loaded
        /// DLLs) until it returns true; functions imported by ordinal are skipped.
        /// </summary>
        /// <returns>True if the predicate has returned true</returns>
        public static bool FindImportedFunction(string executable, Func<string, bool> predicate, ILogger logger)
        {
            bool found = false;
            ParsePeFile(executable, logger, image =>
            {
                bool is64Bit = ((IMAGE_NT_HEADERS*)image.FileHeader)->OptionalHeaderMagic == NativeMethods.IMAGE_NT_OPTIONAL_HDR64_MAGIC;

                uint size = 0u;
                var importDescriptor = (IMAGE_IMPORT_DESCRIPTOR*)NativeMethods.ImageDirectoryEntryToData(image.MappedAddress, 0, NativeMethods.IMAGE_DIRECTORY_ENTRY_IMPORT, &size);
                if (importDescriptor != null)
                {
                    for (; !found && IsInImage(image, importDescriptor, sizeof(IMAGE_IMPORT_DESCRIPTOR)) && importDescriptor->Name != 0u; importDescriptor++)
                    {
                        // the import name table, or the import address table if the executable has been bound
                        uint nameTable = importDescriptor->OriginalFirstThunk != 0u ? importDescriptor->OriginalFirstThunk : importDescriptor->FirstThunk;
                        found = FindFunctionName(image, nameTable, is64Bit, predicate);
                    }
                }

                var delayDescriptor = (IMAGE_DELAYLOAD_DESCRIPTOR*)NativeMethods.ImageDirectoryEntryToData(image.MappedAddress, 0, NativeMethods.IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT, &size);
                if (delayDescriptor != null)
                {
                    for (; !found && IsInImage(image, delayDescriptor, sizeof(IMAGE_DELAYLOAD_DESCRIPTOR)) && delayDescriptor->DllNameRVA != 0u; delayDescriptor++)
                    {
                        // descriptors of old linkers contain addresses rather than RVAs
                        if ((delayDescriptor->Attributes & NativeMethods.DLL_ATTRIBUTE_RVA_BASED) != 0u)
                            found = FindFunctionName(image, delayDescriptor->ImportNameTableRVA, is64Bit, predicate);
                    }
                }
            });
            return found;
        }

        public static List<string> ParseImportedFunctions(string executable, ILogger logger)
        {
            var functions = new List<string>();
            FindImportedFunction(executable, function =>
            {
                functions.Add(function);
                return false; // Always continue.
            }, logger);
            return functions;
        }

        private static bool FindFunctionName(LoadedImage image, uint nameTableRva, bool is64Bit, Func<string, bool> predicate)
        {
            if (nameTableRva == 0u)
                return false;

            var thunk = (byte*)NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, nameTableRva, IntPtr.Zero);
            int thunkSize = is64Bit ? sizeof(ulong) : sizeof(uint);
            for (int i = 0; i < MaxNrOfImportedFunctionsPerDll && IsInImage(image, thunk, thunkSize); i++, thunk += thunkSize)
            {
                ulong value = is64Bit ? *(ulong*)thunk : *(uint*)thunk;
                if (value == 0u)
                    return false;

                bool importedByOrdinal = is64Bit ? (value & 0x8000000000000000UL) != 0u : (value & 0x80000000UL) != 0u;
                if (importedByOrdinal)
                    continue;

                // IMAGE_IMPORT_BY_NAME: 2 bytes hint, followed by the name
                var hintAndName = (byte*)NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, (uint)value, IntPtr.Zero);
                string name = GetBoundedString(image, hintAndName == null ? null : hintAndName + 2);
                if (name != null && predicate(name))
                    return true;
            }
            return false;
        }

        private static bool IsInImage(LoadedImage image, void* pointer, int length)
        {
            var start = (byte*)image.MappedAddress;
            var p = (byte*)pointer;
            return p != null && p >= start && p + length <= start + image.Size;
        }

        private static string GetBoundedString(LoadedImage image, byte* pointer)
        {
            if (!IsInImage(image, pointer, 1))
                return null;

            var end = (byte*)image.MappedAddress + image.Size;
            int length = 0;
            while (pointer + length < end && length < MaxFunctionNameLength && pointer[length] != 0)
                length++;
            if (pointer + length >= end || pointer[length] != 0)
                return null;

            return Encoding.ASCII.GetString(pointer, length);
        }

        // the linker stores strings (e.g. the PDB path) as UTF-8
        private static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            var bytes = (sbyte*)ptr;
            int length = 0;
            while (bytes[length] != 0)
                length++;
            return new string(bytes, 0, length, Encoding.UTF8);
        }

        // Most windows executables contain the path to their PDB
        // in the header. This should be the most stable way to
        // determine the location and the name of the PDB.
        // 
        // This is inspired by 
        // https://deplinenoise.wordpress.com/2013/06/14/getting-your-pdb-name-from-a-running-executable-windows/
        public static string ExtractPdbPath(string executable, ILogger logger)
        {
            string pdbPath = null;

            ParsePeFile(executable, logger, (image) =>
            {
                uint size = 0u;
                var dbgDir = (IMAGE_DEBUG_DIRECTORY*)NativeMethods.ImageDirectoryEntryToData(image.MappedAddress, 0, 6, &size);
                if (dbgDir != null && dbgDir->SizeOfData > 0)
                {
                    var pdbInfo = (PdbInfo*)NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, dbgDir->AddressOfRawData, IntPtr.Zero);
                    pdbPath = PtrToStringUtf8(new IntPtr(&pdbInfo->PdbFileName));
                }
            });

            return pdbPath;
        }
    }
}