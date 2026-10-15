// This file has been modified by Microsoft on 7/2017.

using System;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.VsPackage.Helpers
{
    public class ActivityLogLogger : LoggerBase
    {
        private readonly GoogleTestExtensionOptionsPage _package;

        public ActivityLogLogger(GoogleTestExtensionOptionsPage package, Func<OutputMode> outputMode) : base(outputMode)
        {
            _package = package;
        }

        public override void Log(Severity severity, string message)
        {
            var activityLog = _package.GetActivityLog();
            if (activityLog == null || ErrorHandler.Failed(WriteToActivityLog(activityLog, severity, message)))
            {
                Console.WriteLine($"{Strings.Instance.ExtensionName}: {severity} - {message}");
            }
        }

        private static int WriteToActivityLog(IVsActivityLog activityLog, Severity severity, string message)
        {
            __ACTIVITYLOG_ENTRYTYPE activitylogEntrytype;
            switch (severity)
            {
                case Severity.Info:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;
                    break;
                case Severity.Warning:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;
                    break;
                case Severity.Error:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown enum literal");
            }

            return activityLog.LogEntry((uint)activitylogEntrytype, Strings.Instance.ExtensionName, message);
        }
    }

}