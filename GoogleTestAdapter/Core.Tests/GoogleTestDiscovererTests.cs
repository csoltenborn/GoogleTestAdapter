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
            VerifyExecutableIsTrusted(TestResources.CrashingTests_ReleaseX86);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_DebugX86);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_ReleaseX64);
            VerifyExecutableIsTrusted(TestResources.CrashingTests_DebugX64);
            VerifyExecutableIsTrusted(TestResources.AlwaysCrashingExe);
            VerifyExecutableIsTrusted(TestResources.AlwaysFailingExe);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void VerifyExecutableTrust_ExecutableOnNetworkShare_IsNotTrustedAndErrorNamesOption()
        {
            GoogleTestDiscoverer.VerifyExecutableTrust(@"\\gta-nonexisting-host\share\Tests.exe", MockOptions.Object, MockLogger.Object)
                .Should().BeFalse();

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains(SettingsWrapper.OptionSkipOriginCheck))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void VerifyExecutableTrust_ExecutableOnNetworkShareAndSkipOriginCheck_IsTrusted()
        {
            MockOptions.Setup(o => o.SkipOriginCheck).Returns(true);

            GoogleTestDiscoverer.VerifyExecutableTrust(@"\\gta-nonexisting-host\share\Tests.exe", MockOptions.Object, MockLogger.Object)
                .Should().BeTrue();

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
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
        public void IsGoogleTestFunction_FunctionsOfGoogleTestCore_AreRecognized()
        {
            // imported by a test executable linking Google Test as DLL (x64, MSVC)
            GoogleTestDiscoverer.IsGoogleTestFunction("?MakeAndRegisterTestInfo@internal@testing@@YAPEAVTestInfo@2@V?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@PEBD11UCodeLocation@12@PEBXP6AXXZ4PEAVTestFactoryBase@12@@Z").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("?GetInstance@UnitTest@testing@@SAPEAV12@XZ").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("??0Test@testing@@IEAA@XZ").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("??1Test@testing@@UEAA@XZ").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("??4AssertHelper@internal@testing@@QEBAXAEBVMessage@2@@Z").Should().BeTrue();
            // x86
            GoogleTestDiscoverer.IsGoogleTestFunction("?GetInstance@UnitTest@testing@@SAPAV12@XZ").Should().BeTrue();
            // MinGW, clang
            GoogleTestDiscoverer.IsGoogleTestFunction("_ZN7testing8UnitTest11GetInstanceEv").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("_ZN7testing8internal23MakeAndRegisterTestInfoEPKcS2_S2_S2_NS0_12CodeLocationEPKvPFvvES7_PNS0_15TestFactoryBaseE").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("_ZN7testing4TestC2Ev").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestFunction("__ZN7testing4TestD2Ev").Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestFunction_OtherFunctions_AreNotRecognized()
        {
            GoogleTestDiscoverer.IsGoogleTestFunction("GetLastError").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestFunction("?ReturnZero@@YAHXZ").Should().BeFalse();
            // classes of namespace testing which are not part of Google Test's core
            GoogleTestDiscoverer.IsGoogleTestFunction("??0MyTest@testing@@QEAA@XZ").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestFunction("??1TestPartResult@testing@@QEAA@XZ").Should().BeFalse();
            // namespace testing::internal within another namespace
            GoogleTestDiscoverer.IsGoogleTestFunction("?Foo@internal@testing@mylib@@YAXXZ").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestFunction("_ZN7testing14TestPartResultC2Ev").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestFunction("_ZN5mylib7testing8internal3FooEv").Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IsGoogleTestDll_GoogleTestAndGoogleMockDlls_AreRecognized()
        {
            GoogleTestDiscoverer.IsGoogleTestDll("gtest.dll").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("GTEST.DLL").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("gtestd.dll").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("gtest_main.dll").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("gmock.dll").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("gmockd.dll").Should().BeTrue();
            GoogleTestDiscoverer.IsGoogleTestDll("gmock_maind.dll").Should().BeTrue();

            GoogleTestDiscoverer.IsGoogleTestDll("kernel32.dll").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestDll("gtest_helper.dll").Should().BeFalse();
            GoogleTestDiscoverer.IsGoogleTestDll("mygtest.dll").Should().BeFalse();
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
        public void GetTestsFromExecutable_Utf8TestsDebugX86_FindsTestsWithCorrectNames()
        {
            AssertFindsUtf8Tests(TestResources.Utf8Tests_DebugX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_Utf8TestsReleaseX64_FindsTestsWithCorrectNames()
        {
            AssertFindsUtf8Tests(TestResources.Utf8Tests_ReleaseX64);
        }

        private void AssertFindsUtf8Tests(string executable)
        {
            executable.AsFileInfo().Should().Exist("building the SampleTests solution produces that executable");

            var discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            IList<TestCase> tests = discoverer.GetTestsFromExecutable(executable);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockLogger.Verify(l => l.DebugError(It.IsAny<string>()), Times.Never);
            tests.Select(t => t.FullyQualifiedName).Should().BeEquivalentTo(
                "\u00dcmlaut\u00df.T\u00e4st", "\u00dcmlaut\u00df.F\u00e4il\u00fcng", "\u00dcmlaut\u00df.Tr\u00e4its",
                "\u0395\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03ac.\u0394\u03bf\u03ba\u03b9\u03bc\u03ae", "\u4e2d\u6587.\u6d4b\u8bd5",
                "\u0938\u0942\u091f.\u0928\u093e\u092e", "Suite\u2160.Test\u2161");
            foreach (TestCase test in tests)
            {
                test.CodeFilePath.Should().EndWithEquivalent("Utf8Tests.cpp", test.FullyQualifiedName);
                test.LineNumber.Should().BeGreaterThan(0, test.FullyQualifiedName);
            }
            tests.Single(t => t.FullyQualifiedName == "\u00dcmlaut\u00df.Tr\u00e4its").Traits
                .Should().ContainSingle(t => t.Name == "Type" && t.Value == "\u00dcmlaut");
            // the namespace contains a spacing combining mark, which is not a word character for .NET's regexes
            tests.Single(t => t.FullyQualifiedName == "\u0938\u0942\u091f.\u0928\u093e\u092e").Namespace
                .Should().Be("\u092a\u0930\u0940\u0915\u094d\u0937\u0923");
        }

        private static readonly string[] TearDownTestSuiteTestCasesOfFixtureTests =
        {
            "FailingTearDown.TearDownTestSuite", "PassingFixture.TearDownTestSuite", "LegacyTearDown.TearDownTestSuite",
            "NamespacedFixture.TearDownTestSuite", "TypedFixture/0.TearDownTestSuite", "TypedFixture/1.TearDownTestSuite",
            "Instantiation/ParameterizedFixture.TearDownTestSuite"
        };

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_FixtureTestsDebugX86_TearDownTestSuiteTestCasesForFixturesDefiningTearDownTestSuite()
        {
            AssertFindsTearDownTestSuiteTestCases(TestResources.FixtureTests_DebugX86);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_FixtureTestsReleaseX64_TearDownTestSuiteTestCasesForFixturesDefiningTearDownTestSuite()
        {
            AssertFindsTearDownTestSuiteTestCases(TestResources.FixtureTests_ReleaseX64);
        }

        private void AssertFindsTearDownTestSuiteTestCases(string executable)
        {
            executable.AsFileInfo().Should().Exist("building the SampleTests solution produces that executable");

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options).GetTestsFromExecutable(executable);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            List<TestCase> tearDownTestCases = testCases.Where(tc => tc.IsTearDownTestSuiteTestCase).ToList();
            tearDownTestCases.Select(tc => tc.FullyQualifiedName).Should().BeEquivalentTo(TearDownTestSuiteTestCasesOfFixtureTests);
            testCases.Where(tc => !tc.IsTearDownTestSuiteTestCase).Should().HaveCount(13);
            foreach (TestCase testCase in tearDownTestCases)
            {
                testCase.CodeFilePath.Should().EndWithEquivalent("FixtureTests.cpp", testCase.FullyQualifiedName);
                testCase.LineNumber.Should().BeGreaterThan(0, testCase.FullyQualifiedName);
                testCase.TestGroup.Should().Be(GoogleTestConstants.TearDownTestSuiteTestName);
                testCase.IsExitCodeTestCase.Should().BeFalse();
            }
            tearDownTestCases.Single(tc => tc.FullyQualifiedName == "NamespacedFixture.TearDownTestSuite").Namespace.Should().Be("fixtures");
            tearDownTestCases.Single(tc => tc.FullyQualifiedName == "TypedFixture/1.TearDownTestSuite").TestClass
                .Should().Be(testCases.Single(tc => tc.FullyQualifiedName == "TypedFixture/1.Passes").TestClass);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_TearDownTestSuiteTestCasesOff_NoTearDownTestSuiteTestCases()
        {
            MockOptions.Setup(o => o.TearDownTestSuiteTestCases).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options).GetTestsFromExecutable(TestResources.FixtureTests_DebugX86);

            testCases.Should().HaveCount(13);
            testCases.Should().NotContain(tc => tc.IsTearDownTestSuiteTestCase);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_NoTearDownTestSuiteTestCases()
        {
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options).GetTestsFromExecutable(TestResources.FixtureTests_DebugX86);

            testCases.Should().HaveCount(13);
            testCases.Should().NotContain(tc => tc.IsTearDownTestSuiteTestCase);
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
        public void GetTestsFromExecutable_DoNotParseSymbolInformation_DiaIsNotInvokedAndLocationsAreProvidedByGoogleTest()
        {
            var mockFactory = new Mock<IDiaResolverFactory>();
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options, new ProcessExecutorFactory(), mockFactory.Object)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            mockFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never);
            testCases.Should().HaveCount(TestResources.NrOfTests);
            foreach (TestCase testCase in testCases)
            {
                testCase.CodeFilePath.Should().NotBeNullOrEmpty();
                testCase.LineNumber.Should().BeGreaterThan(0);
                testCase.Traits.Should().BeEmpty();
                testCase.Namespace.Should().BeNull();
                testCase.Source.Should().Be(TestResources.Tests_DebugX86);
                testCase.DisplayName.Should().NotBeNullOrEmpty();
                testCase.FullyQualifiedName.Should().NotBeNullOrEmpty();
            }

            TestCase addPasses = testCases.Single(tc => tc.FullyQualifiedName == "TestMath.AddPasses");
            addPasses.CodeFilePath.Should().BeEquivalentTo(Path.GetFullPath(Path.Combine(TestResources.SampleTestsSolutionDir, @"Tests\BasicTests.cpp")));
            addPasses.LineNumber.Should().Be(59);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_SourcePathMapping_LocationsAreMapped()
        {
            MockOptions.Setup(o => o.SourcePathMapping).Returns(@"$(SolutionDir)=>X:\mapped");

            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            TestCase addPasses = testCases.Single(tc => tc.FullyQualifiedName == "TestMath.AddPasses");
            addPasses.CodeFilePath.Should().BeEquivalentTo(@"X:\mapped\Tests\BasicTests.cpp");
            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains(SettingsWrapper.OptionSourcePathMapping))), Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_TypedAndParameterizedTests_InstancesShareTestClassAndGroup()
        {
            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            testCases.Where(tc => tc.FullyQualifiedName.Contains(".Simple/"))
                .Should().HaveCount(4)
                .And.OnlyContain(tc => tc.TestClass == "ParameterizedTests" && tc.TestGroup == "Simple");
            testCases.Where(tc => tc.FullyQualifiedName.StartsWith("TypedTests/") && tc.FullyQualifiedName.EndsWith(".CanIterate"))
                .Should().HaveCount(3)
                .And.OnlyContain(tc => tc.TestClass == "TypedTests" && tc.TestGroup == "CanIterate");
            testCases.Where(tc => tc.FullyQualifiedName.Contains("/TypeParameterizedTests/") && tc.FullyQualifiedName.EndsWith(".CanIterate"))
                .Should().HaveCount(3)
                .And.OnlyContain(tc => tc.TestClass == "TypeParameterizedTests" && tc.TestGroup == "CanIterate");

            TestCase plainTest = testCases.Single(tc => tc.FullyQualifiedName == "TestMath.AddPasses");
            plainTest.TestClass.Should().Be("TestMath");
            plainTest.TestGroup.Should().Be("AddPasses");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void GetTestsFromExecutable_TestsRegisteredAtRuntime_LocationsAreProvidedByGoogleTest()
        {
            IList<TestCase> testCases = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options)
                .GetTestsFromExecutable(TestResources.Tests_DebugX86);

            // these tests have no TestBody symbol, their location is the one passed to MakeAndRegisterTestInfo()
            foreach (string name in new[] { "Api.Created.Tests.PassingTest", "Api_Created_Tests.FailingTest" })
            {
                TestCase testCase = testCases.Single(tc => tc.FullyQualifiedName == name);
                testCase.CodeFilePath.Should().BeEquivalentTo(Path.GetFullPath(Path.Combine(TestResources.SampleTestsSolutionDir, @"Tests\ApiCreatedTests.cpp")));
                testCase.LineNumber.Should().Be(57);
            }
            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Could not find source location"))), Times.Never);
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