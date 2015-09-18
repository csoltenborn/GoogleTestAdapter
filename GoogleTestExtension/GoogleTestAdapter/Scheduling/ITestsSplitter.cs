using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;

namespace GoogleTestAdapter.Scheduling
{
    internal interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
