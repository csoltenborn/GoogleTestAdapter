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
        private readonly string _debuggingNamedPipeId;
        private readonly TimeSpan _timeout;

        public static string GetPipeName(string id)
        {
            return $"GTA_{id}";
        }

        public MessageBasedDebuggerAttacher(string debuggingNamedPipeId, TimeSpan timeout, ILogger logger)
        {
            _debuggingNamedPipeId = debuggingNamedPipeId;
            _timeout = timeout;
            _logger = logger;
        }

        public MessageBasedDebuggerAttacher(string debuggingNamedPipeId, ILogger logger) : this(debuggingNamedPipeId, AttachDebuggerTimeout, logger)
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

            var client = CreateAndStartPipeClient(_debuggingNamedPipeId, onServerMessage, _logger);
            if (client == null)
                return false;

            client.PushMessage(new AttachDebuggerMessage { ProcessId = processId });

            if (!resetEvent.IsSet && !resetEvent.Wait(_timeout))
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

        public static NamedPipeClient<AttachDebuggerMessage> CreateAndStartPipeClient(string pipeId, ConnectionMessageEventHandler<AttachDebuggerMessage, AttachDebuggerMessage> onServerMessage, ILogger logger)
        {
            var client = new NamedPipeClient<AttachDebuggerMessage>(GetPipeName(pipeId));
            client.Error += exception =>
            {
                logger.DebugError(
                    $"Named pipe error: Named pipe is {GetPipeName(pipeId)}, exception message: {exception.Message}");
            };
            client.ServerMessage += onServerMessage;

            client.Start();

            // workaround for NamedPipeClient not telling if connection has been established :-)
            var stopwatch = Stopwatch.StartNew();
            client.WaitForConnection(TimeSpan.FromSeconds(3));
            stopwatch.Stop();
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(2.5))
            {
                logger.LogError($"Could not connect to NamedPipe {GetPipeName(pipeId)} - debugging will not be available");
                client.Stop();
                return null;
            }

            return client;
        }
    }

}