// This file has been modified by Microsoft on 8/2017.

using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using System;
using System.Diagnostics;
using System.ServiceModel;

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
                var stopWatch = Stopwatch.StartNew();

                var proxy = DebuggerAttacherServiceConfiguration.CreateProxy(_debuggingNamedPipeId, _timeout);
                using (var client = new DebuggerAttacherServiceProxyWrapper(proxy))
                {
                    client.Service.AttachDebugger(processId);
                    stopWatch.Stop();
                    _logger.DebugInfo(String.Format(Resources.DebuggerAttachTime, processId, stopWatch.ElapsedMilliseconds));
                    return true;
                }
            }
            catch (FaultException<DebuggerAttacherServiceFault> serviceFault)
            {
                var errorMessage = serviceFault.Detail.Message;
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = String.Format(Resources.DebuggerAttachMessage, processId);

                errorMessage += $"{Environment.NewLine}{Resources.MoreInformationMessage}";

                _logger.LogError(errorMessage);
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.CouldNotAttachMessage, processId, Environment.NewLine, e));
            }
            return false;
        }
    }

}