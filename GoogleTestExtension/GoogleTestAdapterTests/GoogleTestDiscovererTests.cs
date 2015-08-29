using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {
        public const string x86staticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x86externallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x86crashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        public const string x64staticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x64externallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x64crashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";

        public const string x86traitsTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1Tests.exe";

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

        [TestMethod]
        public void MatchesTestExecutableName()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTest.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletest.exe", MockLogger.Object));

            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTes.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TotallyWrong.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TestStuff.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TestLibrary.exe", MockLogger.Object));
        }

        [TestMethod]
        public void MatchesCustomRegex()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("SomeWeirdExpression", MockLogger.Object, "Some.*Expression"));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("SomeWeirdOtherThing", MockLogger.Object, "Some.*Expression"));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", MockLogger.Object, "Some.*Expression"));
        }

        [TestMethod]
        public void RegistersFoundTestsAtDiscoverySink()
        {
            CheckForDiscoverySinkCalls(2);
        }

        [TestMethod]
        public void MatchesCustomRegexIfSetInOptions()
        {
            CheckForDiscoverySinkCalls(0, "NoMatchAtAll");
        }

        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            Mock<IDiscoveryContext> MockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> MockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(O => O.TestDiscoveryRegex).Returns(() => customRegex);

            GoogleTestDiscoverer Discoverer = new MockedGoogleTestDiscoverer(MockOptions);
            Discoverer.DiscoverTests(x86staticallyLinkedTests.Yield(), MockDiscoveryContext.Object, MockLogger.Object, MockDiscoverySink.Object);

            MockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<TestCase>()), Times.Exactly(expectedNrOfTests));
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

        [TestMethod]
        public void FindsMathTestWithOneTrait()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath", Traits);
        }

        [TestMethod]
        public void FindsMathTestWithTwoTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TestMath", Traits);
        }

        [TestMethod]
        public void FindsMathTestWithThreeTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TestMath", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithOneTrait()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small") };
            FindsTestWithTraits("TheFixture", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithTwoTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TheFixture", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithThreeTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TheFixture", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithOneTrait()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithTwoTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithThreeTraits()
        {
            Trait[] Traits = new Trait[] { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("Category", "Integration") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests", Traits);
        }

        private void FindsTestWithTraits(string testPrefix, Trait[] traits)
        {
            Assert.IsTrue(File.Exists(x86traitsTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer Discoverer = new MockedGoogleTestDiscoverer(MockOptions);
            List<TestCase> Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, x86traitsTests);

            TestCase TestCase = Tests.Find(tc => tc.Traits.Count() == traits.Length && tc.FullyQualifiedName.StartsWith(testPrefix));
            Assert.IsNotNull(TestCase);

            foreach (Trait Trait in traits)
            {
                Trait FoundTrait = TestCase.Traits.FirstOrDefault(T => Trait.Name == T.Name && Trait.Value == T.Value);
                Assert.IsNotNull(FoundTrait, "Didn't find trait: (" + Trait.Name + ", " + Trait.Value + ")");
            }
        }

    }

}