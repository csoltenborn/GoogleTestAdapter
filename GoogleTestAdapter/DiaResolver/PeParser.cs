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

            [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern bool MapAndLoad(string imageName, string dllPath, LOADED_IMAGE* loadedImage, bool dotDll, bool readOnly);

            [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)]
            public static extern bool UnMapAndLoad(ref LOADED_IMAGE loadedImage);

            [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
            public static extern void* ImageDirectoryEntryToData(IntPtr pBase, byte mappedAsImage, ushort directoryEntry, uint* size);

            [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
            public static extern IntPtr ImageRvaToVa(IntPtr pNtHeaders, IntPtr pBase, uint rva, IntPtr pLastRvaSection);
        }

        private static void ParsePeFile(string executable, ILogger logger, Action<LOADED_IMAGE> action)
        {
            LOADED_IMAGE image = new LOADED_IMAGE();
            bool loaded = false;
            try
            {
                loaded = NativeMethods.MapAndLoad(executable, null, &image, true, true);
                if(loaded)
                    action(image);
            }
            finally
            {
                if (loaded && !NativeMethods.UnMapAndLoad(ref image))
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

        private static string GetString(LOADED_IMAGE image, uint offset)
        {
            IntPtr stringPtr = NativeMethods.ImageRvaToVa(image.FileHeader, image.MappedAddress, offset, IntPtr.Zero);
            return Marshal.PtrToStringAnsi(stringPtr);
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
                    pdbPath = Marshal.PtrToStringAnsi(new IntPtr(&pdbInfo->PdbFileName));
                }
            });

            return pdbPath;
        }
    }
}