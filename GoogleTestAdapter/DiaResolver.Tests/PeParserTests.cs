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
            var imports = PeParser.ParseImports(TestResources.DllTests_ReleaseX86, MockLogger.Object);
            imports.Should().HaveCount(14);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86ExternallyLinkedDll_CorrentNumberOfImports()
        {
            var imports = PeParser.ParseImports(TestResources.DllTestsDll_ReleaseX86, MockLogger.Object);
            imports.Should().HaveCount(3);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X64StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.Tests_ReleaseX64, MockLogger.Object);
            string expectedPdb = Path.GetFullPath(Path.ChangeExtension(TestResources.Tests_ReleaseX64, ".pdb"));
            pdb.Should().Be(expectedPdb);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_X86StaticallyLinked_FindsEmbeddedPdbPath()
        {
            string pdb = PeParser.ExtractPdbPath(TestResources.LoadTests_ReleaseX86, MockLogger.Object);
            string expectedPdb = Path.GetFullPath(Path.ChangeExtension(TestResources.LoadTests_ReleaseX86, ".pdb"));
            pdb.Should().Be(expectedPdb);
        }

    }

}