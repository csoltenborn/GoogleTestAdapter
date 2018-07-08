using System.Threading;

namespace GoogleTestAdapter.Common
{
    // from https://www.codeproject.com/Tips/375559/Implement-Thread-Safe-One-shot-Bool-Flag-with-Inte
    public class ThreadSafeSingleShotGuard
    {
        private static int NOTCALLED = 0;
        private static int CALLED = 1;
        private int _state = NOTCALLED;

        /// <summary>Explicit call to check and set if this is the first call</summary>
        public bool CheckAndSetFirstCall => Interlocked.Exchange(ref _state, CALLED) == NOTCALLED;
    }
}