// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.Settings;
using System;
using System.ComponentModel;
using GoogleTestAdapter.Helpers;
// ReSharper disable LocalizableElement

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class TestDiscoveryOptionsDialogPage : NotifyingDialogPage
    {
        #region Misc

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryRegex)]
        [Description(SettingsWrapper.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex
        {
            get => _testDiscoveryRegex;
            set
            {
                Utils.ValidateRegex(value);
                SetAndNotify(ref _testDiscoveryRegex, value);
            }
        }
        private string _testDiscoveryRegex = SettingsWrapper.OptionTestDiscoveryRegexDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryTimeoutInSeconds)]
        [Description(SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDescription)]
        public int TestDiscoveryTimeoutInSeconds
        {
            get => _testDiscoveryTimeoutInSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(TestDiscoveryTimeoutInSeconds), value, "Expected a number greater than or equal to 0.");
                SetAndNotify(ref _testDiscoveryTimeoutInSeconds, value);
            }
        }
        private int _testDiscoveryTimeoutInSeconds = SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTestNameSeparator)]
        [Description(SettingsWrapper.OptionTestNameSeparatorDescription)]
        public string TestNameSeparator
        {
            get => _testNameSeparator;
            set
            {
                if (value.Length > 16)
                    throw new ArgumentOutOfRangeException(nameof(TestNameSeparator), value, "Expected string not longer than 16 characters.");
                SetAndNotify(ref _testNameSeparator, value);
            }
        }
        private string _testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionParseSymbolInformation)]
        [Description(SettingsWrapper.OptionParseSymbolInformationDescription)]
        public bool ParseSymbolInformation
        {
            get => _parseSymbolInformation;
            set => SetAndNotify(ref _parseSymbolInformation, value);
        }
        private bool _parseSymbolInformation = SettingsWrapper.OptionParseSymbolInformationDefaultValue;

        #endregion

        #region Traits

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesBefore)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
        public string TraitsRegexesBefore
        {
            get => _traitsRegexesBefore;
            set
            {
                Utils.ValidateTraitRegexes(value);
                SetAndNotify(ref _traitsRegexesBefore, value);
            }
        }
        private string _traitsRegexesBefore = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesAfter)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
        public string TraitsRegexesAfter
        {
            get => _traitsRegexesAfter;
            set
            {
                Utils.ValidateTraitRegexes(value);
                SetAndNotify(ref _traitsRegexesAfter, value);
            }
        }
        private string _traitsRegexesAfter = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        #endregion

    }

}