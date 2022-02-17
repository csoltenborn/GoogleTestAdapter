using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleTestAdapter.Common
{
    public static class ProcessUtils
    {
        public static void KillProcess(int processId, ILogger logger)
        {
            logger.DebugInfo($"Scheduling to kill process with id {processId}...");
            Task.Run(() => DoKillProcess(processId, logger));
        }

        private static void DoKillProcess(int processId, ILogger logger)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                DateTime startTime = process.StartTime;
                try
                {
                    logger.DebugInfo($"Trying to kill process {process} with startTime={startTime.ToShortTimeString()}");
                    process.Kill();
                    logger.DebugInfo($"Invoked Kill() on process {process} with startTime={startTime.ToShortTimeString()}, waiting for it to exit...");
                    process.WaitForExit();
                    if (process.HasExited)
                    {
                        logger.DebugInfo($"Successfully killed process {process} with startTime={startTime.ToShortTimeString()}");
                    }
                    else
                    {
                        logger.DebugWarning($"Wasn't able to kill process {process} with startTime={startTime.ToShortTimeString()}...");
                    }
                }
                catch (Exception e)
                {
                    logger.DebugWarning($"Could not kill process {process} with startTime={startTime.ToShortTimeString()}: {e.Message}");
                }
            }
            catch (Exception)
            {
                // process was not running - nothing to do
                logger.DebugInfo($"Process with id {processId} should be killed, but apparently was not running");
            }
        }
    }
}
