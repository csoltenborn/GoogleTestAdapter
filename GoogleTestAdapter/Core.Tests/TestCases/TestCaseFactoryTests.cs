using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class TestCaseFactoryTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateTestCases_DiscoveryTimeoutIsExceeded_DiscoveryIsCanceledAndCancelationIsLogged()
        {
            MockOptions.Setup(o => o.TestDiscoveryTimeoutInSeconds).Returns(1);
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            var reportedTestCases = new List<TestCase>();
            var stopWatch = Stopwatch.StartNew();
            var factory = new TestCaseFactory(TestResources.TenSecondsWaiter, MockLogger.Object, TestEnvironment.Options, null);
            var returnedTestCases = factory.CreateTestCases(testCase => reportedTestCases.Add(testCase));
            stopWatch.Stop();

            reportedTestCases.Should().BeEmpty();
            returnedTestCases.Should().BeEmpty();
            stopWatch.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.FromSeconds(1));
            stopWatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
            MockLogger.Verify(o => o.LogError(It.Is<string>(s => s.Contains(TestResources.TenSecondsWaiter))), Times.Once);
            MockLogger.Verify(o => o.DebugError(It.Is<string>(s => s.Contains(Path.GetFileName(TestResources.TenSecondsWaiter)))), Times.Once);
        }

        [TestMethod]
        public void CreateTestCases_AdditionalTestDiscoveryParam_TestDiscoveryUsesAdditionalTestDiscoveryParams()
        {
            MockOptions.Setup(o => o.AdditionalTestDiscoveryParam).Returns("-testDiscoveryFlag");
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(false);

            var reportedTestCases = new List<TestCase>();
            var factory = new TestCaseFactory(TestResources.TestDiscoveryParamExe, MockLogger.Object, TestEnvironment.Options, null);
            var returnedTestCases = factory.CreateTestCases(testCase => reportedTestCases.Add(testCase));

            reportedTestCases.Count.Should().Be(2);
            reportedTestCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestFails");
            reportedTestCases.Should().Contain(t => t.FullyQualifiedName == "TestDiscovery.TestPasses");
        }

    }

}