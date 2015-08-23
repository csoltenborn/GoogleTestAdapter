using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests
    {
        private readonly IMessageLogger Logger = new ConsoleLogger();

        public const string x86staticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x86externallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x86crashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        public const string x64staticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x64externallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x64crashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";

        [TestMethod]
        public void MatchesTestExecutableName()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", Logger));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTest.exe", Logger));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletests.exe", Logger));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletest.exe", Logger));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTes.exe", Logger));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TotallyWrong.exe", Logger));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("TestStuff.exe", Logger));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("TestLibrary.exe", Logger));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer();
            var Tests = Discoverer.GetTestsFromExecutable(Logger, location);
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
            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer();
            var Tests = Discoverer.GetTestsFromExecutable(Logger, location);

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
