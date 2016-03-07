using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    internal class MethodSignatureCreator
    {

        internal IEnumerable<string> GetTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            if (descriptor.TypeParam != null)
            {
                return GetTypedTestMethodSignatures(descriptor);
            }
            if (descriptor.Param != null)
            {
                return GetParameterizedTestMethodSignature(descriptor).Yield();
            }

            return GetTestMethodSignature(descriptor.Suite, descriptor.Name).Yield();
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

            string typeParam = ListTestsParser.GetEnclosedTypeParam(descriptor.TypeParam);

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