using System;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [Serializable]
    public class AttachDebuggerMessage
    {
        public int ProcessId { get; set; }
    }
}