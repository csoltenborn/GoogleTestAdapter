using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Scheduling
{
    public interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
