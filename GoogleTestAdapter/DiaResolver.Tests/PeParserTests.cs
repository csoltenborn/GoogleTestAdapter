using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.DiaResolver
{
    [TestClass]
    public class PeParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void PeParser_X86ExternallyLinkedExe_CorrentNumberOfImports()
        {
            var Imports = PeParser.ParseImports(X86ExternallyLinkedTests, MockLogger.Object);
            Assert.AreEqual(2, Imports.Count);
        }

        [TestMethod]
        public void PeParser_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            var Imports = PeParser.ParseImports(X86ExternallyLinkedTestsDll, MockLogger.Object);
            Assert.AreEqual(1, Imports.Count);
        }


        [TestMethod]
        public void PeParser_X64StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(X64StaticallyLinkedTests, MockLogger.Object);
            Assert.AreEqual("C:\\prod\\gtest-1.7.0\\msvc\\x64\\Debug\\StaticallyLinkedGoogleTests.pdb", pdb);
        }

        [TestMethod]
        public void PeParser_X86StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(X86StaticallyLinkedTests, MockLogger.Object);
            Assert.AreEqual("C:\\prod\\gtest-1.7.0\\msvc\\Debug\\StaticallyLinkedGoogleTests.pdb", pdb);
        }
    }

}