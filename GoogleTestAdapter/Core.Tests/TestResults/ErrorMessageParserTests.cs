using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.TestResults
{

    [TestClass]
    public class ErrorMessageParserTests
    {
        private const string BaseDir = @"C:\mypath\";
        private const string DummyExecutable = "myexecutable.exe";
        private const string FullPathOfDummyExecutable = BaseDir + DummyExecutable;

        [TestMethod]
        public void Parse_EmptyString_EmptyResults()
        {
            ErrorMessageParser parser = new ErrorMessageParser("", BaseDir);
            parser.Parse();

            Assert.AreEqual("", parser.ErrorMessage);
            Assert.AreEqual("", parser.ErrorStackTrace);
        }

        [TestMethod]
        public void Parse_SingleErrorMessage_MessageIsParsedWithoutLink()
        {
            string errorString = $"{FullPathOfDummyExecutable}:42: error: Expected: Foo\nActual: Bar";

            ErrorMessageParser parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            Assert.AreEqual("Expected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"{DummyExecutable}:42"));
        }

        [TestMethod]
        public void Parse_TwoErrorMessages_BothMessagesAreParsedWithLinks()
        {
            string errorString = $"{FullPathOfDummyExecutable}:37: error: Expected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42: Failure\nExpected: Foo\nActual: Bar";

            ErrorMessageParser parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            Assert.AreEqual("#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#1 - {DummyExecutable}:37"));
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#2 - {DummyExecutable}:42"));
        }

        [TestMethod]
        public void Parse_DifferentlyFormattedErrorMessages_BothMessagesAreParsedInCorrectOrder()
        {
            string errorString = $"{FullPathOfDummyExecutable}(37): error: Expected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42: error: Expected: Foo\nActual: Bar";

            ErrorMessageParser parser = new ErrorMessageParser(errorString, BaseDir);
            parser.Parse();

            Assert.AreEqual("#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#1 - {DummyExecutable}:37"));
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#2 - {DummyExecutable}:42"));
        }

    }

}