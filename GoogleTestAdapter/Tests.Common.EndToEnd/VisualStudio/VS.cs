using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.EndToEnd.Helpers;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio
{
    public class Vs
    {
        private const bool KeepDirtyInstanceInit = false;

        public string UiTestsDirectory { get; }
        public string UserSettingsFile { get; }

        private readonly string _vsixPath;

        private static bool _keepDirtyVsInstance = KeepDirtyInstanceInit;
        private static bool? _installIntoProductiveVs;

        public VsExperimentalInstance VisualStudioInstance { get; private set; }

        public Vs()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            // ReSharper disable once AssignNullToNotNullAttribute
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\VsPackage.Tests.*\\bin\\(Debug|Release)\\GoogleTestAdapter.Tests.Common.EndToEnd.dll$");
            match.Success.Should().BeTrue();

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            _vsixPath = Path.Combine(basePath, @"GoogleTestAdapter\VsPackage\bin", debugOrRelease, @"GoogleTestAdapter.VsPackage.vsix");
            UiTestsDirectory = Path.Combine(basePath, @"GoogleTestAdapter\VsPackage.Tests");
            UserSettingsFile = Path.Combine(basePath, @"SampleTests\NonDeterministic.runsettings");
        }

        public void SetupVanillaVsExperimentalInstance(string suffix, string typename)
        {
            AskIfNotOnBuildServerAndProductiveVs(suffix);

            try
            {
                VisualStudioInstance = new VsExperimentalInstance(TestMetadata.Versions.VS2015, suffix);
                if (string.IsNullOrEmpty(suffix))
                {
                    _keepDirtyVsInstance = true;
                    VisualStudioInstance.InstallExtension(_vsixPath);
                }
                else
                {
                    if (!_keepDirtyVsInstance)
                    {
                        _keepDirtyVsInstance = AskToCleanIfExists();
                    }
                    if (!_keepDirtyVsInstance)
                    {
                        VisualStudioInstance.FirstTimeInitialization();
                        VisualStudioInstance.InstallExtension(_vsixPath);
                    }
                }
            }
            // ReSharper disable once RedundantCatchClause
            // ReSharper disable once UnusedVariable
            catch (Exception exception)
            {
                var wrapper = new AutomationException(
                    $"Exception caught: {exception.GetType().Name}", 
                    exception.Message, 
                    exception);
                wrapper.LogAndThrow(typename);
            }
        }

        public void CleanVsExperimentalInstance()
        {
            if (!_keepDirtyVsInstance)
            {
                // wait for removal of locks on some files we want to delete
                // TODO: find more reliable method than using Sleep()
                Thread.Sleep(TimeSpan.FromSeconds(1));
                VisualStudioInstance.Clean();
            }
            VisualStudioInstance = null;
        }


        private static void AskIfNotOnBuildServerAndProductiveVs(string suffix)
        {
            if (string.IsNullOrEmpty(suffix)
                && _installIntoProductiveVs == null
                && !CiSupport.IsRunningOnBuildServer)
            {
                MessageBoxResult result = MessageBoxWithTimeout.Show(
                    "Really launch tests? This will delete a potentially installed GoogleTestAdapter extension "
                    + "from your productive VisualStudio instance and install this build instead!",
                    "Warning!", MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        _installIntoProductiveVs = true;
                        break;
                    default:
                        _installIntoProductiveVs = false;
                        break;
                }
            }

            if (_installIntoProductiveVs.HasValue && !_installIntoProductiveVs.Value)
                Assert.Inconclusive("Didn't get confirmation to execute tests. Cancelling...");
        }

        private bool AskToCleanIfExists()
        {
            bool keepDirtyInstance = false;
            if (VisualStudioInstance.Exists() && !CiSupport.IsRunningOnBuildServer)
            {
                var instanceExists = $"The experimental instance '{VisualStudioInstance.VersionAndSuffix}' already exists.";
                var willReset = "\nShould it be deleted before going on with the tests?";

                MessageBoxResult result = MessageBoxWithTimeout.Show(instanceExists + willReset, "Warning!", MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        VisualStudioInstance.Clean();
                        break;
                    case MessageBoxResult.No:
                        keepDirtyInstance = true;
                        break;
                    case MessageBoxResult.Cancel:
                        Assert.Fail(instanceExists + " Didn't get confirmation to reset experimental instance. Cancelling...");
                        break;
                }
            }
            return keepDirtyInstance;
        }

    }

}