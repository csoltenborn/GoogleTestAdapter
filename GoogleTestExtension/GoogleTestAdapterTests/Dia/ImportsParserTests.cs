using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Dia
{
    [TestClass]
    public class ImportsParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ReadsImports()
        {
            NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(@"kernel32.dll", TestEnvironment);
            List<string> imports = parser.Imports;
            Version version = Environment.OSVersion.Version;
            if (version.Major == 6 && version.Minor == 1)
            {
                // Windows 7
                Assert.AreEqual(24, imports.Count);
            }
            else if (version.Major == 6 && version.Minor == 2 && version.Build < 9200)
            {
                // Windows 8
                Assert.AreEqual(49, imports.Count);
            }
            else if (version.Major == 6 && version.Minor == 2)
            {
                // Windows 10?
                Assert.AreEqual(62, imports.Count);
            }
            else
            {
                throw new Exception("Unknown Windows version, Major: " + version.Major + ", Minor: " + version.Minor);
            }
        }

    }

}