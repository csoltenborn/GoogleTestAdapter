// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Dia;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GoogleTestAdapter.DiaResolver
{
    internal static class DiaFactory
    {
        private const string DiaDll = "msdia140.dll";
        [ThreadStatic] private static IClassFactory DiaSourceFactory;

        private static readonly string[] MsdiaDllPaths;
        private static readonly IntPtr MsdiaDll;

        static DiaFactory()
        {
            // A 64-bit process is an x64 process (also if emulated on ARM64 Windows) or an ARM64 process, which can only
            // load the DLL of its own architecture (loading the other one fails with ERROR_BAD_EXE_FORMAT).
            string[] architectures = Is32Bit() ? new[] { "x86" } : new[] { "x64", "arm64" };
            MsdiaDllPaths = architectures.Select(architecture => Path.Combine(GetAssemblyBaseDir(), architecture, DiaDll)).ToArray();
            foreach (string msdiaDllPath in MsdiaDllPaths)
            {
                MsdiaDll = NativeMethods.LoadLibrary(msdiaDllPath);
                if (MsdiaDll != IntPtr.Zero)
                    break;
            }
        }

        public static IDiaDataSource CreateInstance()
        {
            if (MsdiaDll == IntPtr.Zero)
                throw new Exception($"Cannot load {string.Join(" or ", MsdiaDllPaths)}.");

            if (DiaSourceFactory == null)
            {
                var DiaSourceClassGuid = new Guid("e6756135-1e65-4d17-8576-610761398c3c");
                var IID_IClassFactory = typeof(IClassFactory).GUID;
                DiaSourceFactory = (IClassFactory) NativeMethods.DllGetClassObject(ref DiaSourceClassGuid, ref IID_IClassFactory);
            }

            var IID_IDiaDataSource = typeof(IDiaDataSource).GUID;
            return (IDiaDataSource)DiaSourceFactory.CreateInstance(null, IID_IDiaDataSource);
        }

        private static string GetAssemblyBaseDir()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static bool Is32Bit()
        {
            return IntPtr.Size == 4;
        }

        private static class NativeMethods
        {
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string path);

            [DllImport(DiaDll, ExactSpelling = true, PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface)]
            public static extern object DllGetClassObject([In] ref Guid clsid, [In] ref Guid iid);
        }
    }
}
