using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Runners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;
// ReSharper disable RedundantArgumentDefaultValue

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class ExitCodeTestsAggregatorTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_EmptyInput_EmptyResult()
        {
            GetAggregatedResults(new List<ExecutableResult>()).Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_SingleInput_InputIsReturned()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo")
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(allResults.Single());
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_TwoInputsWithSameExecutable_ExitCodeIsAggregatedCorrectly()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", 0),
                new ExecutableResult("Foo", 1)
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(new ExecutableResult("Foo", 1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_TwoInputsWithSameExecutableAllSkip_SkipIsAggregatedCorrectly()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", exitCodeSkip: true),
                new ExecutableResult("Foo", exitCodeSkip: true)
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(new ExecutableResult("Foo", exitCodeSkip: true));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_TwoInputsWithSameExecutableOneSkip_SkipIsAggregatedCorrectly()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", exitCodeSkip: true),
                new ExecutableResult("Foo")
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(new ExecutableResult("Foo"));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_TwoInputsOneWithOutput_OutputAggregatedCorrectly()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", exitCodeOutput: new List<string> {"Some output"}),
                new ExecutableResult("Foo")
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(new ExecutableResult("Foo", exitCodeOutput: new List<string> {"Some output"}));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_TwoInputsWithSameExecutable_CompleteInputIsAggregated()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", 0, new List<string> {"Output 1"}, true),
                new ExecutableResult("Foo", 1, new List<string> {"Output 2"})
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Single().Should().BeEquivalentTo(new ExecutableResult("Foo", 1, new List<string> {"Output 1", Environment.NewLine, "Output 2"}));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ComputeAggregatedResults_ThreeInputsWithTwoExecutables_CompleteInputIsAggregated()
        {
            var allResults = new List<ExecutableResult>
            {
                new ExecutableResult("Foo", 0, new List<string> {"Output 1"}, true),
                new ExecutableResult("Bar", 0, new List<string> {"Output 1"}, true),
                new ExecutableResult("Foo", 1, new List<string> {"Output 2"})
            };
            var results = GetAggregatedResults(allResults).ToList();

            results.Should().HaveCount(2);
            results[0].Should().BeEquivalentTo(new ExecutableResult("Foo", 1, new List<string> {"Output 1", Environment.NewLine, "Output 2"}));
            results[1].Should().BeEquivalentTo(new ExecutableResult("Bar", 0, new List<string> {"Output 1"}, true));
        }

        private IEnumerable<ExecutableResult> GetAggregatedResults(IEnumerable<ExecutableResult> allResults)
        {
            var aggregator = new ExitCodeTestsAggregator();
            return aggregator.ComputeAggregatedResults(allResults);
        }
    }
}
