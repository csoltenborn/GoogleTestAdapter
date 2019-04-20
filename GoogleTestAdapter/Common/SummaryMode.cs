using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleTestAdapter.Common
{
    [TypeConverter(typeof(SummaryModeConverter))]
    public enum SummaryMode
    {
        Never,
        Error,
        WarningOrError
    }

    public class SummaryModeConverter : EnumConverterBase<SummaryMode>
    {
        public const string Never = "Never";
        public const string Error = "If errors occured";
        public const string WarningOrError = "If warnings or errors occured";

        public SummaryModeConverter() : base(new Dictionary<SummaryMode, string>
        {
            { SummaryMode.Never, Never},
            { SummaryMode.Error, Error},
            { SummaryMode.WarningOrError, WarningOrError},
        }) {}
    }

}