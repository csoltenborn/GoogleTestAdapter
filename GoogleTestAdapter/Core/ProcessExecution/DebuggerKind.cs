using System.Collections.Generic;
using System.ComponentModel;
using GoogleTestAdapter.Common;

// ReSharper disable NotResolvedInText

namespace GoogleTestAdapter.ProcessExecution
{
    [TypeConverter(typeof(DebuggerKindConverter))]
    public enum DebuggerKind { VsTestFramework, Native, ManagedAndNative }

    public static class DebuggerKindExtensions
    {
        private static readonly DebuggerKindConverter Converter = new DebuggerKindConverter();

        public static string ToReadableString(this DebuggerKind debuggerKind)
        {
            return Converter.ConvertToString(debuggerKind);
        }
    }

    public class DebuggerKindConverter : EnumConverterBase<DebuggerKind>
    {
        public const string VsTestFramework = "VsTest framework";
        public const string Native = "Native";
        public const string ManagedAndNative = "Managed and native";

        public DebuggerKindConverter() : base(new Dictionary<DebuggerKind, string>
        {
            { DebuggerKind.VsTestFramework, VsTestFramework},
            { DebuggerKind.Native, Native},
            { DebuggerKind.ManagedAndNative, ManagedAndNative},
        }) {}

    }

}