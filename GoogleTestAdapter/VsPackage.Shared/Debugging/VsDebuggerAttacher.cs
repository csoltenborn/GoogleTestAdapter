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

        // VSConstants.DebugEnginesGuids.NativeOnly and .ManagedAndNative (the latter supporting the .NET Framework only)
        private static readonly Guid NativeEngineGuid = new Guid("3B476D35-A401-11D2-AAD4-00C04F990171");
        private static readonly Guid ManagedAndNativeEngineGuid = new Guid("92EF0900-2251-11D2-B72E-0000F87572EF");
        // CoreCLR debug engine ("Managed (.NET Core, .NET 5+)"), which is not contained in the referenced VS SDK
        private static readonly Guid ManagedCoreEngineGuid = new Guid("2E36F1D4-B23C-435D-AB41-18E608940038");

        public bool AttachDebugger(int processId, DebuggerEngine debuggerEngine)
        {
            Guid[] debuggerEngineGuids = GetDebuggerEngineGuids(debuggerEngine);
            int guidSize = Marshal.SizeOf(typeof(Guid));
            IntPtr pDebugEngines = Marshal.AllocCoTaskMem(guidSize * debuggerEngineGuids.Length);
            try
            {
                for (int i = 0; i < debuggerEngineGuids.Length; i++)
                {
                    Marshal.StructureToPtr(debuggerEngineGuids[i], pDebugEngines + i * guidSize, false);
                }

                var debugTarget = new VsDebugTargetInfo4
                {
                    dlo = (uint) DEBUG_LAUNCH_OPERATION.DLO_AlreadyRunning
                          | (uint) _DEBUG_LAUNCH_OPERATION4.DLO_AttachToSuspendedLaunchProcess,
                    dwProcessId = (uint) processId,
                    dwDebugEngineCount = (uint) debuggerEngineGuids.Length,
                    pDebugEngines = pDebugEngines,
                };

                var debugger = (IVsDebugger4) _serviceProvider.GetService(typeof(SVsShellDebugger));

                AttachDebuggerRetrying(debugger, debugTarget);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pDebugEngines);
            }
            return true;
        }

        // mixed mode debugging of .NET Core is done by the native and the CoreCLR engine together (as VS does for
        // .NET projects with native debugging enabled), while the legacy mixed mode engine only supports the .NET Framework
        public static Guid[] GetDebuggerEngineGuids(DebuggerEngine debuggerEngine)
        {
            switch (debuggerEngine)
            {
                case DebuggerEngine.Native:
                    return new[] { NativeEngineGuid };
                case DebuggerEngine.ManagedAndNative:
                    return new[] { ManagedAndNativeEngineGuid };
                case DebuggerEngine.ManagedCoreAndNative:
                    return new[] { NativeEngineGuid, ManagedCoreEngineGuid };
                default:
                    throw new ArgumentOutOfRangeException(nameof(debuggerEngine), debuggerEngine, "Unknown debugger engine");
            }
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