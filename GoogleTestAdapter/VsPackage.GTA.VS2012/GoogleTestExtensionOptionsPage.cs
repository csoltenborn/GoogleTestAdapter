﻿// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.VsPackage.ReleaseNotes;
using System;
using System.IO;
using System.Threading;
using GoogleTestAdapter.VsPackage.GTA.ReleaseNotes;

namespace GoogleTestAdapter.VsPackage
{
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string OptionsCategoryName = "Google Test Adapter";

        private void DisplayReleaseNotesIfNecessary()
        {
            var thread = new Thread(DisplayReleaseNotesIfNecessaryProc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void DisplayReleaseNotesIfNecessaryProc()
        {
            var versionProvider = new VersionProvider(this);

            Version formerlyInstalledVersion = versionProvider.FormerlyInstalledVersion;
            Version currentVersion = versionProvider.CurrentVersion;

            versionProvider.UpdateLastVersion();

            if ((_generalOptions.ShowReleaseNotes || History.ForceShowReleaseNotes(formerlyInstalledVersion)) &&
                (formerlyInstalledVersion == null || formerlyInstalledVersion < currentVersion))
            {
                var creator = new ReleaseNotesCreator(formerlyInstalledVersion, currentVersion, Donations.IsPreDonationsVersion(formerlyInstalledVersion));
                DisplayReleaseNotes(creator.CreateHtml());
            }
        }

        private void DisplayReleaseNotes(string html)
        {
            string htmlFileBase = Path.GetTempFileName();
            string htmlFile = Path.ChangeExtension(htmlFileBase, "html");
            File.Delete(htmlFileBase);

            File.WriteAllText(htmlFile, html);

            using (var dialog = new ReleaseNotesDialog
            {
                HtmlFile = new Uri($"file://{htmlFile}"),
                ShowReleaseNotesChecked = _generalOptions.ShowReleaseNotes
            })
            {
                dialog.AddExternalUri(Donations.Uri);
                dialog.ShowReleaseNotesChanged +=
                    (sender, args) => _generalOptions.ShowReleaseNotes = args.ShowReleaseNotes;
                dialog.Closed += (sender, args) => File.Delete(htmlFile);
                dialog.ShowDialog();
            }
        }

        private bool ShowReleaseNotes => _generalOptions.ShowReleaseNotes;
    }
}
