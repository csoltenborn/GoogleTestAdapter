using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Scheduling
{
    internal interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
