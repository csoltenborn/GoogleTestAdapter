using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    internal class ReleaseNotesDisplayer
    {
        private const string CollectionName = "GoogleTestAdapter";
        private const string VersionPropertyName = "LastStartedVersion";

        private IGoogleTestExtensionOptionsPage ThePackage { get; }

        internal ReleaseNotesDisplayer(IGoogleTestExtensionOptionsPage thePackage)
        {
            ThePackage = thePackage;
        }

        internal void DisplayReleaseNotesIfNecessary()
        {
            var thread = new Thread(DoDisplayReleaseNotesIfNecessary);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void DoDisplayReleaseNotesIfNecessary()
        {
            Version lastVersion, currentVersion;
            UpdateLastVersion(out lastVersion, out currentVersion);

            //lastVersion = null;
            //currentVersion = new Version(0, 4, 0, 0);
            if (!ThePackage.ShowReleaseNotes || (lastVersion != null && lastVersion >= currentVersion))
                return;

            var creator = new ReleaseNotesCreator(lastVersion, currentVersion);
            DisplayReleaseNotes(creator.CreateHtml());
        }

        private void UpdateLastVersion(out Version lastVersion, out Version currentVersion)
        {
            var settingsManager = new ShellSettingsManager(ThePackage);
            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(CollectionName))
                settingsStore.CreateCollection(CollectionName);

            lastVersion = null;
            if (settingsStore.PropertyExists(CollectionName, VersionPropertyName))
            {
                string lastVersionString = settingsStore.GetString(CollectionName, VersionPropertyName);
                lastVersion = Version.Parse(lastVersionString);
            }

            currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            currentVersion = new Version(currentVersion.Major, currentVersion.Minor, 0, currentVersion.Revision);

            settingsStore.SetString(CollectionName, VersionPropertyName, currentVersion.ToString());
        }

        private void DisplayReleaseNotes(string html)
        {
            string htmlFile = Path.GetTempFileName();
            File.WriteAllText(htmlFile, html);

            var dialog = new ReleaseNotesDialog(ThePackage.ShowReleaseNotes);
            dialog.HtmlFile = new Uri($"file://{htmlFile}");
            dialog.ShowReleaseNotesChanged += 
                (sender, args) => ThePackage.ShowReleaseNotes = dialog.ShowReleaseNotes;
            dialog.Closed += (sender, args) => File.Delete(htmlFile);
            dialog.ShowDialog();
        }

    }

}