using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DebuggerAttacherServiceTests
    {
        internal static readonly TimeSpan WaitingTime = TimeSpan.FromSeconds(1);

        private Mock<IDebuggerAttacher> MockDebuggerAttacher { get; } = new Mock<IDebuggerAttacher>();
        private Mock<ILogger> MockLogger { get; } = new Mock<ILogger>();

        private readonly IList<AttachDebuggerMessage> _messages = new List<AttachDebuggerMessage>();
        private ManualResetEventSlim _resetEvent;

        [TestInitialize]
        public void Setup()
        {
            MockDebuggerAttacher.Reset();
            MockLogger.Reset();
            _messages.Clear();
            _resetEvent = new ManualResetEventSlim(false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_ReceivesMessage_AnswersImmediately()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => true));
            DoTest(true, null);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_AttacherThrows_AnswerIncludesExceptionMessage()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => { throw new Exception("my message"); }));
            DoTest(false, "my message");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_AttacherReturnsFalse_AnswerWithoutReason()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => false));
            DoTest(false, "unknown reasons");
        }

        private void DoTest(bool expectedOutcome, string expectedErrorMessagePart)
        {
            string pipeId = Guid.NewGuid().ToString();
            int debuggeeProcessId = 2017;

            // ReSharper disable once UnusedVariable
            using (var service = new DebuggerAttacherService(pipeId, MockDebuggerAttacher.Object))
            {
                var client = MessageBasedDebuggerAttacher.CreateAndStartPipeClient(
                    pipeId,
                    (connection, msg) => { _messages.Add(msg); _resetEvent.Set(); },
                    MockLogger.Object);
                client.Should().NotBeNull();

                client.PushMessage(new AttachDebuggerMessage { ProcessId = debuggeeProcessId });

                _resetEvent.Wait(WaitingTime).Should().BeTrue();
                _messages.Count.Should().Be(1);

                var message = _messages.Single();
                message.ProcessId.Should().Be(debuggeeProcessId);
                message.DebuggerAttachedSuccessfully.Should().Be(expectedOutcome);
                if (string.IsNullOrEmpty(expectedErrorMessagePart))
                    message.ErrorMessage.Should().Be(expectedErrorMessagePart);
                else
                    message.ErrorMessage.Should().Contain(expectedErrorMessagePart);
            }
        }

    }
}