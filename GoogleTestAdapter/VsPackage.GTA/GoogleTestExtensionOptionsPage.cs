// This file has been modified by Microsoft on 3/2019.

using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.ReleaseNotes;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.AsyncPackageHelpers;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading;

namespace GoogleTestAdapter.VsPackage
{
    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Microsoft.VisualStudio.Shell.ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public partial class GoogleTestExtensionOptionsPage : Package, IAsyncLoadablePackageInitialize
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";
        private const string OptionsCategoryName = "Google Test Adapter";
        private bool _isAsyncLoadSupported;

        protected override void Initialize()
        {
            base.Initialize();

            _isAsyncLoadSupported = this.IsAsyncPackageSupported();
            if (!_isAsyncLoadSupported)
            {
                var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal2>();
                DoInitialize();
            }
        }

        IVsTask IAsyncLoadablePackageInitialize.Initialize(IAsyncServiceProvider serviceProvider, IProfferAsyncService profferService,
            IAsyncProgressCallback progressCallback)
        {
            if (!_isAsyncLoadSupported)
            {
                throw new InvalidOperationException("Async Initialize method should not be called when async load is not supported.");
            }

            return ThreadHelper.JoinableTaskFactory.RunAsync<object>(async () =>
            {
                var componentModel = await serviceProvider.GetServiceAsync<IComponentModel>(typeof(SComponentModel));
                _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal2>();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                DoInitialize();

                return null;
            }).AsVsTask();
        }

        private void DisplayReleaseNotesIfNecessary()
        {
            var thread = new System.Threading.Thread(DisplayReleaseNotesIfNecessaryProc);
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
