using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleTestAdapter.Common
{

    public enum Severity { Info, Warning, Error }
    public enum OutputMode { None = 0, Info = 10, Debug = 20, Verbose = 30 }

    public static class TimeStampModeDescriptions
    {
        public const string Automatic = "Automatic";
        public const string PrintTimestamp = "Print timestamp";
        public const string DoNotPrintTimestamp = "Do not print timestamp";
    }

    [TypeConverter(typeof(AttributedEnumConverter))]
    public enum TimestampMode
    {
        [Description(TimeStampModeDescriptions.Automatic)]
        Automatic, 
        [Description(TimeStampModeDescriptions.PrintTimestamp)]
        PrintTimestamp, 
        [Description(TimeStampModeDescriptions.DoNotPrintTimestamp)]
        DoNotPrintTimestamp
    }

    public static class SeverityModeDescriptions
    {
        public const string Automatic = "Automatic";
        public const string PrintSeverity = "Print severity";
        public const string DoNotPrintSeverity = "Do not print severity";
    }

    [TypeConverter(typeof(AttributedEnumConverter))]
    public enum SeverityMode
    {
        [Description(SeverityModeDescriptions.Automatic)]
        Automatic, 
        [Description(SeverityModeDescriptions.PrintSeverity)]
        PrintSeverity, 
        [Description(SeverityModeDescriptions.DoNotPrintSeverity)]
        DoNotPrintSeverity
    }

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