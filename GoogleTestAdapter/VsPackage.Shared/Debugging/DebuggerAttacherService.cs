using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    public sealed class DebuggerAttacherService : IDisposable
    {
        private readonly string _debuggingNampedPipeId;
        private readonly IDebuggerAttacher _debuggerAttacher;
        private readonly ILogger _logger;

        private NamedPipeServer<AttachDebuggerMessage> _server;

        public DebuggerAttacherService(string debuggingNampedPipeId, IDebuggerAttacher debuggerAttacher, ILogger logger)
        {
            _debuggingNampedPipeId = debuggingNampedPipeId;
            _debuggerAttacher = debuggerAttacher;
            _logger = logger;

            _server = CreateAndStartPipeServer();
            if (_server != null)
                _logger.DebugInfo("Server side of named pipe started");
        }

        private NamedPipeServer<AttachDebuggerMessage> CreateAndStartPipeServer()
        {
            string pipeName = MessageBasedDebuggerAttacher.GetPipeName(_debuggingNampedPipeId);

            var server = new NamedPipeServer<AttachDebuggerMessage>(pipeName);
            server.Error +=
                exception => _logger.LogError($"Error on Named Pipe server:{Environment.NewLine}{exception}");
            server.ClientMessage += OnAttachDebuggerMessageReceived;

            if (!server.Start())
            {
                _logger.LogError($"Server side of named pipe could not be started, pipe name: {MessageBasedDebuggerAttacher.GetPipeName(_debuggingNampedPipeId)}");
                return null;
            }

            return server;
        }

        private void OnAttachDebuggerMessageReceived(NamedPipeConnection<AttachDebuggerMessage, AttachDebuggerMessage> connection,
            AttachDebuggerMessage message)
        {
            try
            {
                message.DebuggerAttachedSuccessfully = _debuggerAttacher.AttachDebugger(message.ProcessId);
                if (!message.DebuggerAttachedSuccessfully)
                    message.ErrorMessage = $"Could not attach debugger to process {message.ProcessId} for unknown reasons";
            }
            catch (Exception e)
            {
                message.DebuggerAttachedSuccessfully = false;
                message.ErrorMessage = $"Could not attach debugger to process {message.ProcessId} because of exception on server side:{Environment.NewLine}{e}";
            }
            finally
            {
                try
                {
                    _server.PushMessage(message);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Exception on server side while sending debugging response message:{Environment.NewLine}{e}");
                }
            }
        }

        public void Dispose()
        {
            if (_server != null)
            {
                _server.Stop();
                _server = null;
                _logger.DebugInfo("Server side of named pipe stopped");
            }
        }
    }

}