using System.ComponentModel;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class DebuggerKindConverterTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void TypeDescriptor_DeliversCorrectType()
        {
            var converter = TypeDescriptor.GetConverter(typeof(DebuggerKind));
            converter.Should().BeOfType<DebuggerKindConverter>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToReadableString_ReturnsCorrectStrings()
        {
            DebuggerKind.VsTestFramework.ToReadableString().Should().Be("VsTest framework");
            DebuggerKind.Native.ToReadableString().Should().Be("Native");
            DebuggerKind.ManagedAndNative.ToReadableString().Should().Be("Managed and native");
            DebuggerKind.ManagedCoreAndNative.ToReadableString().Should().Be("Managed (.NET Core, .NET 5+) and native");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ConvertFrom_ReturnsCorrectLiterals()
        {
            new DebuggerKindConverter().ConvertFrom("VsTest framework").Should().Be(DebuggerKind.VsTestFramework);
            new DebuggerKindConverter().ConvertFrom("VsTestFramework").Should().Be(DebuggerKind.VsTestFramework);
            new DebuggerKindConverter().ConvertFrom("Native").Should().Be(DebuggerKind.Native);
            new DebuggerKindConverter().ConvertFrom("Managed and native").Should().Be(DebuggerKind.ManagedAndNative);
            new DebuggerKindConverter().ConvertFrom("ManagedAndNative").Should().Be(DebuggerKind.ManagedAndNative);
            new DebuggerKindConverter().ConvertFrom("Managed (.NET Core, .NET 5+) and native").Should().Be(DebuggerKind.ManagedCoreAndNative);
            new DebuggerKindConverter().ConvertFrom("ManagedCoreAndNative").Should().Be(DebuggerKind.ManagedCoreAndNative);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetDebuggerEngine_AllDebuggerKindsExceptVsTestFramework_HaveDebuggerEngine()
        {
            GoogleTestAdapter.Runners.SequentialTestRunner.GetDebuggerEngine(DebuggerKind.Native).Should().Be(DebuggerEngine.Native);
            GoogleTestAdapter.Runners.SequentialTestRunner.GetDebuggerEngine(DebuggerKind.ManagedAndNative).Should().Be(DebuggerEngine.ManagedAndNative);
            GoogleTestAdapter.Runners.SequentialTestRunner.GetDebuggerEngine(DebuggerKind.ManagedCoreAndNative).Should().Be(DebuggerEngine.ManagedCoreAndNative);
        }

    }
}