using System.IO;
using System.Text;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Helpers
{
    public static class GoogleTestXmlFile
    {
        // Google Test declares its XML files to be UTF-8 encoded, but writes test names as they are encoded in the
        // executable (i.e., usually in the system's ANSI code page if the tests have not been compiled with /utf-8)
        public static string ReadAllText(string file, ILogger logger)
        {
            byte[] content = File.ReadAllBytes(file);
            try
            {
                return new UTF8Encoding(false, true).GetString(content).TrimStart('\uFEFF');
            }
            catch (DecoderFallbackException)
            {
                logger.DebugInfo($"Google Test XML file {file} is not UTF-8 encoded, falling back to code page {Encoding.Default.CodePage}");
                return Encoding.Default.GetString(content);
            }
        }
    }
}
