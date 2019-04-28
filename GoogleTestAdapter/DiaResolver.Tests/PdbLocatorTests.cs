using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.DiaResolver
{
    [TestClass]
    public class PdbLocatorTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void FindPdbFile_ExeWithPdb_FindsPdb()
        {
            string executable = Path.GetFullPath(TestResources.LoadTests_ReleaseX86);
            executable.AsFileInfo().Should().Exist();
            var fakeLogger = new FakeLogger(() => OutputMode.Verbose);

            string pdbFound = PdbLocator.FindPdbFile(executable, "", fakeLogger);

            string pdb = Path.ChangeExtension(executable, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            pdbFound.Should().Be(pdb);
        }

        [TestMethod]
        [TestCategory(Unit)]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void FindPdbFile_ExeWithoutPdb_AttemptsToFindPdbAreLogged()
        {
            TestResources.LoadTests_ReleaseX86.AsFileInfo().Should().Exist();
            string pdb = Path.ChangeExtension(TestResources.LoadTests_ReleaseX86, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            string renamedPdb = $"{pdb}.bak";
            renamedPdb.AsFileInfo().Should().NotExist();

            string pdbFound;
            var fakeLogger = new FakeLogger(() => OutputMode.Verbose);
            try
            {
                File.Move(pdb, renamedPdb);
                pdb.AsFileInfo().Should().NotExist();

                pdbFound = PdbLocator.FindPdbFile(TestResources.LoadTests_ReleaseX86, "", fakeLogger);
            }
            finally
            {
                File.Move(renamedPdb, pdb);
                pdb.AsFileInfo().Should().Exist();
            }

            pdbFound.Should().BeNull();
            fakeLogger.Infos
                .Should()
                .Contain(msg => msg.Contains("Attempts to find pdb:"));
        }

        [TestMethod]
        [TestCategory(Unit)]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void FindPdbFile_PATHcontainsInvalidChars_ErrorIsLogged()
        {
            TestResources.LoadTests_ReleaseX86.AsFileInfo().Should().Exist();
            string pdb = Path.ChangeExtension(TestResources.LoadTests_ReleaseX86, ".pdb");
            pdb.AsFileInfo().Should().Exist();
            string renamedPdb = $"{pdb}.bak";
            renamedPdb.AsFileInfo().Should().NotExist();

            string pdbFound;
            var fakeLogger = new FakeLogger(() => OutputMode.Verbose);
            var currentPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                File.Move(pdb, renamedPdb);
                pdb.AsFileInfo().Should().NotExist();

                Environment.SetEnvironmentVariable("PATH", $"My<Invalid>Path;{currentPath}");
                pdbFound = PdbLocator.FindPdbFile(TestResources.LoadTests_ReleaseX86, "", fakeLogger);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", currentPath);
                File.Move(renamedPdb, pdb);
                pdb.AsFileInfo().Should().Exist();
            }

            pdbFound.Should().BeNull();
            fakeLogger.Warnings
                .Should()
                .Contain(msg => msg.Contains("invalid path"));
        }

    }
}