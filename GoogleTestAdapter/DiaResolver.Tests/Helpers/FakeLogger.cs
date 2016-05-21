using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver.Helpers
{
    public class FakeLogger : LoggerBase
    {
        private readonly IDictionary<Severity, IList<string>> _messages = new Dictionary<Severity, IList<string>>();

        public IList<string> Infos => GetMessages(Severity.Info);
        public IList<string> Warnings => GetMessages(Severity.Warning);
        public IList<string> Errors => GetMessages(Severity.Error);

        public IList<string> All => Infos.Concat(Warnings).Concat(Errors).ToList();

        public IList<string> MessagesOfType(params Severity[] severities)
        {
            return _messages.Where(p => severities.Contains(p.Key)).SelectMany(p => p.Value).ToList();
        }

        public override void Log(Severity severity, string message)
        {
            GetMessages(severity).Add(message);
        }

        private IList<string> GetMessages(Severity severity)
        {
            IList<string> messages;
            if (!_messages.TryGetValue(severity, out messages))
            {
                messages = new List<string>();
                _messages.Add(severity, messages);
            }
            return messages;
        }

    }

}