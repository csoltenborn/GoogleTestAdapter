using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class ListTestsParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ParseListTestsOutput_SimpleTest_CorrectSuiteAndName()
        {
            var consoleOutput = new List<string>
            {
                "MySuite.",
                "  MyTestCase"
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            Assert.AreEqual(1, descriptors.Count);
            Assert.AreEqual("MySuite", descriptors[0].Suite);
            Assert.AreEqual("MyTestCase", descriptors[0].Name);
            Assert.IsNull(descriptors[0].Param);
            Assert.IsNull(descriptors[0].TypeParam);
            Assert.AreEqual("MySuite.MyTestCase", descriptors[0].DisplayName);
            Assert.AreEqual("MySuite.MyTestCase", descriptors[0].FullyQualifiedName);
        }

        [TestMethod]
        public void ParseListTestsOutput_TestWithParam_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "InstantiationName/ParameterizedTests.",
                "  Simple/0  # GetParam() = (1,)",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            Assert.AreEqual(1, descriptors.Count);
            Assert.AreEqual("InstantiationName/ParameterizedTests", descriptors[0].Suite);
            Assert.AreEqual("Simple/0", descriptors[0].Name);
            Assert.AreEqual("(1,)", descriptors[0].Param);
            Assert.IsNull(descriptors[0].TypeParam);
            Assert.AreEqual("InstantiationName/ParameterizedTests.Simple/0", descriptors[0].FullyQualifiedName);
            Assert.AreEqual("InstantiationName/ParameterizedTests.Simple/0 [(1,)]", descriptors[0].DisplayName);
        }

        [TestMethod]
        public void ParseListTestsOutput_TestWithTypeParam_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "TypedTests/0.  # TypeParam = class std::vector<int,class std::allocator<int> >",
                "  CanIterate",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            Assert.AreEqual(1, descriptors.Count);
            Assert.AreEqual("TypedTests/0", descriptors[0].Suite);
            Assert.AreEqual("CanIterate", descriptors[0].Name);
            Assert.IsNull(descriptors[0].Param);
            Assert.AreEqual("std::vector<int,std::allocator<int> >", descriptors[0].TypeParam);
            Assert.AreEqual("TypedTests/0.CanIterate", descriptors[0].FullyQualifiedName);
            Assert.AreEqual("TypedTests/0.CanIterate<std::vector<int,std::allocator<int> > >", descriptors[0].DisplayName);
        }

        [TestMethod]
        public void ParseListTestsOutput_TestWithTypeParamAndPrefix_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "Arr/TypeParameterizedTests/1.  # TypeParam = struct MyStrangeArray",
                "  CanIterate",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            Assert.AreEqual(1, descriptors.Count);
            Assert.AreEqual("Arr/TypeParameterizedTests/1", descriptors[0].Suite);
            Assert.AreEqual("CanIterate", descriptors[0].Name);
            Assert.IsNull(descriptors[0].Param);
            Assert.AreEqual("MyStrangeArray", descriptors[0].TypeParam);
            Assert.AreEqual("Arr/TypeParameterizedTests/1.CanIterate", descriptors[0].FullyQualifiedName);
            Assert.AreEqual("Arr/TypeParameterizedTests/1.CanIterate<MyStrangeArray>", descriptors[0].DisplayName);
        }

        [TestMethod]
        public void ParseListTestsOutput_TestWithParamAndTestNameSeparator_CorrectParsing()
        {
            MockOptions.Setup(o => o.TestNameSeparator).Returns("::");
            var consoleOutput = new List<string>
            {
                "InstantiationName/ParameterizedTests.",
                "  Simple/0  # GetParam() = (1,)",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            Assert.AreEqual(1, descriptors.Count);
            Assert.AreEqual("InstantiationName/ParameterizedTests", descriptors[0].Suite);
            Assert.AreEqual("Simple/0", descriptors[0].Name);
            Assert.AreEqual("(1,)", descriptors[0].Param);
            Assert.IsNull(descriptors[0].TypeParam);
            Assert.AreEqual("InstantiationName/ParameterizedTests.Simple/0", descriptors[0].FullyQualifiedName);
            Assert.AreEqual("InstantiationName::ParameterizedTests.Simple::0 [(1,)]", descriptors[0].DisplayName);
        }

    }

}