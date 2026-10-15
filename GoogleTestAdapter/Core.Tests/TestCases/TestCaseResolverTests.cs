using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestCases
{

    [TestClass]
    public class TestCaseResolverTests : TestsBase
    {
        private FakeLogger _fakeLogger;

        [TestInitialize]
        public void Setup()
        {
            _fakeLogger = new FakeLogger(() => OutputMode.Verbose, false);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named", 9, "Namespace_1");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named_Named", 16, "Namespace_1::Namespace_2_Nested");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Named_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Named_Anon", 25, "Namespace_1::(anonymous namespace)");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon", 35, "(anonymous namespace)");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_Anon_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon_Anon", 42, "(anonymous namespace)::(anonymous namespace)");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_Namespace_Anon_Named_LocationIsFound()
        {
            AssertCorrectTestLocationIsFound("Namespace_Anon_Named", 51, "(anonymous namespace)::Anon_Nested");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void FindTestCaseLocation_GlobalNamespace_NamespaceIsEmpty()
        {
            var descriptor = new TestCaseDescriptor(
                "TestMath", "AddPasses", "TestMath.AddPasses", "TestMath.AddPasses", TestCaseDescriptor.TestTypes.Simple);
            var signatures = new MethodSignatureCreator().GetTestMethodSignatures(descriptor);
            var resolver = new TestCaseResolver(TestResources.Tests_ReleaseX64,
                new DefaultDiaResolverFactory(), MockOptions.Object, _fakeLogger);

            var testCaseLocation = resolver.FindTestCaseLocation(signatures.ToList());

            testCaseLocation.Should().NotBeNull();
            testCaseLocation.Sourcefile.Should().EndWithEquivalent(@"sampletests\tests\basictests.cpp");
            testCaseLocation.Namespace.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void Constructor_ExecutableWithoutPdb_WarningIsLoggedEvenIfNotInDebugMode()
        {
            var logger = new FakeLogger(() => OutputMode.Info, false);
            string executableWithoutPdb = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            new TestCaseResolver(executableWithoutPdb, new DefaultDiaResolverFactory(), MockOptions.Object, logger);

            logger.Warnings.Should().ContainSingle()
                .Which.Should().Contain($"No .pdb file found for test executable '{executableWithoutPdb}'");
        }

        private void AssertCorrectTestLocationIsFound(string suite, uint line, string expectedNamespace)
        {
            var descriptor = new TestCaseDescriptor(
                suite, 
                "Test", 
                $"{suite}.Test", 
                $"{suite}.Test",
                TestCaseDescriptor.TestTypes.Simple);
            var signatures = new MethodSignatureCreator().GetTestMethodSignatures(descriptor);
            var resolver = new TestCaseResolver(TestResources.Tests_ReleaseX64, 
                new DefaultDiaResolverFactory(), MockOptions.Object, _fakeLogger);

            var testCaseLocation = resolver.FindTestCaseLocation(signatures.ToList());

            _fakeLogger.Errors.Should().BeEmpty();
            testCaseLocation.Should().NotBeNull();
            testCaseLocation.Sourcefile.Should().EndWithEquivalent(@"sampletests\tests\namespacetests.cpp");
            testCaseLocation.Line.Should().Be(line);
            testCaseLocation.Namespace.Should().Be(expectedNamespace);
        }
    }

}