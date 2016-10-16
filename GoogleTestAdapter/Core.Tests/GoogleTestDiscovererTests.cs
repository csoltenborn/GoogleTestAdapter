using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestDiscovererTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_MatchingExamples_AreMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTest.exe", true);
            AssertIsGoogleTestExecutable("mygoogletests.exe", true);
            AssertIsGoogleTestExecutable("mygoogletest.exe", true);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_NotMatchingExamples_AreNotMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTes.exe", false);
            AssertIsGoogleTestExecutable("TotallyWrong.exe", false);
            AssertIsGoogleTestExecutable("TestStuff.exe", false);
            AssertIsGoogleTestExecutable("TestLibrary.exe", false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_WithRegexFromOptions_MatchesCorrectly()
        {
            AssertIsGoogleTestExecutable("SomeWeirdExpression", true, "Some.*Expression");
            AssertIsGoogleTestExecutable("SomeWeirdOtherThing", false, "Some.*Expression");
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", false, "Some.*Expression");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_WithUnparsableRegexFromOptions_ProducesErrorMessage()
        {
            bool result = new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable("my.exe", "d[ddd[");

            result.Should().BeFalse();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("'d[ddd['"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_StaticallyLinkedX86Executable_FindsTestsWitLocation()
        {
            FindStaticallyLinkedTests(TestResources.X86StaticallyLinkedTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsTestsWithLocation()
        {
            FindSampleTests(TestResources.SampleTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_ExternallyLinkedX86Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(TestResources.X86ExternallyLinkedTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_WithPathExtension_FindsTestsWithLocation()
        {
            MockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.ExecutableDirPlaceholder + @"\..\lib");

            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.PathExtensionTestsExe);
            testCases.Count.Should().Be(TestResources.NrOfPathExtensionTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_WithoutPathExtension_ProducesWarning()
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.PathExtensionTestsExe);

            testCases.Count.Should().Be(0);
            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.StartsWith("Could not list test cases of executable"))));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_ExternallyLinkedX64Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(TestResources.X64ExternallyLinkedTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTests()
        {
            AssertFindsTest(
                "InstantiationName/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]");

            AssertFindsTest(
                "PointerParameterizedTests.CheckStringLength/2",
                new Regex("PointerParameterizedTests.CheckStringLength/2 ..[0-9A-F]+ pointing to .ooops., 23.."));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTests()
        {
            AssertFindsTest(
                "Arr/TypeParameterizedTests/0.CanIterate",
                "Arr/TypeParameterizedTests/0.CanIterate<std::array<int,3> >");

            AssertFindsTest(
                "TypedTests/2.CanDefeatMath",
                "TypedTests/2.CanDefeatMath<MyStrangeArray>");

            AssertFindsTest(
                "PrimitivelyTypedTests/0.CanHasBigNumbers",
                "PrimitivelyTypedTests/0.CanHasBigNumbers<signed char>");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTestsWithUmlauts()
        {
            AssertFindsTest(
                "ÜnstanceName/ParameterizedTästs.Täst/0",
                "ÜnstanceName/ParameterizedTästs.Täst/0 [(1,ÄÖÜäöüß)]");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsTestsWithUmlauts()
        {
            AssertFindsTest(
                "Ümlautß.Täst",
                "Ümlautß.Täst");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsFixtureTestsWithUmlauts()
        {
            AssertFindsTest(
                "TheFixtüre.Täst",
                "TheFixtüre.Täst");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTestsWithUmlauts()
        {
            AssertFindsTest(
                "ÜmlautTypedTests/0.Täst",
                "ÜmlautTypedTests/0.Täst<ImplementationA>");
            AssertFindsTest(
                "ÜmlautTypedTests/1.Täst",
                "ÜmlautTypedTests/1.Täst<ImplementationB>");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_ParseSymbolInformation_DiaResolverIsCreated()
        {
            var mockFactory = new Mock<IDiaResolverFactory>();
            var mockResolver = new Mock<IDiaResolver>();
            mockFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>(), false)).Returns(mockResolver.Object);
            mockResolver.Setup(r => r.GetFunctions(It.IsAny<string>())).Returns(new List<SourceFileLocation>());

            new GoogleTestDiscoverer(TestEnvironment, mockFactory.Object).GetTestsFromExecutable(TestResources.SampleTests);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>(), false), Times.AtLeastOnce);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_DiaIsNotInvoked()
        {
            var mockFactory = new Mock<IDiaResolverFactory>();
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment, mockFactory.Object)
                .GetTestsFromExecutable(TestResources.SampleTests);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>(), false), Times.Never);
            testCases.Count.Should().Be(TestResources.NrOfSampleTests);
            foreach (TestCase testCase in testCases)
            {
                testCase.CodeFilePath.Should().Be("");
                testCase.LineNumber.Should().Be(0);
                testCase.Source.Should().Be(TestResources.SampleTests);
                testCase.DisplayName.Should().NotBeNullOrEmpty();
                testCase.FullyQualifiedName.Should().NotBeNullOrEmpty();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_LoadTests_AllTestsAreFound()
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests);

            testCases.Count.Should().Be(5000);
            for (int i = 0; i < 5000; i++)
            {
                string fullyQualifiedName = $"LoadTests.Test/{i}";
                bool contains = testCases.Any(tc => tc.FullyQualifiedName == fullyQualifiedName);
                contains.Should().BeTrue($" Test not found: {fullyQualifiedName}");
            }
        }

        [TestMethod]
        [TestCategory(Load)]
        public void GetTestsFromExecutable_LoadTests_AreFoundInReasonableTime()
        {
            var stopwatch = Stopwatch.StartNew();
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests);
            stopwatch.Stop();
            var actualDuration = stopwatch.Elapsed;

            int testParsingDurationInMs = CiSupport.GetWeightedDuration(0.5 * testCases.Count); // .5ms per test (discovery and processing)
            int overheadInMs = CiSupport.GetWeightedDuration(1000); // pretty much arbitrary - let's see...
            var maxDuration = TimeSpan.FromMilliseconds(testParsingDurationInMs + overheadInMs);

            actualDuration.Should().BeLessThan(maxDuration);
        }

        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable(executable, regex)
                .Should()
                .Be(isGoogleTestExecutable);
        }

        private void FindSampleTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Count.Should().Be(TestResources.NrOfSampleTests);

            TestCase testCase =
               testCases.Single(tc => tc.FullyQualifiedName == "Arr/TypeParameterizedTests/1.CanDefeatMath");

            testCase.DisplayName.Should().Be("Arr/TypeParameterizedTests/1.CanDefeatMath<MyStrangeArray>");
            testCase.CodeFilePath.Should().EndWith(@"sampletests\tests\typeparameterizedtests.cpp");
            testCase.LineNumber.Should().Be(53);
        }

        private void FindStaticallyLinkedTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Count.Should().Be(2);

            testCases[0].DisplayName.Should().Be("FooTest.MethodBarDoesAbc");
            testCases[0].CodeFilePath.Should().Be(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp");
            testCases[0].LineNumber.Should().Be(36);

            testCases[1].DisplayName.Should().Be("FooTest.DoesXyz");
            testCases[1].CodeFilePath.Should().Be(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp");
            testCases[1].LineNumber.Should().Be(45);
        }

        private void FindExternallyLinkedTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Count.Should().Be(2);

            testCases[0].DisplayName.Should().Be("BarTest.MethodBarDoesAbc");
            testCases[0].CodeFilePath.Should().Be(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp");
            testCases[0].LineNumber.Should().Be(36);

            testCases[1].DisplayName.Should().Be("BarTest.DoesXyz");
            testCases[1].CodeFilePath.Should().Be(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp");
            testCases[1].LineNumber.Should().Be(44);
        }

        private void AssertFindsTest(string fullyQualifiedName, string displayName)
        {
            AssertFindsTest(fullyQualifiedName, new Regex(Regex.Escape(displayName)));
        }

        // ReSharper disable once UnusedParameter.Local
        private void AssertFindsTest(string fullyQualifiedName, Regex displayNameRegex)
        {
            TestResources.SampleTests.AsFileInfo()
                .Should().Exist("building the SampleTests solution produces that executable");

            var discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> tests = discoverer.GetTestsFromExecutable(TestResources.SampleTests);

            TestCase testCase = tests.Single(t => t.FullyQualifiedName == fullyQualifiedName);
            testCase.DisplayName.Should().MatchRegex(displayNameRegex.ToString());
        }

    }

}