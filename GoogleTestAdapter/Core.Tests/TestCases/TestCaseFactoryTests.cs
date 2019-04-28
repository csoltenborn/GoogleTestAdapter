using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class TestCaseFactoryTests : TestsBase
    {
        private readonly IProcessExecutorFactory _processExecutorFactory = new ProcessExecutorFactory();

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateTestCases_DiscoveryTimeoutIsExceeded_DiscoveryIsCanceledAndCancellationIsLogged()
        {
            MockOptions.Setup(o => o.TestDiscoveryTimeoutInSeconds).Returns(1);
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            var reportedTestCases = new List<TestCase>();
            var stopWatch = Stopwatch.StartNew();
            var factory = new TestCaseFactory(TestResources.TenSecondsWaiter, MockLogger.Object, TestEnvironment.Options, null, _processExecutorFactory);
            var returnedTestCases = factory.CreateTestCases(testCase => reportedTestCases.Add(testCase));
            stopWatch.Stop();

            reportedTestCases.Should().BeEmpty();
            returnedTestCases.Should().BeEmpty();
            stopWatch.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.FromSeconds(1));
            stopWatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
            MockLogger.Verify(o => o.LogError(It.Is<string>(s => s.Contains(TestResources.TenSecondsWaiter))), Times.Once);
            MockLogger.Verify(o => o.DebugError(It.Is<string>(s => s.Contains(Path.GetFileName(TestResources.TenSecondsWaiter)))), Times.AtLeastOnce);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateTestCases_OldExeDiscoveryTimeoutIsExceeded_DiscoveryIsCanceledAndCancellationIsLogged()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);
            CreateTestCases_DiscoveryTimeoutIsExceeded_DiscoveryIsCanceledAndCancellationIsLogged();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void CreateTestCases_OldExeWithAdditionalPdb_TestCasesAreFound()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);
            CheckIfSourceLocationsAreFound();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void CreateTestCases_NewExeWithAdditionalPdb_TestCasesAreFound()
        {
            CheckIfSourceLocationsAreFound();
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private void CheckIfSourceLocationsAreFound()
        { 
            string executable = TestResources.LoadTests_ReleaseX86;

            executable.AsFileInfo().Should().Exist();
            string pdb = Path.ChangeExtension(executable, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            string renamedPdb = $"{pdb}.bak";
            renamedPdb.AsFileInfo().Should().NotExist();

            try
            {
                File.Move(pdb, renamedPdb);
                pdb.AsFileInfo().Should().NotExist();

                var reportedTestCases = new List<TestCase>();
                var diaResolverFactory = new DefaultDiaResolverFactory();
                var factory = new TestCaseFactory(executable, MockLogger.Object, TestEnvironment.Options, diaResolverFactory, _processExecutorFactory);
                var returnedTestCases = factory.CreateTestCases(testCase => reportedTestCases.Add(testCase));

                returnedTestCases.Should().OnlyContain(tc => !HasSourceLocation(tc));
                reportedTestCases.Should().OnlyContain(tc => !HasSourceLocation(tc));

                reportedTestCases.Clear();
                MockOptions.Setup(o => o.AdditionalPdbs).Returns("$(ExecutableDir)\\*.pdb.bak");
                MockOptions.Setup(o => o.TestDiscoveryTimeoutInSeconds).Returns(10000);
                factory = new TestCaseFactory(executable, MockLogger.Object, TestEnvironment.Options, diaResolverFactory, _processExecutorFactory);
                returnedTestCases = factory.CreateTestCases(testCase => reportedTestCases.Add(testCase));

                reportedTestCases.Should().OnlyContain(tc => HasSourceLocation(tc));
                returnedTestCases.Should().OnlyContain(tc => HasSourceLocation(tc));
            }
            finally
            {
                File.Move(renamedPdb, pdb);
                pdb.AsFileInfo().Should().Exist();
            }
        }

        private bool HasSourceLocation(TestCase testCase)
        {
            return !string.IsNullOrEmpty(testCase.CodeFilePath) && testCase.LineNumber != 0;
        }
    }

}