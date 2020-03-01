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
            converter.Should().BeOfType<AttributedEnumConverter>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToReadableString_ReturnsCorrectStrings()
        {
            DebuggerKind.VsTestFramework.ToReadableString().Should().Be("VsTest framework");
            DebuggerKind.Native.ToReadableString().Should().Be("Native");
            DebuggerKind.ManagedAndNative.ToReadableString().Should().Be("Managed and native");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ConvertFrom_ReturnsCorrectLiterals()
        {
            var converter = TypeDescriptor.GetConverter(typeof(DebuggerKind));
            converter.ConvertFrom("VsTest framework").Should().Be(DebuggerKind.VsTestFramework);
            converter.ConvertFrom("VsTestFramework").Should().Be(DebuggerKind.VsTestFramework);
            converter.ConvertFrom("Native").Should().Be(DebuggerKind.Native);
            converter.ConvertFrom("Managed and native").Should().Be(DebuggerKind.ManagedAndNative);
            converter.ConvertFrom("ManagedAndNative").Should().Be(DebuggerKind.ManagedAndNative);
        }

    }
}