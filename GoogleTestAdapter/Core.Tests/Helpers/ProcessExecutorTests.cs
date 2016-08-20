using System;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
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
            string[] standardOutput, errorOutput;
            int exitCode = processCreator.ExecuteCommandBlocking(
                Path.Combine(Environment.SystemDirectory, "ping.exe"),
                "localhost",
                "",
                null, 
                out standardOutput, 
                out errorOutput);
                
            exitCode.Should().Be(0);
            standardOutput.Should().Contain(s => s.Contains("Ping"));
            standardOutput.Length.Should().BeGreaterOrEqualTo(11);
            standardOutput.Length.Should().BeLessOrEqualTo(12);
            errorOutput.Length.Should().Be(0);
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SampleTests()
        {
            var mockLogger = new Mock<ILogger>();
            var processCreator = new ProcessExecutor(null, mockLogger.Object);
            string[] standardOutput, errorOutput;
            int exitCode = processCreator.ExecuteCommandBlocking(
                TestResources.SampleTests,
                null,
                null,
                "", 
                out standardOutput, 
                out errorOutput);

            exitCode.Should().Be(1);
            standardOutput.Should().Contain(s => s.Contains("LongRunningTests.Test1"));
            standardOutput.Length.Should().Be(405);
            errorOutput.Length.Should().Be(0);
        }

    }

}