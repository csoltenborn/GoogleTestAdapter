using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{
    public class ProcessExecutor : IProcessExecutor
    {
        public const int ExecutionFailed = int.MaxValue;

        private readonly IDebuggerAttacher _debuggerAttacher;
        private readonly ILogger _logger;

        public ProcessExecutor(IDebuggerAttacher debuggerAttacher, ILogger logger)
        {
            _debuggerAttacher = debuggerAttacher;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, out string[] standardOutput, out string[] errorOutput)
        {
            IList<string> standardOutputLines = new List<string>();
            IList<string> errorOutputLines = new List<string>();

            int exitCode = ExecuteCommandBlocking(command, parameters, workingDir, pathExtension,
                s => standardOutputLines.Add(s),
                s => errorOutputLines.Add(s));

            standardOutput = standardOutputLines.ToArray();
            errorOutput = errorOutputLines.ToArray();
            return exitCode;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, Action<string> reportStandardOutputLine, Action<string> reportStandardErrorLine)
        {
            return NativeMethods.ExecuteCommandBlocking(command, parameters, workingDir, pathExtension, _debuggerAttacher, 
                new OutputSplitter(reportStandardOutputLine), new OutputSplitter(reportStandardErrorLine), _logger);
        }

        private class OutputSplitter
        {
            private readonly Action<string> _reportLineAction;

            private string _currentOutput = "";

            internal OutputSplitter(Action<string> reportLineAction)
            {
                _reportLineAction = reportLineAction;
            }

            internal void ReportOutputPart(string part)
            {
                _currentOutput += part;
                string[] lines = Regex.Split(_currentOutput, "\r\n|\r|\n");
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    _reportLineAction(lines[i]);
                }
                _currentOutput = lines.Last();
            }

            internal void Flush()
            {
                if (!string.IsNullOrEmpty(_currentOutput))
                    _reportLineAction(_currentOutput);

                _currentOutput = "";
            }

        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static class NativeMethods
        {
            private const int STILL_ACTIVE = 259;
            private const int STARTF_USESTDHANDLES = 0x00000100;
            private const uint CREATE_SUSPENDED = 0x00000004;
            private const uint CREATE_EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            private const uint HANDLE_FLAG_INHERIT = 0x00000001;

            private static readonly Encoding Encoding = Encoding.Default;

            internal static int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, IDebuggerAttacher debuggerAttacher, 
                OutputSplitter standardOutputSplitter, OutputSplitter errorOutputSplitter, ILogger logger)
            {
                var processInfo = new PROCESS_INFORMATION
                {
                    hProcess = IntPtr.Zero,
                    hThread = IntPtr.Zero
                };
                IntPtr stdoutReadingEnd = IntPtr.Zero, stdoutWritingEnd = IntPtr.Zero;
                IntPtr stderrReadingEnd = IntPtr.Zero, stderrWritingEnd = IntPtr.Zero;
                try
                {
                    if (!CreatePipe(out stdoutReadingEnd, out stdoutWritingEnd))
                        return LogAndReturnExecutionFailed(logger, "Could not create pipe for standard output");
                    if (!CreatePipe(out stderrReadingEnd, out stderrWritingEnd))
                        return LogAndReturnExecutionFailed(logger, "Could not create pipe for error output");

                    if (!CreateProcess(command, parameters, workingDir, pathExtension, stdoutWritingEnd, stderrWritingEnd, out processInfo))
                        return LogAndReturnExecutionFailed(logger, $"Could not create process. Command: '{command}', parameters: '{parameters}', working dir: '{workingDir}'");

                    debuggerAttacher?.AttachDebugger(processInfo.dwProcessId);

                    ResumeThread(processInfo.hThread);

                    ReadProcessOutput(processInfo, stdoutReadingEnd, standardOutputSplitter, stderrReadingEnd, errorOutputSplitter);
                    standardOutputSplitter.Flush();
                    errorOutputSplitter.Flush();

                    int exitCode;
                    if (GetExitCodeProcess(processInfo.hProcess, out exitCode))
                        return exitCode;

                    return LogAndReturnExecutionFailed(logger, "Could not receive exit code of process");
                }
                finally
                {
                    SafeCloseHandle(ref processInfo.hProcess);
                    SafeCloseHandle(ref processInfo.hThread);
                    SafeCloseHandle(ref stdoutReadingEnd);
                    SafeCloseHandle(ref stdoutWritingEnd);
                    SafeCloseHandle(ref stderrReadingEnd);
                    SafeCloseHandle(ref stderrWritingEnd);
                }
            }

            private static bool CreatePipe(out IntPtr readingEnd, out IntPtr writingEnd)
            {
                var securityAttributes = new SECURITY_ATTRIBUTES();
                securityAttributes.nLength = Marshal.SizeOf(securityAttributes);
                securityAttributes.bInheritHandle = true;
                securityAttributes.lpSecurityDescriptor = IntPtr.Zero;

                IntPtr securityAttributesPointer = Marshal.AllocHGlobal(Marshal.SizeOf(securityAttributes));
                Marshal.StructureToPtr(securityAttributes, securityAttributesPointer, true);

                if (!CreatePipe(out readingEnd, out writingEnd, securityAttributesPointer, 0))
                    return false;

                if (!SetHandleInformation(readingEnd, HANDLE_FLAG_INHERIT, 0))
                    return false;

                return true;
            }

            private static StringBuilder CreateEnvironment(string pathExtension)
            {
                StringDictionary envVariables = new ProcessStartInfo().EnvironmentVariables;
                
                if (!string.IsNullOrEmpty(pathExtension))
                    envVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

                List<string> envVariablesList = new List<string>();
                foreach (string key in envVariables.Keys)
                {
                    envVariablesList.Add($"{key}={envVariables[key]}");
                }
                envVariablesList.Sort();

                StringBuilder result = new StringBuilder();
                foreach (string envVariable in envVariablesList)
                {
                    result.Append(envVariable);
                    result.Length++;
                }
                result.Length++;

                return result;
            }

            private static bool CreateProcess(string command, string parameters, string workingDir, string pathExtension, 
                IntPtr stdoutWritingEnd, IntPtr stderrWritingEnd, 
                out PROCESS_INFORMATION processInfo)
            {
                var startupinfoex = new STARTUPINFOEX
                {
                    StartupInfo = new STARTUPINFO
                    {
                        hStdOutput = stdoutWritingEnd,
                        hStdError = stderrWritingEnd
                    }
                };
                startupinfoex.StartupInfo.dwFlags |= STARTF_USESTDHANDLES;
                startupinfoex.StartupInfo.cb = Marshal.SizeOf(startupinfoex);

                var processSecurityAttributes = new SECURITY_ATTRIBUTES();
                processSecurityAttributes.nLength = Marshal.SizeOf(processSecurityAttributes);
                var threadSecurityAttributes = new SECURITY_ATTRIBUTES();
                threadSecurityAttributes.nLength = Marshal.SizeOf(threadSecurityAttributes);

                if (!string.IsNullOrEmpty(parameters))
                    parameters = $"{command} {parameters}";
                if (string.IsNullOrEmpty(workingDir))
                    workingDir = null;

                // ReSharper disable ArgumentsStyleNamedExpression
                // ReSharper disable ArgumentsStyleLiteral
                // ReSharper disable ArgumentsStyleOther
                return CreateProcess(
                    lpApplicationName: command,
                    lpCommandLine: parameters,
                    lpProcessAttributes: ref processSecurityAttributes,
                    lpThreadAttributes: ref threadSecurityAttributes,
                    bInheritHandles: true,
                    dwCreationFlags: CREATE_EXTENDED_STARTUPINFO_PRESENT | CREATE_SUSPENDED,
                    lpEnvironment: CreateEnvironment(pathExtension),
                    lpCurrentDirectory: workingDir,
                    lpStartupInfo: ref startupinfoex,
                    lpProcessInformation: out processInfo);
                // ReSharper restore ArgumentsStyleNamedExpression
                // ReSharper restore ArgumentsStyleLiteral
                // ReSharper restore ArgumentsStyleOther
            }

            // after http://edn.embarcadero.com/article/10387
            private static void ReadProcessOutput(
                PROCESS_INFORMATION processInfo, 
                IntPtr stdoutReadingEnd, OutputSplitter standardOutputSplitter, 
                IntPtr stderrReadingEnd, OutputSplitter errorOutputSplitter)
            {
                for (;;)
                {
                    ReadPipe(stdoutReadingEnd, standardOutputSplitter);
                    ReadPipe(stderrReadingEnd, errorOutputSplitter);

                    int exitCode;
                    if (!GetExitCodeProcess(processInfo.hProcess, out exitCode)
                        || exitCode != STILL_ACTIVE)
                        break;
                }
            }

            private static void ReadPipe(IntPtr readingEnd, OutputSplitter outputSplitter)
            {
                byte[] buffer = new byte[1024];
                uint bytesRead = 0, bytesAvailable = 0, bytesLeftThisMessage = 0;
                PeekNamedPipe(readingEnd, buffer, (uint) buffer.Length, ref bytesRead, ref bytesAvailable, ref bytesLeftThisMessage);

                if (bytesRead != 0)
                {
                    buffer.Initialize();
                    if (bytesAvailable > buffer.Length - 1)
                    {
                        while (bytesRead >= buffer.Length - 1)
                        {
                            ReadPipe(readingEnd, outputSplitter, buffer, ref bytesRead);
                        }
                    }
                    else
                    {
                        ReadPipe(readingEnd, outputSplitter, buffer, ref bytesRead);
                    }
                }
            }

            private static unsafe void ReadPipe(IntPtr readingEnd, OutputSplitter outputSplitter, byte[] buffer, ref uint bytesRead)
            {
                ReadFile(readingEnd, buffer, buffer.Length - 1, ref bytesRead, null);
                string content = Encoding.GetString(buffer, 0, (int) bytesRead);
                outputSplitter.ReportOutputPart(content);
                buffer.Initialize();
            }

            private static void SafeCloseHandle(ref IntPtr hObject)
            {
                if (hObject != IntPtr.Zero)
                {
                    CloseHandle(hObject);
                    hObject = IntPtr.Zero;
                }
            }

            private static int LogAndReturnExecutionFailed(ILogger logger, string message)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMessage = new Win32Exception(errorCode).Message;
                logger.LogError($"{message} ({errorCode}: {errorMessage})");
                return ExecutionFailed;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CreateProcess(
                string lpApplicationName, string lpCommandLine, 
                ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
                bool bInheritHandles, uint dwCreationFlags,
                [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CreatePipe(
                out IntPtr hReadPipe, out IntPtr hWritePipe, 
                IntPtr securityAttributes, int nSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool PeekNamedPipe(
                IntPtr handle, byte[] buffer, uint nBufferSize, 
                ref uint bytesRead, ref uint bytesAvail, ref uint BytesLeftThisMessage);

            [DllImport("kernel32", SetLastError = true)]
            static extern unsafe bool ReadFile(
                IntPtr hFile, byte[] pBuffer, int NumberOfBytesToRead, 
                ref uint pNumberOfBytesRead, NativeOverlapped* overlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern int ResumeThread(IntPtr hThread);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct STARTUPINFOEX
            {
                public STARTUPINFO StartupInfo;
                public IntPtr lpAttributeList;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct STARTUPINFO
            {
                public Int32 cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public Int32 dwX;
                public Int32 dwY;
                public Int32 dwXSize;
                public Int32 dwYSize;
                public Int32 dwXCountChars;
                public Int32 dwYCountChars;
                public Int32 dwFillAttribute;
                public Int32 dwFlags;
                public Int16 wShowWindow;
                public Int16 cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public int dwProcessId;
                public int dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

        }

    }

}