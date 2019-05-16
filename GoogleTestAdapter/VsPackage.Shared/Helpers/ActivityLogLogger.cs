﻿// This file has been modified by Microsoft on 7/2017.

using System;
using GoogleTestAdapter.Common;
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
            if (activityLog == null)
            {
                Console.WriteLine($"{Common.Resources.ExtensionName}: {severity} - {message}");
                return;
            }

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
                    throw new Exception($"Unknown enum literal: {severity}");
            }

            activityLog.LogEntry((uint)activitylogEntrytype, Common.Resources.ExtensionName, message);
        }
    }

}