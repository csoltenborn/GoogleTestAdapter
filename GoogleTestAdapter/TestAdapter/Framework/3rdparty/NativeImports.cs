// Stripped version of: https://github.com/SymbolSource/Microsoft.Samples.Debugging/tree/master/src/debugger/NativeDebugWrappers
//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Part of managed wrappers for native debugging APIs.
// NativeImports.cs: raw definitions of native methods and structures 
//  for native debugging API.
//  Also includes some useful utility methods.
//---------------------------------------------------------------------


using System;
using System.Runtime.InteropServices;

namespace Microsoft.Samples.Debugging.Native
{
    #region Native Structures
    /// <summary>
    /// Native debug event Codes that are returned through NativeStop event
    /// </summary>
    public enum NativeDebugEventCode
    {
        None = 0,
        EXCEPTION_DEBUG_EVENT = 1,
        CREATE_THREAD_DEBUG_EVENT = 2,
        CREATE_PROCESS_DEBUG_EVENT = 3,
        EXIT_THREAD_DEBUG_EVENT = 4,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        LOAD_DLL_DEBUG_EVENT = 6,
        UNLOAD_DLL_DEBUG_EVENT = 7,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        RIP_EVENT = 9,
    }

    // Debug header for debug events.
    [StructLayout(LayoutKind.Sequential)]
    public struct DebugEventHeader
    {
        public NativeDebugEventCode dwDebugEventCode;
        public UInt32 dwProcessId;
        public UInt32 dwThreadId;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_PROCESS_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr hProcess;
        public IntPtr hThread;
        public IntPtr lpBaseOfImage;
        public UInt32 dwDebugInfoFileOffset;
        public UInt32 nDebugInfoSize;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
        public IntPtr lpImageName;
        public UInt16 fUnicode;
    } // end of class CREATE_PROCESS_DEBUG_INFO

    [StructLayout(LayoutKind.Sequential)]
    public struct LOAD_DLL_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr lpBaseOfDll;
        public UInt32 dwDebugInfoFileOffset;
        public UInt32 nDebugInfoSize;
        public IntPtr lpImageName;
        public UInt16 fUnicode;
    } // end of class LOAD_DLL_DEBUG_INFO

    [StructLayout(LayoutKind.Explicit)]
    public struct DebugEventUnion
    {
        [FieldOffset(0)]
        public CREATE_PROCESS_DEBUG_INFO CreateProcess;

        //[FieldOffset(0)]
        //public EXCEPTION_DEBUG_INFO Exception;

        //[FieldOffset(0)]
        //public CREATE_THREAD_DEBUG_INFO CreateThread;

        //[FieldOffset(0)]
        //public EXIT_THREAD_DEBUG_INFO ExitThread;

        //[FieldOffset(0)]
        //public EXIT_PROCESS_DEBUG_INFO ExitProcess;

        [FieldOffset(0)]
        public LOAD_DLL_DEBUG_INFO LoadDll;

        //[FieldOffset(0)]
        //public UNLOAD_DLL_DEBUG_INFO UnloadDll;

        //[FieldOffset(0)]
        //public OUTPUT_DEBUG_STRING_INFO OutputDebugString;
    }

    // 32-bit and 64-bit have sufficiently different alignment that we need 
    // two different debug event structures.

    /// <summary>
    /// Matches DEBUG_EVENT layout on 32-bit architecture
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DebugEvent32
    {
        [FieldOffset(0)]
        public DebugEventHeader header;

        [FieldOffset(12)]
        public DebugEventUnion union;
    }

    /// <summary>
    /// Matches DEBUG_EVENT layout on 64-bit architecture
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DebugEvent64
    {
        [FieldOffset(0)]
        public DebugEventHeader header;

        [FieldOffset(16)]
        public DebugEventUnion union;
    }

    #endregion Native Structures


    // These extend the Mdbg native definitions.
    public static class NativeMethods
    {
        private const string Kernel32LibraryName = "kernel32.dll";

        #region Attach / Detach APIS
        // Attach to a process
        [DllImport(Kernel32LibraryName, SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DebugActiveProcess(uint dwProcessId);

        // Detach from a process
        [DllImport(Kernel32LibraryName, SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);
        #endregion // Attach / Detach APIS


        #region Stop-Go APIs
        // We have two separate versions of kernel32!WaitForDebugEvent to cope with different structure
        // layout on each platform.
        [DllImport(Kernel32LibraryName, EntryPoint = "WaitForDebugEvent", SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent32(ref DebugEvent32 pDebugEvent, int dwMilliseconds);

        [DllImport(Kernel32LibraryName, EntryPoint = "WaitForDebugEvent", SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent64(ref DebugEvent64 pDebugEvent, int dwMilliseconds);

        /// <summary>
        /// Values to pass to ContinueDebugEvent for ContinueStatus
        /// </summary>
        public enum ContinueStatus : uint
        {
            /// <summary>
            /// This is our own "empty" value
            /// </summary>
            CONTINUED = 0,

            /// <summary>
            /// Debugger consumes exceptions. Debuggee will never see the exception. Like "gh" in Windbg.
            /// </summary>
            DBG_CONTINUE = 0x00010002,

            /// <summary>
            /// Debugger does not interfere with exception processing, this passes the exception onto the debuggee.
            /// Like "gn" in Windbg.
            /// </summary>
            DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
        }

        [DllImport(Kernel32LibraryName, SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus);

        #endregion // Stop-Go
    } // NativeMethods

}