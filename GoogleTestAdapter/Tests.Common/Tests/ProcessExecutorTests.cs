using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                new Dictionary<string, string>(),
                s => output.Add(s));
                
            exitCode.Should().Be(0);
            output.Should().Contain(s => s.Contains("Ping"));
            output.Should().HaveCountGreaterOrEqualTo(11);
            output.Should().HaveCountLessOrEqualTo(12);
        }

        protected void Test_ExecuteProcessBlocking_SampleTests()
        {
            List<string> output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking(
                TestResources.Tests_DebugX86,
                null,
                null,
                "",
                new Dictionary<string, string>(),
                s => output.Add(s));

            exitCode.Should().Be(1);
            output.Should().Contain(s => s.Contains("TestMath.AddPasses"));
            output.Should().HaveCount(712);
        }

        protected void Test_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            var output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", new Dictionary<string, string>(), line => output.Add(line));

            exitCode.Should().Be(0);
            output.Should().ContainSingle();
            output.Should().HaveElementAt(0, "2");
        }

        protected void Test_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", new Dictionary<string, string>(), line => { });
        }

        protected void Test_CancelBeforeExecution_ProcessIsNotStarted()
        {
            ProcessExecutor.Cancel();

            var output = new List<string>();
            var stopwatch = Stopwatch.StartNew();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking(
                Path.Combine(Environment.SystemDirectory, "ping.exe"), "-n 5 127.0.0.1", ".", "", new Dictionary<string, string>(),
                line => output.Add(line));
            stopwatch.Stop();

            exitCode.Should().Be(int.MaxValue);
            output.Should().BeEmpty();
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }

        protected void Test_Cancel_KillsProcessTree()
        {
            // cmd.exe starts ping.exe, which keeps running (and keeps the output pipe open) if only cmd.exe is killed
            var output = new List<string>();
            Task<int> execution = Task.Run(() => ProcessExecutor.ExecuteCommandBlocking(
                "cmd.exe", "/C \"ping -n 30 127.0.0.1\"", ".", "", new Dictionary<string, string>(),
                line => { lock (output) output.Add(line); }));

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
            {
                lock (output)
                {
                    if (output.Any(line => line.Contains("127.0.0.1")))
                        break;
                }
                Thread.Sleep(50);
            }
            ProcessExecutor.Cancel();

            execution.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("canceling should kill cmd.exe and ping.exe");
        }

        protected void Test_WithEnvSetting_EnvVariableIsSet()
        {
            string envVarName = "MyVar";
            string envVarValue = "MyValue";

            Environment.GetEnvironmentVariable(envVarName).Should().BeNull();

            var output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"set\"", ".", "", new Dictionary<string, string> {{ envVarName, envVarValue}}, line => output.Add(line));

            exitCode.Should().Be(0);
            output.Should().Contain($"{envVarName}={envVarValue}");
            Environment.GetEnvironmentVariable(envVarName).Should().BeNull();
        }

        protected void Test_WithOverridingEnvSetting_EnvVariableHasNewValue()
        {
            string newValue = "NewValue";

            var envVar = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .First(v => !string.IsNullOrWhiteSpace(v.Value?.ToString()) && v.Value.ToString() != newValue);
            string valueBeforeChange = envVar.Value.ToString();

            var output = new List<string>();
            int exitCode = ProcessExecutor.ExecuteCommandBlocking("cmd.exe", "/C \"set\"", ".", "", new Dictionary<string, string> {{ envVar.Key.ToString(), newValue}}, line => output.Add(line));

            exitCode.Should().Be(0);
            output.Should().Contain($"{envVar.Key}={newValue}");
            Environment.GetEnvironmentVariable(envVar.Key.ToString()).Should().Be(valueBeforeChange);
        }

    }
}