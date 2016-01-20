using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.DiaResolver;

namespace GoogleTestAdapter.Dia
{
    [TestClass]
    public class ImportsParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void Imports_X86ExternallyLinkedExe_CorrentNumberOfImports()
        {
            NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(X86ExternallyLinkedTests, new List<string>());
            Assert.AreEqual(2, parser.Imports.Count);
        }

        [TestMethod]
        public void Imports_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(X86ExternallyLinkedTestsDll, new List<string>());
            Assert.AreEqual(1, parser.Imports.Count);
        }

    }

}