using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleTestAdapter.Helpers
{

    public static class Utils
    {

        public static string GetTempDirectory()
        {
            string tempDirectory;
            do
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(tempDirectory) || File.Exists(tempDirectory));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static bool DeleteDirectory(string directory, out string errorMessage)
        {
            try
            {
                Directory.Delete(directory, true);
                errorMessage = null;
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        public static void AssertIsNotNull(object parameter, string parameterName)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void AssertIsNull(object parameter, string parameterName)
        {
            if (parameter != null)
            {
                throw new ArgumentException(parameterName + " must be null");
            }
        }

        /// <exception cref="AggregateException">If at least one of the actions has thrown an exception</exception>
        public static bool SpawnAndWait(Action[] actions, int timeoutInMs = Timeout.Infinite)
        {
            var tasks = new Task[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(actions[i]);
            }
      
            return Task.WaitAll(tasks, timeoutInMs);
        }

    }

}