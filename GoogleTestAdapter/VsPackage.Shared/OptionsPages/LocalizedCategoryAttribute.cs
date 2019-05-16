// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{
    class LocalizedCategoryAttribute : CategoryAttribute
    {
        private readonly string ResourceName;

        public LocalizedCategoryAttribute(string resourceName)
            : base()
        {
            this.ResourceName = resourceName;
        }

        protected override string GetLocalizedString(string value)
        {
            return (string)typeof(SettingsWrapper).GetField(ResourceName).GetValue(null);
        }
    }
}
