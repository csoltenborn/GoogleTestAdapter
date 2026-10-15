using System;
using FluentAssertions;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    [TestClass]
    public class VsDebuggerAttacherTests
    {
        private static readonly Guid NativeEngine = new Guid("3B476D35-A401-11D2-AAD4-00C04F990171");
        private static readonly Guid LegacyManagedAndNativeEngine = new Guid("92EF0900-2251-11D2-B72E-0000F87572EF");
        private static readonly Guid CoreClrEngine = new Guid("2E36F1D4-B23C-435D-AB41-18E608940038");

        [TestMethod]
        [TestCategory(Unit)]
        public void GetDebuggerEngineGuids_Native_NativeEngine()
        {
            VsDebuggerAttacher.GetDebuggerEngineGuids(DebuggerEngine.Native).Should().Equal(NativeEngine);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetDebuggerEngineGuids_ManagedAndNative_LegacyMixedModeEngine()
        {
            VsDebuggerAttacher.GetDebuggerEngineGuids(DebuggerEngine.ManagedAndNative).Should().Equal(LegacyManagedAndNativeEngine);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetDebuggerEngineGuids_ManagedCoreAndNative_NativeAndCoreClrEngines()
        {
            VsDebuggerAttacher.GetDebuggerEngineGuids(DebuggerEngine.ManagedCoreAndNative).Should().Equal(NativeEngine, CoreClrEngine);
        }
    }
}
