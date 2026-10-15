using System;
using System.IO;
using System.Text;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class XmlTestListParserTests : TestsBase
    {
        private string _folder;
        private string _executable;
        private string _xmlFile;

        [TestInitialize]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "GTA_XmlTestListParserTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _executable = Path.Combine(_folder, "MyTests.exe");
            _xmlFile = Path.Combine(_folder, "list.xml");
        }

        [TestCleanup]
        public void TearDown()
        {
            Directory.Delete(_folder, true);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_TestList_LocationsAreReturnedByFullyQualifiedName()
        {
            WriteXml(
                @"<testsuite name=""Suite"" tests=""2"">" +
                @"<testcase name=""Test"" file=""C:\src\MyTests.cpp"" line=""10"" />" +
                @"<testcase name=""Simple/0"" value_param=""1"" file=""C:\src\MyTests.cpp"" line=""20"" />" +
                @"</testsuite>" +
                @"<testsuite name=""Inst/TypedTests/0"" tests=""1"">" +
                @"<testcase name=""CanIterate"" type_param=""int"" file=""C:\src\TypedTests.cpp"" line=""30"" />" +
                @"</testsuite>");

            var locations = new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile);

            locations.Should().HaveCount(3);
            locations["Suite.Test"].Sourcefile.Should().Be(@"C:\src\MyTests.cpp");
            locations["Suite.Test"].Line.Should().Be(10);
            locations["Suite.Simple/0"].Line.Should().Be(20);
            locations["Inst/TypedTests/0.CanIterate"].Sourcefile.Should().Be(@"C:\src\TypedTests.cpp");
            locations["Inst/TypedTests/0.CanIterate"].Line.Should().Be(30);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_FileDoesNotExist_NoLocations()
        {
            new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile).Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_EmptyFile_NoLocations()
        {
            // Google Test < 1.8.1 does not write the file, which has been created by Path.GetTempFileName()
            File.WriteAllBytes(_xmlFile, new byte[0]);

            new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile).Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_InvalidXml_NoLocationsAndWarningIsLogged()
        {
            File.WriteAllText(_xmlFile, @"<?xml version=""1.0"" encoding=""UTF-8""?><testsuites><testsuite name=""Suite""");

            new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile).Should().BeEmpty();
            MockLogger.Verify(l => l.DebugWarning(It.Is<string>(s => s.Contains(_xmlFile))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_TestsWithoutFileOrLine_TestsAreIgnored()
        {
            WriteXml(
                @"<testsuite name=""Suite"" tests=""3"">" +
                @"<testcase name=""NoFile"" line=""10"" />" +
                @"<testcase name=""NoLine"" file=""C:\src\MyTests.cpp"" />" +
                @"<testcase name=""LineZero"" file=""C:\src\MyTests.cpp"" line=""0"" />" +
                @"</testsuite>");

            new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile).Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_RelativePaths_PathsAreResolvedRelativeToExecutableIfFileExists()
        {
            Directory.CreateDirectory(Path.Combine(_folder, "src"));
            File.WriteAllText(Path.Combine(_folder, @"src\Existing.cpp"), "");
            WriteXml(
                @"<testsuite name=""Suite"" tests=""2"">" +
                @"<testcase name=""Existing"" file=""src\Existing.cpp"" line=""10"" />" +
                @"<testcase name=""Missing"" file=""src\Missing.cpp"" line=""20"" />" +
                @"</testsuite>");

            var locations = new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile);

            locations.Should().HaveCount(1);
            locations["Suite.Existing"].Sourcefile.Should().Be(Path.Combine(_folder, @"src\Existing.cpp"));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseTestLocations_TestNamesInAnsiCodePage_NamesAreDecoded()
        {
            // Google Test declares the file as UTF-8, but writes names as they are encoded in the executable
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><testsuites>" +
                         @"<testsuite name=""Ümlaut"" tests=""1""><testcase name=""Täst"" file=""C:\src\MyTests.cpp"" line=""10"" /></testsuite>" +
                         @"</testsuites>";
            File.WriteAllBytes(_xmlFile, Encoding.Default.GetBytes(xml));

            var locations = new XmlTestListParser(_executable, MockLogger.Object).ParseTestLocations(_xmlFile);

            locations.Should().ContainKey("Ümlaut.Täst");
        }

        private void WriteXml(string testSuites)
        {
            File.WriteAllText(_xmlFile,
                @"<?xml version=""1.0"" encoding=""UTF-8""?><testsuites name=""AllTests"">" + testSuites + "</testsuites>",
                new UTF8Encoding(false));
        }
    }

}
