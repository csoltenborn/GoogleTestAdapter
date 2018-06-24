using System;

namespace GoogleTestAdapter.VsPackage.GTA.ReleaseNotes
{
    public static class Donations
    {
        public static readonly Version Version = new Version(0, 13, 0);
        public static readonly Uri Uri = new Uri("https://github.com/csoltenborn/GoogleTestAdapter#donations");

        public const string Note = @"**On my own account:**
In the last couple of months, I noticed that my private laptop certainly has a finite lifetime. Thinking about the requirements a new one has to stand up to, I realized that developing and supporting *Google Test Adapter* has in the last years been one of the major use cases of that laptop. Thus, I decided to take this as reason for from now on accepting donations :-)

Therefore, if you would like to appreciate development and support of *Google Test Adapter*, **please consider to [donate!](https://github.com/csoltenborn/GoogleTestAdapter#donations)** Thanks in advance...
<br>
<br>";

        public const string Footer = @"
<br>
<br>
<center><small>Please consider to <b><a href=""https://github.com/csoltenborn/GoogleTestAdapter#donations"">donate!</a></b></small></center>";

        public static bool IsPreDonationsVersion(Version formerVersion)
        {
            return formerVersion == null || formerVersion < Version;
        }
    }
}
