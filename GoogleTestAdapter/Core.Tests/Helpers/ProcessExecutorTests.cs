using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Tests.Common;
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
            List<string> output = new List<string>();
            int exitCode = processCreator.ExecuteCommandBlocking(
                Path.Combine(Environment.SystemDirectory, "ping.exe"),
                "localhost",
                "",
                null,
                null, 
                s => output.Add(s));
                
            exitCode.Should().Be(0);
            output.Should().Contain(s => s.Contains("Ping"));
            output.Count.Should().BeGreaterOrEqualTo(11);
            output.Count.Should().BeLessOrEqualTo(12);
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SampleTests()
        {
            var mockLogger = new Mock<ILogger>();
            var processCreator = new ProcessExecutor(null, mockLogger.Object);
            List<string> output = new List<string>();
            int exitCode = processCreator.ExecuteCommandBlocking(
                TestResources.Tests_DebugX86,
                null,
                null,
                null,
                "",
                s => output.Add(s));

            exitCode.Should().Be(1);
            output.Should().Contain(s => s.Contains("TestMath.AddPasses"));
            output.Count.Should().Be(504);
        }

    }

}