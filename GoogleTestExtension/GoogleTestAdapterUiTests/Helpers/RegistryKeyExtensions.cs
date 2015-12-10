using Microsoft.Win32;

namespace GoogleTestAdapterUiTests.Helpers
{
    public static class RegistryKeyExtensions
    {
        public static bool HasSubKey(this RegistryKey key, string subKey)
        {
            using (var regKey = key.OpenSubKey(subKey, false))
            {
                return regKey != null;
            }
        }
    }
}
