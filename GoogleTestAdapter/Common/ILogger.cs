using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleTestAdapter.Common
{

    public enum Severity { Info, Warning, Error }
    public enum OutputMode { None = 0, Info = 10, Debug = 20, Verbose = 30 }

    [TypeConverter(typeof(TimestampModeConverter))]
    public enum TimestampMode { Automatic, PrintTimestamp, DoNotPrintTimestamp }

    [TypeConverter(typeof(SeverityModeConverter))]
    public enum SeverityMode { Automatic, PrintSeverity, DoNotPrintSeverity }

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

    public class TimestampModeConverter : EnumConverterBase<TimestampMode>
    {
        public const string Automatic = "Automatic";
        public const string PrintTimeStamp = "Print timestamp";
        public const string DoNotPrintTimeStamp = "Do not print timestamp";

        public TimestampModeConverter() : base(new Dictionary<TimestampMode, string>
        {
            {TimestampMode.Automatic, Automatic},
            {TimestampMode.PrintTimestamp, PrintTimeStamp},
            {TimestampMode.DoNotPrintTimestamp, DoNotPrintTimeStamp}
        }){}
    }

    public class SeverityModeConverter : EnumConverterBase<SeverityMode>
    {
        public const string Automatic = "Automatic";
        public const string PrintSeverity = "Print severity";
        public const string DoNotPrintSeverity = "Do not print severity";

        public SeverityModeConverter() : base(new Dictionary<SeverityMode, string>
        {
            {SeverityMode.Automatic, Automatic},
            {SeverityMode.PrintSeverity, PrintSeverity},
            {SeverityMode.DoNotPrintSeverity, DoNotPrintSeverity}
        }){}
    }

}