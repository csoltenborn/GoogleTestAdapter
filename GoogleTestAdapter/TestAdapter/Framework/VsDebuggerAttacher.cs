using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using GoogleTestAdapter.Framework;
using Microsoft.Samples.Debugging.Native;
using Microsoft.Win32.SafeHandles;
using GoogleTestAdapter.Helpers;
using DTEProcess = EnvDTE.Process;
using Process = System.Diagnostics.Process;
using NativeDebuggingMethods = Microsoft.Samples.Debugging.Native.NativeMethods;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class LastWin32Exception : Win32Exception
    {
        public LastWin32Exception()
            : base(Marshal.GetLastWin32Error())
        { }
    }

    public class VsDebuggerAttacher : IDebuggerAttacher
    {
        private readonly Process _visualStudioProcess;
        private readonly _DTE _visualStudioInstance;
        private readonly TestEnvironment _testEnvironment;

        public VsDebuggerAttacher(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
            _visualStudioProcess = Process.GetProcessById(testEnvironment.Options.VisualStudioProcessId);

            _DTE visualStudioInstance;
            if (NativeMethods.TryGetVsInstance(_visualStudioProcess.Id, out visualStudioInstance))
                _visualStudioInstance = visualStudioInstance;
            else
            {
                testEnvironment.LogError("Could not find Visual Studio instance");
                throw new InvalidOperationException("Could not find Visual Studio instance");
            }
        }

        public bool AttachDebugger(Process processToAttachTo)
        {
            try
            {
                NativeMethods.SkipInitialDebugBreak((uint)processToAttachTo.Id);
                NativeMethods.AttachVisualStudioToProcess(_visualStudioProcess, _visualStudioInstance, processToAttachTo);
                _testEnvironment.DebugInfo($"Attached debugger to process {processToAttachTo.Id}:{processToAttachTo.ProcessName}");
                return true;
            }
            catch (Exception)
            {
                _testEnvironment.LogError($"Failed attaching debugger to process {processToAttachTo.Id}:{processToAttachTo.ProcessName}");
                return false;
            }
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

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern int SuspendThread(IntPtr hThread);

            internal static void AttachVisualStudioToProcess(Process visualStudioProcess, _DTE visualStudioInstance, Process applicationProcess)
            {
                DTEProcess processToAttachTo = visualStudioInstance.Debugger.LocalProcesses.Cast<DTEProcess>().FirstOrDefault(process => process.ProcessID == applicationProcess.Id);

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

            internal static void SkipInitialDebugBreak(uint dwProcessId)
            {
                if (!NativeDebuggingMethods.DebugActiveProcess(dwProcessId))
                    throw new LastWin32Exception();

                try
                {
                    bool done = false;
                    uint mainThread = 0;
                    while (!done)
                    {
                        var debugEvent = new DebugEvent32();
                        if (!NativeDebuggingMethods.WaitForDebugEvent32(ref debugEvent, (int) TimeSpan.FromSeconds(10).TotalMilliseconds))
                            throw new LastWin32Exception();

                        switch (debugEvent.header.dwDebugEventCode)
                        {
                            case NativeDebugEventCode.CREATE_PROCESS_DEBUG_EVENT:
                                new SafeFileHandle(debugEvent.union.CreateProcess.hFile, true).Dispose();
                                break;
                            case NativeDebugEventCode.LOAD_DLL_DEBUG_EVENT:
                                new SafeFileHandle(debugEvent.union.LoadDll.hFile, true).Dispose();
                                break;
                            case NativeDebugEventCode.EXCEPTION_DEBUG_EVENT:
                            case NativeDebugEventCode.EXIT_PROCESS_DEBUG_EVENT:
                                mainThread = debugEvent.header.dwThreadId;
                                done = true;
                                break;
                        }

                        if(!NativeDebuggingMethods.ContinueDebugEvent(debugEvent.header.dwProcessId,
                                debugEvent.header.dwThreadId, NativeDebuggingMethods.ContinueStatus.DBG_CONTINUE))
                            throw new LastWin32Exception();

                    }

                    SuspendThread(new IntPtr(mainThread));
                }
                finally
                {
                    if (!NativeDebuggingMethods.DebugActiveProcessStop(dwProcessId))
                        throw new LastWin32Exception();
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