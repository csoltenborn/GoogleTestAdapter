using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    public class VsTestFrameworkLogger : LoggerBase
    {
        private readonly IMessageLogger _logger;
        private readonly Func<TimestampMode> _timeStampMode;
        private readonly Func<SeverityMode> _severityMode;

        public VsTestFrameworkLogger(IMessageLogger logger, Func<OutputMode> outputMode, Func<TimestampMode> timestampMode, Func<SeverityMode> severityMode)
            : base(outputMode)
        {
            _logger = logger;
            _timeStampMode = timestampMode;
            _severityMode = severityMode;
        }


        public override void Log(Severity severity, string message)
        {
            TestMessageLevel level = GetTestMessageLevel(severity);

            var timestampMode = _timeStampMode();
            string timestamp = "";
            if (timestampMode == TimestampMode.PrintTimestamp ||
                timestampMode == TimestampMode.Automatic && VsVersionUtils.GetVisualStudioVersion() < VsVersion.VS2017)
            {
                timestamp = Utils.GetTimestamp();
            }

            var severityMode = _severityMode();
            string severityString = "";
            if (severityMode == SeverityMode.PrintSeverity ||
                severityMode == SeverityMode.Automatic && VsVersionUtils.GetVisualStudioVersion() < VsVersion.VS2017)
            {
                severityString = GetSeverity(level);
            }

            if (string.IsNullOrWhiteSpace(timestamp))
            {
                if (!string.IsNullOrWhiteSpace(severityString))
                {
                    message = $"{severityString} - {message}";
                }
            }
            else
            {
                message = string.IsNullOrWhiteSpace(severityString) 
                    ? $"{timestamp} - {message}" 
                    : $"{timestamp} {severityString} - {message}";
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                message = "\u2063";
            }

            _logger.SendMessage(level, message);
            ReportFinalLogEntry(
                new LogEntry
                {
                    Severity = level.GetSeverity(),
                    Message = message
                });
        }

        private TestMessageLevel GetTestMessageLevel(Severity severity)
        {
            switch (severity)
            {
                case Severity.Info:
                    return TestMessageLevel.Informational;
                case Severity.Warning:
                    return TestMessageLevel.Warning;
                case Severity.Error:
                    return TestMessageLevel.Error;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }
        }

        private string GetSeverity(TestMessageLevel level)
        {
            switch (level)
            {
                case TestMessageLevel.Informational: return "";
                case TestMessageLevel.Warning: return "Warning";
                case TestMessageLevel.Error: return "ERROR";
                default:
                    throw new InvalidOperationException($"Unknown literal {level}");
            }
        }
    }

}