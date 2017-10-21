// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.VsPackage.ReleaseNotes;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;

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
            var versionProvider = new VersionProvider(this);

            Version formerlyInstalledVersion = versionProvider.FormerlyInstalledVersion;
            Version currentVersion = versionProvider.CurrentVersion;

            versionProvider.UpdateLastVersion();

            if (!_generalOptions.ShowReleaseNotes
                || (formerlyInstalledVersion != null && formerlyInstalledVersion >= currentVersion))
                return;

            var creator = new ReleaseNotesCreator(formerlyInstalledVersion, currentVersion);
            DisplayReleaseNotes(creator.CreateHtml());
        }

        private void DisplayReleaseNotes(string html)
        {
            string htmlFileBase = Path.GetTempFileName();
            string htmlFile = Path.ChangeExtension(htmlFileBase, "html");
            File.Delete(htmlFileBase);

            File.WriteAllText(htmlFile, html);

            using (var dialog = new ReleaseNotesDialog { HtmlFile = new Uri($"file://{htmlFile}") })
            {
                dialog.ShowReleaseNotesChanged +=
                    (sender, args) => _generalOptions.ShowReleaseNotes = args.ShowReleaseNotes;
                dialog.Closed += (sender, args) => File.Delete(htmlFile);
                dialog.ShowDialog();
            }
        }

        private bool ShowReleaseNotes
        {
            get { return _generalOptions.ShowReleaseNotes; }
        }
    }
}
