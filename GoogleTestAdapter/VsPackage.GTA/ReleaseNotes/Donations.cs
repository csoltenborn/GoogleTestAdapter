using System;

namespace GoogleTestAdapter.VsPackage.GTA.ReleaseNotes
{
    public static class Donations
    {
        public static readonly Version Version = new Version(0, 13, 0);
        public static readonly Uri Uri = new Uri("https://github.com/csoltenborn/GoogleTestAdapter#donations");

        public const string Note = @"<b>On my own account:</b>
In the last couple of months, I noticed that my private laptop certainly has a finite lifetime. Thinking about the requirements a new one has to stand up to, I realized that developing and supporting <i>Google Test Adapter</i> has in the last years been one of the major use cases of that laptop. Thus, I decided to take this as reason for from now on accepting donations :-)

Therefore, if you would like to appreciate development and support of <i>Google Test Adapter</i>, <b>please consider to [donate!](https://github.com/csoltenborn/GoogleTestAdapter#donations)</b> Thanks in advance...
<br>
<br>
<b>Update (09/12/2018):</b> Given the fact that I have a couple of thousands users, I must admit that I have been unpleasantly surprised by the amount of donations I received (thanks, Tim and
Arne! I appreciate it more than you might imagine). Thus, I have decided to disable the <i>Do not show release notes</i> option until I have reached my donation goals (sorry for this, Tim and Arne). Please consider to [donate](https://github.com/csoltenborn/GoogleTestAdapter#donations) - and note that Christmas is pretty close ;-)
<br>
<br>
";

        public const string Footer = @"
<br>
<br>
" + ConsiderDonating;

        public const string Header = ConsiderDonating + @"
<br>
<br>
";

        private const string ConsiderDonating = @"<center><small>Please consider to <b><a href=""https://github.com/csoltenborn/GoogleTestAdapter#donations"">donate!</a></b></small></center>";

        public static bool IsPreDonationsVersion(Version formerVersion)
        {
            return formerVersion == null || formerVersion < Version;
        }
    }
}