namespace GoogleTestAdapter.Common
{

    public enum Severity { Info, Warning, Error }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);

        void Log(Severity severity, string message);
    }

}