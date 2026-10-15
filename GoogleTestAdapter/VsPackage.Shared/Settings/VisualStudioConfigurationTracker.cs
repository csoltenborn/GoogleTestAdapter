using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    /// <summary>
    /// Keeps track of the solution and the active configuration of Visual Studio. These can only be determined on the
    /// UI thread, so they are updated whenever they change rather than when they are needed, e.g. by
    /// <see cref="RunSettingsService"/>, which is called on background threads and must not wait for the UI thread.
    /// </summary>
    public sealed class VisualStudioConfigurationTracker : IVsSolutionEvents, IVsUpdateSolutionEvents, IDisposable
    {
        private static volatile VisualStudioConfigurationTracker _instance;

        private readonly IServiceProvider _serviceProvider;
        private readonly Action<string> _logError;
        private readonly IVsSolution _solution;
        private readonly IVsSolutionBuildManager _buildManager;
        private readonly uint _solutionEventsCookie;
        private readonly uint _updateSolutionEventsCookie;

        private volatile VisualStudioConfiguration _current;

        /// <summary>
        /// The configuration as of the last change, or null if GTA's package has not been initialized yet
        /// </summary>
        public static VisualStudioConfiguration Current => _instance?._current;

        /// <summary>
        /// Updates <see cref="Current"/> without waiting for the update, in case a change has not been noticed
        /// (e.g., because VS does not raise events for it)
        /// </summary>
        public static void RequestUpdate()
        {
            var instance = _instance;
            if (instance == null)
                return;

#pragma warning disable VSTHRD110 // the update is not awaited on purpose
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                instance.Update();
            });
#pragma warning restore VSTHRD110
        }

        /// <summary>
        /// Must be called on the UI thread
        /// </summary>
        public VisualStudioConfigurationTracker(IServiceProvider serviceProvider, Action<string> logError)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logError = logError;

            _solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (_solution == null || ErrorHandler.Failed(_solution.AdviseSolutionEvents(this, out _solutionEventsCookie)))
                _solutionEventsCookie = 0;

            _buildManager = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            if (_buildManager == null || ErrorHandler.Failed(_buildManager.AdviseUpdateSolutionEvents(this, out _updateSolutionEventsCookie)))
                _updateSolutionEventsCookie = 0;

            Update();
            _instance = this;
        }

        private void Update()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _current = VisualStudioConfiguration.FromServiceProvider(_serviceProvider, _logError);
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_instance == this)
                _instance = null;
            if (_solutionEventsCookie != 0 && ErrorHandler.Failed(_solution.UnadviseSolutionEvents(_solutionEventsCookie)))
                _logError("Could not unsubscribe from solution events");
            if (_updateSolutionEventsCookie != 0 && ErrorHandler.Failed(_buildManager.UnadviseUpdateSolutionEvents(_updateSolutionEventsCookie)))
                _logError("Could not unsubscribe from solution update events");
        }

        #region IVsSolutionEvents

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            Update();
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            Update();
            return VSConstants.S_OK;
        }

        // the active configuration is taken from the first project; only updated as long as there is none to not slow
        // down loading of large solutions
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (_current?.ConfigurationName == null)
                Update();
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        #endregion

        #region IVsUpdateSolutionEvents

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            Update();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.S_OK;
        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => VSConstants.S_OK;
        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;
        public int UpdateSolution_Cancel() => VSConstants.S_OK;

        #endregion
    }
}
