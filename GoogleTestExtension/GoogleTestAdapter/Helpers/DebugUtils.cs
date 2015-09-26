using System;

namespace GoogleTestAdapter.Helpers
{
    static class DebugUtils
    {
        internal static void AssertIsNotNull(object parameter, string parameterName)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        internal static void AssertIsNull(object parameter, string parameterName)
        {
            if (parameter != null)
            {
                throw new ArgumentException(parameterName + " must be null");
            }
        }

    }

}