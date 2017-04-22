using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    [TestClass]
    public class MessageBasedDebuggerAttacherTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(4);
        public static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(25);

        private Mock<IDebuggerAttacher> MockDebuggerAttacher { get; } = new Mock<IDebuggerAttacher>();
        private Mock<ILogger> MockLogger { get; } = new Mock<ILogger>();

        [TestInitialize]
        public void Setup()
        {
            MockDebuggerAttacher.Reset();
            MockLogger.Reset();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingSucceeds_DebugOutputGenerated()
        {
            MockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Returns(true);

            DoTest(true);

            MockLogger.Verify(l => l.DebugInfo(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingFails_ErrorOutputGenerated()
        {
            MockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Returns(false);

            DoTest(false);

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unknown reasons"))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingThrows_ErrorOutputGenerated()
        {
            MockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Throws(new Exception("my message"));

            DoTest(false);

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("my message"))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingDoesNotReturn_TimeoutErrorOutputGenerated()
        {
            int visualStudioProcessId = 4711;
            int debuggeeProcessId = 2017;

            bool attacherReturned = false;
            MockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Returns(() =>
            {
                Thread.Sleep((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
                attacherReturned = true;
                return false;
            });

            // ReSharper disable once UnusedVariable
            using (var service = new DebuggerAttacherService(visualStudioProcessId, MockDebuggerAttacher.Object))
            {
                var stopwatch = Stopwatch.StartNew();
                var client = new MessageBasedDebuggerAttacher(visualStudioProcessId, Timeout, MockLogger.Object);
                client.AttachDebugger(debuggeeProcessId).Should().BeFalse();
                stopwatch.Stop();

                attacherReturned.Should().BeFalse();
                stopwatch.Elapsed.Should().BeGreaterOrEqualTo(Timeout - Tolerance);
                MockDebuggerAttacher.Verify(a => a.AttachDebugger(It.Is<int>(processId => processId == debuggeeProcessId)), Times.Once);
                MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("timed out"))), Times.Once);

                // attacher is still running - let's see if we can attach another debugger in the meantime
                int debuggee2ProcessId = debuggeeProcessId + 1;
                MockDebuggerAttacher.Reset();
                MockLogger.Reset();
                MockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Returns(true);

                stopwatch.Restart();
                client = new MessageBasedDebuggerAttacher(visualStudioProcessId, Timeout, MockLogger.Object);
                client.AttachDebugger(debuggee2ProcessId);
                stopwatch.Stop();

                attacherReturned.Should().BeFalse();
                stopwatch.Elapsed.Should().BeLessThan(DebuggerAttacherServiceTests.WaitingTime + Tolerance);
                MockDebuggerAttacher.Verify(a => a.AttachDebugger(It.Is<int>(processId => processId == debuggee2ProcessId)), Times.Once);
                MockLogger.Verify(l => l.DebugInfo(It.IsAny<string>()), Times.Once);
            }
        }

        private void DoTest(bool expectedResult)
        {
            int visualStudioProcessId = 4711;
            int debuggeeProcessId = 2017;

            // ReSharper disable once UnusedVariable
            using (var service = new DebuggerAttacherService(visualStudioProcessId, MockDebuggerAttacher.Object))
            {
                var client = new MessageBasedDebuggerAttacher(visualStudioProcessId, Timeout, MockLogger.Object);

                client.AttachDebugger(debuggeeProcessId).Should().Be(expectedResult);

                MockDebuggerAttacher.Verify(a => a.AttachDebugger(It.Is<int>(processId => processId == debuggeeProcessId)),
                    Times.Once);
            }
        }

    }
}