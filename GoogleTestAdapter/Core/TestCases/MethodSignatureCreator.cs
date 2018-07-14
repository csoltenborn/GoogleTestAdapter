using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

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


        public IEnumerable<MethodSignature> GetTestMethodSignatures(TestCaseDescriptor descriptor)
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

        private IEnumerable<MethodSignature> GetTypedTestMethodSignatures(TestCaseDescriptor descriptor)
        {
            var result = new List<MethodSignature>();

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
            result.Add(new MethodSignature(signature, true));

            return result;
        }

        private MethodSignature GetParameterizedTestMethodSignature(TestCaseDescriptor descriptor)
        {
            // remove instance number
            int index = descriptor.Suite.IndexOf('/');
            string suite = index < 0 ? descriptor.Suite : descriptor.Suite.Substring(index + 1);

            index = descriptor.Name.IndexOf('/');
            string testName = index < 0 ? descriptor.Name : descriptor.Name.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private MethodSignature GetTestMethodSignature(string suite, string testCase, string typeParam = "")
        {
            return new MethodSignature(suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature, !string.IsNullOrEmpty(typeParam));
        }

    }

}