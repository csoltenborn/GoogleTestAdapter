using Microsoft.Win32;

namespace GoogleTestAdapter
{
    public static class RegistryReader
    {

        public static string ReadString(string keyName, string valueName, string defaultValue)
        {
            string TypedValue = (string) ReadObject(keyName, valueName, defaultValue);
            if (TypedValue == defaultValue)
            {
                return TypedValue;
            }
            int IndexOfLastStar = TypedValue.LastIndexOf('*');
            return TypedValue.Substring(IndexOfLastStar + 1).Trim();
        }

        public static bool ReadBool(string keyName, string valueName, bool defaultValue)
        {
            return bool.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        public static int ReadInt(string keyName, string valueName, int defaultValue)
        {
            return int.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        private static object ReadObject(string keyName, string valueName, object defaultValue)
        {
            object Result = Registry.GetValue(keyName, valueName, null);
            return Result ?? defaultValue;
        }

    }

}