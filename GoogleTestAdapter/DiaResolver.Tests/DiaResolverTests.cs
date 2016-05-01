using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.DiaResolver
{

    [TestClass]
    public class DiaResolverTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void GetFunctions_SampleTests_TestFunctionsMatch_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(SampleTests, "*_GTA_TRAIT", 54, 0, false);
        }

        [TestMethod]
        public void GetFunctions_X86_EverythingMatches_ResultSizeIsCorrect()
        {
            DoResolveTest(X86StaticallyLinkedTests, "*", 5125, 356);
        }

        [TestMethod]
        public void GetFunctions_X86_NonMatchingFilter_NoResults()
        {
            DoResolveTest(X86StaticallyLinkedTests, "ThisFunctionDoesNotExist", 0, 0);
        }

        [TestMethod]
        public void GetFunctions_X64_EverythingMatches_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest(X64ExternallyLinkedTests, "*", 1232, 61, false);
        }

        [TestMethod]
        public void GetFunctions_X64_NonMatchingFilter_NoResults()
        {
            DoResolveTest(X64ExternallyLinkedTests, "ThisFunctionDoesNotExist", 0, 0);
        }

        private void DoResolveTest(string executable, string filter, int expectedLocations, int expectedErrorMessages, bool disposeResolver = true)
        {
            List<SourceFileLocation> locations = new List<SourceFileLocation>();
            FakeLogger fakeLogger = new FakeLogger();

            IDiaResolver resolver = DefaultDiaResolverFactory.Instance.Create(executable, "", fakeLogger);
            locations.AddRange(resolver.GetFunctions(filter));

            if (disposeResolver)
            {
                resolver.Dispose();
            }

            Assert.AreEqual(expectedLocations, locations.Count);
            Assert.AreEqual(expectedErrorMessages, fakeLogger.MessagesOfType(Severity.Warning, Severity.Error).Count);
        }

    }

}