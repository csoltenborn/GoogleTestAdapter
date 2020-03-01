using System.ComponentModel;

namespace GoogleTestAdapter.Common
{
    public static class MissingTestsReportModeDescriptions
    {
        public const string DoNotReport = "Do not report";
        public const string ReportAsNotFound = "Report as not found";
        public const string ReportAsSkipped = "Report as skipped";
        public const string ReportAsFailed = "Report as failed";
    }

    [TypeConverter(typeof(AttributedEnumConverter))]
    public enum MissingTestsReportMode
    {
        [Description(MissingTestsReportModeDescriptions.DoNotReport)]
        DoNotReport,
        [Description(MissingTestsReportModeDescriptions.ReportAsNotFound)]
        ReportAsNotFound,
        [Description(MissingTestsReportModeDescriptions.ReportAsSkipped)]
        ReportAsSkipped,
        [Description(MissingTestsReportModeDescriptions.ReportAsFailed)]
        ReportAsFailed
    }
}