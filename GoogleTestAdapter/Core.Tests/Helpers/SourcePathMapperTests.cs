using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.TestResults;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{

    [TestClass]
    public class SourcePathMapperTests
    {
        private static SourcePathMapper Create(string option, bool mapOnlyMissingPaths = false)
            => new SourcePathMapper(SourcePathMapper.Parse(option), mapOnlyMissingPaths);

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_PathStartsWithBuildPath_PrefixIsReplaced()
        {
            var mapper = Create(@"C:\agent\_work\1\s=>L:\src\project");

            mapper.Map(@"C:\agent\_work\1\s\tests\FooTests.cpp").Should().Be(@"L:\src\project\tests\FooTests.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_TrailingSeparatorsAndCaseAndSlashes_AreIgnored()
        {
            var mapper = Create(@"c:/Agent/_work/1/s/=>L:\src\project\");

            mapper.Map(@"C:\agent\_work\1\s\tests/FooTests.cpp").Should().Be(@"L:\src\project\tests\FooTests.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_BuildPathIsPrefixOfDirectoryName_PathIsNotMapped()
        {
            var mapper = Create(@"C:\src=>L:\src");

            mapper.Map(@"C:\src2\FooTests.cpp").Should().Be(@"C:\src2\FooTests.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_SeveralMatchingPairs_FirstOneWins()
        {
            var mapper = Create(@"C:\build\deps=>C:\deps//||//C:\build=>L:\src");

            mapper.Map(@"C:\build\deps\lib.h").Should().Be(@"C:\deps\lib.h");
            mapper.Map(@"C:\build\tests\FooTests.cpp").Should().Be(@"L:\src\tests\FooTests.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_NoMatchingPairOrNoPath_PathIsReturnedUnchanged()
        {
            var mapper = Create(@"C:\build=>L:\src");

            mapper.Map(@"D:\other\FooTests.cpp").Should().Be(@"D:\other\FooTests.cpp");
            mapper.Map("").Should().Be("");
            mapper.Map(null).Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_MapOnlyMissingPaths_ExistingFilesAreNotMapped()
        {
            string existingFile = Path.GetTempFileName();
            try
            {
                string folder = Path.GetDirectoryName(existingFile);
                var mapper = Create($"{folder}=>L:\\src", mapOnlyMissingPaths: true);

                mapper.Map(existingFile).Should().Be(existingFile);
                mapper.Map(Path.Combine(folder, "DoesNotExist_" + Guid.NewGuid().ToString("N") + ".cpp"))
                    .Should().StartWith(@"L:\src\DoesNotExist_");
            }
            finally
            {
                File.Delete(existingFile);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Map_MapAllPaths_ExistingFilesAreMapped()
        {
            string existingFile = Path.GetTempFileName();
            try
            {
                var mapper = Create($"{Path.GetDirectoryName(existingFile)}=>L:\\src");

                mapper.Map(existingFile).Should().Be(Path.Combine(@"L:\src", Path.GetFileName(existingFile)));
            }
            finally
            {
                File.Delete(existingFile);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_InvalidPairs_ArgumentExceptionIsThrown()
        {
            foreach (string option in new[] { @"C:\build", @"=>L:\src", @"C:\build=>", @"C:\build=>L:\src//||//D:\deps" })
            {
                Action parse = () => SourcePathMapper.Parse(option);
                parse.Should().Throw<ArgumentException>(option);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_EmptyOption_NoMappings()
        {
            SourcePathMapper.Parse("").Should().BeEmpty();
            SourcePathMapper.Parse(null).Should().BeEmpty();
            Create("").IsIdentity.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ErrorMessageParser_WithMapper_StackTracePathsAreMapped()
        {
            var mapper = Create(@"C:\agent\s=>L:\src");
            string output = "C:\\agent\\s\\FooTests.cpp(42): error: Expected: 1\nActual: 2\nGoogle Test trace:\nC:\\agent\\s\\Helper.cpp(7): in helper\n";

            var parser = new ErrorMessageParser(output, "Suite.Test", mapper);
            parser.Parse();

            parser.ErrorStackTrace.Should().Be(
                ErrorMessageParser.CreateStackTraceEntry("Suite.Test", @"L:\src\FooTests.cpp", "42") +
                ErrorMessageParser.CreateStackTraceEntry("-->in helper", @"L:\src\Helper.cpp", "7"));
        }
    }

}
