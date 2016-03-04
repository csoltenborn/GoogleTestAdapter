using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.DiaResolver;

namespace GoogleTestAdapter.Dia
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
            List<string> errorMessages = new List<string>();

            DiaResolver.DiaResolver resolver = new DiaResolver.DiaResolver(executable, "");
            locations.AddRange(resolver.GetFunctions(filter));
            errorMessages.AddRange(resolver.ErrorMessages);

            if (disposeResolver)
            {
                resolver.Dispose();
            }

            Assert.AreEqual(expectedLocations, locations.Count);
            Assert.AreEqual(expectedErrorMessages, errorMessages.Count);
        }

    }

}