using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162

namespace GoogleTestAdapter.Helpers
{
    static class DebugUtils
    {
        private static bool DebugMode = false;

        internal static void LogDebugMessage(IMessageLogger logger, TestMessageLevel level, string message)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (DebugMode)
            {
                logger.SendMessage(level, message);
            }
        }

        internal static void LogUserDebugMessage(IMessageLogger logger, AbstractOptions options, TestMessageLevel level, string message)
        {
            if (options.UserDebugMode)
            {
                logger.SendMessage(level, message);
            }
        }

        internal static void CheckDebugModeForExecutionCode(IMessageLogger logger = null)
        {
            CheckDebugMode("GTA: Test execution code", logger);
        }

        internal static void CheckDebugModeForDiscoverageCode(IMessageLogger logger = null)
        {
            CheckDebugMode("GTA: Test discoverage code", logger);
        }

        private static void CheckDebugMode(string codeType, IMessageLogger logger = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (DebugMode)
            {
                string message = "GTA: " + codeType + " is running on the process with id " + Process.GetCurrentProcess().Id;
                logger?.SendMessage(TestMessageLevel.Informational, message);
                if (!Constants.UnitTestMode)
                {
                    MessageBox.Show(message + ". Attach debugger if necessary, then click ok.",
                        "Attach debugger", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }
            }
        }

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

#pragma warning restore 162