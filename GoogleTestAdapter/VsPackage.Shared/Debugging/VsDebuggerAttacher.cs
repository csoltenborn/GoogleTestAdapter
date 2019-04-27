using System;
using System.Reflection;
using System.Runtime.InteropServices;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.TestAdapter.ProcessExecution;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Thread = System.Threading.Thread;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    public class VsDebuggerAttacher : IDebuggerAttacher
    {
        private const int AttachRetryWaitingTimeInMs = 100;
        private const int MaxAttachTries = 10; // let's try for 1s

        private readonly IServiceProvider _serviceProvider;

        static VsDebuggerAttacher()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveVisualStudioShell;
        }

        internal VsDebuggerAttacher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool AttachDebugger(int processId, DebuggerEngine debuggerEngine)
        {
            IntPtr pDebugEngine = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid)));
            try
            {
                Guid debuggerEngineGuid = debuggerEngine == DebuggerEngine.Native
                    ? VSConstants.DebugEnginesGuids.NativeOnly
                    : VSConstants.DebugEnginesGuids.ManagedAndNative;
                Marshal.StructureToPtr(debuggerEngineGuid, pDebugEngine, false);

                var debugTarget = new VsDebugTargetInfo4
                {
                    dlo = (uint) DEBUG_LAUNCH_OPERATION.DLO_AlreadyRunning
                          | (uint) _DEBUG_LAUNCH_OPERATION4.DLO_AttachToSuspendedLaunchProcess,
                    dwProcessId = (uint) processId,
                    dwDebugEngineCount = 1,
                    pDebugEngines = pDebugEngine,
                };

                var debugger = (IVsDebugger4) _serviceProvider.GetService(typeof(SVsShellDebugger));

                AttachDebuggerRetrying(debugger, debugTarget);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pDebugEngine);
            }
            return true;
        }

        private static void AttachDebuggerRetrying(IVsDebugger4 debugger, VsDebugTargetInfo4 debugTarget)
        {
            bool attachedSuccesfully = false;
            int tries = 0;
            while (!attachedSuccesfully)
            {
                try
                {
                    debugger.LaunchDebugTargets4(1, new[] {debugTarget}, new VsDebugTargetProcessInfo[1]);
                    attachedSuccesfully = true;
                }
                catch (Exception)
                {
                    // workaround for exceptions: System.Runtime.InteropServices.COMException (0x80010001): Call was rejected by callee. (Exception from HRESULT: 0x80010001 (RPC_E_CALL_REJECTED))
                    tries++;
                    if (tries == MaxAttachTries)
                        throw;

                    Thread.Sleep(AttachRetryWaitingTimeInMs);
                }
            }
        }

        private static Assembly ResolveVisualStudioShell(object sender, ResolveEventArgs args)
        {
            if (args.Name == "Microsoft.VisualStudio.Shell.11.0, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveVisualStudioShell;

                var assembly = new AssemblyName(args.Name);

                for (var version = 11; version <= 15; version++)
                {
                    try
                    {
                        assembly.Version = new Version(version, 0, 0, 0);
                        return Assembly.Load(assembly);
                    }
                    catch (Exception)
                    {
                        // try next version
                    }
                }
            }
            return null;
        }

    }
}