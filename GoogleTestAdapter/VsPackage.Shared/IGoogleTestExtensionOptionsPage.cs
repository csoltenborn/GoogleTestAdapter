using System;

namespace GoogleTestAdapter.VsPackage
{
    public interface IGoogleTestExtensionOptionsPage : IServiceProvider
    {
        bool CatchExtensions { get; set; }
        bool BreakOnFailure { get; set; }
        bool ParallelTestExecution { get; set; }
        bool PrintTestOutput { get; set; }
    }
}