using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoogleTestAdapter.Settings
{
    public class SettingsPrinter
    {
        private static readonly string[] NotPrintedProperties =
        {
            nameof(SettingsWrapper.RegexTraitParser),
            nameof(SettingsWrapper.DebuggingNamedPipeId),
            nameof(SettingsWrapper.SolutionDir)
        };

        private static readonly PropertyInfo[] PropertiesToPrint = typeof(SettingsWrapper)
            .GetProperties()
            .Where(pi => !NotPrintedProperties.Contains(pi.Name))
            .OrderBy(p => p.Name)
            .ToArray();


        private readonly SettingsWrapper _settings;

        public SettingsPrinter(SettingsWrapper settings)
        {
            _settings = settings;
        }

        public string ToReadableString()
        {
            return string.Join(", ", PropertiesToPrint.Select(ToString));
        }

        /// <summary>The values of the settings, by property name, in the order of ToReadableString()</summary>
        public IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            return PropertiesToPrint.Select(pi => new KeyValuePair<string, object>(pi.Name, pi.GetValue(_settings)));
        }

        private string ToString(PropertyInfo propertyInfo)
        {
            var value = propertyInfo.GetValue(_settings);
            if (value is string)
                return $"{propertyInfo.Name}: '{value}'";

            if (value is IEnumerable<RegexTraitPair> pairs)
                return $"{propertyInfo.Name}: {{{string.Join(", ", pairs)}}}";

            return $"{propertyInfo.Name}: {value}";
        }

    }
}