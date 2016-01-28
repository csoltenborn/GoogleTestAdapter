using System;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment : ILogger
    {

        private enum LogType { Normal, Debug }


        public Options Options { get; }
        private ILogger Logger { get; }


        public TestEnvironment(Options options, ILogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }


        public void LogInfo(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogInfo(message);
            }
        }

        public void LogWarning(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogWarning(message);
            }
        }

        public void LogError(string message)
        {
            if (ShouldBeLogged(LogType.Normal))
            {
                Logger.LogError(message);
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