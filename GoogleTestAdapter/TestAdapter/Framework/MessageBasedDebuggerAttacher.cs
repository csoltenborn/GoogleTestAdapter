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
            var stopWatch = Stopwatch.StartNew();

            var resetEvent = new ManualResetEventSlim(false);

            var client = new NamedPipeClient<AttachDebuggerMessage>(GetPipeName(_visualStudioProcessId));
            client.Error += exception =>
            {
                _logger.LogError($"Named pipe client error: {exception.Message}");
            };
            client.ServerMessage += (connection, message) =>
            {
                if (message.ProcessId == processId)
                    resetEvent.Set();
            };

            client.Start();
            client.WaitForConnection();
            client.PushMessage(new AttachDebuggerMessage { ProcessId = processId });

            resetEvent.Wait(AttachDebuggerTimeout);
            stopWatch.Stop();

            if (resetEvent.IsSet)
                _logger.DebugInfo($"Debugger attached to process {processId}, took {stopWatch.ElapsedMilliseconds}ms");
            else
                _logger.LogError($"Could not attach debugger to process {processId}!");

            client.Stop();

            return resetEvent.IsSet;
        }
    }

}