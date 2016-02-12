using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    [TestClass]
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class TestCaseFilterTests : AbstractVSTests
    {
        readonly Mock<ITestCaseFilterExpression> MockFilterExpression = new Mock<ITestCaseFilterExpression>();
        readonly ISet<string> traitNames = new HashSet<string>();

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockRunContext.Setup(rc => rc.GetTestCaseFilter(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<Func<string, TestProperty>>()))
                .Returns(MockFilterExpression.Object);
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockFilterExpression.Reset();
            traitNames.Clear();
        }


        [TestMethod]
        public void Filter_NoFilterExpressionProvided_NoFiltering()
        {
            MockRunContext.Setup(rc => rc.GetTestCaseFilter(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<Func<string, TestProperty>>()))
                .Returns((ITestCaseFilterExpression)null);
            IEnumerable<TestCase> testCases = CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(DataConversionExtensions.ToVsTestCase);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        [TestMethod]
        public void Filter_ExpressionAcceptsAnything_NoFiltering()
        {
            MockFilterExpression.Setup(e => e.MatchTestCase(It.IsAny<TestCase>(), It.IsAny<Func<string, object>>())).Returns(true);
            IEnumerable<TestCase> testCases = CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(DataConversionExtensions.ToVsTestCase);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        [TestMethod]
        public void Filter_ExpressionMatchesNothing_EmptyResult()
        {
            MockFilterExpression.Setup(e => e.MatchTestCase(It.IsAny<TestCase>(), It.IsAny<Func<string, object>>())).Returns(false);
            IEnumerable<TestCase> testCases = CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(DataConversionExtensions.ToVsTestCase);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(new List<TestCase>(), filteredTestCases);
        }

        [TestMethod]
        public void Filter_ExpressionMatchesDisplayName_CorrectFiltering()
        {
            List<TestCase> testCases = CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(DataConversionExtensions.ToVsTestCase).ToList();
            MockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCases[0]), It.Is<Func<string, object>>(f => f("DisplayName").ToString() == "Foo.Bar"))).Returns(true);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases[0].Yield(), filteredTestCases);
        }

        [TestMethod]
        public void Matches_ExpressionMatchesDisplayName_CorrectFiltering()
        {
            List<TestCase> testCases = CreateDummyTestCases("Foo.Bar").Select(DataConversionExtensions.ToVsTestCase).ToList();
            MockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCases[0]), It.Is<Func<string, object>>(f => f("DisplayName").ToString() == "Foo.Bar"))).Returns(true);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            bool matches = filter.Matches(testCases[0]);

            Assert.IsTrue(matches);
        }

        [TestMethod]
        public void Filter_Trait_CorrectFiltering()
        {
            List<TestCase> testCases = CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(DataConversionExtensions.ToVsTestCase).ToList();
            testCases[0].Traits.Add(new Trait("MyTrait", "value1"));
            SetupFilterToAcceptTraitForTestCase(testCases[0], "MyTrait", "value1");
            testCases[1].Traits.Add(new Trait("MyTrait", "value2"));
            SetupFilterToAcceptTraitForTestCase(testCases[1], "MyTrait", "value2");
            traitNames.Add("MyTrait");

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, traitNames, TestEnvironment);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        private void SetupFilterToAcceptTraitForTestCase(TestCase testCase, string traitName, string traitValue)
        {
            MockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCase), It.Is<Func<string, object>>(f => f(traitName).ToString() == traitValue))).Returns(true);
        }


        private static void AssertAreEqual(IEnumerable<TestCase> testCases1, IEnumerable<TestCase> testCases2)
        {
            Assert.AreEqual(testCases1.Count(), testCases2.Count());

            IEnumerator<TestCase> enumerator1 = testCases1.GetEnumerator();
            IEnumerator<TestCase> enumerator2 = testCases2.GetEnumerator();
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                AssertAreEqual(enumerator1.Current, enumerator2.Current);
            }
        }

        private static void AssertAreEqual(TestCase testCase1, TestCase testCase2)
        {
            Assert.AreEqual(testCase1.FullyQualifiedName, testCase2.FullyQualifiedName);
            Assert.AreEqual(testCase1.DisplayName, testCase2.DisplayName);
            Assert.AreEqual(testCase1.CodeFilePath, testCase2.CodeFilePath);
            Assert.AreEqual(testCase1.Source, testCase2.Source);
            Assert.AreEqual(testCase1.LineNumber, testCase2.LineNumber);
            Assert.AreEqual(testCase1.Id, testCase2.Id);
            Assert.AreEqual(testCase1.ExecutorUri, testCase2.ExecutorUri);
            Assert.AreEqual(testCase1.Traits.Count(), testCase2.Traits.Count());
        }

    }

}