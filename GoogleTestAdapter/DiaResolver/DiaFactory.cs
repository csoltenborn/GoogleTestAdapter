// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Dia;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GoogleTestAdapter.DiaResolver
{
    internal static class DiaFactory
    {
        private const string DiaDll = "msdia140.dll";
        [ThreadStatic] private static IClassFactory DiaSourceFactory;

        private static readonly string MsdiaDllPath;
        private static readonly IntPtr MsdiaDll;

        static DiaFactory()
        {
            MsdiaDllPath = Path.Combine(GetAssemblyBaseDir(), Is32Bit() ? "x86" : "x64", DiaDll);
            MsdiaDll = NativeMethods.LoadLibrary(MsdiaDllPath);
        }

        public static IDiaDataSource CreateInstance()
        {
            if (MsdiaDll == IntPtr.Zero)
                throw new Exception($"Cannot load {MsdiaDllPath}.");

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
