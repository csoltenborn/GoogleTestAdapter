using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class ExtensionsTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void MatchesCompletely_RegexNull_Throws()
        {
            Regex regex = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            Action action = () => regex.MatchesCompletely("");
            action.ShouldThrow<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void MatchesCompletely_InputNull_Throws()
        {
            Regex regex = new Regex(".*");
            Action action = () => regex.MatchesCompletely(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void MatchesCompletely_RegexMatchesCompleteString_ResultIsTrue()
        {
            new Regex("[a-c]*").MatchesCompletely("abc").Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void MatchesCompletely_RegexDoesNotMatchCompleteString_ResultIsFalse()
        {
            Regex regex = new Regex("[a-c]*");
            regex.IsMatch("abcd").Should().BeTrue();
            regex.MatchesCompletely("abcd").Should().BeFalse();
        }

    }

}