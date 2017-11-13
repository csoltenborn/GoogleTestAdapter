using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GoogleTestAdapter.Common
{
    public static class Win32Utils
    {
        public static string GetLastWin32Error()
        {
            int errorCode = Marshal.GetLastWin32Error();
            string errorMessage = new Win32Exception(errorCode).Message;
            return string.IsNullOrWhiteSpace(errorMessage)
                ? $"LastWin32Error={errorCode}"
                : $"{errorCode}: {errorMessage}";
        }
    }
}