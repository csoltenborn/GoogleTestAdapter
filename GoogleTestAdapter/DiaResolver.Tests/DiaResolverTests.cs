// This file has been modified by Microsoft on 8/2017.

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
        public void GetFunctions_X86_EverythingMatches_ResultSizeIsCorrect()
        {
            DoResolveTest(
                TestResources.LoadTests_ReleaseX86, 
                "*", 
                TestMetadata.VersionUnderTest == VsVersion.VS2017 ? 765 : 728, 
                111);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X86_NonMatchingFilter_NoResults()
        {
            DoResolveTest(TestResources.LoadTests_ReleaseX86, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X64_EverythingMatches_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(
                TestResources.DllTests_ReleaseX64, 
                "*", 
                TestMetadata.VersionUnderTest == VsVersion.VS2017 ? 1278 : 1250, 
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
        public void GetFunctions_ExeWithoutPdb_AttemptsToFindPdbAreLogged()
        {
            TestResources.LoadTests_ReleaseX86.AsFileInfo().Should().Exist();
            string pdb = Path.ChangeExtension(TestResources.LoadTests_ReleaseX86, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            string renamedPdb = $"{pdb}.bak";
            renamedPdb.AsFileInfo().Should().NotExist();

            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger(() => true);
            try
            {
                File.Move(pdb, renamedPdb);
                pdb.AsFileInfo().Should().NotExist();

                using (
                    IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(TestResources.LoadTests_ReleaseX86, "",
                        fakeLogger))
                {
                    locations.AddRange(resolver.GetFunctions("*"));
                }
            }
            finally
            {
                File.Move(renamedPdb, pdb);
                pdb.AsFileInfo().Should().Exist();
            }

            locations.Count.Should().Be(0);
            fakeLogger.Warnings
                .Should()
                .Contain(msg => msg.Contains("Couldn't find the .pdb file"));
            fakeLogger.Infos
                .Should()
                .Contain(msg => msg.Contains("Attempts to find PDB"));
        }

        private void DoResolveTest(string executable, string filter, int expectedLocations, int expectedErrorMessages, bool disposeResolver = true)
        {
            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger(() => false);

            IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(executable, "", fakeLogger);
            locations.AddRange(resolver.GetFunctions(filter));

            if (disposeResolver)
            {
                resolver.Dispose();
            }

            locations.Count.Should().Be(expectedLocations);
            fakeLogger.GetMessages(Severity.Warning, Severity.Error).Count.Should().Be(expectedErrorMessages);
        }

    }

}