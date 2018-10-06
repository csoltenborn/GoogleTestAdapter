using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    [TestClass]
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class TestCaseFilterTests : TestAdapterTestsBase
    {
        private readonly Mock<ITestCaseFilterExpression> _mockFilterExpression = new Mock<ITestCaseFilterExpression>();
        private readonly ISet<string> _traitNames = new HashSet<string>();

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockRunContext.Setup(rc => rc.GetTestCaseFilter(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<Func<string, TestProperty>>()))
                .Returns(_mockFilterExpression.Object);
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            _mockFilterExpression.Reset();
            _traitNames.Clear();
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void Filter_NoFilterExpressionProvided_NoFiltering()
        {
            MockRunContext.Setup(rc => rc.GetTestCaseFilter(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<Func<string, TestProperty>>()))
                .Returns((ITestCaseFilterExpression)null);
            IEnumerable<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(tc => tc.ToVsTestCase());

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Filter_ExpressionAcceptsAnything_NoFiltering()
        {
            _mockFilterExpression.Setup(e => e.MatchTestCase(It.IsAny<TestCase>(), It.IsAny<Func<string, object>>())).Returns(true);
            IEnumerable<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(tc => tc.ToVsTestCase());

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Filter_ExpressionMatchesNothing_EmptyResult()
        {
            _mockFilterExpression.Setup(e => e.MatchTestCase(It.IsAny<TestCase>(), It.IsAny<Func<string, object>>())).Returns(false);
            IEnumerable<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(tc => tc.ToVsTestCase());

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(new List<TestCase>(), filteredTestCases);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Filter_ExpressionMatchesDisplayName_CorrectFiltering()
        {
            List<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(tc => tc.ToVsTestCase()).ToList();
            _mockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCases[0]), It.Is<Func<string, object>>(f => f("DisplayName").ToString() == "Foo.Bar"))).Returns(true);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases[0].Yield(), filteredTestCases);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Matches_ExpressionMatchesDisplayName_CorrectFiltering()
        {
            List<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar").Select(tc => tc.ToVsTestCase()).ToList();
            _mockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCases[0]), It.Is<Func<string, object>>(f => f("DisplayName").ToString() == "Foo.Bar"))).Returns(true);

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            bool matches = filter.Matches(testCases[0]);

            matches.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Filter_Trait_CorrectFiltering()
        {
            List<TestCase> testCases = TestDataCreator.CreateDummyTestCases("Foo.Bar", "Foo.Baz").Select(tc => tc.ToVsTestCase()).ToList();
            testCases[0].Traits.Add(new Trait("MyTrait", "value1"));
            SetupFilterToAcceptTraitForTestCase(testCases[0], "MyTrait", "value1");
            testCases[1].Traits.Add(new Trait("MyTrait", "value2"));
            SetupFilterToAcceptTraitForTestCase(testCases[1], "MyTrait", "value2");
            _traitNames.Add("MyTrait");

            TestCaseFilter filter = new TestCaseFilter(MockRunContext.Object, _traitNames, TestEnvironment.Logger);
            IEnumerable<TestCase> filteredTestCases = filter.Filter(testCases).ToList();

            AssertAreEqual(testCases, filteredTestCases);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SetPropertyValue_Trait_CorrectValidation()
        {
            var testCase = TestDataCreator.CreateDummyTestCases("Foo.Bar").Single().ToVsTestCase();
            testCase.Traits.Add(new Trait("MyTrait", "value1"));

            //registers TestProperty objects for trait names
            // ReSharper disable once ObjectCreationAsStatement
            new TestCaseFilter(MockRunContext.Object, new HashSet<string>{"MyTrait"}, TestEnvironment.Logger);
            TestProperty property = TestProperty.Find("MyTrait");

            Action action = () => testCase.SetPropertyValue(property, "i");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "_i");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "äöüÄÖÜß$");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "_äöüÄÖÜß$");
            action.Should().NotThrow();

            // since we are not at the beginning of the method name
            action = () => testCase.SetPropertyValue(property, "1");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "_1");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "_");
            action.Should().NotThrow();

            action = () => testCase.SetPropertyValue(property, "");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");

            action = () => testCase.SetPropertyValue(property, "_(");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");

            action = () => testCase.SetPropertyValue(property, "a(");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");

            action = () => testCase.SetPropertyValue(property, "1(");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");

            action = () => testCase.SetPropertyValue(property, "%");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");

            action = () => testCase.SetPropertyValue(property, "+");
            action.Should().Throw<ArgumentException>().WithMessage("MyTrait");
        }

        private void SetupFilterToAcceptTraitForTestCase(TestCase testCase, string traitName, string traitValue)
        {
            _mockFilterExpression.Setup(e => e.MatchTestCase(It.Is<TestCase>(tc => tc == testCase), It.Is<Func<string, object>>(f => f(traitName).ToString() == traitValue))).Returns(true);
        }


        private static void AssertAreEqual(IEnumerable<TestCase> testCases1, IEnumerable<TestCase> testCases2)
        {
            testCases1.Count().Should().Be(testCases2.Count());

            using (IEnumerator<TestCase> enumerator1 = testCases1.GetEnumerator())
            using (IEnumerator<TestCase> enumerator2 = testCases2.GetEnumerator())
            {
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    AssertAreEqual(enumerator1.Current, enumerator2.Current);
                }
            }
        }

        private static void AssertAreEqual(TestCase testCase1, TestCase testCase2)
        {
            testCase1.FullyQualifiedName.Should().BeEquivalentTo(testCase2.FullyQualifiedName);
            testCase1.DisplayName.Should().BeEquivalentTo(testCase2.DisplayName);
            testCase1.CodeFilePath.Should().BeEquivalentTo(testCase2.CodeFilePath);
            testCase1.Source.Should().BeEquivalentTo(testCase2.Source);
            testCase1.LineNumber.Should().Be(testCase2.LineNumber);
            testCase1.Id.Should().Be(testCase2.Id);
            testCase1.ExecutorUri.Should().BeEquivalentTo(testCase2.ExecutorUri);
            testCase1.Traits.Count().Should().Be(testCase2.Traits.Count());
        }

    }

}