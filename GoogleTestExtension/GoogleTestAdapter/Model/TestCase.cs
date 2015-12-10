using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Model
{

    public class TestCase2
    {
        public string FullyQualifiedName { get; set; }
        public string Source { get; set; }
        public string DisplayName { get; set; }
        public string CodeFilePath { get; set; }
        public int LineNumber { get; set; }
        public Uri ExecutorUri { get; set; }
        public List<Trait2> Traits { get; } = new List<Trait2>();

        public TestCase2(string fullyQualifiedName, Uri executorUri, string source)
        {
            FullyQualifiedName = fullyQualifiedName;
            ExecutorUri = executorUri;
            Source = source;
        }

    }

}