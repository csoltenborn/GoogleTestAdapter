using System;
using System.Diagnostics;

namespace GoogleTestAdapter.Common
{
    public static class ProcessUtils
    {
        public static void KillProcess(int processId, ILogger logger)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                DateTime startTime = process.StartTime;
                try
                {
                    process.Kill();
                    logger.DebugInfo($"Killed process {process} with startTime={startTime.ToShortTimeString()}");
                }
                catch (Exception e)
                {
                    logger.DebugWarning($"Could not kill process {process} with startTime={startTime.ToShortTimeString()}: {e.Message}");
                }
            }
            catch (Exception)
            {
                // process was not running - nothing to do
            }
        }
    }
}
