using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class ProcessExecutorTests
    {

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_PingLocalHost()
        {
            var mockLogger = new Mock<ILogger>();
            var processCreator = new ProcessExecutor(null, mockLogger.Object);
            List<string> standardOutput = new List<string>(), errorOutput = new List<string>();
            int exitCode = processCreator.ExecuteCommandBlocking(
                Path.Combine(Environment.SystemDirectory, "ping.exe"),
                "localhost",
                "",
                null, 
                s => standardOutput.Add(s), 
                s => errorOutput.Add(s));
                
            exitCode.Should().Be(0);
            standardOutput.Should().Contain(s => s.Contains("Ping"));
            standardOutput.Count.Should().BeGreaterOrEqualTo(11);
            standardOutput.Count.Should().BeLessOrEqualTo(12);
            errorOutput.Count.Should().Be(0);
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SampleTests()
        {
            var mockLogger = new Mock<ILogger>();
            var processCreator = new ProcessExecutor(null, mockLogger.Object);
            List<string> standardOutput = new List<string>(), errorOutput = new List<string>();
            int exitCode = processCreator.ExecuteCommandBlocking(
                TestResources.SampleTests,
                null,
                null,
                "",
                s => standardOutput.Add(s),
                s => errorOutput.Add(s));

            exitCode.Should().Be(1);
            standardOutput.Should().Contain(s => s.Contains("LongRunningTests.Test1"));
            standardOutput.Count.Should().Be(405);
            errorOutput.Count.Should().Be(0);
        }

    }

}