using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class ListTestsParserTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseListTestsOutput_SimpleTest_CorrectSuiteAndName()
        {
            var consoleOutput = new List<string>
            {
                "MySuite.",
                "  MyTestCase"
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            descriptors.Count.Should().Be(1);
            descriptors[0].Suite.Should().Be("MySuite");
            descriptors[0].Name.Should().Be("MyTestCase");

            descriptors[0].Param.Should().BeNull();
            descriptors[0].TypeParam.Should().BeNull();
            descriptors[0].DisplayName.Should().Be("MySuite.MyTestCase");
            descriptors[0].FullyQualifiedName.Should().Be("MySuite.MyTestCase");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseListTestsOutput_TestWithParam_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "InstantiationName/ParameterizedTests.",
                "  Simple/0  # GetParam() = (1,)",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            descriptors.Count.Should().Be(1);
            descriptors[0].Suite.Should().Be("InstantiationName/ParameterizedTests");
            descriptors[0].Name.Should().Be("Simple/0");
            descriptors[0].Param.Should().Be("(1,)");
            descriptors[0].TypeParam.Should().BeNull();
            descriptors[0].FullyQualifiedName.Should().Be("InstantiationName/ParameterizedTests.Simple/0");
            descriptors[0].DisplayName.Should().Be("InstantiationName/ParameterizedTests.Simple/0 [(1,)]");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseListTestsOutput_TestWithTypeParam_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "TypedTests/0.  # TypeParam = class std::vector<int,class std::allocator<int> >",
                "  CanIterate",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            descriptors.Count.Should().Be(1);
            descriptors[0].Suite.Should().Be("TypedTests/0");
            descriptors[0].Name.Should().Be("CanIterate");
            descriptors[0].Param.Should().BeNull();
            descriptors[0].TypeParam.Should().Be("std::vector<int,std::allocator<int> >");
            descriptors[0].FullyQualifiedName.Should().Be("TypedTests/0.CanIterate");
            descriptors[0].DisplayName.Should().Be("TypedTests/0.CanIterate<std::vector<int,std::allocator<int> > >");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseListTestsOutput_TestWithTypeParamAndPrefix_CorrectParsing()
        {
            var consoleOutput = new List<string>
            {
                "Arr/TypeParameterizedTests/1.  # TypeParam = struct MyStrangeArray",
                "  CanIterate",
            };

            IList<TestCaseDescriptor> descriptors = new ListTestsParser(TestEnvironment)
                .ParseListTestsOutput(consoleOutput);

            descriptors.Count.Should().Be(1);
            descriptors[0].Suite.Should().Be("Arr/TypeParameterizedTests/1");
            descriptors[0].Name.Should().Be("CanIterate");
            descriptors[0].Param.Should().BeNull();
            descriptors[0].TypeParam.Should().Be("MyStrangeArray");
            descriptors[0].FullyQualifiedName.Should().Be("Arr/TypeParameterizedTests/1.CanIterate");
            descriptors[0].DisplayName.Should().Be("Arr/TypeParameterizedTests/1.CanIterate<MyStrangeArray>");
        }

        [TestMethod]
        [TestCategory(Unit)]
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

            descriptors.Count.Should().Be(1);
            descriptors[0].Suite.Should().Be("InstantiationName/ParameterizedTests");
            descriptors[0].Name.Should().Be("Simple/0");
            descriptors[0].Param.Should().Be("(1,)");
            descriptors[0].TypeParam.Should().BeNull();
            descriptors[0].FullyQualifiedName.Should().Be("InstantiationName/ParameterizedTests.Simple/0");
            descriptors[0].DisplayName.Should().Be("InstantiationName::ParameterizedTests.Simple::0 [(1,)]");
        }

    }

}