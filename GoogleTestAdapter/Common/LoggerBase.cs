namespace GoogleTestAdapter.Common
{

    public abstract class LoggerBase : ILogger
    {

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

    }

}