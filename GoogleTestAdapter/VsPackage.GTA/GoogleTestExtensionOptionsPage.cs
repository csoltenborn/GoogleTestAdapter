// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.VsPackage.ReleaseNotes;
using System;
using System.IO;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapter.VsPackage.GTA.Helpers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Threading;

namespace GoogleTestAdapter.VsPackage
{
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string OptionsCategoryName = "Google Test Adapter";

        // Package GUID of Microsoft's Test Adapter for Google Test (TAfGT), which is part of Visual Studio's C++ workloads
        private static readonly Guid TestAdapterForGoogleTestPackageGuid = new Guid("6fac3232-df1d-400a-95ac-7daeaaee74ac");
        private const string SuppressTafgtWarningPropertyName = "SuppressTestAdapterForGoogleTestWarning";

        private const string TestAdapterForGoogleTestConflict =
            "Microsoft's Test Adapter for Google Test is installed alongside Google Test Adapter. " +
            "Both adapters are derived from the same code and ship assemblies with identical names, " +
            "so the test platform loads only one of them, and which one is not predictable. " +
            "If it picks Test Adapter for Google Test, Google Test Adapter's options (including the " +
            "CMake support) are ignored.";
        private const string TestAdapterForGoogleTestRemedy =
            "To use Google Test Adapter, remove the \"Test Adapter for Google Test\" individual component " +
            "using the Visual Studio Installer.";

        // Called on the main thread
        private void WarnIfTestAdapterForGoogleTestIsInstalled()
        {
            try
            {
                if (!IsTestAdapterForGoogleTestInstalled())
                    return;

                var logger = new ActivityLogLogger(this, () => OutputMode.Verbose);
                logger.LogWarning($"{TestAdapterForGoogleTestConflict} {TestAdapterForGoogleTestRemedy}");

                if (VsSettingsStorage.Instance.PropertyExists(SuppressTafgtWarningPropertyName))
                    return;

                // do not block package initialization with a modal dialog
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new Action(ShowTestAdapterForGoogleTestWarning));
            }
            catch (Exception e)
            {
                new ActivityLogLogger(this, () => OutputMode.Verbose).LogError(
                    $"Exception while checking for Test Adapter for Google Test:{Environment.NewLine}{e}");
            }
        }

        private bool IsTestAdapterForGoogleTestInstalled()
        {
            var shell = (IVsShell) GetService(typeof(SVsShell));
            Guid packageGuid = TestAdapterForGoogleTestPackageGuid;
            return shell != null
                && ErrorHandler.Succeeded(shell.IsPackageInstalled(ref packageGuid, out int installed))
                && installed != 0;
        }

        private void ShowTestAdapterForGoogleTestWarning()
        {
            int result = VsShellUtilities.ShowMessageBox(this,
                string.Join(Environment.NewLine + Environment.NewLine,
                    TestAdapterForGoogleTestConflict, TestAdapterForGoogleTestRemedy, "Show this warning again next time?"),
                "Google Test Adapter",
                OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            if (result == (int) VSConstants.MessageBoxResult.IDNO)
                VsSettingsStorage.Instance.SetString(SuppressTafgtWarningPropertyName, "true");
        }

        private void DisplayReleaseNotesIfNecessary()
        {
            // the colors of VS's theme are only available on the UI thread, the dialog has its own thread
            ReleaseNotesTheme theme = ReleaseNotesTheme.FromVisualStudio();
            var thread = new Thread(() => DisplayReleaseNotesIfNecessaryProc(theme));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void DisplayReleaseNotesIfNecessaryProc(ReleaseNotesTheme theme)
        {
            try
            {
                TryDisplayReleaseNotesIfNecessary(theme);
            }
            catch (Exception e)
            {
                string msg = $"Exception while trying to update last version and show release notes:{Environment.NewLine}{e}";
                try
                {
                    new ActivityLogLogger(this, () => OutputMode.Verbose).LogError(msg);
                }
                catch (Exception)
                {
                    // well...
                    Console.Error.WriteLine(msg);
                }
            }
        }

        private void TryDisplayReleaseNotesIfNecessary(ReleaseNotesTheme theme)
        {
            var versionProvider = new VersionProvider();

            Version formerlyInstalledVersion = versionProvider.FormerlyInstalledVersion;
            Version currentVersion = versionProvider.CurrentVersion;

            versionProvider.UpdateLastVersion();

            if (formerlyInstalledVersion == null || formerlyInstalledVersion < currentVersion)
            {
                var creator = new ReleaseNotesCreator(formerlyInstalledVersion, currentVersion);
                DisplayReleaseNotes(creator.CreateHtml(theme), theme);
            }
        }

        private void DisplayReleaseNotes(string html, ReleaseNotesTheme theme)
        {
            string htmlFileBase = Path.GetTempFileName();
            string htmlFile = Path.ChangeExtension(htmlFileBase, "html");
            File.Delete(htmlFileBase);

            File.WriteAllText(htmlFile, html);

            using (var dialog = new ReleaseNotesDialog(theme)
            {
                HtmlFile = new Uri($"file://{htmlFile}")
            })
            {
                dialog.Closed += (sender, args) => File.Delete(htmlFile);
                dialog.ShowDialog();
            }
        }
    }
}
