﻿// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Common.ProcessExecution;
using GoogleTestAdapter.Common.ProcessExecution.Contracts;
using Microsoft.Win32.SafeHandles;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public class NativeDebuggedProcessExecutor : IDebuggedProcessExecutor
    {
        public const int ExecutionFailed = int.MaxValue;

        private readonly IDebuggerAttacher _debuggerAttacher;
        private readonly DebuggerEngine _debuggerEngine;
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;

        public NativeDebuggedProcessExecutor(IDebuggerAttacher debuggerAttacher, DebuggerEngine debuggerEngine, bool printTestOutput, ILogger logger)
        {
            _debuggerAttacher = debuggerAttacher ?? throw new ArgumentNullException(nameof(debuggerAttacher));
            _debuggerEngine = debuggerEngine;
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, IDictionary<string, string> environmentVariables, Action<string> reportOutputLine)
        {
            try
            {
                int exitCode = NativeMethods.ExecuteCommandBlocking(command, parameters, workingDir, pathExtension, environmentVariables, _debuggerAttacher, _debuggerEngine, _logger, _printTestOutput, reportOutputLine, processId => _processId = processId);
                _logger.DebugInfo($"Executable {command} returned with exit code {exitCode}");
                return exitCode;
            }
            catch (Win32Exception ex)
            {
                string nativeErrorMessage = new Win32Exception(ex.NativeErrorCode).Message;
                _logger.LogError($"{ex.Message} ({ex.NativeErrorCode}: {nativeErrorMessage})");
                return ExecutionFailed;
            }
        }

        private int? _processId;

        public void Cancel()
        {
            if (_processId.HasValue)
                ProcessUtils.KillProcess(_processId.Value, _logger);
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
#pragma warning disable 169
#pragma warning disable 414
        private static class NativeMethods
        {
            private class ProcessOutputPipeStream : PipeStream
            {
                public readonly SafePipeHandle _writingEnd;

                public ProcessOutputPipeStream() : base(PipeDirection.In, 0)
                {
                    SafePipeHandle readingEnd;
                    CreatePipe(out readingEnd, out _writingEnd);
                    InitializeHandle(readingEnd, false, false);
                }

                public void ConnectedToChildProcess()
                {
                    // Close the writing end of the pipe - it's still open in the child process.
                    // If we didn't close it, a StreamReader would never reach EndOfStream.
                    _writingEnd?.Dispose();
                    IsConnected = true;
                }

                protected override void Dispose(bool disposing)
                {
                    base.Dispose(disposing);
                    if (disposing)
                        _writingEnd?.Dispose();
                }
            }

            private const int STARTF_USESTDHANDLES = 0x00000100;
            private const uint CREATE_SUSPENDED = 0x00000004;
            private const uint CREATE_EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            private const uint HANDLE_FLAG_INHERIT = 0x00000001;
            private const uint INFINITE = 0xFFFFFFFF;

            internal static int ExecuteCommandBlocking(
                string command, string parameters, string workingDir, string pathExtension, IDictionary<string, string> environmentVariables, 
                IDebuggerAttacher debuggerAttacher, DebuggerEngine debuggerEngine, 
                ILogger logger, bool printTestOutput, Action<string> reportOutputLine, Action<int> reportProcessId)
            {
                ProcessOutputPipeStream pipeStream = null;
                try
                {
                    pipeStream = new ProcessOutputPipeStream();

                    var processInfo = CreateProcess(command, parameters, workingDir, pathExtension, environmentVariables, pipeStream._writingEnd);
                    reportProcessId(processInfo.dwProcessId);
                    using (var process = new SafeWaitHandle(processInfo.hProcess, true))
                    using (var thread  = new SafeWaitHandle(processInfo.hThread, true))
                    {
                        pipeStream.ConnectedToChildProcess();

                        logger.DebugInfo($"Attaching debugger to '{command}' via {debuggerEngine} engine");
                        if (!debuggerAttacher.AttachDebugger(processInfo.dwProcessId, debuggerEngine))
                        {
                            logger.LogError($"Could not attach debugger to process {processInfo.dwProcessId}");
                        }

                        if (printTestOutput)
                        {
                            DotNetProcessExecutor.LogStartOfOutput(logger, command, parameters);
                        }

                        ResumeThread(thread);

                        using (var reader = new StreamReader(pipeStream, Encoding.Default))
                        {
                            pipeStream = null;

                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                reportOutputLine(line);
                                if (printTestOutput)
                                {
                                    logger.LogInfo(line);
                                }
                            }
                        }

                        WaitForSingleObject(process, INFINITE);

                        if (printTestOutput)
                        {
                            DotNetProcessExecutor.LogEndOfOutput(logger);
                        }

                        int exitCode;
                        if (!GetExitCodeProcess(process, out exitCode))
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get exit code of process");

                        return exitCode;
                    }
                }
                finally
                {
                    pipeStream?.Dispose();
                }
            }

            public static void CreatePipe(out SafePipeHandle readingEnd, out SafePipeHandle writingEnd)
            {
                if (!CreatePipe(out readingEnd, out writingEnd, IntPtr.Zero, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create pipe");

                if (!SetHandleInformation(writingEnd, HANDLE_FLAG_INHERIT, HANDLE_FLAG_INHERIT))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set handle information");
            }

            private static StringBuilder CreateEnvironment(string pathExtension, IDictionary<string, string> environmentVariables)
            {
                StringDictionary envVariables = new ProcessStartInfo().EnvironmentVariables;
                
                if (!string.IsNullOrEmpty(pathExtension))
                    envVariables["PATH"] = CommonUtils.GetExtendedPath(pathExtension);
                foreach (var environmentVariable in environmentVariables)
                    envVariables[environmentVariable.Key] = environmentVariable.Value;

                var envVariablesList = new List<string>();
                foreach (DictionaryEntry entry in envVariables)
                    envVariablesList.Add($"{entry.Key}={entry.Value}");
                envVariablesList.Sort();

                var result = new StringBuilder();
                foreach (string envVariable in envVariablesList)
                {
                    result.Append(envVariable);
                    result.Length++;
                }
                result.Length++;

                return result;
            }

            private static PROCESS_INFORMATION CreateProcess(string command, string parameters, string workingDir, string pathExtension, IDictionary<string, string> environmentVariables, 
                SafePipeHandle outputPipeWritingEnd)
            {
                var startupinfoex = new STARTUPINFOEX
                {
                    StartupInfo = new STARTUPINFO
                    {
                        hStdOutput = outputPipeWritingEnd,
                        hStdError = outputPipeWritingEnd,
                        dwFlags = STARTF_USESTDHANDLES,
                        cb = Marshal.SizeOf(typeof(STARTUPINFOEX)),
                    }
                };

                string commandLine = $"\"{command}\"";
                if (!string.IsNullOrEmpty(parameters))
                    commandLine += $" {parameters}";
                if (string.IsNullOrEmpty(workingDir))
                    workingDir = null;

                PROCESS_INFORMATION processInfo;
                // ReSharper disable ArgumentsStyleNamedExpression
                // ReSharper disable ArgumentsStyleLiteral
                // ReSharper disable ArgumentsStyleOther
                if (!CreateProcess(
                    lpApplicationName: null,
                    lpCommandLine: commandLine,
                    lpProcessAttributes: null, 
                    lpThreadAttributes: null, 
                    bInheritHandles: true,
                    dwCreationFlags: CREATE_EXTENDED_STARTUPINFO_PRESENT | CREATE_SUSPENDED,
                    lpEnvironment: CreateEnvironment(pathExtension, environmentVariables),
                    lpCurrentDirectory: workingDir,
                    lpStartupInfo: startupinfoex,
                    lpProcessInformation: out processInfo))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        $"Could not create process. Command: '{command}', parameters: '{parameters}', working dir: '{workingDir}'");
                }
                // ReSharper restore ArgumentsStyleNamedExpression
                // ReSharper restore ArgumentsStyleLiteral
                // ReSharper restore ArgumentsStyleOther

                return processInfo;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CreateProcess(
                string lpApplicationName, string lpCommandLine, 
                SECURITY_ATTRIBUTES lpProcessAttributes, SECURITY_ATTRIBUTES lpThreadAttributes,
                bool bInheritHandles, uint dwCreationFlags,
                [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder lpEnvironment, string lpCurrentDirectory, [In] STARTUPINFOEX lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern uint WaitForSingleObject(SafeHandle hProcess, uint dwMilliseconds);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetExitCodeProcess(SafeHandle hProcess, out int lpExitCode);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetHandleInformation(SafeHandle hObject, uint dwMask, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CreatePipe(
                out SafePipeHandle hReadPipe, out SafePipeHandle hWritePipe, 
                IntPtr securityAttributes, int nSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern int ResumeThread(SafeHandle hThread);

            private static readonly SafeHandle NULL_HANDLE = new SafePipeHandle(IntPtr.Zero, false);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private class STARTUPINFOEX
            {
                public STARTUPINFO StartupInfo;
                public IntPtr lpAttributeList;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [BestFitMapping(false, ThrowOnUnmappableChar = true)]
            private class STARTUPINFO
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
                public SafeHandle hStdInput = NULL_HANDLE;
                public SafeHandle hStdOutput = NULL_HANDLE;
                public SafeHandle hStdError = NULL_HANDLE;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESS_INFORMATION
            {
                public IntPtr hProcess; // I would like to make these two IntPtr a SafeWaiHandle but the
                public IntPtr hThread;  // marshaller doesn't seem to support this for out structs/classes
                public int dwProcessId;
                public int dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            private class SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }
        }
#pragma warning restore 414
#pragma warning restore 169
    }
}