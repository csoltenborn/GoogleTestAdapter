using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    internal class MethodSignatureCreator
    {

        internal IEnumerable<string> GetTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            switch (descriptor.TestType)
            {
                case TestCaseDescriptor.TestTypes.TypeParameterized:
                    return GetTypedTestMethodSignatures(descriptor);
                case TestCaseDescriptor.TestTypes.Parameterized:
                    return GetParameterizedTestMethodSignature(descriptor).Yield();
                case TestCaseDescriptor.TestTypes.Simple:
                    return GetTestMethodSignature(descriptor.Suite, descriptor.Name).Yield();
                default:
                    throw new InvalidOperationException($"Unknown literal {descriptor.TestType}");
            }
        }

        private IEnumerable<string> GetTypedTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            var result = new List<string>();

            // remove instance number
            string suite = descriptor.Suite.Substring(0, descriptor.Suite.LastIndexOf("/", StringComparison.Ordinal));

            // remove prefix
            if (suite.Contains("/"))
            {
                int index = suite.IndexOf("/", StringComparison.Ordinal);
                suite = suite.Substring(index + 1, suite.Length - index - 1);
            }

            string typeParam = "<.+>";

            // <testcase name>_<test name>_Test<type param value>::TestBody
            result.Add(GetTestMethodSignature(suite, descriptor.Name, typeParam));

            // gtest_case_<testcase name>_::<test name><type param value>::TestBody
            string signature =
                $"gtest_case_{suite}_::{descriptor.Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
            result.Add(signature);

            return result;
        }

        private string GetParameterizedTestMethodSignature(TestCaseDescriptor descriptor)
        {
            // remove instance number
            int index = descriptor.Suite.IndexOf('/');
            string suite = index < 0 ? descriptor.Suite : descriptor.Suite.Substring(index + 1);

            index = descriptor.Name.IndexOf('/');
            string testName = index < 0 ? descriptor.Name : descriptor.Name.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private string GetTestMethodSignature(string suite, string testCase, string typeParam = "")
        {
            return suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature;
        }

    }

}