// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage
{
    [ProvideAutoLoad(UIContextGuid)]
    [ProvideUIContextRule(UIContextGuid, OptionsCategoryName, "VCProject & TestExplorer",
        new string[] { "VCProject", "TestExplorer" },
        new string[] { VSConstants.UICONTEXT.VCProject_string, TestExplorerContextGuid })]
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string PackageGuidString = "1db31773-234b-424b-a887-b451fb1ba837";
        private const string UIContextGuid = "7517f9ae-397f-48e1-8e1b-dac609d9f52d";
        private const string TestExplorerContextGuid = "ec25b527-d893-4ec0-a814-d2c9f1782997";
        private const string OptionsCategoryName = "Test Adapter for Google Test";

        private void DisplayReleaseNotesIfNecessary()
        {
            // TAfGT does not display release notes.
        }

        private bool ShowReleaseNotes
        {
            get { return false; }
        }
    }
}
