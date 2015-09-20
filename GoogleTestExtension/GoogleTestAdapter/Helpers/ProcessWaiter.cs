using System;
using System.Diagnostics;
using System.Threading;

namespace GoogleTestAdapter.Helpers
{

    class ProcessWaiter
    {
        internal int ProcessExitCode { get; private set; } = -1;
        internal bool Exited { get; private set; } = false;

        internal ProcessWaiter(Process process)
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnExited;
        }

        internal void WaitForExit()
        {
            lock (this)
            {
                while (!Exited)
                {
                    Monitor.Wait(this);
                }
            }
        }

        public void OnExited(object sender, EventArgs e)
        {
            Process process = sender as Process;
            if (process != null)
            {
                lock (this)
                {
                    ProcessExitCode = process.ExitCode;
                    process.Exited -= OnExited;

                    Exited = true;
                    Monitor.Pulse(this);
                }
            }
        }

    }

}