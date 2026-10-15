using System;
using FluentAssertions;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VsTestProperty = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestProperty;
using VsTestResultMessage = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResultMessage;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class DataConversionExtensionsTests : TestAdapterTestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestResult_ResultWithStandardOutput_OutputIsAttachedAsStandardOutMessage()
        {
            var testResult = new TestResult(TestDataCreator.ToTestCase("Suite.Test"))
            {
                ComputerName = Environment.MachineName,
                Outcome = TestOutcome.Passed,
                Duration = TimeSpan.FromMilliseconds(1),
                StandardOutput = "Some output\n"
            };

            var vsTestResult = testResult.ToVsTestResult();

            vsTestResult.Messages.Should().ContainSingle();
            vsTestResult.Messages[0].Category.Should().Be(VsTestResultMessage.StandardOutCategory);
            vsTestResult.Messages[0].Text.Should().Be("Some output\n");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestResult_ResultWithoutStandardOutput_NoMessageIsAttached()
        {
            var testResult = new TestResult(TestDataCreator.ToTestCase("Suite.Test"))
            {
                ComputerName = Environment.MachineName,
                Outcome = TestOutcome.Passed
            };

            testResult.ToVsTestResult().Messages.Should().BeEmpty();
        }
        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestCase_TestCaseWithNamespace_HierarchyIsSet()
        {
            var testCase = new TestCase("Suite/0.Test", "foo.exe", "Suite/0.Test", "foo.cpp", 1)
            {
                Namespace = "outer::(anonymous namespace)"
            };

            var hierarchy = (string[])testCase.ToVsTestCase().GetPropertyValue(HierarchyProperty);

            hierarchy.Should().Equal(null, "outer::(anonymous namespace)", "Suite/0", "Test");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestCase_TestCaseInGlobalNamespace_HierarchyHasEmptyNamespace()
        {
            var testCase = new TestCase("Suite.Test", "foo.exe", "Suite.Test", "foo.cpp", 1) { Namespace = "" };

            var hierarchy = (string[])testCase.ToVsTestCase().GetPropertyValue(HierarchyProperty);

            hierarchy.Should().Equal(null, "", "Suite", "Test");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestCase_TestCaseWithUnknownNamespace_HierarchyHasEmptyNamespace()
        {
            var testCase = new TestCase("Suite.Test", "foo.exe", "Suite.Test", "", 0);

            var hierarchy = (string[])testCase.ToVsTestCase().GetPropertyValue(HierarchyProperty);

            hierarchy.Should().Equal(null, "", "Suite", "Test");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToVsTestCase_ParameterizedTestCase_HierarchyContainsTestClassAndGroup()
        {
            var testCase = new TestCase("Prefix/Suite.Test/0", "foo.exe", "Prefix/Suite.Test/0 [42]", "foo.cpp", 1)
            {
                Namespace = "outer",
                TestClass = "Suite",
                TestGroup = "Test"
            };

            var vsTestCase = testCase.ToVsTestCase();

            ((string[])vsTestCase.GetPropertyValue(HierarchyProperty)).Should().Equal(null, "outer", "Suite", "Test");
            vsTestCase.FullyQualifiedName.Should().Be("Prefix/Suite.Test/0");
            vsTestCase.DisplayName.Should().Be("Prefix/Suite.Test/0 [42]");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToTestCase_VsTestCaseWithHierarchy_TestClassAndGroupAreRestored()
        {
            var testCase = new TestCase("Prefix/Suite.Test/0", "foo.exe", "Prefix/Suite.Test/0", "foo.cpp", 1)
            {
                TestClass = "Suite",
                TestGroup = "Test"
            };

            TestCase restored = testCase.ToVsTestCase().ToTestCase();

            restored.TestClass.Should().Be("Suite");
            restored.TestGroup.Should().Be("Test");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToTestCase_VsTestCaseWithHierarchy_NamespaceIsRestored()
        {
            var testCase = new TestCase("Suite.Test", "foo.exe", "Suite.Test", "foo.cpp", 1) { Namespace = "outer::inner" };

            testCase.ToVsTestCase().ToTestCase().Namespace.Should().Be("outer::inner");
        }

        private static VsTestProperty HierarchyProperty => VsTestProperty.Find("TestCase.Hierarchy");
    }
}
