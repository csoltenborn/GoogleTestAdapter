﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.Tests.Common.Helpers;
//using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;
using Microsoft.Win32;
using TestStack.White;
using TestStack.White.Configuration;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;

namespace GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio
{
    public class VsExperimentalInstance
    {
        public readonly VsVersion Version;
        public readonly string Suffix;
        public readonly string VersionAndSuffix;

        public VsExperimentalInstance(VsVersion version, string suffix)
        {
            Version = version;
            Suffix = suffix;
            VersionAndSuffix = version == VsVersion.VS2017 
                ? $"15.0_17426ca5{Suffix}"
                : $"{Version:d}.0{Suffix}";
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

        //public void InstallExtension(string vsixPath)
        //{
        //    if (!Exists())
        //        throw new InvalidOperationException("Cannot install VSIX in non-existing instance.");

        //    IInstallableExtension installableExtension = ExtensionManagerService.CreateInstallableExtension(vsixPath);

        //    using (var settings = ExternalSettingsManager.CreateForApplication(GetExePath(), Suffix))
        //    {
        //        var extensionManager = new ExtensionManagerService(settings);
        //        if (extensionManager.IsInstalled(installableExtension))
        //        {
        //            IInstalledExtension installedExtension = extensionManager.GetInstalledExtension(installableExtension.Header.Identifier);
        //            extensionManager.Uninstall(installedExtension);
        //            if (extensionManager.IsInstalled(installableExtension))
        //                throw new InvalidOperationException("Could not uninstall already installed GoogleTestAdapter.");
        //        }

        //        extensionManager.Install(installableExtension, perMachine: false);
        //        if (!extensionManager.IsInstalled(installableExtension))
        //            throw new InvalidOperationException("Could not install GoogleTestAdapter.");

        //        extensionManager.Close();
        //    }
        //}

        public Application Launch()
        {
            ProcessStartInfo startInfo = string.IsNullOrEmpty(Suffix)
                ? new ProcessStartInfo(GetExePath())
                : new ProcessStartInfo(GetExePath(), $"/rootSuffix {Suffix}");
            return Application.Launch(startInfo);
        }

        private string GetExePath()
        {
            string exePath;
            switch (Version)
            {
                case VsVersion.VS2012:
                case VsVersion.VS2012_1:
                case VsVersion.VS2013:
                case VsVersion.VS2015:
                    exePath = $@"C:\Program Files (x86)\Microsoft Visual Studio {Version:d}.0\Common7\IDE\devenv.exe";
                    break;
                case VsVersion.VS2017:
                    exePath = $@"C:\Program Files (x86)\Microsoft Visual Studio\{Version.Year()}\Community\Common7\IDE\devenv.exe";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown enum literal: {Version}");
            }

            if (!File.Exists(exePath))
                throw new AutomationException($"VS executable does not exist at '{exePath}'", "");

            return exePath;
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
                yield return $"{path}{additionalSuffix}";

            // Remove this key for now, as it makes trouble being cleaned after test run (issue #19).
            //yield return Path.Combine(@"SOFTWARE\Microsoft\VSCommon", Suffix);
            yield return Path.Combine(@"SOFTWARE\Microsoft\VsHub\ServiceModules\Settings\PerHubName", Suffix);
        }
    }
}
