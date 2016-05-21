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