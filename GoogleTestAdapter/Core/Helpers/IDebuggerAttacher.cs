using System.Diagnostics;

namespace GoogleTestAdapter.Framework
{
    public interface IDebuggerAttacher
    {
        bool AttachDebugger(Process processToAttachTo);
        bool AttachDebugger(int processIdToAttachTo);
    }
}