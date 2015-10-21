using System;
using System.Diagnostics;
using System.Threading;

namespace GoogleTestAdapter.Helpers
{

    class ProcessWaiter
    {
        internal int ProcessExitCode { get; private set; } = -1;
        private bool Exited { get; set; } = false;


        internal ProcessWaiter(Process process)
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnExited;
        }


        internal int WaitForExit()
        {
            lock (this)
            {
                while (!Exited)
                {
                    Monitor.Wait(this);
                }
            }

            return ProcessExitCode;
        }

        private void OnExited(object sender, EventArgs e)
        {
            Process process = sender as Process;
            if (process != null)
            {
                lock (this)
                {
                    ProcessExitCode = process.ExitCode;
                    Exited = true;

                    process.Exited -= OnExited;

                    Monitor.Pulse(this);
                }
            }
        }

    }

}