using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class UtilsTests
    {

        [TestMethod]
        public void DeleteDirectory_CanNotBeDeleted_ReturnsFalseAndMessage()
        {
            string dir = Utils.GetTempDirectory();
            SetReadonlyFlag(dir);

            string errorMessage;
            bool result = Utils.DeleteDirectory(dir, out errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains(dir));

            RemoveReadonlyFlag(dir);

            result = Utils.DeleteDirectory(dir, out errorMessage);

            Assert.IsTrue(result);
            Assert.IsNull(errorMessage);
        }

        [TestMethod]
        public void GetTempDirectory__DirectoryDoesExistAndCanBeDeleted()
        {
            string dir = Utils.GetTempDirectory();

            Assert.IsTrue(Directory.Exists(dir));
            string errorMessage;
            Assert.IsTrue(Utils.DeleteDirectory(dir, out errorMessage));
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