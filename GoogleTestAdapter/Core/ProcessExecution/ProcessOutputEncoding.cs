using System.Text;

namespace GoogleTestAdapter.ProcessExecution
{

    /// <summary>
    /// Google Test prints test names as they are encoded within the executable, i.e., in UTF-8 if the tests
    /// have been compiled with /utf-8, and in the system's ANSI code page otherwise. Process output is
    /// therefore read with <see cref="Raw"/> (which maps each byte to one char), and each line is then
    /// decoded with <see cref="DecodeLine"/>.
    /// </summary>
    public static class ProcessOutputEncoding
    {
        public static readonly Encoding Raw = Encoding.GetEncoding(28591);

        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static string DecodeLine(string rawLine)
        {
            if (rawLine == null || IsAscii(rawLine))
                return rawLine;

            byte[] bytes = Raw.GetBytes(rawLine);
            try
            {
                return Utf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Default.GetString(bytes);
            }
        }

        private static bool IsAscii(string s)
        {
            foreach (char c in s)
            {
                if (c > 127)
                    return false;
            }
            return true;
        }
    }

}
