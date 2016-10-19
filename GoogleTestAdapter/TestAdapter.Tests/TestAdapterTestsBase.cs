using System.IO;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter
{

    public abstract class TestAdapterTestsBase : TestsBase
    {
        protected readonly Mock<IRunContext> MockRunContext = new Mock<IRunContext>();
        protected readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();
        protected readonly Mock<IMessageLogger> MockVsLogger = new Mock<IMessageLogger>();

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockRunContext.Setup(rc => rc.SolutionDirectory).Returns(Path.GetFullPath(TestResources.SampleTestsSolutionDir));
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockRunContext.Reset();
            MockFrameworkHandle.Reset();
            MockVsLogger.Reset();
        }

    }

}