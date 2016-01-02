using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public Uri ExecutorUri { get { return GoogleTestExecutor.ExecutorUri; } }
        public string Source { get; }

        public string FullyQualifiedName { get; }
        public string DisplayName { get; }

        public string CodeFilePath { get; private set; }
        public int LineNumber { get; private set; }

        public List<Trait> Traits { get; } = new List<Trait>();

        private string Suite { get; }

        private string NameAndParam { get; }

        private string TypeParam = null;

        private string Name { get; }

        private string Param { get; }

        public TestCase(string fullyQualifiedName, string source, string displayName, string codeFilePath, int lineNumber)
        {
            FullyQualifiedName = fullyQualifiedName;
            Source = source;
            DisplayName = displayName;
            CodeFilePath = codeFilePath;
            LineNumber = lineNumber;
        }

        internal TestCase(string executable, string suiteLine, string testCaseLine)
        {
            string[] split = suiteLine.Split(new[] { GoogleTestConstants.TypedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
            Suite = split.Length > 0 ? split[0] : suiteLine;
            if (split.Length > 1)
            {
                TypeParam = split[1];
                TypeParam = TypeParam.Replace("class ", "");
            }

            NameAndParam = testCaseLine;

            int startOfParamInfo = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
            Name = startOfParamInfo > 0 ? NameAndParam.Substring(0, startOfParamInfo).Trim() : NameAndParam;

            int indexOfMarker = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
            if (indexOfMarker < 0)
            {
                Param = "";
            }
            else
            {
                int startOfParam = indexOfMarker + GoogleTestConstants.ParameterizedTestMarker.Length;
                Param = NameAndParam.Substring(startOfParam, NameAndParam.Length - startOfParam).Trim();
            }

            string fullName = Suite + "." + Name;
            string displayName = Suite + "." + Name;
            if (!string.IsNullOrEmpty(Param))
            {
                displayName += $" [{Param}]";
            }
            if (!string.IsNullOrEmpty(TypeParam))
            {
                displayName += $" [{TypeParam}]";
            }

            FullyQualifiedName = fullName;
            DisplayName = displayName;
            Source = executable;
        }

        internal void AddLocationInfo(List<TestCaseLocation> testCaseLocations, TestEnvironment testEnvironment)
        {
            foreach (string symbolName in GetTestMethodSignatures())
            {
                foreach (TestCaseLocation location in testCaseLocations)
                {
                    if (location.Symbol.Contains(symbolName))
                    {
                        CodeFilePath = location.Sourcefile;
                        LineNumber = (int)location.Line;
                        Traits.AddRange(GetTraits(DisplayName, location.Traits, testEnvironment));
                        return;
                    }
                }
            }

            testEnvironment.LogWarning("Could not find source location for test " + FullyQualifiedName);
        }

        internal IEnumerable<string> GetTestMethodSignatures()
        {
            if (TypeParam != null)
            {
                return GetTypedTestMethodSignatures();
            }
            if (NameAndParam.Contains(GoogleTestConstants.ParameterizedTestMarker))
            {
                return GetParameterizedTestMethodSignature().Yield();
            }

            return GetTestMethodSignature(Suite, NameAndParam).Yield();
        }


        internal string GetTestsuiteName_CommandLineGenerator()
        {
            return FullyQualifiedName.Split('.')[0];
        }

        private IEnumerable<string> GetTypedTestMethodSignatures()
        {
            List<string> result = new List<string>();

            // remove instance number
            string suite = Suite.Substring(0, Suite.LastIndexOf("/"));

            if (suite.Contains("/"))
            {
                int index = suite.IndexOf("/");
                suite = suite.Substring(index + 1, suite.Length - index - 1);
            }
            // <testcase name>_<test name>_Test<type param value>::TestBody
            string signature = suite + "_" + Name + "_Test";
            signature += GetEnclosedTypeParam();
            signature += GoogleTestConstants.TestBodySignature;
            result.Add(signature);

            // gtest_case_<testcase name>_::<test name><type param value>::TestBody
            signature = "gtest_case_" + suite + "_::" + Name;
            signature += GetEnclosedTypeParam();
            signature += GoogleTestConstants.TestBodySignature;
            result.Add(signature);

            return result;
        }

        private string GetParameterizedTestMethodSignature()
        {
            // remove instance number
            int index = Suite.IndexOf('/');
            string suite = index < 0 ? Suite : Suite.Substring(index + 1);

            index = NameAndParam.IndexOf('/');
            string testName = index < 0 ? NameAndParam : NameAndParam.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private string GetTestMethodSignature(string suite, string testCase)
        {
            return suite + "_" + testCase + "_Test" + GoogleTestConstants.TestBodySignature;
        }

        private string GetEnclosedTypeParam()
        {
            string typeParam = TypeParam.EndsWith(">") ? TypeParam + " " : TypeParam;
            return $"<{typeParam}>";
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