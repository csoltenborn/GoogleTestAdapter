// This file has been modified by Microsoft on 4/2020.

using GoogleTestAdapter.Common;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GoogleTestAdapter.DiaResolver
{
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

    [StructLayout(LayoutKind.Sequential)]
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
    unsafe struct IMAGE_NT_HEADERS
    {
        public int Signature;
        public fixed byte FileHeader[20];
        public fixed byte OptionalHeader[224];
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

    struct LoadedImage
    {
        public IntPtr MappedAddress;
        public IntPtr FileHeader;
    }

    unsafe public static class PeParser
    {
        private static class NativeMethods
        {
            public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;
            public const ushort IMAGE_NT_SIGNATURE = 0x00004550;

            public const uint GENERIC_READ = unchecked(0x80000000);
            public const uint FILE_MAP_READ = 0x0004;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint OPEN_EXISTING = 3;
            public const uint PAGE_READONLY = 0x02;

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
                uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFileMapping(
                SafeFileHandle hFile, IntPtr lpFileMappingAttributes, uint flProtect,
                uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetFileSizeEx(SafeFileHandle hFile, out long lpFileSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr MapViewOfFile(
                SafeFileHandle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh,
                uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

            [DllImport("dbghelp.dll")]
            public static extern void* ImageDirectoryEntryToData(IntPtr pBase, byte mappedAsImage, ushort directoryEntry, uint* size);

            [DllImport("dbghelp.dll")]
            public static extern IntPtr ImageRvaToVa(IntPtr pNtHeaders, IntPtr pBase, uint rva, IntPtr pLastRvaSection);
        }

        private static bool MapAndLoad(string imageName, out LoadedImage loadedImage)
        {
            loadedImage = new LoadedImage();

            long fileSize;
            IntPtr mapAddr;
            using (var hFile = NativeMethods.CreateFile(imageName, NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (hFile.IsInvalid)
                    return false;

                if (!NativeMethods.GetFileSizeEx(hFile, out fileSize))
                    return false;

                using (var hMapping = NativeMethods.CreateFileMapping(hFile, IntPtr.Zero, NativeMethods.PAGE_READONLY, 0, 0, null))
                {
                    if (hMapping.IsInvalid)
                        return false;

                    mapAddr = NativeMethods.MapViewOfFile(hMapping, NativeMethods.FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                    if (mapAddr == IntPtr.Zero)
                        return false;
                }
            }

            unsafe
            {
                if (fileSize < Marshal.SizeOf<IMAGE_DOS_HEADER>())
                    return false;

                var dosHeader = (IMAGE_DOS_HEADER*)mapAddr;
                IMAGE_NT_HEADERS* rawFileHeader;
                if (dosHeader->e_magic == NativeMethods.IMAGE_DOS_SIGNATURE)
                {
                    if (dosHeader->e_lfanew <= 0
                        || fileSize < dosHeader->e_lfanew + Marshal.SizeOf<IMAGE_NT_HEADERS>())
                    {
                        return false;
                    }

                    rawFileHeader = (IMAGE_NT_HEADERS*)((byte*)mapAddr + dosHeader->e_lfanew);
                }
                else if (dosHeader->e_magic == NativeMethods.IMAGE_NT_SIGNATURE)
                {
                    if (fileSize < Marshal.SizeOf<IMAGE_NT_HEADERS>())
                        return false;

                    rawFileHeader = (IMAGE_NT_HEADERS*)mapAddr;
                }
                else
                {
                    return false;
                }

                if (rawFileHeader->Signature != NativeMethods.IMAGE_NT_SIGNATURE)
                    return false;

                loadedImage.MappedAddress = mapAddr;
                loadedImage.FileHeader = (IntPtr)rawFileHeader;
                return true;
            }
        }

        private static bool UnMapAndLoad(ref LoadedImage loadedImage)
        {
            if (NativeMethods.UnmapViewOfFile(loadedImage.MappedAddress))
            {
                loadedImage = new LoadedImage();
                return true;
            }
            return false;
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
                if (loaded && !UnMapAndLoad(ref image))
                    logger.LogError(Resources.UnMapLoad);
            }
        }

        private static void ProcessImports(string executable, ILogger logger, Func<string, bool> predicate)
        {
            ParsePeFile(executable, logger, (image) =>
            {
                bool shouldContinue = true;
                uint size = 0u;
                var directoryEntry = (IMAGE_IMPORT_DESCRIPTOR*)NativeMethods.ImageDirectoryEntryToData(image.MappedAddress, 0, 1, &size);
                if (directoryEntry == null)
                {
                    logger.LogError(Resources.ImageDirectoryEntryToData);
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

        public static bool FindImport(string executable, List<string> imports, StringComparison comparisonType, ILogger logger)
        {
            var found = false;
            ProcessImports(executable, logger, (currentImport) =>
            {
                foreach (var import in imports)
                    found = found || String.Compare(import, currentImport, comparisonType) == 0;

                return !found; // Continue only if not found yet.
            });
            return found;
        }

        private static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            int size = 0;
            while (Marshal.ReadByte(ptr, size) != 0)
                ++size;

            byte[] buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

        private static string GetString(LoadedImage image, uint offset)
        {
            IntPtr stringPtr = NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, offset, IntPtr.Zero);
            return PtrToStringUtf8(stringPtr);
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
                if (dbgDir == null)
                {
                    logger.LogError(Resources.ImageDirectoryEntryToData);
                }
                else if (dbgDir->SizeOfData > 0)
                {
                    var pdbInfo = (PdbInfo*)NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, dbgDir->AddressOfRawData, IntPtr.Zero);
                    pdbPath = PtrToStringUtf8(new IntPtr(&pdbInfo->PdbFileName));
                }
            });

            return pdbPath;
        }
    }
}