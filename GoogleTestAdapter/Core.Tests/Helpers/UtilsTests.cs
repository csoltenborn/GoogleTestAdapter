using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class UtilsTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void DeleteDirectory_CanNotBeDeleted_ReturnsFalseAndMessage()
        {
            string dir = Utils.GetTempDirectory();
            SetReadonlyFlag(dir);

            string errorMessage;
            bool result = Utils.DeleteDirectory(dir, out errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain(dir);

            RemoveReadonlyFlag(dir);

            result = Utils.DeleteDirectory(dir, out errorMessage);

            result.Should().BeTrue();
            errorMessage.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTempDirectory__DirectoryDoesExistAndCanBeDeleted()
        {
            string dir = Utils.GetTempDirectory();
            Directory.Exists(dir).Should().BeTrue();

            string errorMessage;
            Utils.DeleteDirectory(dir, out errorMessage).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TimestampMessage_MessageIsNullOrEmpty_ResultIsTheSame()
        {
            string timestampSeparator = " - ";
            string resultRegex = @"[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}" + timestampSeparator;

            string nullMessage = null;
            Utils.TimestampMessage(ref nullMessage);
            nullMessage.Should().MatchRegex(resultRegex);
            nullMessage.Should().EndWith(timestampSeparator);

            string emptyMessage = "";
            Utils.TimestampMessage(ref emptyMessage);
            emptyMessage.Should().MatchRegex(resultRegex);
            emptyMessage.Should().EndWith(timestampSeparator);

            string fooMessage = "foo";
            Utils.TimestampMessage(ref fooMessage);
            fooMessage.Should().MatchRegex(resultRegex);
            fooMessage.Should().EndWith(timestampSeparator + "foo");
        }

        private void SetReadonlyFlag(string dir)
        {
            FileAttributes fileAttributes = File.GetAttributes(dir);
            fileAttributes |= FileAttributes.ReadOnly;
            File.SetAttributes(dir, fileAttributes);
        }

        private void RemoveReadonlyFlag(string dir)
        {
            FileAttributes fileAttributes = File.GetAttributes(dir);
            fileAttributes = fileAttributes & ~FileAttributes.ReadOnly;
            File.SetAttributes(dir, fileAttributes);
        }

    }

}