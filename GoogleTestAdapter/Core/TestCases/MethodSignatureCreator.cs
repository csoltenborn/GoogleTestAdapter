using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    public class MethodSignatureCreator
    {
        public class MethodSignature
        {
            public string Signature { get; }
            public bool IsRegex { get; }

            public MethodSignature(string signature, bool isRegex)
            {
                Signature = signature;
                IsRegex = isRegex;
            }
        }


        public IEnumerable<MethodSignature> GetTestMethodSignatures(TestCase testcase)
        {
            switch (testcase.TestType)
            {
                case TestCase.TestTypes.TypeParameterized:
                    return GetTypedTestMethodSignatures(testcase);
                case TestCase.TestTypes.Parameterized:
                    return GetParameterizedTestMethodSignature(testcase).Yield();
                case TestCase.TestTypes.Simple:
                    return GetTestMethodSignature(testcase.Suite, testcase.Name).Yield();
                default:
                    throw new InvalidOperationException($"Unknown literal {testcase.TestType}");
            }
        }

        private IEnumerable<MethodSignature> GetTypedTestMethodSignatures(TestCase testcase)
        {
            var result = new List<MethodSignature>();

            // remove instance number
            string suite = testcase.Suite.Substring(0, testcase.Suite.LastIndexOf("/", StringComparison.Ordinal));

            // remove prefix
            if (suite.Contains("/"))
            {
                int index = suite.IndexOf("/", StringComparison.Ordinal);
                suite = suite.Substring(index + 1, suite.Length - index - 1);
            }

            string typeParam = "<.+>";

            // <testcase name>_<test name>_Test<type param value>::TestBody
            result.Add(GetTestMethodSignature(suite, testcase.Name, typeParam));

            // gtest_case_<testcase name>_::<test name><type param value>::TestBody
            string signature =
                $"gtest_case_{suite}_::{testcase.Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
            result.Add(new MethodSignature(signature, true));

            return result;
        }

        private MethodSignature GetParameterizedTestMethodSignature(TestCase testcase)
        {
            // remove instance number
            int index = testcase.Suite.IndexOf('/');
            string suite = index < 0 ? testcase.Suite : testcase.Suite.Substring(index + 1);

            index = testcase.Name.IndexOf('/');
            string testName = index < 0 ? testcase.Name : testcase.Name.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private MethodSignature GetTestMethodSignature(string suite, string testCase, string typeParam = "")
        {
            return new MethodSignature(suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature, !string.IsNullOrEmpty(typeParam));
        }

    }

}