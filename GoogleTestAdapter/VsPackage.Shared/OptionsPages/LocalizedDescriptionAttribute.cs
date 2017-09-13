// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{
    class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string ResourceName;

        public LocalizedDescriptionAttribute(string resourceName)
            : base()
        {
            this.ResourceName = resourceName;
        }

        public override string Description
        {
            get { return (string)typeof(SettingsWrapper).GetField(ResourceName).GetValue(null); }
        }
    }
}
