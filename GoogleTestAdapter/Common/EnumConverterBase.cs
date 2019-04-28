using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace GoogleTestAdapter.Common
{
    public abstract class EnumConverterBase<TEnum> : EnumConverter
    {
        private readonly IDictionary<TEnum, string> _stringMap;

        protected EnumConverterBase(IDictionary<TEnum, string> stringMap) : base(typeof(TEnum))
        {
            if (stringMap == null)
                throw new ArgumentNullException(nameof(stringMap));

            if (stringMap.Count != Enum.GetValues(typeof(TEnum)).Length)
                throw new ArgumentException(nameof(stringMap), "Map must have the same size as the enum");

            if (stringMap.Values.Distinct().Count() != stringMap.Values.Count)
                throw new ArgumentException(nameof(stringMap), "Values of map must be pairwise distinct strings");

            _stringMap = stringMap;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(TEnum) || 
                   base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || 
                   base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString)
            {
                foreach (KeyValuePair<TEnum, string> kvp in _stringMap)
                {
                    if (kvp.Value == valueString)
                        return kvp.Key;
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!(value is TEnum enumLiteral) || destinationType != typeof(string))
                return base.ConvertTo(context, culture, value, destinationType);

            if (_stringMap.TryGetValue(enumLiteral, out string stringValue))
                return stringValue;

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}