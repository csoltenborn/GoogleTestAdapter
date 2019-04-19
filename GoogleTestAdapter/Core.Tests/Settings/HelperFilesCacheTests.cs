using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{
    [TestClass]
    public class HelperFilesCacheTests : TestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_NoFile_EmptyDictionary()
        {
            string executable = TestResources.Tests_DebugX86;
            var extraSettings = HelperFilesCache.GetHelperFile(executable);
            extraSettings.AsFileInfo().Should().NotExist();

            var cache = new HelperFilesCache(MockLogger.Object);
            var map = cache.GetReplacementsMap(executable);

            map.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_EmptyString_EmptyDictionary()
        {
            DoTest("", map =>
            {
                map.Should().BeEmpty();
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_InvalidString_EmptyDictionary()
        {
            DoTest("Foo", map =>
            {
                map.Should().BeEmpty();
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_SingleValue_ProperDictionary()
        {
            DoTest("Foo=Bar", map =>
            {
                map.Should().HaveCount(1);
                map.Should().Contain(new KeyValuePair<string, string>("Foo", "Bar"));
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_TwoValues_ProperDictionary()
        {
            DoTest($"Placeholder1=value1{HelperFilesCache.SettingsSeparator}Placeholder2=value2", map =>
            {
                map.Should().HaveCount(2);
                map.Should().Contain(new KeyValuePair<string, string>("Placeholder1", "value1"));
                map.Should().Contain(new KeyValuePair<string, string>("Placeholder2", "value2"));
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_SingleWithEmptyValue_ProperDictionary()
        {
            DoTest("Placeholder1=", map =>
            {
                map.Should().HaveCount(1);
                map.Should().Contain(new KeyValuePair<string, string>("Placeholder1", ""));
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_SingleWithTrailingSeparator_ProperDictionary()
        {
            DoTest($"Ph=V{HelperFilesCache.SettingsSeparator}", map =>
            {
                map.Should().HaveCount(1);
                map.Should().Contain(new KeyValuePair<string, string>("Ph", "V"));
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_OnlySeparator_ProperDictionary()
        {
            DoTest($"{HelperFilesCache.SettingsSeparator}", map =>
            {
                map.Should().BeEmpty();
            });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetReplacementsMap_OnlyTwoSeparators_ProperDictionary()
        {
            DoTest($"{HelperFilesCache.SettingsSeparator}{HelperFilesCache.SettingsSeparator}", map =>
            {
                map.Should().BeEmpty();
            });
        }

        private void DoTest(string content, Action<IDictionary<string, string>> assertions, string executable = TestResources.Tests_DebugX86)
        {
            var extraSettings = HelperFilesCache.GetHelperFile(executable);
            try
            {
                extraSettings.AsFileInfo().Should().NotExist();
                File.WriteAllText(extraSettings, content);

                var cache = new HelperFilesCache(MockLogger.Object);
                var map = cache.GetReplacementsMap(executable);
                assertions(map);
            }
            finally
            {
                File.Delete(extraSettings);
            }
        }
    }
}