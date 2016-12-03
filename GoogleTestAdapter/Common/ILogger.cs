using System.Collections.Generic;

namespace GoogleTestAdapter.Common
{

    public enum Severity { Info, Warning, Error }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void DebugInfo(string message);
        void DebugWarning(string message);
        void DebugError(string message);

        IList<string> GetMessages(params Severity[] severities);

    }

}