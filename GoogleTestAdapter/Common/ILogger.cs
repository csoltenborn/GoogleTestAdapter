using System.Collections.Generic;

namespace GoogleTestAdapter.Common
{

    public enum Severity { Info, Warning, Error }
    public enum OutputMode { None = 0, Info = 10, Debug = 20, Verbose = 30 }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void DebugInfo(string message);
        void DebugWarning(string message);
        void DebugError(string message);
        void VerboseInfo(string message);

        IList<string> GetMessages(params Severity[] severities);

    }

}