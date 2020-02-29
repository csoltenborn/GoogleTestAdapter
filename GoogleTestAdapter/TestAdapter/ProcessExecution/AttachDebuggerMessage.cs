using System;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    [Serializable]
    public class AttachDebuggerMessage
    {
        public int ProcessId { get; set; }
        public bool DebuggerAttachedSuccessfully { get; set; } = false;
        public string ErrorMessage { get; set; } = null;
    }
}