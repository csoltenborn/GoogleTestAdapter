using System;
using System.Collections.Generic;
namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {
        public const string ExecutorUriString = "executor://GoogleTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
    }

}