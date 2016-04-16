namespace GoogleTestAdapter.Common
{

    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

}