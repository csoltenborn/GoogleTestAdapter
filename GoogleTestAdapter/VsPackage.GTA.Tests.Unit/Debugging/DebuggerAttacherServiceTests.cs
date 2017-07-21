// This file has been modified by Microsoft on 7/2017.

using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ServiceModel;
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

        [TestInitialize]
        public void Setup()
        {
            MockDebuggerAttacher.Reset();
            MockLogger.Reset();
            _messages.Clear();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_ReceivesMessage_AnswersImmediately()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => true));
            DoTest(null);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_AttacherThrows_AnswerIncludesExceptionMessage()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => { throw new Exception("my message"); }));
            DoTest("my message");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebuggerAttacherService_AttacherReturnsFalse_AnswerWithoutReason()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(MessageBasedDebuggerAttacherTests.GetAttachDebuggerAction(() => false));
            DoTest("unknown reasons");
        }

        private void DoTest(string expectedErrorMessagePart)
        {
            string pipeId = Guid.NewGuid().ToString();
            int debuggeeProcessId = 2017;

            // ReSharper disable once UnusedVariable
            var host = new DebuggerAttacherServiceHost(pipeId, MockDebuggerAttacher.Object, MockLogger.Object);
            try
            {
                host.Open();

                var proxy = DebuggerAttacherServiceConfiguration.CreateProxy(pipeId, WaitingTime);
                using (var client = new DebuggerAttacherServiceProxyWrapper(proxy))
                {
                    client.Should().NotBeNull();
                    client.Service.Should().NotBeNull();

                    Action attaching = () => client.Service.AttachDebugger(debuggeeProcessId);
                    if (expectedErrorMessagePart == null)
                    {
                        attaching.ShouldNotThrow();
                    }
                    else
                    {
                        attaching.ShouldThrow<FaultException<DebuggerAttacherServiceFault>>().Where(
                            (FaultException<DebuggerAttacherServiceFault> ex) => ex.Detail.Message.Contains(expectedErrorMessagePart));
                    }
                }

                host.Close();
            }
            catch (CommunicationException)
            {
                host.Abort();
                throw;
            }
        }

    }
}