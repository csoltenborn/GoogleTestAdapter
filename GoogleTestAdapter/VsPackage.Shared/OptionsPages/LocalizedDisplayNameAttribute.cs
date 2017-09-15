// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{
    class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string ResourceName;

        public LocalizedDisplayNameAttribute(string resourceName)
            : base()
        {
            this.ResourceName = resourceName;
        }

        public override string DisplayName
        {
            get { return (string)typeof(SettingsWrapper).GetField(ResourceName).GetValue(null); }
        }
    }
}
