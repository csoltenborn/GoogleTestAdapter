using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class TestCaseResolverTests : TestsBase
    {
        private FakeLogger _fakeLogger;

        [TestInitialize]
        public void Setup()
        {
            _fakeLogger = new FakeLogger(() => true, false);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named", 9);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named_Named", 16);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named_Anon", 25);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon", 35);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon_Anon", 42);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon_Named", 51);
        }

        private void AssertCorrectTestLocationIsFound(string suite, uint line)
        {
            var testcase = new TestCase(
                suite,
                $"{suite}.Test",
                "",
                "Test",
                $"{suite}.Test")
            { TestType = TestCase.TestTypes.Simple };

            var signatures = new MethodSignatureCreator().GetTestMethodSignatures(testcase);
            var resolver = new TestCaseResolver(TestResources.Tests_ReleaseX64, 
                new DefaultDiaResolverFactory(), MockOptions.Object, _fakeLogger);

            var testCaseLocation = resolver.FindTestCaseLocation(signatures.ToList());

            _fakeLogger.Errors.Should().BeEmpty();
            testCaseLocation.Should().NotBeNull();
            testCaseLocation.Sourcefile.Should().EndWithEquivalent(@"sampletests\tests\namespacetests.cpp");
            testCaseLocation.Line.Should().Be(line);
        }
    }

}