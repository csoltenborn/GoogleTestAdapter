using System;

namespace GoogleTestAdapter.Helpers
{
    public static class ByteUtils
    {
        /// <summary>
        /// Implementation of the Boyer-Moore algorithm 
        /// (after https://en.wikipedia.org/wiki/Boyer%E2%80%93Moore_string_search_algorithm, Java version)
        /// </summary>
        /// <returns>Index of the first occurence of <code>pattern</code>, or <code>-1</code> if <code>pattern</code> is not contained in <code>bytes</code></returns>
        public static int IndexOf(this byte[] bytes, byte[] pattern)
        {
            if (pattern.Length == 0)
                return 0;

            int[] byteBasedJumpTable = CreateByteBasedJumpTable(pattern);
            int[] offsetBasedJumpTable = CreateOffsetBasedJumpTable(pattern);

            for (int i = pattern.Length - 1; i < bytes.Length;)
            {
                int j;
                for (j = pattern.Length - 1; pattern[j] == bytes[i]; --i, --j)
                {
                    if (j == 0)
                        return i;
                }

                i += Math.Max(offsetBasedJumpTable[pattern.Length - 1 - j], byteBasedJumpTable[bytes[i]]);
            }

            return -1;
        }

        private static int[] CreateByteBasedJumpTable(byte[] pattern)
        {
            int[] table = new int[byte.MaxValue + 1];
            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = pattern.Length;
            }
            for (int i = 0; i < pattern.Length - 1; ++i)
            {
                table[pattern[i]] = pattern.Length - 1 - i;
            }
            return table;
        }

        private static int[] CreateOffsetBasedJumpTable(byte[] pattern)
        {
            int[] table = new int[pattern.Length];
            int lastPrefixPosition = pattern.Length;
            for (int i = pattern.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(pattern, i + 1))
                    lastPrefixPosition = i + 1;

                table[pattern.Length - 1 - i] = lastPrefixPosition - i + pattern.Length - 1;
            }
            for (int i = 0; i < pattern.Length - 1; ++i)
            {
                int suffixLength = GetSuffixLength(pattern, i);
                table[suffixLength] = pattern.Length - 1 - i + suffixLength;
            }
            return table;
        }

        private static bool IsPrefix(byte[] pattern, int position)
        {
            for (int i = position, j = 0; i < pattern.Length; ++i, ++j)
            {
                if (pattern[i] != pattern[j])
                    return false;
            }
            return true;
        }

        private static int GetSuffixLength(byte[] pattern, int position)
        {
            int length = 0;
            for (int i = position, j = pattern.Length - 1; i >= 0 && pattern[i] == pattern[j]; --i, --j)
            {
                length++;
            }
            return length;
        }
    }
}