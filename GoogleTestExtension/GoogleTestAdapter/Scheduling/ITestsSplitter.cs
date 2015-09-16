using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;

namespace GoogleTestAdapter.Scheduling
{
    interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
