using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleTestAdapter.Common
{
    [TypeConverter(typeof(MissingTestsReportModeConverter))]
    public enum MissingTestsReportMode
    {
        DoNotReport,
        ReportAsNotFound,
        ReportAsSkipped,
        ReportAsFailed
    }

    public class MissingTestsReportModeConverter : EnumConverterBase<MissingTestsReportMode>
    {
        public const string DoNotReport = "Do not report";
        public const string ReportAsNotFound = "Report as not found";
        public const string ReportAsSkipped = "Report as skipped";
        public const string ReportAsFailed = "Report as failed";

        public MissingTestsReportModeConverter() : base(new Dictionary<MissingTestsReportMode, string>
        {
            { MissingTestsReportMode.DoNotReport, DoNotReport},
            { MissingTestsReportMode.ReportAsNotFound, ReportAsNotFound},
            { MissingTestsReportMode.ReportAsSkipped, ReportAsSkipped},
            { MissingTestsReportMode.ReportAsFailed, ReportAsFailed},
        }) {}
    }

}