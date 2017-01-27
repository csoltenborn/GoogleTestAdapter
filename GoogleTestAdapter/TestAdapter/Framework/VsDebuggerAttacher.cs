using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IBindCtx = System.Runtime.InteropServices.ComTypes.IBindCtx;
using IEnumMoniker = System.Runtime.InteropServices.ComTypes.IEnumMoniker;
using IMoniker = System.Runtime.InteropServices.ComTypes.IMoniker;
using IRunningObjectTable = System.Runtime.InteropServices.ComTypes.IRunningObjectTable;
using Process = System.Diagnostics.Process;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    internal class LastWin32Exception : Win32Exception
    {
        public LastWin32Exception()
            : base(Marshal.GetLastWin32Error())
        {}
    }

    internal sealed class CoTaskMemSafeHandle : SafeHandle
    {

        public CoTaskMemSafeHandle(IntPtr handle)
            : base(handle, true)
        {}

        public override bool IsInvalid => IsClosed || handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

    public class VsDebuggerAttacher : IDebuggerAttacher
    {
        private readonly Process _visualStudioProcess;
        private readonly _DTE _visualStudioInstance;
        private readonly ILogger _logger;

        public VsDebuggerAttacher(ILogger logger, int processId)
        {
            _logger = logger;
            _visualStudioProcess = Process.GetProcessById(processId);

            _DTE visualStudioInstance;
            if (NativeMethods.TryGetVsInstance(_visualStudioProcess.Id, out visualStudioInstance))
                _visualStudioInstance = visualStudioInstance;
            else
            {
                logger.LogError("Could not find Visual Studio instance");
                throw new InvalidOperationException("Could not find Visual Studio instance");
            }
        }

        public bool AttachDebugger(int processId)
        {
            try
            {
                if (Environment.Is64BitProcess)
                {
                    // Cannot use Visual Studio API from 64-bit process
                    // => delegate it to 32-bit wrapper process
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    string baseDir = Path.GetDirectoryName(path);

                    var command = Path.Combine(baseDir, "VsDebuggerAttacherWrapper.exe");
                    var param = $"{processId} {_visualStudioProcess.Id}";
                    return new ProcessExecutor(null, _logger).ExecuteCommandBlocking(command, param, null, null, _logger.LogError) == 0;
                }
                else
                {
                    IntPtr pDebugEngine = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid)));
                    try
                    {
                        Marshal.StructureToPtr(VSConstants.DebugEnginesGuids.NativeOnly_guid, pDebugEngine, false);

                        var debugTarget = new VsDebugTargetInfo4
                        {
                            dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_AlreadyRunning
                                | (uint)_DEBUG_LAUNCH_OPERATION4.DLO_AttachToSuspendedLaunchProcess,
                            dwProcessId = (uint)processId,
                            dwDebugEngineCount = 1,
                            pDebugEngines = pDebugEngine,
                        };

                        // ReSharper disable once SuspiciousTypeConversion.Global
                        var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_visualStudioInstance.DTE);
                        var debugger = (IVsDebugger4)serviceProvider.GetService(typeof(SVsShellDebugger));
                        debugger.LaunchDebugTargets4(1, new[] { debugTarget }, new VsDebugTargetProcessInfo[1]);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pDebugEngine);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed attaching debugger to process {processId}: {e}");
                return false;
            }
        }

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

            [DllImport("ole32.dll")]
            private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

            internal static bool TryGetVsInstance(int processId, out _DTE instance)
            {
                IntPtr numFetched = IntPtr.Zero;
                IRunningObjectTable runningObjectTable;
                IEnumMoniker monikerEnumerator;
                IMoniker[] monikers = new IMoniker[1];

                GetRunningObjectTable(0, out runningObjectTable);
                runningObjectTable.EnumRunning(out monikerEnumerator);
                monikerEnumerator.Reset();

                while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
                {
                    IBindCtx ctx;
                    CreateBindCtx(0, out ctx);

                    string runningObjectName;
                    monikers[0].GetDisplayName(ctx, null, out runningObjectName);

                    object runningObjectVal;
                    runningObjectTable.GetObject(monikers[0], out runningObjectVal);

                    if (runningObjectVal is _DTE && runningObjectName.StartsWith("!VisualStudio"))
                    {
                        int currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                        if (currentProcessId == processId)
                        {
                            instance = (_DTE)runningObjectVal;
                            return true;
                        }
                    }
                }

                instance = null;
                return false;
            }
        }
    }
}