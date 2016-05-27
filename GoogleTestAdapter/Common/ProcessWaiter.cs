using System;
using System.Diagnostics;
using System.Threading;

namespace GoogleTestAdapter.Common
{

    public class ProcessWaiter
    {
        public int ProcessExitCode { get; private set; } = -1;
        private bool _exited;


        public ProcessWaiter(Process process)
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnExited;
        }


        public int WaitForExit()
        {
            lock (this)
            {
                while (!_exited)
                {
                    Monitor.Wait(this);
                }
            }

            return ProcessExitCode;
        }

        private void OnExited(object sender, EventArgs e)
        {
            var process = sender as Process;
            if (process != null)
            {
                lock (this)
                {
                    ProcessExitCode = process.ExitCode;
                    _exited = true;

                    process.Exited -= OnExited;

                    Monitor.Pulse(this);
                }
            }
        }

    }

}