// This file has been modified by Microsoft on 9/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestDiscovererTests : TestsBase
    {
        private readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();

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
            bool result = GoogleTestDiscoverer.IsGoogleTestExecutable("my.exe", "d[ddd[", TestEnvironment.Logger);

            result.Should().BeFalse();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("'d[ddd['"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_WithIndicatorFile_IsRecognizedAsTestExecutable()
        {
            string testExecutable = SetupIndicatorFileTest(true);
            try
            {
                bool result = GoogleTestDiscoverer
                    .IsGoogleTestExecutable(testExecutable, "", TestEnvironment.Logger);

                result.Should().BeTrue();
            }
            finally
            {
                string errorMessage;
                Utils.DeleteDirectory(Path.GetDirectoryName(testExecutable), out errorMessage).Should().BeTrue();
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_WithoutIndicatorFile_IsRecognizedAsTestExecutable()
        {
            string testExecutable = SetupIndicatorFileTest(false);
            try
            {
                bool result = GoogleTestDiscoverer
                    .IsGoogleTestExecutable(testExecutable, "", TestEnvironment.Logger);

                result.Should().BeTrue();
            }
            finally
            {
                string errorMessage;
                Utils.DeleteDirectory(Path.GetDirectoryName(testExecutable), out errorMessage).Should().BeTrue();
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_DependingOnGtestDll_IsRecognizedAsTestExecutable()
        {
            bool result = GoogleTestDiscoverer
                .IsGoogleTestExecutable(TestResources.FakeGtestDllExe, "", TestEnvironment.Logger);

            result.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_DependingOnGtestDllX64_IsRecognizedAsTestExecutable()
        {
            bool result = GoogleTestDiscoverer
                .IsGoogleTestExecutable(TestResources.FakeGtestDllExeX64, "", TestEnvironment.Logger);

            result.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTestsDebug_FindsTestsWithLocation()
        {
            FindTests(TestResources.Tests_DebugX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTestsRelease_FindsTestsWithLocation()
        {
            FindTests(TestResources.Tests_ReleaseX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTests170_FindsTestsWithLocation()
        {
            FindTests(TestResources.Tests_DebugX86_Gtest170);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_ExternallyLinkedX86Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(TestResources.DllTests_ReleaseX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_WithPathExtension_FindsTestsWithLocation()
        {
            string baseDir = TestDataCreator.PreparePathExtensionTest();
            try
            {
                string targetExe = Path.Combine(baseDir, "exe", Path.GetFileName(TestResources.DllTests_ReleaseX86));
                MockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.ExecutableDirPlaceholder + @"\..\dll");

                var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
                IList<TestCase> testCases = discoverer.GetTestsFromExecutable(targetExe);

                testCases.Count.Should().Be(TestResources.NrOfDllTests);
            }
            finally
            {
                Utils.DeleteDirectory(baseDir).Should().BeTrue();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_WithoutPathExtension_ProducesWarning()
        {
            string baseDir = TestDataCreator.PreparePathExtensionTest();
            try
            {
                string targetExe = TestDataCreator.GetPathExtensionExecutable(baseDir);

                var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
                IList<TestCase> testCases = discoverer.GetTestsFromExecutable(targetExe);

                testCases.Count.Should().Be(0);
                MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.StartsWith("Could not list test cases for executable"))));
            }
            finally
            {
                Utils.DeleteDirectory(baseDir).Should().BeTrue();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_ExternallyLinkedX64Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(TestResources.DllTests_ReleaseX64);
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
            mockFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>())).Returns(mockResolver.Object);
            mockResolver.Setup(r => r.GetFunctions(It.IsAny<string>())).Returns(new List<SourceFileLocation>());

            new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options, mockFactory.Object).GetTestsFromExecutable(TestResources.Tests_DebugX86);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.AtLeastOnce);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_DiaIsNotInvoked()
        {
            var mockFactory = new Mock<IDiaResolverFactory>();
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options, mockFactory.Object)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
            testCases.Count.Should().Be(TestResources.NrOfTests);
            foreach (TestCase testCase in testCases)
            {
                testCase.CodeFilePath.Should().Be("");
                testCase.LineNumber.Should().Be(0);
                testCase.Source.Should().Be(TestResources.Tests_DebugX86);
                testCase.DisplayName.Should().NotBeNullOrEmpty();
                testCase.FullyQualifiedName.Should().NotBeNullOrEmpty();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_TestDiscoveryParam_TestsFoundWithAdditionalDiscoveryParam()
        {
            MockFrameworkHandle.Reset();
            MockOptions.Setup(o => o.AdditionalTestDiscoveryParam).Returns("-testDiscoveryFlag");
            MockOptions.Setup(o => o.UseNewTestExecutionFramework).Returns(true);

            List<TestCase> testCases = new List<TestCase>();

            MockFrameworkReporter.Setup(o => o.ReportTestsFound(It.IsAny<IEnumerable<TestCase>>())).Callback
            (
                (IEnumerable<TestCase> discoveredTestCases) =>
                {
                    testCases.AddRange(discoveredTestCases);
                }
            );

            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            discoverer.DiscoverTests(TestResources.TestDiscoveryParamExe.Yield(), MockFrameworkReporter.Object);

            testCases.Count.Should().Be(2);
            testCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestFails");
            testCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestPasses");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecubable_TestDiscoveryParam_TestsFoundWithAdditionalDiscoveryParam()
        { 
            MockOptions.Setup(o => o.AdditionalTestDiscoveryParam).Returns("-testDiscoveryFlag");

            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.TestDiscoveryParamExe);

            testCases.Count.Should().Be(2);
            testCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestFails");
            testCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestPasses");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_LoadTests_AllTestsAreFound()
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests_ReleaseX86);

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
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests_ReleaseX86);
            stopwatch.Stop();
            var actualDuration = stopwatch.Elapsed;

            int testParsingDurationInMs = CiSupport.GetWeightedDuration(0.5 * testCases.Count); // .5ms per test (discovery and processing)
            int overheadInMs = CiSupport.GetWeightedDuration(1000); // pretty much arbitrary - let's see...
            var maxDuration = TimeSpan.FromMilliseconds(testParsingDurationInMs + overheadInMs);

            actualDuration.Should().BeLessThan(maxDuration);
        }

        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            GoogleTestDiscoverer.IsGoogleTestExecutable(executable, regex, TestEnvironment.Logger)
                .Should()
                .Be(isGoogleTestExecutable);
        }

        private void FindTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Count.Should().Be(TestResources.NrOfTests);

            TestCase testCase = testCases.Single(tc => tc.FullyQualifiedName == "TheFixture.AddFails");
            testCase.DisplayName.Should().Be("TheFixture.AddFails");
            testCase.CodeFilePath.Should().EndWith(@"sampletests\tests\fixturetests.cpp");
            testCase.LineNumber.Should().Be(11);

            testCase = testCases.Single(tc => tc.FullyQualifiedName == "Arr/TypeParameterizedTests/1.CanDefeatMath");
            testCase.DisplayName.Should().Be("Arr/TypeParameterizedTests/1.CanDefeatMath<MyStrangeArray>");
            testCase.CodeFilePath.Should().EndWith(@"sampletests\tests\typeparameterizedtests.cpp");
            testCase.LineNumber.Should().Be(53);
        }

        private void FindExternallyLinkedTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Count.Should().Be(2);

            string expectedCodeFilePath = Path.GetFullPath($@"{TestResources.SampleTestsSolutionDir}dlldependentproject\dlltests.cpp").ToLower();
            testCases[0].DisplayName.Should().Be("Passing.InvokeFunction");
            testCases[0].CodeFilePath.Should().Be(expectedCodeFilePath);
            testCases[0].LineNumber.Should().Be(5);

            testCases[1].DisplayName.Should().Be("Failing.InvokeFunction");
            testCases[1].CodeFilePath.Should().Be(expectedCodeFilePath);
            testCases[1].LineNumber.Should().Be(10);
        }

        private void AssertFindsTest(string fullyQualifiedName, string displayName)
        {
            AssertFindsTest(fullyQualifiedName, new Regex(Regex.Escape(displayName)));
        }

        // ReSharper disable once UnusedParameter.Local
        private void AssertFindsTest(string fullyQualifiedName, Regex displayNameRegex)
        {
            TestResources.Tests_DebugX86.AsFileInfo()
                .Should().Exist("building the SampleTests solution produces that executable");

            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> tests = discoverer.GetTestsFromExecutable(TestResources.Tests_DebugX86);

            TestCase testCase = tests.Single(t => t.FullyQualifiedName == fullyQualifiedName);
            testCase.DisplayName.Should().MatchRegex(displayNameRegex.ToString());
        }

        private string SetupIndicatorFileTest(bool withIndicatorFile)
        {
            string dir = Utils.GetTempDirectory();
            string targetFile = Path.Combine(dir, "SomeWeirdName.exe");
            File.Copy(TestResources.LoadTests_ReleaseX86, targetFile);
            if (withIndicatorFile)
                File.Create(targetFile + GoogleTestDiscoverer.GoogleTestIndicator).Dispose();
            return targetFile;
        }

    }

}