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
        public enum Versions { VS2012=11, VS2013=12, VS2015=14 }

        public readonly Versions Version;
        public readonly string Suffix;
        public readonly string VersionAndSuffix;

        public VsExperimentalInstance(Versions version, string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
                throw new ArgumentException("suffix may not be empty");

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
            foreach(var dir in GetVsDirectories().Where(Directory.Exists))
                Directory.Delete(dir, true);
            foreach (var key in GetVsHkcuKeys().Where(Registry.CurrentUser.HasSubKey))
                Registry.CurrentUser.DeleteSubKeyTree(key);
        }

        public void FirstTimeInitialization()
        {
            if (Exists())
                throw new InvalidOperationException("Cannot first-time initialize existing instance.");

            using (Application application = Launch())
            using (CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(c => { c.BusyTimeout = c.FindWindowTimeout = TimeSpan.FromMinutes(3).Milliseconds; }))
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
                var vsix = ExtensionManagerService.CreateInstallableExtension(vsixPath);
                ems.Install(vsix, perMachine: false);
                ems.Close();
            }
        }

        public Application Launch()
        {
            return Application.Launch(new ProcessStartInfo(GetExePath(), $"/rootSuffix {Suffix}"));
        }

        private string GetExePath()
        {
            return @"C:\Program Files (x86)\" + $"Microsoft Visual Studio {Version:d}.0" + @"\Common7\IDE\devenv.exe";
        }

        private IEnumerable<string> GetVsDirectories()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach(var folder in new[]{ VersionAndSuffix, Suffix })
                yield return Path.Combine(localAppData, "Microsoft", "VisualStudio", folder);
        }

        private IEnumerable<string> GetVsHkcuKeys()
        {
            string path = Path.Combine("SOFTWARE", "Microsoft", "VisualStudio", VersionAndSuffix);
            foreach (var additionalSuffix in new[] { "", "_Config", "_Remote" })
                yield return path + additionalSuffix;
        }
    }
}
