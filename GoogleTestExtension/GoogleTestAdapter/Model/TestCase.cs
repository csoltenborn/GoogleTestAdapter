using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Model
{
    public class TestCase
    {
        public string Source { get; }

        public string FullyQualifiedName { get; }
        public string DisplayName { get; }

        public string CodeFilePath { get; }
        public int LineNumber { get; }

        public List<Trait> Traits { get; } = new List<Trait>();

        public TestCase(string fullyQualifiedName, string source, string displayName, string codeFilePath, int lineNumber)
        {
            FullyQualifiedName = fullyQualifiedName;
            Source = source;
            DisplayName = displayName;
            CodeFilePath = codeFilePath;
            LineNumber = lineNumber;
        }

        internal string GetTestsuiteName_CommandLineGenerator()
        {
            return FullyQualifiedName.Split('.')[0];
        }

        public override string ToString()
        {
            return DisplayName;
        }

    }

}