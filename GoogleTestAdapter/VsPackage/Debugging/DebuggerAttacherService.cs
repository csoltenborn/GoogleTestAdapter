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
            _server = SetupNamedPipeServer();
        }

        private NamedPipeServer<AttachDebuggerMessage> SetupNamedPipeServer()
        {
            var server = new NamedPipeServer<AttachDebuggerMessage>(MessageBasedDebuggerAttacher.GetPipeName(_visualStudioProcessId));
            server.ClientMessage += AttachDebugger;
            server.Start();
            return server;
        }

        private void AttachDebugger(NamedPipeConnection<AttachDebuggerMessage, AttachDebuggerMessage> connection,
            AttachDebuggerMessage message)
        {
            var debuggerAttacher = new VsDebuggerAttacher(new ConsoleLogger());
            if (debuggerAttacher.AttachDebugger(message.ProcessId))
                _server.PushMessage(message);
        }

    }

}