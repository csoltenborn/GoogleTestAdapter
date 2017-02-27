using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GoogleTestAdapter.Common
{

    public abstract class LoggerBase : ILogger
    {
        protected class LogEntry
        {
            public Severity Severity { get; set; }
            public string Message { get; set; }
        }

        private readonly IList<LogEntry> _messages = new List<LogEntry>();

        private readonly Func<bool> _inDebugMode;

        protected LoggerBase(Func<bool> inDebugMode)
        {
            _inDebugMode = inDebugMode;
        }

        public abstract void Log(Severity severity, string message);

        protected void ReportFinalLogEntry(LogEntry logEntry)
        {
            _messages.Add(logEntry);
        }

        public IList<string> GetMessages(params Severity[] severities)
        {
            return _messages
                .Where(le => severities.Contains(le.Severity))
                .Select(le => le.Message)
                .ToList();
        }

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

        public static void TimestampMessage(ref string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            message = $"{timestamp} - {message ?? ""}";
        }
    }

}