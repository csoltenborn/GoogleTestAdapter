using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;
using Moq;

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

            // ReSharper disable once UnusedVariable
            Utils.DeleteDirectory(dir, out string errorMessage).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetExtendedPath_WithExtension_ExtendsPath()
        {
            const string toAdd = @"c:\some\path\to\add";
            string result = Utils.GetExtendedPath(toAdd);

            string path = Environment.GetEnvironmentVariable("PATH");
            result.Should().HaveLength(path.Length + toAdd.Length + 1);
            result.Should().Contain(path);
            string[] pathParts = result.Split(';');
            pathParts.Should().Contain(s => s.Equals(toAdd));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetExtendedPath_NoExtension_ReturnsPath()
        {
            string result = Utils.GetExtendedPath("");

            string path = Environment.GetEnvironmentVariable("PATH");
            result.Should().Be(path);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX86Release_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_ReleaseX86, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX64Release_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_ReleaseX64, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX86Debug_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_DebugX86, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX64Debug_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_DebugX64, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TenSecondsWaiter_ShouldNotContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.TenSecondsWaiter, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_EmptyFile_ShouldNotContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.TenSecondsWaiter, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SpawnAndWait_SeveralTasks_AreExecutedInParallel()
        {
            int nrOfTasks = Environment.ProcessorCount - 2;
            if (nrOfTasks < 2)
                Assert.Inconclusive("System does not have enough processors, skipping test");

            int taskDurationInMs = 500;

            var tasks = new Action[nrOfTasks];
            for (int i = 0; i < nrOfTasks; i++)
            {
                tasks[i] = () => Thread.Sleep(taskDurationInMs);
            }

            var stopWatch = Stopwatch.StartNew();
            Utils.SpawnAndWait(tasks);
            stopWatch.Stop();

            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(taskDurationInMs);
            stopWatch.ElapsedMilliseconds.Should().BeLessThan((int)(1.9 * taskDurationInMs));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SpawnAndWait_TaskWithTimeout_TimeoutsAndReturnsFalse()
        {
            int taskDurationInMs = 500, timeoutInMs = taskDurationInMs / 2;
            var tasks = new Action[] { () => Thread.Sleep(taskDurationInMs) };

            var stopWatch = Stopwatch.StartNew();
            bool hasFinishedTasks = Utils.SpawnAndWait(tasks, timeoutInMs);
            stopWatch.Stop();

            hasFinishedTasks.Should().BeFalse();
            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(timeoutInMs - TestMetadata.ToleranceInMs);
            stopWatch.ElapsedMilliseconds.Should().BeLessThan(taskDurationInMs);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_EmptyPattern_BothPartsReported()
        {
            bool result = Utils.ValidatePattern("", out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain("file pattern part");
            errorMessage.Should().Contain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_InvalidPattern_BothPartsReported()
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();
            if (invalidPathChars.Length < 1)
                Assert.Inconclusive("Cannot test invalid path chars as none are reported.");

            bool result = Utils.ValidatePattern(""+invalidPathChars[0], out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain("file pattern part");
            errorMessage.Should().Contain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_TempDir_FilePartReported()
        {
            bool result = Utils.ValidatePattern(Path.GetTempPath(), out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain("file pattern part");
            errorMessage.Should().NotContain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_LocalFile_PathPartReported()
        {
            bool result = Utils.ValidatePattern(@"InvalidPath::\Foo.exe", out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().NotContain("file pattern part");
            errorMessage.Should().Contain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_ValidInput_ValidationSuceeds()
        {
            bool result = Utils.ValidatePattern(@"C:\foo\Bar.exe", out string errorMessage);

            result.Should().BeTrue();
            errorMessage.Should().BeNullOrEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetMatchingFiles_ValidInput_ReturnsValidList()
        {
            Mock<ILogger> MockLogger = new Mock<ILogger>();
            string[] result = Utils.GetMatchingFiles(Path.GetDirectoryName(TestResources.Tests_DebugX86) + @"\*.exe", MockLogger.Object);

            result.Should().Contain(s => s.Equals(TestResources.Tests_DebugX86, StringComparison.CurrentCultureIgnoreCase));
            result.Should().NotContain(s => !s.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase));
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetMatchingFiles_InvalidInput_ErrorReported()
        {
            Mock<ILogger> MockLogger = new Mock<ILogger>();
            string[] result = Utils.GetMatchingFiles(null, MockLogger.Object);

            result.Length.Should().Be(0);
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetMatchingFiles_NonExistingPath_ErrorReported()
        {
            Mock<ILogger> MockLogger = new Mock<ILogger>();
            string[] result = Utils.GetMatchingFiles(@"some\non\exisiting\path", MockLogger.Object);

            result.Length.Should().Be(0);
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
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