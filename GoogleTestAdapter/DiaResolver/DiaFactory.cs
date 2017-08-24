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

        static DiaFactory()
        {
            string path = Path.Combine(GetAssemblyBaseDir(), Is32Bit() ? "x86" : "x64", DiaDll);
            var ptrDll = LoadLibrary(path);
            if (ptrDll == IntPtr.Zero)
                throw new Exception(String.Format(Resources.LoadError, path));
        }

        public static IDiaDataSource CreateInstance()
        {
            if (DiaSourceFactory == null)
            {
                var DiaSourceClassGuid = new Guid("e6756135-1e65-4d17-8576-610761398c3c");
                var IID_IClassFactory = typeof(IClassFactory).GUID;
                DiaSourceFactory = (IClassFactory)DllGetClassObject(ref DiaSourceClassGuid, ref IID_IClassFactory);
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

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DiaDll, ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object DllGetClassObject([In] ref Guid clsid, [In] ref Guid iid);
    }
}
