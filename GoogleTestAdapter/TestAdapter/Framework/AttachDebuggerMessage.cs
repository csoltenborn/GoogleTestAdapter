using System;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [Serializable]
    public class AttachDebuggerMessage
    {
        public int ProcessId { get; set; }
        public bool DebuggerAttachedSuccessfully { get; set; } = false;
        public string ErrorMessage { get; set; } = null;
    }
}