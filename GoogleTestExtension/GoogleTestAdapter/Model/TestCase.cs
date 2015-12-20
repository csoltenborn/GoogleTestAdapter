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

        internal string Suite { get; }

        internal string NameAndParam { get; }

        internal string Name
        {
            get
            {
                int startOfParamInfo = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
                return startOfParamInfo > 0 ? NameAndParam.Substring(0, startOfParamInfo).Trim() : NameAndParam;
            }
        }

        internal string Param
        {
            get
            {
                int indexOfMarker = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
                if (indexOfMarker < 0)
                {
                    return "";
                }
                int startOfParam = indexOfMarker + GoogleTestConstants.ParameterizedTestMarker.Length;
                return NameAndParam.Substring(startOfParam, NameAndParam.Length - startOfParam).Trim();
            }
        }

        internal TestCase(string suite, string nameAndParam)
        {
            this.Suite = suite;
            this.NameAndParam = nameAndParam;
        }

        public string GetTestMethodSignature()
        {
            if (!NameAndParam.Contains(GoogleTestConstants.ParameterizedTestMarker))
            {
                return GoogleTestConstants.GetTestMethodSignature(Suite, NameAndParam);
            }

            int index = Suite.IndexOf('/');
            string suite = index < 0 ? Suite : Suite.Substring(index + 1);

            index = NameAndParam.IndexOf('/');
            string testName = index < 0 ? NameAndParam : NameAndParam.Substring(0, index);

            return GoogleTestConstants.GetTestMethodSignature(suite, testName);
        }

        public TestCase(string fullyQualifiedName, Uri executorUri, string source)
        {
            FullyQualifiedName = fullyQualifiedName;
            ExecutorUri = executorUri;
            Source = source;
        }

    }

}