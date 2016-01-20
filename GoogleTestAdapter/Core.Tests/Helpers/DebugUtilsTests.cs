using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{

    [TestClass]
    public class DebugUtilsTests
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AssertIsNotNull_Null_ThrowsException()
        {
            DebugUtils.AssertIsNotNull(null, "foo");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssertIsNull_NotNull_ThrowsException()
        {
            DebugUtils.AssertIsNull("", "foo");
        }

    }

}