using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

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
        public void AssertIsNotNull_Null_ThrowsException()
        {
            Action action = () => Utils.AssertIsNotNull(null, "foo");
            action.ShouldThrow<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AssertIsNull_NotNull_ThrowsException()
        {
            Action action = () => Utils.AssertIsNull("", "foo");
            action.ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SpawnAndWait_TwoTasks_AreExecutedInParallel()
        {
            int nrOfTasks = Environment.ProcessorCount;
            if (nrOfTasks < 2)
                Assert.Inconclusive("System only has one processor, skipping test");

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
            stopWatch.ElapsedMilliseconds.Should().BeLessThan(2 * taskDurationInMs);
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
            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(timeoutInMs - 10 /* arbitrary tolerance */);
            stopWatch.ElapsedMilliseconds.Should().BeLessThan(taskDurationInMs);
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