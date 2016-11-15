using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    public class VsTestFrameworkLogger : LoggerBase
    {
        private static readonly object Lock = new object();

        private readonly IMessageLogger _logger;
        private readonly Func<bool> _timeStampOutput;

        public VsTestFrameworkLogger(IMessageLogger logger, Func<bool> inDebugMode, Func<bool> timestampOutput)
            : base(inDebugMode)
        {
            _logger = logger;
            _timeStampOutput = timestampOutput;
        }


        public override void Log(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Info:
                    LogSafe(TestMessageLevel.Informational, message);
                    break;
                case Severity.Warning:
                    LogSafe(TestMessageLevel.Warning, $"Warning: {message}");
                    break;
                case Severity.Error:
                    LogSafe(TestMessageLevel.Error, $"ERROR: {message}");
                    break;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }
        }


        private void LogSafe(TestMessageLevel level, string message)
        {
            if (_timeStampOutput())
                Utils.TimestampMessage(ref message);

            if (string.IsNullOrWhiteSpace(message))
            {
                // Visual Studio 2013 is very picky about empty lines...
                // But it accepts an 'INVISIBLE SEPARATOR' (U+2063)  :-)
                message = "\u2063";
            }

            lock (Lock)
            {
                _logger.SendMessage(level, message);
            }
        }

    }

}