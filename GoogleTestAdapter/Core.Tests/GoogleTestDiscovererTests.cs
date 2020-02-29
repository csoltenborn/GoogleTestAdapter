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
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
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
        public void VerifyExecutableTrust_TestsResources_AreVerified()
        {
            VerifyExecutableIsTrusted(TestResources.SemaphoreExe);
            VerifyExecutableIsTrusted(TestResources.Tests_ReleaseX86);
            VerifyExecutableIsTrusted(TestResources.Tests_DebugX86);
            VerifyExecutableIsTrusted(TestResources.Tests_ReleaseX64);
            VerifyExecutableIsTrusted(TestResources.Tests_DebugX64);
            VerifyExecutableIsTrusted(TestResources.Tests_ReleaseX64);
            VerifyExecutableIsTrusted(TestResources.Tests_DebugX86_Gtest170);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_ReleaseX86);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_DebugX86);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_ReleaseX64);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_DebugX64);
            VerifyExecutableIsTrusted(TestResources.AlwaysCrashingExe);
            VerifyExecutableIsTrusted(TestResources.AlwaysFailingExe);
        }

        private void VerifyExecutableIsTrusted(string executable)
        {
            executable = Path.GetFullPath(executable);
            executable.AsFileInfo().Should().Exist();
            GoogleTestDiscoverer.VerifyExecutableTrust(executable, MockOptions.Object, MockLogger.Object)
                .Should().BeTrue($"'{executable}' is built by us");
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
        [TestCategory(Unit)]
        public void IsGoogleTestExecutable_WithSlowRegex_TimesOutAndProducesErrorMessage()
        {
            // regex from https://stackoverflow.com/questions/9687596/slow-regex-performance
            string slowRegex = "\"(([^\\\\\"]*)(\\\\.)?)*\"";

            var stopwatch = Stopwatch.StartNew();
            bool result = GoogleTestDiscoverer.IsGoogleTestExecutable(
                "\"This is an unterminated string and takes FOREVER to match", 
                slowRegex, 
                TestEnvironment.Logger);
            stopwatch.Stop();

            result.Should().BeFalse();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains($"'{slowRegex}'") && s.Contains("timed out"))), Times.Exactly(1));
            stopwatch.Elapsed.Should().BeCloseTo(GoogleTestDiscoverer.RegexTimeout, 250);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTestsDebug_FindsTestsWithLocation()
        {
            FindTests(TestResources.Tests_DebugX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SampleTestsDebugWithExitCodeTest_FindsTestsWithLocationAndExitCodeTest()
        {
            string testCaseName = "ExitCode";
            MockOptions.Setup(o => o.ExitCodeTestCase).Returns(testCaseName);

            var testCases = FindTests(TestResources.Tests_DebugX86, TestResources.NrOfTests + 1);

            string finalName = testCaseName + "." + Path.GetFileName(TestResources.Tests_DebugX86).Replace(".", "_");
            var exitCodeTestCase = testCases.Single(tc => tc.FullyQualifiedName == finalName);
            exitCodeTestCase.DisplayName.Should().Be(finalName);
            exitCodeTestCase.Source.Should().Be(TestResources.Tests_DebugX86);
            exitCodeTestCase.CodeFilePath.Should().ContainEquivalentOf(@"sampletests\tests\main.cpp");
            exitCodeTestCase.LineNumber.Should().Be(8);

            MockLogger.Verify(l => l.DebugInfo(It.Is<string>(msg => msg.Contains("Exit code") && msg.Contains("ignored"))), Times.Once);
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
            FindTests(TestResources.Tests_DebugX86_Gtest170, TestResources.NrOfGtest170CompatibleTests);
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
                MockOptions.Setup(o => o.PathExtension).Returns(PlaceholderReplacer.ExecutableDirPlaceholder + @"\..\dll");

                var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
                IList<TestCase> testCases = discoverer.GetTestsFromExecutable(targetExe);

                testCases.Should().HaveCount(TestResources.NrOfDllTests);
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

                testCases.Should().BeEmpty();
                MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.StartsWith("Could not list test cases of executable"))));
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

            new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options, new ProcessExecutorFactory(), mockFactory.Object).GetTestsFromExecutable(TestResources.Tests_DebugX86);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.AtLeastOnce);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_DiaIsNotInvoked()
        {
            var mockFactory = new Mock<IDiaResolverFactory>();
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options, new ProcessExecutorFactory(), mockFactory.Object)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
            testCases.Should().HaveCount(TestResources.NrOfTests);
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
        public void GetTestsFromExecutable_LoadTests_AllTestsAreFound()
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests_ReleaseX86);

            testCases.Should().HaveCount(5000);
            for (int i = 0; i < 5000; i++)
            {
                string fullyQualifiedName = $"LoadTests.Test/{i}";
                bool contains = testCases.Any(tc => tc.FullyQualifiedName == fullyQualifiedName);
                contains.Should().BeTrue($" Test not found: {fullyQualifiedName}");
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_Tests_TestNamesBeingPrefixesAreFoundWithCorrectSourceLocation()
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.Tests_ReleaseX86);

            var abcdTest = testCases.Single(tc => tc.DisplayName == "abcd.t");
            abcdTest.LineNumber.Should().Be(167);
            var bbcdTest = testCases.Single(tc => tc.DisplayName == "bbcd.t");
            bbcdTest.LineNumber.Should().Be(172);
            var bcdTest = testCases.Single(tc => tc.DisplayName == "bcd.t");
            bcdTest.LineNumber.Should().Be(177);
        }

        [TestMethod]
        [TestCategory(Load)]
        public void GetTestsFromExecutable_LoadTests_AreFoundInReasonableTime()
        {
            if (CiSupport.IsRunningOnBuildServer)
            {
                Assert.Inconclusive("Skipping test since it is unstable on the build server");
            }

            var stopwatch = Stopwatch.StartNew();
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests_Generated);
            stopwatch.Stop();
            var actualDuration = stopwatch.Elapsed;

            testCases.Count.Should().BePositive();
            int testParsingDurationInMs = CiSupport.GetWeightedDuration(testCases.Count); // .5ms per test (discovery and processing)
            int overheadInMs = CiSupport.GetWeightedDuration(1000); // pretty much arbitrary - let's see...
            var maxDuration = TimeSpan.FromMilliseconds(testParsingDurationInMs + overheadInMs);

            actualDuration.Should().BeLessThan(maxDuration);
        }

        [TestMethod]
        [TestCategory(Load)]
        public void GetTestsFromExecutable_OldExecutionEnvironment_LoadTests_AreFoundInReasonableTime()
        {
            if (CiSupport.IsRunningOnBuildServer)
            {
                Assert.Inconclusive("Skipping test since it is unstable on the build server");
            }

            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);

            var stopwatch = Stopwatch.StartNew();
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(TestResources.LoadTests_Generated);
            stopwatch.Stop();
            var actualDuration = stopwatch.Elapsed;

            testCases.Count.Should().BePositive();
            int testParsingDurationInMs = CiSupport.GetWeightedDuration(testCases.Count); // .5ms per test (discovery and processing)
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

        private IList<TestCase> FindTests(string location, int expectedNrOfTestCases = TestResources.NrOfTests)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Should().HaveCount(expectedNrOfTestCases);

            TestCase testCase = testCases.Single(tc => tc.FullyQualifiedName == "TheFixture.AddFails");
            testCase.DisplayName.Should().Be("TheFixture.AddFails");
            testCase.CodeFilePath.Should().EndWithEquivalent(@"sampletests\tests\fixturetests.cpp");
            testCase.LineNumber.Should().Be(11);

            testCase = testCases.Single(tc => tc.FullyQualifiedName == "Arr/TypeParameterizedTests/1.CanDefeatMath");
            testCase.DisplayName.Should().Be("Arr/TypeParameterizedTests/1.CanDefeatMath<MyStrangeArray>");
            testCase.CodeFilePath.Should().EndWithEquivalent(@"sampletests\tests\typeparameterizedtests.cpp");
            testCase.LineNumber.Should().Be(56);

            return testCases;
        }

        private void FindExternallyLinkedTests(string location)
        {
            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            testCases.Should().HaveCount(2);

            string expectedCodeFilePath = Path.GetFullPath($@"{TestResources.SampleTestsSolutionDir}dlldependentproject\dlltests.cpp").ToLower();
            testCases[0].DisplayName.Should().Be("Passing.InvokeFunction");
            testCases[0].CodeFilePath.Should().BeEquivalentTo(expectedCodeFilePath);
            testCases[0].LineNumber.Should().Be(5);

            testCases[1].DisplayName.Should().Be("Failing.InvokeFunction");
            testCases[1].CodeFilePath.Should().BeEquivalentTo(expectedCodeFilePath);
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

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockLogger.Verify(l => l.DebugError(It.IsAny<string>()), Times.Never);
            tests.Should().NotBeEmpty();

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