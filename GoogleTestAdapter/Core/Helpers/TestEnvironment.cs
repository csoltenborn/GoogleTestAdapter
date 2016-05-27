using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment : LoggerBase
    {

        private enum LogType { Normal, Debug }


        public SettingsWrapper Options { get; }
        private readonly ILogger _logger;


        public TestEnvironment(SettingsWrapper options, ILogger logger)
        {
            Options = options;
            _logger = logger;
        }


        public override void Log(Severity severity, string message)
        {
            if (!ShouldBeLogged(LogType.Normal))
                return;

            switch (severity)
            {
                case Severity.Info:
                    _logger.LogInfo(message);
                    break;
                case Severity.Warning:
                    _logger.LogWarning(message);
                    break;
                case Severity.Error:
                    _logger.LogError(message);
                    break;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }
        }

        public void DebugInfo(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                _logger.LogInfo(message);
            }
        }

        public void DebugWarning(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                _logger.LogWarning(message);
            }
        }

        public void DebugError(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                _logger.LogError(message);
            }
        }


        private bool ShouldBeLogged(LogType logType)
        {
            switch (logType)
            {
                case LogType.Normal:
                    return true;
                case LogType.Debug:
                    return Options.DebugMode;
                default:
                    throw new Exception("Unknown LogType: " + logType);
            }
        }

    }

}