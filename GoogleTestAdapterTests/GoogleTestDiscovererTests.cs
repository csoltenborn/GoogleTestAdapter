using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {
        class MockedGoogleTestDiscoverer : GoogleTestDiscoverer
        {
            private readonly Mock<IOptions> MockedOptions;

            internal MockedGoogleTestDiscoverer(Mock<IOptions> mockedOptions) : base()
            {
                this.MockedOptions = mockedOptions;
            }

            protected override IOptions Options
            {
                get
                {
                    return MockedOptions.Object;
                }
            }
        }

        public const string x86staticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x86externallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x86crashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        public const string x64staticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x64externallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x64crashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";

        [TestMethod]
        public void MatchesTestExecutableName()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTest.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletest.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTes.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TotallyWrong.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("TestStuff.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("TestLibrary.exe", MockLogger.Object));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer Discoverer = new MockedGoogleTestDiscoverer(MockOptions);
            var Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, location);
            Assert.AreEqual(2, Tests.Count);
            Assert.AreEqual("FooTest.DoesXyz", Tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", Tests[0].CodeFilePath);
            Assert.AreEqual(45, Tests[0].LineNumber);
            Assert.AreEqual("FooTest.MethodBarDoesAbc", Tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", Tests[1].CodeFilePath);
            Assert.AreEqual(36, Tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(x86staticallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(x64staticallyLinkedTests);
        }


        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer Discoverer = new MockedGoogleTestDiscoverer(MockOptions);
            var Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, location);

            Assert.AreEqual(2, Tests.Count);

            Assert.AreEqual("BarTest.DoesXyz", Tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", Tests[0].CodeFilePath);
            Assert.AreEqual(44, Tests[0].LineNumber);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", Tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", Tests[1].CodeFilePath);
            Assert.AreEqual(36, Tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(x86externallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(x64externallyLinkedTests);
        }

    }

}