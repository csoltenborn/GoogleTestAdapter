using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestResults
{

    [TestClass]
    public class ErrorMessageParserTests
    {
        private const string BaseDir = @"C:\mypath\";
        private const string DummyExecutable = "myexecutable.exe";
        private const string FullPathOfDummyExecutable = BaseDir + DummyExecutable;

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_EmptyString_EmptyResults()
        {
            var parser = new ErrorMessageParser("", BaseDir);
            parser.Parse();

            parser.ErrorMessage.Should().Be("");
            parser.ErrorStackTrace.Should().Be("");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_SingleErrorMessage_MessageIsParsedWithoutLink()
        {
            string errorString = $"{FullPathOfDummyExecutable}:42: error: Expected: Foo\nActual: Bar";

            var parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            parser.ErrorMessage.Should().Be("Expected: Foo\nActual: Bar");
            parser.ErrorStackTrace.Should().Contain($"{DummyExecutable}:42");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_TwoErrorMessages_BothMessagesAreParsedWithLinks()
        {
            string errorString = $"{FullPathOfDummyExecutable}:37: error: Expected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42: Failure\nExpected: Foo\nActual: Bar";

            var parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            parser.ErrorMessage.Should().Be("#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar");
            parser.ErrorStackTrace.Should().Contain($"#1 - {DummyExecutable}:37");
            parser.ErrorStackTrace.Should().Contain($"#2 - {DummyExecutable}:42");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_DifferentlyFormattedErrorMessages_BothMessagesAreParsedInCorrectOrder()
        {
            string errorString = $"{FullPathOfDummyExecutable}(37): error: Expected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42: error: Expected: Foo\nActual: Bar";

            var parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            parser.ErrorMessage.Should().Be("#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar");
            parser.ErrorStackTrace.Should().Contain($"#1 - {DummyExecutable}:37");
            parser.ErrorStackTrace.Should().Contain($"#2 - {DummyExecutable}:42");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Parse_NullBaseDir()
        {
            string errorString = $"{FullPathOfDummyExecutable}:37: error: Expected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42: Failure\nExpected: Foo\nActual: Bar";

            var parser = new ErrorMessageParser(errorString, null);
            parser.Parse();

            parser.ErrorMessage.Should().Be("#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar");
            parser.ErrorStackTrace.Should().Contain($"#1 - {DummyExecutable}:37");
            parser.ErrorStackTrace.Should().Contain($"#2 - {DummyExecutable}:42");
        }

    }

}