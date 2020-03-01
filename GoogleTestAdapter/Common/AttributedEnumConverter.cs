using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace GoogleTestAdapter.Common
{
    public class AttributedEnumConverter : EnumConverter
    {
        public AttributedEnumConverter(Type type) : base(type)
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) 
                   || TypeDescriptor.GetConverter(typeof(Enum)).CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            object result = null;
            switch (value)
            {
                case string stringValue:
                    result = GetEnumValue(EnumType, stringValue);
                    break;
            }

            return result ?? base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            object result = null;
            switch (value)
            {
                case Enum enumValue when destinationType == typeof(string):
                    result = GetEnumDescription(enumValue);
                    break;
                case string stringValue when destinationType == typeof(string):
                    result = GetEnumDescription(EnumType, stringValue);
                    break;
            }
            
            return result ?? base.ConvertTo(context, culture, value, destinationType);
        }

        private static string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null)
                return null;

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.FirstOrDefault()?.Description;
        }

        private static string GetEnumDescription(Type value, string name)
        {
            var fieldInfo = value.GetField(name);
            if (fieldInfo == null)
                return null;

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.FirstOrDefault()?.Description ?? name;
        }

        private static object GetEnumValue(Type value, string description)
        {
            var fields = value.GetFields();
            foreach (var fieldInfo in fields)
            {
                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0 && attributes[0].Description == description)
                    return fieldInfo.GetValue(fieldInfo.Name);
                if (fieldInfo.Name == description)
                    return fieldInfo.GetValue(fieldInfo.Name);
            }

            return null;
        }
    }
}