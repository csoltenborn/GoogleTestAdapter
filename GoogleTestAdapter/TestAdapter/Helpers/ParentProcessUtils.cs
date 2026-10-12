using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    /// <summary>
    /// A utility class to determine a process' parent and executable.
    /// From http://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
    /// </summary>
    /// <remarks>
    /// Processes are opened with limited query rights only, and their executables are determined with
    /// QueryFullProcessImageName(): Process.MainModule fails for 64-bit processes if called from a 32-bit process (e.g.
    /// a 32-bit test host of the 64-bit VS 2022 or later) and for processes with higher privileges.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessUtils
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const int MaxPathLength = 32767;

        private static class NativeMethods
        {
            [DllImport("ntdll.dll")]
            internal static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtils processInformation, int processInformationLength, out int returnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern bool QueryFullProcessImageName(IntPtr processHandle, int flags, StringBuilder exeName, ref int size);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr handle);
        }

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class or null if an error occurred.</returns>
        public static Process GetParentProcess(int id)
        {
            int parentId = GetParentProcessId(id);
            if (parentId <= 0)
                return null;

            try
            {
                return Process.GetProcessById(parentId);
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }

        /// <returns>The id of the process' parent, or 0 if it can not be determined</returns>
        public static int GetParentProcessId(int id)
        {
            return WithProcessHandle(id, handle =>
            {
                ParentProcessUtils pbi = new ParentProcessUtils();
                int status = NativeMethods.NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
                return status == 0 ? pbi.InheritedFromUniqueProcessId.ToInt32() : 0;
            }, 0);
        }

        /// <returns>The full path of the process' executable, or null if it can not be determined</returns>
        public static string GetExecutablePath(int id)
        {
            return WithProcessHandle(id, handle =>
            {
                var path = new StringBuilder(MaxPathLength);
                int size = path.Capacity;
                return NativeMethods.QueryFullProcessImageName(handle, 0, path, ref size) ? path.ToString() : null;
            }, null);
        }

        private static T WithProcessHandle<T>(int id, Func<IntPtr, T> action, T defaultValue)
        {
            IntPtr handle = NativeMethods.OpenProcess(ProcessQueryLimitedInformation, false, id);
            if (handle == IntPtr.Zero)
                return defaultValue;

            try
            {
                return action(handle);
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }
    }
}
