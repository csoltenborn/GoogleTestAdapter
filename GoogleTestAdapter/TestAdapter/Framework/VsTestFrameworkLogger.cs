using System;
using System.Text;
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
        private readonly Func<bool> _prefixOutput;

        public VsTestFrameworkLogger(IMessageLogger logger, Func<OutputMode> outputMode, Func<TimestampMode> timestampMode, Func<SeverityMode> severityMode, Func<bool> prefixOutput)
            : base(outputMode)
        {
            _logger = logger;
            _timeStampMode = timestampMode;
            _severityMode = severityMode;
            _prefixOutput = prefixOutput;
        }


        public override void Log(Severity severity, string message)
        {
            TestMessageLevel level = severity.GetTestMessageLevel();

            var builder = new StringBuilder();
            AppendOutputPrefix(builder, level);
            builder.Append(message);

            var finalMessage = builder.ToString();
            if (string.IsNullOrWhiteSpace(finalMessage))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                finalMessage = "\u2063";
            }

            _logger.SendMessage(level, finalMessage);
            ReportFinalLogEntry(
                new LogEntry
                {
                    Severity = level.GetSeverity(),
                    Message = finalMessage
                });
        }

        private void AppendOutputPrefix(StringBuilder builder, TestMessageLevel level)
        {
            if (_prefixOutput())
            {
                builder.Append("GTA");
            }

            var timestampMode = _timeStampMode();
            if (timestampMode == TimestampMode.PrintTimestamp ||
                timestampMode == TimestampMode.Automatic && !VsVersionUtils.VsVersion.PrintsTimeStampAndSeverity())
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(Utils.GetTimestamp());
            }

            var severityMode = _severityMode();
            if (level > TestMessageLevel.Informational &&
                (severityMode == SeverityMode.PrintSeverity ||
                 severityMode == SeverityMode.Automatic && !VsVersionUtils.VsVersion.PrintsTimeStampAndSeverity()))
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(GetSeverity(level));
            }

            if (builder.Length > 0)
            {
                builder.Insert(0, '[');
                builder.Append("] ");
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