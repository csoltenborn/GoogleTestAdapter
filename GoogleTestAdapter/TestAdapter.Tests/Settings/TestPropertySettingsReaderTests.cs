using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    [TestClass]
    public class TestPropertySettingsReaderTests : TestsBase
    {
        private const string Executable = @"C:\build\tests\MyTests.exe";

        [TestMethod]
        [TestCategory(Unit)]
        public void Read_RunSettingsWithTestProperties_SettingsAreRead()
        {
            string runSettings = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <RunConfiguration />
  <TestPropertySettingsForGoogleAdapter>
    <Tests>
      <TestProperties>
        <Name>Suite.Test1</Name>
        <Command>{Executable}</Command>
        <Environment>
          <EnvVar>
            <Name>PATH</Name>
            <Value>C:\build\bin;C:\Windows</Value>
          </EnvVar>
          <EnvVar>
            <Name>EMPTY</Name>
          </EnvVar>
        </Environment>
        <WorkingDirectory>C:\build\tests\dir1</WorkingDirectory>
      </TestProperties>
      <TestProperties>
        <Name>Suite.Test2</Name>
        <Command>""{Executable}""</Command>
        <WorkingDirectory>C:\build\tests\dir2</WorkingDirectory>
      </TestProperties>
    </Tests>
  </TestPropertySettingsForGoogleAdapter>
</RunSettings>";

            var container = TestPropertySettingsReader.Read(runSettings, MockLogger.Object);

            container.Should().NotBeNull();
            var settings1 = container.GetSettingsForTest(Executable, "Suite.Test1");
            settings1.WorkingDirectory.Should().Be(@"C:\build\tests\dir1");
            settings1.Environment.Should().HaveCount(2);
            settings1.Environment["path"].Should().Be(@"C:\build\bin;C:\Windows");
            settings1.Environment["EMPTY"].Should().Be("");

            var settings2 = container.GetSettingsForTest(Executable, "Suite.Test2");
            settings2.WorkingDirectory.Should().Be(@"C:\build\tests\dir2");
            settings2.Environment.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Read_RunSettingsWithoutTestProperties_ReturnsNull()
        {
            TestPropertySettingsReader.Read(null, MockLogger.Object).Should().BeNull();
            TestPropertySettingsReader.Read("", MockLogger.Object).Should().BeNull();
            TestPropertySettingsReader.Read("<RunSettings><RunConfiguration /></RunSettings>", MockLogger.Object).Should().BeNull();
            TestPropertySettingsReader.Read(
                "<RunSettings><TestPropertySettingsForGoogleAdapter><Tests /></TestPropertySettingsForGoogleAdapter></RunSettings>",
                MockLogger.Object).Should().BeNull();

            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Read_InvalidXml_ReturnsNullAndLogsWarning()
        {
            TestPropertySettingsReader.Read("<RunSettings><TestPropertySettingsForGoogleAdapter>", MockLogger.Object)
                .Should().BeNull();

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains(TestPropertySettingsReader.TestPropertySettingsName))), Times.Once);
        }
    }
}
