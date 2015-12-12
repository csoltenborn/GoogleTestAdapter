using System.IO;
using GoogleTestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapterVSIX
{

    public abstract class AbstractVSIXTests : AbstractGoogleTestExtensionTests
    {
        protected readonly Mock<IRunContext> MockRunContext = new Mock<IRunContext>();
        protected readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockRunContext.Setup(rc => rc.SolutionDirectory).Returns(Path.GetFullPath(SampleTestsSolutionDir));
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockRunContext.Reset();
            MockFrameworkHandle.Reset();
        }

    }

}