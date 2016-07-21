using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.DiaResolver
{

    [TestClass]
    public class DiaResolverTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_SampleTests_TestFunctionsMatch_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(TestResources.SampleTests, "*_GTA_TRAIT", 85, 0, false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X86_EverythingMatches_ResultSizeIsCorrect()
        {
            DoResolveTest(TestResources.X86StaticallyLinkedTests, "*", 5125, 356);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X86_NonMatchingFilter_NoResults()
        {
            DoResolveTest(TestResources.X86StaticallyLinkedTests, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X64_EverythingMatches_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(TestResources.X64ExternallyLinkedTests, "*", 1232, 61, false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_X64_NonMatchingFilter_NoResults()
        {
            DoResolveTest(TestResources.X64ExternallyLinkedTests, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetFunctions_ExeWithoutPdb_AttemptsToFindPdbAreLogged()
        {
            File.Exists(TestResources.X86TestsWithoutPdb).Should().BeTrue();

            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger();

            using (
                IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(TestResources.X86TestsWithoutPdb, "",
                    fakeLogger, true))
            {
                locations.AddRange(resolver.GetFunctions("*"));
            }

            locations.Count.Should().Be(0);
            fakeLogger.MessagesOfType(Severity.Warning)
                .Should()
                .Contain(msg => msg.Contains("Couldn't find the .pdb file"));
            fakeLogger.MessagesOfType(Severity.Info)
                .Should()
                .Contain(msg => msg.Contains("Attempts to find pdb:"));
        }

        private void DoResolveTest(string executable, string filter, int expectedLocations, int expectedErrorMessages, bool disposeResolver = true)
        {
            var locations = new List<SourceFileLocation>();
            var fakeLogger = new FakeLogger();

            IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(executable, "", fakeLogger, false);
            locations.AddRange(resolver.GetFunctions(filter));

            if (disposeResolver)
            {
                resolver.Dispose();
            }

            locations.Count.Should().Be(expectedLocations);
            fakeLogger.MessagesOfType(Severity.Warning, Severity.Error).Count.Should().Be(expectedErrorMessages);
        }

    }

}