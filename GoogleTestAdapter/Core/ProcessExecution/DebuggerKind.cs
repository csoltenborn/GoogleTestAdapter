using System.ComponentModel;
using GoogleTestAdapter.Common;

// ReSharper disable NotResolvedInText

namespace GoogleTestAdapter.ProcessExecution
{
    public static class DebuggerKindDescriptions
    {
        public const string VsTestFramework = "VsTest framework";
        public const string Native = "Native";
        public const string ManagedAndNative = "Managed and native";
    }

    [TypeConverter(typeof(AttributedEnumConverter))]
    public enum DebuggerKind
    {
        [Description(DebuggerKindDescriptions.VsTestFramework)]
        VsTestFramework, 
        [Description(DebuggerKindDescriptions.Native)]
        Native, 
        [Description(DebuggerKindDescriptions.ManagedAndNative)]
        ManagedAndNative
    }

    public static class DebuggerKindExtensions
    {
        private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(DebuggerKind));

        public static string ToReadableString(this DebuggerKind debuggerKind)
        {
            return Converter.ConvertToString(debuggerKind);
        }
    }
}