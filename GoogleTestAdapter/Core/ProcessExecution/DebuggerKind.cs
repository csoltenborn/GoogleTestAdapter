using System;
using System.ComponentModel;
using System.Globalization;

namespace GoogleTestAdapter.ProcessExecution
{
    [TypeConverter(typeof(DebuggerKindConverter))]
    public enum DebuggerKind { VsTestFramework, Native, ManagedAndNative }

    public static class DebuggerKindExtensions
    {
        public static string ToReadableString(this DebuggerKind debuggerKind)
        {
            return TypeDescriptor.GetConverter(typeof(DebuggerKind)).ConvertToString(debuggerKind);
        }
    }

    public class DebuggerKindConverter : EnumConverter
    {
        public const string VsTestFramework = "VsTest framework";
        public const string Native = "Native";
        public const string ManagedAndNative = "Managed and native";


        public DebuggerKindConverter(Type enumType) : base(enumType) {}

        public DebuggerKindConverter() : this(typeof(DebuggerKind)) {}

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || 
                   base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(DebuggerKind) || 
                   base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!(value is DebuggerKind debuggerKind) || destinationType != typeof(string))
                return base.ConvertTo(context, culture, value, destinationType);

            switch (debuggerKind)
            {
                case DebuggerKind.VsTestFramework: return VsTestFramework;
                case DebuggerKind.Native: return Native;
                case DebuggerKind.ManagedAndNative: return ManagedAndNative;
                default:
                    return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (!(value is string valueString)) 
                return base.ConvertFrom(context, culture, value);

            switch (valueString)
            {
                case VsTestFramework: return DebuggerKind.VsTestFramework;
                case Native: return DebuggerKind.Native;
                case ManagedAndNative: return DebuggerKind.ManagedAndNative;
                default:
                    return base.ConvertFrom(context, culture, value);
            }
        }

    }

}