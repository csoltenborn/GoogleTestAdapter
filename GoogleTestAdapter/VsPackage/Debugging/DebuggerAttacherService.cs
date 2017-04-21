using System;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.VsPackage.Helpers;
using NamedPipeWrapper;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    public class DebuggerAttacherService
    {
        private readonly int _visualStudioProcessId;
        private readonly NamedPipeServer<AttachDebuggerMessage> _server;

        public DebuggerAttacherService(int visualStudioProcessId)
        {
            _visualStudioProcessId = visualStudioProcessId;
            _server = CreateAndStartPipeServer();
        }

        private NamedPipeServer<AttachDebuggerMessage> CreateAndStartPipeServer()
        {
            // TODO what to do if NamedPipe can not be created?
            var server = new NamedPipeServer<AttachDebuggerMessage>(MessageBasedDebuggerAttacher.GetPipeName(_visualStudioProcessId));
            server.ClientMessage += AttachDebugger;
            server.Start(); 
            return server;
        }

        private void AttachDebugger(NamedPipeConnection<AttachDebuggerMessage, AttachDebuggerMessage> connection,
            AttachDebuggerMessage message)
        {
            try
            {
                var debuggerAttacher = new VsDebuggerAttacher(new ConsoleLogger());
                message.DebuggerAttachedSuccessfully = debuggerAttacher.AttachDebugger(message.ProcessId);
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

    }

}