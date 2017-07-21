// This file has been modified by Microsoft on 7/2017.

using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using System.ServiceModel;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    /// <summary>
    /// Implements IDebuggerAttacherService to expose Visual Studio interfaces for the out-of-process adapter.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class DebuggerAttacherService : IDebuggerAttacherService
    {
        private readonly IDebuggerAttacher _debuggerAttacher;
        private readonly ILogger _logger;

        public DebuggerAttacherService(IDebuggerAttacher debuggerAttacher, ILogger logger)
        {
            _debuggerAttacher = debuggerAttacher;
            _logger = logger;
        }

        public void AttachDebugger(int processId)
        {
            bool success = false;
            try
            {
                success = _debuggerAttacher.AttachDebugger(processId);
            }
            catch (Exception e)
            {
                ThrowFaultException($"Could not attach debugger to process {processId} because of exception on the server side:{Environment.NewLine}{e}");
            }
            if (!success)
            {
                ThrowFaultException($"Could not attach debugger to process {processId} for unknown reasons");
            }
        }

        private void ThrowFaultException(string message)
        {
            throw new FaultException<DebuggerAttacherServiceFault>(new DebuggerAttacherServiceFault(message));
        }
    }

    public class DebuggerAttacherServiceHost : ServiceHost
    {
        /// <summary>
        /// Constructs the host for DebuggerAttacherService.
        /// </summary>
        /// <param name="id">Identifier of the service end-point</param>
        public DebuggerAttacherServiceHost(string id, IDebuggerAttacher debuggerAttacher, ILogger logger) :
            base(new DebuggerAttacherService(debuggerAttacher, logger), new Uri[] {
                DebuggerAttacherServiceConfiguration.ConstructPipeUri(id)
            })
        {
            AddServiceEndpoint(typeof(IDebuggerAttacherService), new NetNamedPipeBinding(), DebuggerAttacherServiceConfiguration.InterfaceAddress);
        }
    }
}