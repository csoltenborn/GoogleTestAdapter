using System;
using System.Diagnostics;
using System.Threading;

namespace GoogleTestAdapter.Common
{

    public class ProcessWaiter
    {
        private readonly object _lock = new object();

        public int ProcessExitCode { get; private set; } = -1;
        private bool _exited;


        public ProcessWaiter(Process process)
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnExited;
        }


        public int WaitForExit()
        {
            lock (_lock)
            {
                while (!_exited)
                {
                    Monitor.Wait(_lock);
                }
            }

            return ProcessExitCode;
        }

        private void OnExited(object sender, EventArgs e)
        {
            if (sender is Process process)
            {
                lock (_lock)
                {
                    ProcessExitCode = process.ExitCode;
                    _exited = true;

                    process.Exited -= OnExited;

                    Monitor.Pulse(_lock);
                }
            }
        }

    }

}