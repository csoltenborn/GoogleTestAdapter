using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    [TestClass]
    public class ImportsParserTests : AbstractGoogleTestExtensionTests
    {
        
        [TestMethod]
        public void ReadsImports()
        {
            Native.ImportsParser Parser = new Native.ImportsParser(@"kernel32.dll", MockLogger.Object);
            List<string> Imports = Parser.Imports;
            Version Version = Environment.OSVersion.Version;
            if (Version.Major == 6 && Version.Minor == 1)
            {
                // Windows 7
                Assert.AreEqual(24, Imports.Count);
            }
            else if (Version.Major == 6 && Version.Minor == 2)
            {
                // Windows 8
                Assert.AreEqual(49, Imports.Count);
            }
            else
            {
                throw new Exception("Unknown Windows version, Major: " + Version.Major + ", Minor: " + Version.Minor);
            }
        }

    }

}