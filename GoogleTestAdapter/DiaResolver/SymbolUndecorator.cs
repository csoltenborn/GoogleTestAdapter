using System.Runtime.InteropServices;
using System.Text;

namespace GoogleTestAdapter.DiaResolver
{
    /// <summary>
    /// Demangles names decorated by the MSVC compiler, e.g. "testing::UnitTest::GetInstance" for
    /// "?GetInstance@UnitTest@testing@@SAPEAV12@XZ".
    /// </summary>
    public static class SymbolUndecorator
    {
        // dbghelp's functions are not thread-safe
        private static readonly object Lock = new object();

        private const uint UndnameNameOnly = 0x1000;
        private const int MaxNameLength = 4096;

        private static class NativeMethods
        {
            [DllImport("dbghelp.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern uint UnDecorateSymbolName(string name, StringBuilder outputString, uint maxStringLength, uint flags);
        }

        /// <returns>The fully qualified name of the function or variable (without return type, parameters, and
        /// modifiers), or null if the name is not decorated by MSVC or can not be undecorated</returns>
        public static string UndecorateNameOnly(string decoratedName)
        {
            if (string.IsNullOrEmpty(decoratedName) || decoratedName[0] != '?')
                return null;

            var name = new StringBuilder(MaxNameLength);
            uint length;
            lock (Lock)
            {
                length = NativeMethods.UnDecorateSymbolName(decoratedName, name, MaxNameLength, UndnameNameOnly);
            }
            return length == 0 ? null : name.ToString();
        }
    }
}
