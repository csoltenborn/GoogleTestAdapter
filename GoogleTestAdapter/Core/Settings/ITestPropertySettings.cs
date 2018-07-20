using System.Collections.Generic;

namespace GoogleTestAdapter.Settings
{
    public interface ITestPropertySettings
    {
        IDictionary<string, string> Environment { get; }
        string WorkingDirectory { get; }
    }
}
