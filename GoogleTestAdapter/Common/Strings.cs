// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace GoogleTestAdapter.Common
{
    public class Strings
    {
        private static IStrings _strings;

        static Strings()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GoogleTestAdapter.Common.Dynamic.dll");
            var asm = Assembly.LoadFile(path);
            var type = asm.GetType("GoogleTestAdapter.Common.Strings");
            _strings = (IStrings)Activator.CreateInstance(type);
        }

        public static IStrings Instance => _strings;
    }
}
