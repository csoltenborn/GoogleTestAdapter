using System.IO;

namespace GoogleTestAdapter.Helpers
{
    static class Utils
    {

        internal static string GetTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

    }

}