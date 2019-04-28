using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Runners
{
    public class ExecutableResult
    {
        public string Executable { get; }
        public int ExitCode { get; }
        public IReadOnlyList<string> ExitCodeOutput { get; }
        public bool ExitCodeSkip { get; }

        public ExecutableResult(string executable, int exitCode = 0, IList<string> exitCodeOutput = null, bool exitCodeSkip = false)
        {
            if (string.IsNullOrWhiteSpace(executable))
            {
                throw new ArgumentException(nameof(executable));
            }

            Executable = executable;
            ExitCode = exitCode;
            ExitCodeOutput = (IReadOnlyList<string>) (exitCodeOutput ?? new List<string>());
            ExitCodeSkip = exitCodeSkip;
        }
    }
}