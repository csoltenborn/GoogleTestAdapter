using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Common
{
    [TestClass]
    public class LoggerBaseTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void TimestampMessage_MessageIsNullOrEmpty_ResultIsTheSame()
        {
            string timestampSeparator = " - ";
            string resultRegex = @"[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}" + timestampSeparator;

            string nullMessage = null;
            LoggerBase.TimestampMessage(ref nullMessage);
            nullMessage.Should().MatchRegex(resultRegex);
            nullMessage.Should().EndWith(timestampSeparator);

            string emptyMessage = "";
            LoggerBase.TimestampMessage(ref emptyMessage);
            emptyMessage.Should().MatchRegex(resultRegex);
            emptyMessage.Should().EndWith(timestampSeparator);

            string fooMessage = "foo";
            LoggerBase.TimestampMessage(ref fooMessage);
            fooMessage.Should().MatchRegex(resultRegex);
            fooMessage.Should().EndWith(timestampSeparator + "foo");
        }

    }

}