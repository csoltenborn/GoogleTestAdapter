using System;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment : LoggerBase
    {

        private enum LogType { Normal, Debug }


        public Options Options { get; }
        private ILogger Logger { get; }


        public TestEnvironment(Options options, ILogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }


        public override void Log(Severity severity, string message)
        {
            if (!ShouldBeLogged(LogType.Normal))
                return;

            switch (severity)
            {
                case Severity.Info:
                    Logger.LogInfo(message);
                    break;
                case Severity.Warning:
                    Logger.LogWarning(message);
                    break;
                case Severity.Error:
                    Logger.LogError(message);
                    break;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }
        }

        public void DebugInfo(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogInfo(message);
            }
        }

        public void DebugWarning(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogWarning(message);
            }
        }

        public void DebugError(string message)
        {
            if (ShouldBeLogged(LogType.Debug))
            {
                Logger.LogError(message);
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