using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiaAdapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Dia
{

    [TestClass]
    public class DiaResolverTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void GetFunctions_EverythingMatches_ResultSizeIsCorrect()
        {
            DoResolveTest("*", 3378, 434);
        }

        [TestMethod]
        public void GetFunctions_TestFunctionsMatch_ResultSizeIsCorrect()
        {
            // also triggers destructor
            DoResolveTest("*" + TestCaseResolver.TraitAppendix, 18, 0, false);
        }

        [TestMethod]
        public void GetFunctions_NonMatchingFilter_NoResults()
        {
            DoResolveTest("ThisFunctionDoesNotExist", 0, 0);
        }

        private void DoResolveTest(string filter, int expectedLocations, int expectedErrorMessages, bool disposeResolver = true)
        {
            List<SourceFileLocation> locations = new List<SourceFileLocation>();
            List<string> errorMessages = new List<string>();

            DiaResolver resolver = new DiaResolver(SampleTests);
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