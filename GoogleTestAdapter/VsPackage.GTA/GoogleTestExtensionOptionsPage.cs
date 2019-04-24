﻿// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.VsPackage.ReleaseNotes;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;
using GoogleTestAdapter.VsPackage.GTA.ReleaseNotes;
using GoogleTestAdapter.VsPackage.Helpers;

namespace GoogleTestAdapter.VsPackage
{
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";
        private const string OptionsCategoryName = "Google Test Adapter";

        private void DisplayReleaseNotesIfNecessary()
        {
            var thread = new Thread(DisplayReleaseNotesIfNecessaryProc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void DisplayReleaseNotesIfNecessaryProc()
        {
            try
            {
                TryDisplayReleaseNotesIfNecessary();
            }
            catch (Exception e)
            {
                string msg = $"Exception while trying to update last version and show release notes:{Environment.NewLine}{e}";
                try
                {
                    new ActivityLogLogger(this, () => true).LogError(msg);
                }
                catch (Exception)
                {
                    // well...
                    Console.Error.WriteLine(msg);
                }
            }
        }

        private void TryDisplayReleaseNotesIfNecessary()
        {
            var versionProvider = new VersionProvider(this);

            Version formerlyInstalledVersion = versionProvider.FormerlyInstalledVersion;
            Version currentVersion = versionProvider.CurrentVersion;

            versionProvider.UpdateLastVersion();

            if (formerlyInstalledVersion == null || formerlyInstalledVersion < currentVersion)
            {
                var creator = new ReleaseNotesCreator(formerlyInstalledVersion, currentVersion);
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
                HtmlFile = new Uri($"file://{htmlFile}")
            })
            {
                dialog.AddExternalUri(Donations.Uri);
                dialog.Closed += (sender, args) => File.Delete(htmlFile);
                dialog.ShowDialog();
            }
        }
    }
}
