using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution.Contracts;
using Moq;

namespace GoogleTestAdapter.Tests.Common.Tests
{
    public abstract class ProcessExecutorTests
    {
        protected Mock<ILogger> MockLogger { get; } = new Mock<ILogger>();
        protected IProcessExecutor ProcessExecutor { get; set; }

        public virtual void Teardown()
        {
            MockLogger.Reset();
        }

        protected void Test_ExecuteProcessBlocking_PingLocalHost()
        {
            List<string> output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking(
                Path.Combine(Environment.SystemDirectory, "ping.exe"),
                "localhost",
                "",
                null, 
                s => output.Add(s));
                
            exitCode.Should().Be(0);
            output.Should().Contain(s => s.Contains("Ping"));
            output.Count.Should().BeGreaterOrEqualTo(11);
            output.Count.Should().BeLessOrEqualTo(12);
        }

        protected void Test_ExecuteProcessBlocking_SampleTests()
        {
            List<string> output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking(
                TestResources.Tests_DebugX86,
                null,
                null,
                "",
                s => output.Add(s));

            exitCode.Should().Be(1);
            output.Should().Contain(s => s.Contains("TestMath.AddPasses"));
            output.Should().HaveCount(563);
        }

        protected void Test_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            var output = new List<string>();
            int returnCode = ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", line => output.Add(line));

            returnCode.Should().Be(0);
            output.Should().ContainSingle();
            output[0].Should().Be("2");
        }

        protected void Test_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", line => { });
        }

    }
}