using Microsoft.Win32;

namespace GoogleTestAdapter.Helpers
{
    static class RegistryReader
    {

        internal static string ReadString(string keyName, string valueName, string defaultValue)
        {
            string typedValue = (string) ReadObject(keyName, valueName, defaultValue);
            if (typedValue == defaultValue)
            {
                return typedValue;
            }
            int indexOfLastStar = typedValue.LastIndexOf('*');
            return typedValue.Substring(indexOfLastStar + 1).Trim();
        }

        internal static bool ReadBool(string keyName, string valueName, bool defaultValue)
        {
            return bool.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        internal static int ReadInt(string keyName, string valueName, int defaultValue)
        {
            return int.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        private static object ReadObject(string keyName, string valueName, object defaultValue)
        {
            object result = Registry.GetValue(keyName, valueName, null);
            return result ?? defaultValue;
        }

    }

}