using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public static class History
    {
        public static Version[] Versions => VersionData.Keys.ToArray();

        public static DateTime GetDate(Version version)
        {
            return VersionData[version];
        }

        private static IDictionary<Version, DateTime> VersionData { get; }

        static History()
        {
            VersionData = new Dictionary<Version, DateTime> {
                { new Version(0, 1, 0, 0), new DateTime(2015, 10, 25) },
                { new Version(0, 2, 0, 0), new DateTime(2015, 11, 15) },
                { new Version(0, 2, 0, 1), new DateTime(2015, 11, 16) },
                { new Version(0, 2, 0, 2), new DateTime(2015, 11, 17) },
                { new Version(0, 2, 0, 3), new DateTime(2016, 1, 7) },
                { new Version(0, 3, 0, 0), new DateTime(2016, 2, 14) },
                { new Version(0, 4, 0, 0), new DateTime(2016, 3, 9) },
                { new Version(0, 4, 0, 1), new DateTime(2016, 3, 15) },
                { new Version(0, 5, 0, 0), new DateTime(2016, 3, 25) },
                { new Version(0, 5, 0, 1), new DateTime(2016, 3, 27) },
                { new Version(0, 6, 0, 0), new DateTime(2016, 5, 4) }
            };
        }

    }

}