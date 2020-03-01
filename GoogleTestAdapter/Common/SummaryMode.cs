using System.ComponentModel;

namespace GoogleTestAdapter.Common
{
    [TypeConverter(typeof(AttributedEnumConverter))]
    public enum SummaryMode
    {
        [Description("Never")]
        Never,
        [Description("If errors occured")]
        Error,
        [Description("If warnings or errors occured")]
        WarningOrError
    }
}