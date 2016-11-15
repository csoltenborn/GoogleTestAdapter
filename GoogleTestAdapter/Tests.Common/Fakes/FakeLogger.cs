using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Tests.Common.Fakes
{
    public class FakeLogger : LoggerBase
    {
        private readonly IDictionary<Severity, IList<string>> _groupedMessages = new Dictionary<Severity, IList<string>>();
        private readonly IList<string> _allMessages = new List<string>();

        private readonly bool _timeStampLogMessages;

        public IList<string> Infos => MessagesOfType(Severity.Info);
        public IList<string> Warnings => MessagesOfType(Severity.Warning);
        public IList<string> Errors => MessagesOfType(Severity.Error);

        public IList<string> All => _allMessages;

        public IList<string> MessagesOfType(params Severity[] severities)
        {
            return _groupedMessages.Where(p => severities.Contains(p.Key)).SelectMany(p => p.Value).ToList();
        }

        public FakeLogger(Func<bool> inDebugMode, bool timestampLogMessages = true)
            : base(inDebugMode)
        {
            _timeStampLogMessages = timestampLogMessages;
        }

        public override void Log(Severity severity, string message)
        {
            if (_timeStampLogMessages)
                Utils.TimestampMessage(ref message);

            lock (this)
            {
                IList<string> messageGroup;
                if (!_groupedMessages.TryGetValue(severity, out messageGroup))
                    _groupedMessages.Add(severity, messageGroup = new List<string>());

                _allMessages.Add(message);
                messageGroup.Add(message);
            }
        }

    }

}