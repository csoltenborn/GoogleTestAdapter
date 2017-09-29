using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class ByteUtilsTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_FooEmptyPattern_ReturnsFound()
        {
            var bytes = Encoding.ASCII.GetBytes("foo");
            var pattern = Encoding.ASCII.GetBytes("");
            bytes.IndexOf(pattern).Should().Be(0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_EmptyBytesFoo_ReturnsNotFound()
        {
            var bytes = Encoding.ASCII.GetBytes("");
            var pattern = Encoding.ASCII.GetBytes("foo");
            bytes.IndexOf(pattern).Should().Be(-1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_EmptyBytesEmptyPattern_ReturnsFound()
        {
            var bytes = Encoding.ASCII.GetBytes("");
            var pattern = Encoding.ASCII.GetBytes("");
            bytes.IndexOf(pattern).Should().Be(0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_FooBar_ReturnsNotFound()
        {
            var bytes = Encoding.ASCII.GetBytes("foofoofoo");
            var pattern = Encoding.ASCII.GetBytes("bar");
            bytes.IndexOf(pattern).Should().Be(-1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_FooAtBeginning_ReturnsFound()
        {
            var bytes = Encoding.ASCII.GetBytes("fooxxx");
            var pattern = Encoding.ASCII.GetBytes("foo");
            bytes.IndexOf(pattern).Should().Be(0);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_FooAtEnd_ReturnsFound()
        {
            var bytes = Encoding.ASCII.GetBytes("xxxfoo");
            var pattern = Encoding.ASCII.GetBytes("foo");
            bytes.IndexOf(pattern).Should().Be(3);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void IndexOf_FooInMiddle_ReturnsFound()
        {
            var bytes = Encoding.ASCII.GetBytes("xxxfooxxx");
            var pattern = Encoding.ASCII.GetBytes("foo");
            bytes.IndexOf(pattern).Should().Be(3);
        }

    }
}