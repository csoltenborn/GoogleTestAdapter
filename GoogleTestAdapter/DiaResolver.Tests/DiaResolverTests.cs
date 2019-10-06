using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.DiaResolver
{

    [TestClass]
    public class DiaResolverTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_SampleTests_TestFunctionsMatch_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(TestResources.Tests_DebugX86, "*_GTA_TRAIT", 96, 0, false);
        }
        
        [TestMethod]
        [TestCategory(Unit)]
        [SuppressMessage("ReSharper", "UnreachableCode")]
        public void GetFunctions_X86_EverythingMatches_ResultSizeIsCorrect()
        {
            DoResolveTest(
                TestResources.LoadTests_ReleaseX86, 
                "*", 
                TestMetadata.VersionUnderTest == VsVersion.VS2017 ? 628 : 728, 
                88);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X86_NonMatchingFilter_NoResults()
        {
            DoResolveTest(TestResources.LoadTests_ReleaseX86, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        [SuppressMessage("ReSharper", "UnreachableCode")]
        public void GetFunctions_X64_EverythingMatches_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(
                TestResources.DllTests_ReleaseX64, 
                "*", 
                TestMetadata.VersionUnderTest == VsVersion.VS2017 ? 1201 : 1250, 
                TestMetadata.VersionUnderTest == VsVersion.VS2017 ? 687 : 686, 
                false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X64_NonMatchingFilter_NoResults()
        {
            DoResolveTest(TestResources.DllTests_ReleaseX64, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void GetFunctions_ExeWithoutPdb_ErrorIsLogged()
        {
            string executable = TestResources.LoadTests_ReleaseX86;
            executable.AsFileInfo().Should().Exist();
            string pdb = Path.ChangeExtension(executable, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            string renamedPdb = $"{pdb}.bak";
            renamedPdb.AsFileInfo().Should().NotExist();

            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger(() => OutputMode.Verbose);
            try
            {
                File.Move(pdb, renamedPdb);
                pdb.AsFileInfo().Should().NotExist();

                using (IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(executable, pdb, fakeLogger))
                {
                    locations.AddRange(resolver.GetFunctions("*"));
                }
            }
            finally
            {
                File.Move(renamedPdb, pdb);
                pdb.AsFileInfo().Should().Exist();
            }

            locations.Should().BeEmpty();
            fakeLogger.Errors.Should().Contain(msg => msg.Contains("PDB file") && msg.Contains("does not exist"));
        }

        private void DoResolveTest(string executable, string filter, int expectedLocations, int expectedErrorMessages, bool disposeResolver = true)
        {
            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger(() => OutputMode.Info);

            string pdb = PdbLocator.FindPdbFile(executable, "", fakeLogger);
            IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(executable, pdb, fakeLogger);
            locations.AddRange(resolver.GetFunctions(filter));

            if (disposeResolver)
            {
                resolver.Dispose();
            }

            locations.Should().HaveCountGreaterOrEqualTo(expectedLocations);
            fakeLogger.GetMessages(Severity.Warning, Severity.Error).Should().HaveCountGreaterOrEqualTo(expectedErrorMessages);
        }
    }

}