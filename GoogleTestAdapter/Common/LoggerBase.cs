using System;

namespace GoogleTestAdapter.Common
{

    public abstract class LoggerBase : ILogger
    {
        private readonly Func<bool> _inDebugMode;

        protected LoggerBase(Func<bool> inDebugMode)
        {
            _inDebugMode = inDebugMode;
        }

        public abstract void Log(Severity severity, string message);

        public virtual void LogInfo(string message)
        {
            Log(Severity.Info, message);
        }

        public virtual void LogWarning(string message)
        {
            Log(Severity.Warning, message);
        }

        public virtual void LogError(string message)
        {
            Log(Severity.Error, message);
        }

        public void DebugInfo(string message)
        {
            if (_inDebugMode())
                LogInfo(message);
        }

        public void DebugWarning(string message)
        {
            if (_inDebugMode())
                LogWarning(message);
        }

        public void DebugError(string message)
        {
            if (_inDebugMode())
                LogError(message);
        }
    }

}