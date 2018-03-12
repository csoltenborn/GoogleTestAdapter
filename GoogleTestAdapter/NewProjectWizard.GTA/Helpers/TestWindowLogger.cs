using System;
using EnvDTE;
using EnvDTE80;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NewProjectWizard.GTA.Helpers
{
    public class TestWindowLogger : LoggerBase
    {
        private readonly Func<bool> _isTimeStampingEnabled;
        private const string TestOutputWindowGuid = "{B85579AA-8BE0-4C4F-A850-90902B317581}";

        public TestWindowLogger(Func<bool> isDebugModeEnabled, Func<bool> isTimeStampingEnabled) : base(isDebugModeEnabled)
        {
            _isTimeStampingEnabled = isTimeStampingEnabled;
        }

        public override void Log(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Info:
                    LogSafe(severity, message);
                    break;
                case Severity.Warning:
                    LogSafe(severity, $"Warning: {message}");
                    break;
                case Severity.Error:
                    LogSafe(severity, $"ERROR: {message}");
                    break;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }
        }

        private void LogSafe(Severity level, string message)
        {
            if (_isTimeStampingEnabled())
                Utils.TimestampMessage(ref message);

            if (string.IsNullOrWhiteSpace(message))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                message = "\u2063";
            }

            LogToTestWindow(message, level != Severity.Info);
            ReportFinalLogEntry(
                new LogEntry
                {
                    Severity = level,
                    Message = message
                });
        }


        private void LogToTestWindow(string message, bool bringToFront = false)
        {
            var testOutputPane = GetTestOutputWindowPane();
            if (bringToFront)
            {
                testOutputPane?.Activate();
            }

            testOutputPane?.OutputString($"{message}{Environment.NewLine}");
        }

        private OutputWindowPane GetTestOutputWindowPane()
        {
            if (!(Package.GetGlobalService(typeof(SDTE)) is DTE2 dte))
            {
                return null;
            }
  
            foreach (OutputWindowPane pane in dte.ToolWindows.OutputWindow.OutputWindowPanes)
            {
                if (pane.Guid.Equals(TestOutputWindowGuid))
                {
                    return pane;
                }  
            }

            return null;
        }

    }

}