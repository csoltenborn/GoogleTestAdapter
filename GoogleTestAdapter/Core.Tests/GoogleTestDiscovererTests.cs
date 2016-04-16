using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void IsGoogleTestExecutable_MatchingExamples_AreMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTest.exe", true);
            AssertIsGoogleTestExecutable("mygoogletests.exe", true);
            AssertIsGoogleTestExecutable("mygoogletest.exe", true);
        }

        [TestMethod]
        public void IsGoogleTestExecutable_NotMatchingExamples_AreNotMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTes.exe", false);
            AssertIsGoogleTestExecutable("TotallyWrong.exe", false);
            AssertIsGoogleTestExecutable("TestStuff.exe", false);
            AssertIsGoogleTestExecutable("TestLibrary.exe", false);
        }

        [TestMethod]
        public void IsGoogleTestExecutable_WithRegexFromOptions_MatchesCorrectly()
        {
            AssertIsGoogleTestExecutable("SomeWeirdExpression", true, "Some.*Expression");
            AssertIsGoogleTestExecutable("SomeWeirdOtherThing", false, "Some.*Expression");
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", false, "Some.*Expression");
        }

        [TestMethod]
        public void IsGoogleTestExecutable_WithUnparsableRegexFromOptions_ProducesErrorMessage()
        {
            bool result = new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable("my.exe", "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("'d[ddd['"))), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestsFromExecutable_StaticallyLinkedX86Executable_FindsTestsWitLocation()
        {
            FindStaticallyLinkedTests(X86StaticallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTestsWithLocation()
        {
            FindSampleTests(SampleTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_ExternallyLinkedX86Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(X86ExternallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_WithPathExtension_FindsTestsWithLocation()
        {
            MockOptions.Setup(o => o.PathExtension).Returns(PathExtensionTestsDllDir);

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(PathExtensionTestsExe);
            Assert.AreEqual(NrOfSampleTests, testCases.Count);
        }

        [TestMethod]
        public void GetTestsFromExecutable_ExternallyLinkedX64Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(X64ExternallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTests()
        {
            AssertFindsParameterizedTest(
                "InstantiationName/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]");

            AssertFindsParameterizedTest(
                "PointerParameterizedTests.CheckStringLength/2",
                new Regex("PointerParameterizedTests.CheckStringLength/2 ..[0-9A-F]+ pointing to .ooops., 23.."));
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTests()
        {
            AssertFindsParameterizedTest(
                "Arr/TypeParameterizedTests/0.CanIterate",
                "Arr/TypeParameterizedTests/0.CanIterate<std::array<int,3> >");

            AssertFindsParameterizedTest(
                "TypedTests/2.CanDefeatMath",
                "TypedTests/2.CanDefeatMath<MyStrangeArray>");

            AssertFindsParameterizedTest(
                "PrimitivelyTypedTests/0.CanHasBigNumbers",
                "PrimitivelyTypedTests/0.CanHasBigNumbers<signed char>");
        }

        [TestMethod]
        public void GetTestsFromExecutable_ParseSymbolInformation_DiaResolverIsCreated()
        {
            Mock<IDiaResolverFactory> mockFactory = new Mock<IDiaResolverFactory>();
            Mock<IDiaResolver> mockResolver = new Mock<IDiaResolver>();
            mockFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>())).Returns(mockResolver.Object);
            mockResolver.Setup(r => r.GetFunctions(It.IsAny<string>())).Returns(new List<SourceFileLocation>());

            new GoogleTestDiscoverer(TestEnvironment, mockFactory.Object).GetTestsFromExecutable(SampleTests);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_DiaIsNotInvoked()
        {
            Mock<IDiaResolverFactory> mockFactory = new Mock<IDiaResolverFactory>();
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment, mockFactory.Object)
                .GetTestsFromExecutable(SampleTests);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
            Assert.AreEqual(NrOfSampleTests, testCases.Count);
            foreach (TestCase testCase in testCases)
            {
                Assert.AreEqual("", testCase.CodeFilePath);
                Assert.AreEqual(0, testCase.LineNumber);
                Assert.AreEqual(SampleTests, testCase.Source);
                Assert.IsFalse(string.IsNullOrEmpty(testCase.DisplayName));
                Assert.IsFalse(string.IsNullOrEmpty(testCase.FullyQualifiedName));
            }
        }

        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            Assert.AreEqual(isGoogleTestExecutable,
                new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable(executable, regex));
        }

        private void FindSampleTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(NrOfSampleTests, testCases.Count);

            TestCase testCase =
               testCases.Single(tc => tc.FullyQualifiedName == "Arr/TypeParameterizedTests/1.CanDefeatMath");

            Assert.AreEqual("Arr/TypeParameterizedTests/1.CanDefeatMath<MyStrangeArray>", testCase.DisplayName);
            Assert.IsTrue(testCase.CodeFilePath.EndsWith(@"sampletests\tests\typeparameterizedtests.cpp"));
            Assert.AreEqual(53, testCase.LineNumber);
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("FooTest.MethodBarDoesAbc", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(36, testCases[0].LineNumber);

            Assert.AreEqual("FooTest.DoesXyz", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(45, testCases[1].LineNumber);
        }

        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(36, testCases[0].LineNumber);

            Assert.AreEqual("BarTest.DoesXyz", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(44, testCases[1].LineNumber);
        }

        private void AssertFindsParameterizedTest(string fullyQualifiedName, string displayName)
        {
            AssertFindsParameterizedTest(fullyQualifiedName, new Regex(Regex.Escape(displayName)));
        }

        // ReSharper disable once UnusedParameter.Local
        private void AssertFindsParameterizedTest(string fullyQualifiedName, Regex displayNameRegex)
        {
            Assert.IsTrue(File.Exists(SampleTests), "Build SampleTests in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> tests = discoverer.GetTestsFromExecutable(SampleTests);

            TestCase testCase = tests.Single(t => t.FullyQualifiedName == fullyQualifiedName);
            Assert.IsTrue(displayNameRegex.IsMatch(testCase.DisplayName));
        }

    }

}