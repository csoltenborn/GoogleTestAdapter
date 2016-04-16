using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.DiaResolver
{
    [TestClass]
    public class ImportsParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void Imports_X86ExternallyLinkedExe_CorrentNumberOfImports()
        {
            NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(X86ExternallyLinkedTests, MockLogger.Object);
            Assert.AreEqual(2, parser.Imports.Count);
        }

        [TestMethod]
        public void Imports_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            NativeMethods.ImportsParser parser = new NativeMethods.ImportsParser(X86ExternallyLinkedTestsDll, MockLogger.Object);
            Assert.AreEqual(1, parser.Imports.Count);
        }

    }

}