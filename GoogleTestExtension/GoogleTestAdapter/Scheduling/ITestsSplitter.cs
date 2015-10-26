using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Scheduling
{
    public interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
