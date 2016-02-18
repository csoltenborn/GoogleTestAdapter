using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.TestResults
{

    [TestClass]
    public class ErrorMessageParserTests
    {
        private const string DummyExecutable = "myexecutable.exe";
        private const string FullPathOfDummyExecutable = @"C:\mypath\" + DummyExecutable;

        [TestMethod]
        public void Constructor_EmptyString_EmptyResults()
        {
            ErrorMessageParser parser = new ErrorMessageParser("", FullPathOfDummyExecutable);

            Assert.AreEqual("", parser.ErrorMessage);
            Assert.AreEqual("", parser.ErrorStackTrace);
        }

        [TestMethod]
        public void Constructor_SingleErrorMessage_CorrectResult()
        {
            string errorString = $"{FullPathOfDummyExecutable}:42\nExpected: Foo\nActual: Bar";
            ErrorMessageParser parser = new ErrorMessageParser(errorString, FullPathOfDummyExecutable);

            Assert.AreEqual("\nExpected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"{DummyExecutable}:42"));
        }

        [TestMethod]
        public void Constructor_TwoErrorMessages_CorrectResult()
        {
            string errorString = $"{FullPathOfDummyExecutable}:37\nExpected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42\nExpected: Foo\nActual: Bar";

            ErrorMessageParser parser = new ErrorMessageParser(errorString, FullPathOfDummyExecutable);

            Assert.AreEqual("\n#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#1 - {DummyExecutable}:37"));
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#2 - {DummyExecutable}:42"));
        }

        [TestMethod]
        public void Constructor_DifferentlyFormattedErrorMessages_CorrectResult()
        {
            string errorString = $"{FullPathOfDummyExecutable}(37):\nExpected: Yes\nActual: Maybe";
            errorString += $"\n{FullPathOfDummyExecutable}:42\nExpected: Foo\nActual: Bar";

            ErrorMessageParser parser = new ErrorMessageParser(errorString, FullPathOfDummyExecutable);

            Assert.AreEqual("\n#1 - Expected: Yes\nActual: Maybe\n#2 - Expected: Foo\nActual: Bar", parser.ErrorMessage);
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#1 - {DummyExecutable}:37"));
            Assert.IsTrue(parser.ErrorStackTrace.Contains($"#2 - {DummyExecutable}:42"));
        }

    }

}