using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleTestAdapter.Scheduling
{
    interface ITestsSplitter
    {
        List<List<TestCase>> SplitTestcases();
    }

}
