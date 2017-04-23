using System;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Framework;
using NamedPipeWrapper;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    public class DebuggerAttacherService : IDisposable
    {
        private readonly int _visualStudioProcessId;
        private readonly IDebuggerAttacher _debuggerAttacher;

        private NamedPipeServer<AttachDebuggerMessage> _server;

        public DebuggerAttacherService(int visualStudioProcessId, IDebuggerAttacher debuggerAttacher)
        {
            _visualStudioProcessId = visualStudioProcessId;
            _debuggerAttacher = debuggerAttacher;
            _server = CreateAndStartPipeServer();
        }

        private NamedPipeServer<AttachDebuggerMessage> CreateAndStartPipeServer()
        {
            // TODO what to do if NamedPipe can not be created?
            var server = new NamedPipeServer<AttachDebuggerMessage>(MessageBasedDebuggerAttacher.GetPipeName(_visualStudioProcessId));
            server.ClientMessage += OnAttachDebuggerMessageReceived;
            server.Start();
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
                message.ErrorMessage = $"Could not attach debugger to process {message.ProcessId} because of exception on server side: {e.Message}";
            }
            finally
            {
                try
                {
                    _server.PushMessage(message);
                }
                catch (Exception e)
                {
                    // TODO logging
                }
            }
        }

        public void Dispose()
        {
            _server?.Stop();
            _server = null;
        }
    }

}