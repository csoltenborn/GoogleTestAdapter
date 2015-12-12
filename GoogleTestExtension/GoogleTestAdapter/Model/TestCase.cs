using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Model
{

    public class TestCase
    {
        public Uri ExecutorUri { get; set; }
        public string Source { get; set; }

        public string FullyQualifiedName { get; set; }
        public string DisplayName { get; set; }

        public string CodeFilePath { get; set; }
        public int LineNumber { get; set; }

        public List<Trait> Traits { get; } = new List<Trait>();

        public TestCase(string fullyQualifiedName, Uri executorUri, string source)
        {
            FullyQualifiedName = fullyQualifiedName;
            ExecutorUri = executorUri;
            Source = source;
        }

    }

}