using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.DiaResolver
{
    [TestClass]
    public class PeParserTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86ExternallyLinkedExe_CorrentNumberOfImports()
        {
            var imports = PeParser.ParseImports(TestResources.X86ExternallyLinkedTests, MockLogger.Object);
            imports.Count.Should().Be(2);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            var imports = PeParser.ParseImports(TestResources.X86ExternallyLinkedTestsDll, MockLogger.Object);
            imports.Count.Should().Be(1);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X64StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.X64StaticallyLinkedTests, MockLogger.Object);
            pdb.Should().Be(@"C:\prod\gtest-1.7.0\msvc\x64\Debug\StaticallyLinkedGoogleTests.pdb");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.X86StaticallyLinkedTests, MockLogger.Object);
            pdb.Should().Be(@"C:\prod\gtest-1.7.0\msvc\Debug\StaticallyLinkedGoogleTests.pdb");
        }

    }

}