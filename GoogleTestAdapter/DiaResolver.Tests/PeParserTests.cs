using System;
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
        public void ParseImportedFunctions_X86AndX64Exe_FunctionsImportedFromDllAreFound()
        {
            foreach (string executable in new[] { TestResources.DllTests_ReleaseX86, TestResources.DllTests_ReleaseX64 })
            {
                var functions = PeParser.ParseImportedFunctions(executable, MockLogger.Object);

                functions.Should().Contain("?ReturnZero@@YAHXZ", executable);
                functions.Should().Contain("GetCurrentProcessId", executable);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void FindImportedFunction_PredicateMatches_SearchStops()
        {
            int nrOfCalls = 0;
            PeParser.FindImportedFunction(TestResources.DllTests_ReleaseX64, f => { nrOfCalls++; return true; }, MockLogger.Object)
                .Should().BeTrue();
            nrOfCalls.Should().Be(1);

            PeParser.FindImportedFunction(TestResources.DllTests_ReleaseX64, f => false, MockLogger.Object).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void FindImportedFunction_NoPeFile_False()
        {
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "MZ but not really an executable");
                PeParser.FindImportedFunction(file, f => true, MockLogger.Object).Should().BeFalse();
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void UndecorateNameOnly_MsvcNames_QualifiedNameIsReturned()
        {
            SymbolUndecorator.UndecorateNameOnly("?GetInstance@UnitTest@testing@@SAPEAV12@XZ").Should().Be("testing::UnitTest::GetInstance");
            SymbolUndecorator.UndecorateNameOnly("??0Test@testing@@IEAA@XZ").Should().Be("testing::Test::Test");
            SymbolUndecorator.UndecorateNameOnly("?ReturnZero@@YAHXZ").Should().Be("ReturnZero");
            SymbolUndecorator.UndecorateNameOnly("GetLastError").Should().BeNull();
            SymbolUndecorator.UndecorateNameOnly("_ZN7testing4TestC2Ev").Should().BeNull();
            SymbolUndecorator.UndecorateNameOnly(null).Should().BeNull();
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

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_PathWithCharactersOutsideAnsiCodePage_IsParsed()
        {
            // Chinese and Greek characters can not be represented in any single ANSI code page
            string tempDir = Path.Combine(Path.GetTempPath(), "GTA_\u6D4B\u8BD5_\u03A9_" + Path.GetRandomFileName());
            string executable = Path.Combine(tempDir, "\u6D4B\u8BD5_\u03A9.exe");
            Directory.CreateDirectory(tempDir);
            try
            {
                File.Copy(TestResources.DllTests_ReleaseX86, executable);

                PeParser.ParseImports(executable, MockLogger.Object).Should().HaveCount(14);
                PeParser.FindImport(executable, "DllProject.dll", StringComparison.OrdinalIgnoreCase, MockLogger.Object).Should().BeTrue();
                PeParser.ExtractPdbPath(executable, MockLogger.Object).Should().Be(
                    PeParser.ExtractPdbPath(TestResources.DllTests_ReleaseX86, MockLogger.Object));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PeParser_NoPeFile_NothingIsFound()
        {
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "MZ but certainly not a PE file, which is longer than an IMAGE_DOS_HEADER");

                PeParser.ParseImports(file, MockLogger.Object).Should().BeEmpty();
                PeParser.ExtractPdbPath(file, MockLogger.Object).Should().BeNull();
            }
            finally
            {
                File.Delete(file);
            }
        }

    }

}