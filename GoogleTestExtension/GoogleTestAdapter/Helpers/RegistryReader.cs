using Microsoft.Win32;

namespace GoogleTestAdapter.Helpers
{

    public interface IRegistryReader
    {
        string ReadString(string keyName, string valueName, string defaultValue);
        bool ReadBool(string keyName, string valueName, bool defaultValue);
        int ReadInt(string keyName, string valueName, int defaultValue);
    }

    class RegistryReader : IRegistryReader
    {

        public string ReadString(string keyName, string valueName, string defaultValue)
        {
            string typedValue = (string)ReadObject(keyName, valueName, defaultValue);
            if (typedValue == defaultValue)
            {
                return typedValue;
            }
            int indexOfLastStar = typedValue.LastIndexOf('*');
            return typedValue.Substring(indexOfLastStar + 1).Trim();
        }

        public bool ReadBool(string keyName, string valueName, bool defaultValue)
        {
            return bool.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        public int ReadInt(string keyName, string valueName, int defaultValue)
        {
            return int.Parse(ReadString(keyName, valueName, defaultValue.ToString()));
        }

        private object ReadObject(string keyName, string valueName, object defaultValue)
        {
            object result = Registry.GetValue(keyName, valueName, null);
            return result ?? defaultValue;
        }

    }

}