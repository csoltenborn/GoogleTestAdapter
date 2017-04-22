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
        private readonly TimeSpan _timeout;

        public static string GetPipeName(int visualStudioProcessId)
        {
            return $"GTA_{visualStudioProcessId}";
        }

        public MessageBasedDebuggerAttacher(int visualStudioProcessId, TimeSpan timeout, ILogger logger)
        {
            _visualStudioProcessId = visualStudioProcessId;
            _timeout = timeout;
            _logger = logger;
        }

        public MessageBasedDebuggerAttacher(int visualStudioProcessId, ILogger logger) : this(visualStudioProcessId, AttachDebuggerTimeout, logger)
        {
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

            var client = CreateAndStartPipeClient(_visualStudioProcessId, onServerMessage, _logger);
            client.PushMessage(new AttachDebuggerMessage { ProcessId = processId });

            if (!resetEvent.Wait(_timeout))
                errorMessage = $"Could not attach debugger to process {processId} since attaching timed out after {_timeout.TotalSeconds}s";

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

        public static NamedPipeClient<AttachDebuggerMessage> CreateAndStartPipeClient(int visualStudioProcessId, ConnectionMessageEventHandler<AttachDebuggerMessage, AttachDebuggerMessage> onServerMessage, ILogger logger)
        {
            var client = new NamedPipeClient<AttachDebuggerMessage>(GetPipeName(visualStudioProcessId));
            client.Error += exception =>
            {
                logger.DebugError(
                    $"Named pipe error: Named pipe is {GetPipeName(visualStudioProcessId)}, exception message: {exception.Message}");
            };
            client.ServerMessage += onServerMessage;

            client.Start();
            client.WaitForConnection();

            return client;
        }
    }

}