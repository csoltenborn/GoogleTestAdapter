using System;

namespace GoogleTestAdapter.Tests.Common.Helpers
{

    public static class CiSupport
    {

        public static bool IsRunningOnBuildServer => Environment.GetEnvironmentVariable("APPVEYOR") != null;
        private static double Weight => IsRunningOnBuildServer ? 2.5 : 1;


        public static int GetWeightedDuration(double duration)
        {
            return (int)Math.Round(duration * Weight);
        }

    }

}