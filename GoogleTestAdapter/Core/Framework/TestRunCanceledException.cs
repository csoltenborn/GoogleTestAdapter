using System;
using System.Runtime.Serialization;

namespace GoogleTestAdapter.Framework
{
    [Serializable]
    public sealed class TestRunCanceledException : Exception
    {
        public TestRunCanceledException(string message, Exception innerException) 
            : base(message, innerException) { }

        private TestRunCanceledException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}