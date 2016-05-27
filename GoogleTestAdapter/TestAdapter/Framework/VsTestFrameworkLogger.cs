using System;
using System.Globalization;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    public class VsTestFrameworkLogger : LoggerBase
    {
        private static readonly object Lock = new object();

        private readonly SettingsWrapper _settings;
        private readonly IMessageLogger _logger;

        public VsTestFrameworkLogger(IMessageLogger logger, SettingsWrapper settings)
        {
            _logger = logger;
            _settings = settings;
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
            if (_settings.TimestampOutput)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                message = $"{timestamp} - {message ?? ""}";
            }

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