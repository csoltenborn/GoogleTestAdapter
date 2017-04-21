using System;
using System.Diagnostics;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using NamedPipeWrapper;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class MessageBasedDebuggerAttacher : IDebuggerAttacher
    {
        private static readonly TimeSpan AttachDebuggerTimeout = TimeSpan.FromSeconds(10);

        private readonly ILogger _logger;
        private readonly int _visualStudioProcessId;

        public static string GetPipeName(int visualStudioProcessId)
        {
            return $"GTA_{visualStudioProcessId}";
        }

        public MessageBasedDebuggerAttacher(int visualStudioProcessId, ILogger logger)
        {
            _visualStudioProcessId = visualStudioProcessId;
            _logger = logger;
        }

        public bool AttachDebugger(int processId)
        {
            try
            {
                return TryAttachDebugger(processId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not attach debugger to process {processId} because of exception on client side: {e.Message}");
                return false;
            }
        }

        private bool TryAttachDebugger(int processId)
        {
            var stopWatch = Stopwatch.StartNew();
            var resetEvent = new ManualResetEventSlim(false);
            bool debuggerAttachedSuccessfully = false;
            string errorMessage = null;

            ConnectionMessageEventHandler<AttachDebuggerMessage, AttachDebuggerMessage> onServerMessage
                = (connection, message) =>
                {
                    if (message.ProcessId == processId)
                    {
                        debuggerAttachedSuccessfully = message.DebuggerAttachedSuccessfully;
                        errorMessage = message.ErrorMessage;
                        resetEvent.Set();
                    }
                };

            var client = CreateAndStartPipeClient(onServerMessage);
            client.PushMessage(new AttachDebuggerMessage { ProcessId = processId });

            if (!resetEvent.Wait(AttachDebuggerTimeout))
                errorMessage = $"Could not attach debugger to process {processId} since attaching timed out after {AttachDebuggerTimeout.TotalSeconds}s";

            stopWatch.Stop();

            if (debuggerAttachedSuccessfully)
            {
                _logger.DebugInfo($"Debugger attached to process {processId}, took {stopWatch.ElapsedMilliseconds}ms");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = $"Could not attach debugger to process {processId}, no error message available";

                _logger.LogError(errorMessage);
            }

            client.Stop();

            return debuggerAttachedSuccessfully;
        }

        private NamedPipeClient<AttachDebuggerMessage> CreateAndStartPipeClient(ConnectionMessageEventHandler<AttachDebuggerMessage, AttachDebuggerMessage> onServerMessage)
        {
            var client = new NamedPipeClient<AttachDebuggerMessage>(GetPipeName(_visualStudioProcessId));
            client.Error += exception =>
            {
                _logger.DebugError(
                    $"Named pipe error: Named pipe is {GetPipeName(_visualStudioProcessId)}, exception message: {exception.Message}");
            };
            client.ServerMessage += onServerMessage;

            client.Start();
            client.WaitForConnection();

            return client;
        }
    }

}