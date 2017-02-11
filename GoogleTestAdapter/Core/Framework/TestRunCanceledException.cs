using System;

namespace GoogleTestAdapter.Framework
{
    public sealed class TestRunCanceledException : Exception
    {
        public TestRunCanceledException(string message, Exception innerException) : base(message, innerException) { }
    }
}