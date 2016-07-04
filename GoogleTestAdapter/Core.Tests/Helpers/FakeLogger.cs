using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    public class FakeLogger : LoggerBase
    {
        private readonly IDictionary<Severity, IList<string>> _groupedMessages = new Dictionary<Severity, IList<string>>();
        private readonly IList<string> _allMessages = new List<string>();

        private readonly bool _timeStampLogMessages;

        public IList<string> GetAllMessages()
        {
            return _allMessages;
        }

        public IList<string> GetMessages(Severity severity)
        {
            IList<string> messages;
            if (!_groupedMessages.TryGetValue(severity, out messages))
                return new List<string>();

            return messages;
        }

        public FakeLogger(bool timestampLogMessages = true)
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