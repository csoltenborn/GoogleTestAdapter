using GoogleTestAdapterUiTests.Helpers;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TestStack.White;
using TestStack.White.Configuration;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;

namespace GoogleTestAdapterUiTests
{
    public class VsExperimentalInstance
    {
        public enum Versions { VS2012 = 11, VS2013 = 12, VS2015 = 14 }

        public readonly Versions Version;
        public readonly string Suffix;
        public readonly string VersionAndSuffix;

        public VsExperimentalInstance(Versions version, string suffix)
        {
            Version = version;
            Suffix = suffix;
            VersionAndSuffix = $"{Version:d}.0{Suffix}";
        }

        public bool Exists()
        {
            if (GetVsDirectories().Where(Directory.Exists).Any())
                return true;
            if (GetVsHkcuKeys().Where(Registry.CurrentUser.HasSubKey).Any())
                return true;
            return false;
        }

        public void Clean()
        {
            if (string.IsNullOrEmpty(Suffix))
                throw new InvalidOperationException("We do not want to clean the non-experimental VS instance.");

            foreach (var dir in GetVsDirectories().Where(Directory.Exists))
                Directory.Delete(dir, true);
            foreach (var key in GetVsHkcuKeys().Where(Registry.CurrentUser.HasSubKey))
                Registry.CurrentUser.DeleteSubKeyTree(key);
        }

        public void FirstTimeInitialization()
        {
            if (Exists())
                throw new InvalidOperationException("Cannot first-time initialize existing instance.");

            int timeOut = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            using (Application application = Launch())
            using (CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(c => { c.BusyTimeout = c.FindWindowTimeout = timeOut; }))
            using (Window win = application.GetWindow("Microsoft Visual Studio"))
            {
                win.Get<Hyperlink>(SearchCriteria.ByText("Not now, maybe later.")).Click();
                win.Get<Button>(SearchCriteria.ByText("Start Visual Studio")).Click();
                win.WaitWhileBusy();
                win.Close();
            }
        }

        internal void InstallExtension(string vsixPath)
        {
            if (!Exists())
                throw new InvalidOperationException("Cannot install VSIX in non-existing instance.");

            using (var settings = ExternalSettingsManager.CreateForApplication(GetExePath(), Suffix))
            {
                var ems = new ExtensionManagerService(settings);
                IInstallableExtension vsix = ExtensionManagerService.CreateInstallableExtension(vsixPath);

                if (ems.IsInstalled(vsix))
                {
                    IInstalledExtension installedVsix = ems.GetInstalledExtension(vsix.Header.Identifier);
                    ems.Uninstall(installedVsix);
                    if (ems.IsInstalled(vsix))
                        throw new InvalidOperationException("Could not uninstall already installed GoogleTestAdapter.");
                }

                ems.Install(vsix, perMachine: false);
                if (!ems.IsInstalled(vsix))
                    throw new InvalidOperationException("Could not install GoogleTestAdapter.");

                ems.Close();
            }
        }

        public Application Launch()
        {
            ProcessStartInfo startInfo = string.IsNullOrEmpty(Suffix)
                ? new ProcessStartInfo(GetExePath())
                : new ProcessStartInfo(GetExePath(), $"/rootSuffix {Suffix}");
            return Application.Launch(startInfo);
        }

        public static string GetVsTestConsolePath(Versions version)
        {
            return @"C:\Program Files (x86)\" + $"Microsoft Visual Studio {version:d}.0" + @"\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
        }

        private string GetExePath()
        {
            return @"C:\Program Files (x86)\" + $"Microsoft Visual Studio {Version:d}.0" + @"\Common7\IDE\devenv.exe";
        }

        private IEnumerable<string> GetVsDirectories()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (var folder in new[] { VersionAndSuffix, Suffix })
                yield return Path.Combine(localAppData, @"Microsoft\VisualStudio", folder);
        }

        private IEnumerable<string> GetVsHkcuKeys()
        {
            string path = Path.Combine(@"SOFTWARE\Microsoft\VisualStudio", VersionAndSuffix);
            foreach (var additionalSuffix in new[] { "", "_Config", "_Remote" })
                yield return path + additionalSuffix;

            // Remove this key for now, as it makes trouble being cleaned after test run (issue #19).
            //yield return Path.Combine(@"SOFTWARE\Microsoft\VSCommon", Suffix);
            yield return Path.Combine(@"SOFTWARE\Microsoft\VsHub\ServiceModules\Settings\PerHubName", Suffix);
        }
    }
}
