using System;

namespace GoogleTestAdapter.Tests.Common
{

    public enum VsVersion { VS2012 = 11, VS2013 = 12, VS2015 = 14, VS2017 = 15 }

    public static class VsVersionExtensions
    {
        public static int Year(this VsVersion version)
        {
            switch (version)
            {
                case VsVersion.VS2012:
                    return 2012;
                case VsVersion.VS2013:
                    return 2013;
                case VsVersion.VS2015:
                    return 2015;
                case VsVersion.VS2017:
                    return 2017;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

}