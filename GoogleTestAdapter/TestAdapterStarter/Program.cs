using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static String getDir([System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
    {
        return Path.GetDirectoryName(fileName);
    }
    static DTE2 dte = null;

    static void Main()
    {
#if DEBUG
        String config = "Debug";
#else
        String config = "Release";
#endif

        String dir = getDir();
        String exe = Assembly.GetExecutingAssembly().Location;
        //String exe2scan = Path.Combine(dir, @"..\..\out\binaries\SampleTests", config, "Tests_gta.exe");
        String exe2scan = @"C:\Prototyping\syncProj\syncproj.exe";

        Assembly asm = Assembly.LoadFile(@"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe");
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            if (args.RequestingAssembly == null)
                return null;

            String dll = Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), args.Name.Split(',')[0] + ".dll");

            if (File.Exists(dll))
                return Assembly.LoadFrom(dll);

            return null;
        };
        var main = asm.GetTypes().Select(x => x.GetMethod("Main", BindingFlags.Static | BindingFlags.Public)).Where(x => x != null).First();

        System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        System.Diagnostics.Process debuggerProcess = null;
        debuggerProcess = GetParentProcess(currentProcess);

        //
        // Visual studio is either debug process by itself, or it starts msvsmon.exe, when we locate
        // parent of parent approach.
        //
        if (debuggerProcess != null && debuggerProcess.ProcessName.ToLower() == "msvsmon")
            debuggerProcess = GetParentProcess(debuggerProcess);

        if (debuggerProcess != null && debuggerProcess.ProcessName.ToLower() != "devenv")
            debuggerProcess = null;     // Not a visual studio, e.g. cmd

        if (debuggerProcess != null)
        {
            MessageFilter.Register();
            dte = GetDTE(debuggerProcess.Id);

            // Break debugger in testhost.exe
            Environment.SetEnvironmentVariable("VSTEST_HOST_DEBUG", "1");
        }

        ConsoleWriter cw = new ConsoleWriter();
        var co = Console.Out;
        Console.SetOut(cw);

        main.Invoke(null, 
            new object[] {
                new string[]
                {
                    "-lt" ,
                    "--TestAdapterPath:"  + Path.Combine(dir, @"..\..\out\binaries\GoogleTestAdapter", config, "TestAdapter.Tests"),
                    exe2scan
                }
            }
        );

        cw.Flush();
        Console.SetOut(co);
        Console.WriteLine(cw.sb.ToString());


        if (debuggerProcess != null)
            MessageFilter.Revoke();
    }

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    /// <summary>
    /// Gets the DTE object from any devenv process.
    /// </summary>
    /// <param name="processId">
    /// <returns>
    /// Retrieved DTE object or <see langword="null"> if not found.
    /// </see></returns>
    private static DTE2 GetDTE(int processId)
    {
        object runningObject = null;

        IBindCtx bindCtx = null;
        IRunningObjectTable rot = null;
        IEnumMoniker enumMonikers = null;

        try
        {
            Marshal.ThrowExceptionForHR(CreateBindCtx(reserved: 0, ppbc: out bindCtx));
            bindCtx.GetRunningObjectTable(out rot);
            rot.EnumRunning(out enumMonikers);

            IMoniker[] moniker = new IMoniker[1];
            IntPtr numberFetched = IntPtr.Zero;
            while (enumMonikers.Next(1, moniker, numberFetched) == 0)
            {
                IMoniker runningObjectMoniker = moniker[0];

                string name = null;

                try
                {
                    if (runningObjectMoniker != null)
                    {
                        runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Do nothing, there is something in the ROT that we do not have access to.
                }

                Regex monikerRegex = new Regex(@"!VisualStudio.DTE\.\d+\.\d+\:" + processId, RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(name) && monikerRegex.IsMatch(name))
                {
                    Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                    break;
                }
            }
        }
        finally
        {
            if (enumMonikers != null)
            {
                Marshal.ReleaseComObject(enumMonikers);
            }

            if (rot != null)
            {
                Marshal.ReleaseComObject(rot);
            }

            if (bindCtx != null)
            {
                Marshal.ReleaseComObject(bindCtx);
            }
        }

        return runningObject as DTE2;
    }


    /// <summary>
    /// Queries for parent process, returns null if cannot be identified.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>parent process or null if not found</returns>
    private static System.Diagnostics.Process GetParentProcess(System.Diagnostics.Process process)
    {
        System.Diagnostics.Process parentProcess = null;
        var processName = process.ProcessName;
        var nbrOfProcessWithThisName = System.Diagnostics.Process.GetProcessesByName(processName).Length;

        for (var index = 0; index < nbrOfProcessWithThisName; index++)
        {
            var processIndexdName = index == 0 ? processName : processName + "#" + index;
            var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
            if ((int)processId.NextValue() == process.Id)
            {
                var parentId = new PerformanceCounter("Process", "Creating Process ID", processIndexdName);
                try
                {
                    parentProcess = System.Diagnostics.Process.GetProcessById((int)parentId.NextValue());
                }
                catch (ArgumentException)
                {
                    // Expected when starting with debugger to single process
                }
                break;
            }
        }

        return parentProcess;
    }

    public class ConsoleWriter : TextWriter
    {
        public StringBuilder sb = new StringBuilder();

        public override Encoding Encoding { get { return Encoding.UTF8; } }


        static Regex reHostId = new Regex("Process Id: ([0-9]+), Name: testhost");

        public void CheckInput(String s)
        { 
            Match m = reHostId.Match(s);
            if (!m.Success || dte == null)
                return;

            int processId = Int32.Parse(m.Groups[1].Value);
            var processes = dte.Debugger.LocalProcesses.Cast<Process2>().ToArray();
            Process2 process = processes.Where(x => x.ProcessID == processId).Cast<Process2>().FirstOrDefault();
            if (process == null)
                return;

            process.Attach();
        }


        public override void Write(string value)
        {
            sb.Append(value);
            CheckInput(value);
        }

        public override void WriteLine(string value)
        {
            sb.AppendLine(value);
            CheckInput(value);
        }
    }
}


