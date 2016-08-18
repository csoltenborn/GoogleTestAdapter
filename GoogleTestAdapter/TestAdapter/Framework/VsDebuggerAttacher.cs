using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using GoogleTestAdapter.Framework;
using DTEProcess = EnvDTE.Process;
using Process = System.Diagnostics.Process;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class VsDebuggerAttacher : IDebuggerAttacher
    {
        private readonly Process _visualStudioProcess;
        private readonly _DTE _visualStudioInstance;

        public VsDebuggerAttacher(int visualStudioProcessId)
        {
            _visualStudioProcess = Process.GetProcessById(visualStudioProcessId);

            _DTE visualStudioInstance;
            if (NativeMethods.TryGetVsInstance(_visualStudioProcess.Id, out visualStudioInstance))
                _visualStudioInstance = visualStudioInstance;
            else
                throw new InvalidOperationException("Could not find VS instance");
        }

        public bool AttachDebugger(Process processToAttachTo)
        {
            try
            {
                NativeMethods.AttachVisualStudioToProcess(_visualStudioProcess, _visualStudioInstance, processToAttachTo);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AttachDebugger(int processIdToAttachTo)
        {
            return AttachDebugger(Process.GetProcessById(processIdToAttachTo));
        }

        private static class NativeMethods
        {
            [DllImport("User32")]
            private static extern int ShowWindow(int hwnd, int nCmdShow);

            [DllImport("ole32.dll")]
            private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

            [DllImport("ole32.dll")]
            private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);


            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            internal static void AttachVisualStudioToProcess(Process visualStudioProcess, _DTE visualStudioInstance, Process applicationProcess)
            {
                //Find the process you want the VS instance to attach to...
                DTEProcess processToAttachTo = visualStudioInstance.Debugger.LocalProcesses.Cast<DTEProcess>().FirstOrDefault(process => process.ProcessID == applicationProcess.Id);

                //AttachDebugger to the process.
                if (processToAttachTo != null)
                {
                    processToAttachTo.Attach();

                    ShowWindow((int)visualStudioProcess.MainWindowHandle, 3);
                    SetForegroundWindow(visualStudioProcess.MainWindowHandle);
                }
                else
                {
                    throw new InvalidOperationException("Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
                }
            }

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