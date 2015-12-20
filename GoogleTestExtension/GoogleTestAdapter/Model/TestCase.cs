using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiaAdapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Model
{
    /*
    Simple tests: 
        Suite=<test_case_name>
        NameAndParam=<test_name>
    Tests with fixture
        Suite=<test_fixture>
        NameAndParam=<test_name>
    Parameterized case: 
        Suite=[<prefix>/]<test_case_name>, 
        NameAndParam=<test_name>/<parameter instantiation nr>  # GetParam() = <parameter instantiation>
    */
    public class TestCase
    {
        public Uri ExecutorUri { get; set; }
        public string Source { get; set; }

        public string FullyQualifiedName { get; set; }
        public string DisplayName { get; set; }

        public string CodeFilePath { get; set; }
        public int LineNumber { get; set; }

        public List<Trait> Traits { get; } = new List<Trait>();

        internal SourceFileLocation Location { get; set; }

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

        public TestCase(string fullyQualifiedName, Uri executorUri, string source)
        {
            FullyQualifiedName = fullyQualifiedName;
            ExecutorUri = executorUri;
            Source = source;
        }

        internal TestCase(string suite, string nameAndParam)
        {
            string[] split = suite.Split(new[] { GoogleTestConstants.ParameterValueMarker }, StringSplitOptions.RemoveEmptyEntries);
            Suite = split.Length > 0 ? split[0] : suite;

            NameAndParam = nameAndParam;
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

        public string GetTestsuiteName_CommandLineGenerator()
        {
            return FullyQualifiedName.Split('.')[0];
        }

        public string GetTestcaseNameForFiltering_CommandLineGenerator()
        {
            int index = FullyQualifiedName.IndexOf(GoogleTestConstants.ParameterizedTestMarker,
                StringComparison.Ordinal);
            if (index < 0)
            {
                return FullyQualifiedName;
            }
            return FullyQualifiedName.Substring(0, index);
        }

        internal void ConfigureTestCase(string executable, List<TestCaseLocation> testCaseLocations, TestEnvironment testEnvironment)
        {
            string fullName = Suite + "." + NameAndParam;
            string displayName = Suite + "." + Name;
            if (!string.IsNullOrEmpty(Param))
            {
                displayName += $" [{Param}]";
            }
            string symbolName = GetTestMethodSignature();

            foreach (TestCaseLocation location in testCaseLocations)
            {
                if (location.Symbol.Contains(symbolName))
                {
                    FullyQualifiedName = fullName;
                    ExecutorUri = new Uri(GoogleTestExecutor.ExecutorUriString);
                    Source = executable;
                    DisplayName = displayName;
                    CodeFilePath = location.Sourcefile;
                    LineNumber = (int)location.Line;
                    Traits.AddRange(GetTraits(DisplayName, location.Traits, testEnvironment));
                    return;
                }
            }

            testEnvironment.LogWarning("Could not find source location for test " + fullName);
            FullyQualifiedName = fullName;
            ExecutorUri = new Uri(GoogleTestExecutor.ExecutorUriString);
            Source = executable;
            DisplayName = displayName;
        }

        private IEnumerable<Trait> GetTraits(string displayName, List<Trait> traits, TestEnvironment testEnvironment)
        {
            foreach (RegexTraitPair pair in testEnvironment.Options.TraitsRegexesBefore.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                if (!traits.Exists(T => T.Name == pair.Trait.Name))
                {
                    traits.Add(pair.Trait);
                }
            }

            foreach (RegexTraitPair pair in testEnvironment.Options.TraitsRegexesAfter.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                bool replacedTrait = false;
                foreach (Trait traitToModify in traits.ToArray().Where(T => T.Name == pair.Trait.Name))
                {
                    replacedTrait = true;
                    traits.Remove(traitToModify);
                    if (!traits.Contains(pair.Trait))
                    {
                        traits.Add(pair.Trait);
                    }
                }
                if (!replacedTrait)
                {
                    traits.Add(pair.Trait);
                }
            }
            return traits;
        }

    }

}