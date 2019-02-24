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
<b>Update (2/24/2019):</b> This is most likely the final version with support for VS2012. No more donations so far - I guess I have reached my potential (allthough not my donation goals), so I'm not going to annoy you any further. Thanks once more to the donators - I've really appreciated it!
<br>
<br>
<b>Update (2/5/2019):</b> I have again received some (and quite generous) donations - thanks a lot to Treb and Sebastian! Still not at my donation goal, though... If you happen to work with GTA in a professional manner (e.g. in a company which uses GTA to execute the C++ tests, be it within Visual Studio or on the build server), may I ask you to reconsider donating? If you need some more motivation, note that my birthday was just a couple of days ago ;-)
<br>
<br>
<b>Update (12/16/2019):</b> Welcome again to my donation weekly soap :-) Responds to my last request for donations were quite a bit better and included some rather generous ones, but still a way to go until my donation goals are met. Thanks a lot to Yehuda, Walter, Thomas, Lewis, Greg, and my colleague Benedikt! I loved to hear that GTA just works for you and is indeed quite helpful.
<br>
<br>
<b>Update (12/09/2018):</b> Given the fact that I have a couple of thousands users, I must admit that I have been unpleasantly surprised by the amount of donations I received (thanks, Tim and
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