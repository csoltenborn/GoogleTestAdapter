using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GoogleTestAdapter.Common
{
    /// <summary>
    /// Windows job object holding a test executable and all processes it starts. Terminating the job kills the
    /// whole process tree (killing the test executable only leaves its child processes running, which might
    /// furthermore keep the executable's output pipe open). Since the job is configured to kill its processes when
    /// its last handle is closed, the processes are also killed if the process running GTA dies (e.g., if the test
    /// host is killed by VS after canceling a test run).
    /// </summary>
    public sealed class JobObject : IDisposable
    {
        private readonly SafeWaitHandle _handle;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private JobObject(SafeWaitHandle handle, ILogger logger)
        {
            _handle = handle;
            _logger = logger;
        }

        /// <summary>
        /// Creates a job and assigns the process to it. Returns null if that fails.
        /// </summary>
        public static JobObject TryCreate(IntPtr processHandle, int processId, ILogger logger)
        {
            var handle = CreateJobObject(IntPtr.Zero, null);
            if (handle.IsInvalid)
            {
                logger.DebugWarning($"Could not create job object for process {processId}: {Win32Utils.GetLastWin32Error()}");
                return null;
            }

            var job = new JobObject(handle, logger);
            if (!job.SetKillOnJobClose(true))
            {
                logger.DebugWarning($"Could not configure job object for process {processId}: {Win32Utils.GetLastWin32Error()}");
                job.Dispose();
                return null;
            }

            if (!AssignProcessToJobObject(handle, processHandle))
            {
                logger.DebugWarning($"Could not assign process {processId} to job object: {Win32Utils.GetLastWin32Error()}");
                job.Dispose();
                return null;
            }

            return job;
        }

        public void Terminate()
        {
            lock (_lock)
            {
                if (_handle.IsClosed)
                    return;

                _logger.DebugInfo("Terminating job object of test executable and its child processes");
                if (!TerminateJobObject(_handle, unchecked((uint)-1)))
                    _logger.DebugWarning($"Could not terminate job object: {Win32Utils.GetLastWin32Error()}");
            }
        }

        /// <summary>
        /// Closes the job without killing processes which are left behind by the (finished) test executable.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_handle.IsClosed)
                    return;

                SetKillOnJobClose(false);
                _handle.Dispose();
            }
        }

        private bool SetKillOnJobClose(bool killOnJobClose)
        {
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = killOnJobClose ? JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE : 0
                }
            };
            return SetInformationJobObject(_handle, JobObjectExtendedLimitInformation, ref info,
                (uint)Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)));
        }

        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeWaitHandle CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(SafeWaitHandle hJob, int jobObjectInfoClass,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(SafeWaitHandle hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateJobObject(SafeWaitHandle hJob, uint uExitCode);
    }
}
