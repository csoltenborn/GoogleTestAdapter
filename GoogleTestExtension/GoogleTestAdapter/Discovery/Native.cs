using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct LOADED_IMAGE
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
    unsafe struct IMAGE_IMPORT_DESCRIPTOR
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


    unsafe public static class Native
    {
        [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern unsafe bool MapAndLoad(string imageName, string dllPath, LOADED_IMAGE* loadedImage, bool dotDll, bool readOnly);

        [DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern bool UnMapAndLoad(ref LOADED_IMAGE loadedImage);

        [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern unsafe IMAGE_IMPORT_DESCRIPTOR* ImageDirectoryEntryToData(IntPtr pBase, bool mappedAsImage, ushort directoryEntry, uint* size);

        [DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr ImageRvaToVa(IntPtr pNtHeaders, IntPtr pBase, uint rva, IntPtr pLastRvaSection);

        public static void ReleaseCom(object obj)
        {
            Marshal.FinalReleaseComObject(obj);
        }

        public class ImportsParser
        {
            private LOADED_IMAGE loadedImage = new LOADED_IMAGE();

            private List<string> imports = new List<string>();
            public List<string> Imports { get { return imports; } }

            public ImportsParser(string fileName, IMessageLogger logger)
            {
                fixed (LOADED_IMAGE* fixedLoadedImage = &loadedImage)
                {
                    if (MapAndLoad(fileName, null, fixedLoadedImage, true, true))
                    {
                        uint size = 0u;
                        IMAGE_IMPORT_DESCRIPTOR* directoryEntryPtr = ImageDirectoryEntryToData(fixedLoadedImage->MappedAddress, false, 1, &size);
                        IMAGE_IMPORT_DESCRIPTOR directoryEntry = *directoryEntryPtr;
                        while (directoryEntry.OriginalFirstThunk != 0u)
                        {
                            imports.Add(GetString(directoryEntry.Name));
                            directoryEntryPtr++;
                            directoryEntry = *directoryEntryPtr;
                        }
                        if (UnMapAndLoad(ref loadedImage))
                        {
                            logger.SendMessage(TestMessageLevel.Informational, "UnMapAndLoad succeeded");
                        }
                        else
                        {
                            logger.SendMessage(TestMessageLevel.Error, "UnMapAndLoad failed!");
                        }
                    }
                }
            }

            private string GetString(uint name)
            {
                IntPtr stringPtr = ImageRvaToVa(loadedImage.FileHeader, loadedImage.MappedAddress, name, IntPtr.Zero);
                return Marshal.PtrToStringAnsi(stringPtr);
            }
        }

    }

}