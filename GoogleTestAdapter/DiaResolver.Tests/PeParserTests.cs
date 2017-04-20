using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.DiaResolver
{
    [TestClass]
    public class PeParserTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86ExternallyLinkedExe_CorrentNumberOfImports()
        {
            var imports = PeParser.ParseImports(TestResources.X86ExternallyLinkedTests, MockLogger.Object);
            imports.Count.Should().Be(14);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            var imports = PeParser.ParseImports(TestResources.X86ExternallyLinkedTestsDll, MockLogger.Object);
            imports.Count.Should().Be(3);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X64StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.SampleTestsX64, MockLogger.Object);
            string expectedPdb = Path.GetFullPath(Path.ChangeExtension(TestResources.SampleTestsX64, ".pdb"));
            pdb.Should().Be(expectedPdb);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.LoadTests, MockLogger.Object);
            string expectedPdb = Path.GetFullPath(Path.ChangeExtension(TestResources.LoadTests, ".pdb"));
            pdb.Should().Be(expectedPdb);
        }

    }

}