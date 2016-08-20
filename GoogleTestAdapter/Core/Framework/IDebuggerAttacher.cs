using System.Diagnostics;

namespace GoogleTestAdapter.Framework
{

    public interface IDebuggerAttacher
    {
        bool AttachDebugger(Process processToAttachTo);
    }

    // ReSharper disable once InconsistentNaming
    public static class IDebuggerAttacherExtensions
    {
        public static bool AttachDebugger(this IDebuggerAttacher attacher, int processIdToAttachTo)
        {
            return attacher.AttachDebugger(Process.GetProcessById(processIdToAttachTo));
        }
    }

}