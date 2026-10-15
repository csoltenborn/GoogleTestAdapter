using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.VsPackage.Automation
{
    /// <summary>
    /// Collects warnings and errors (including debug ones) as JSON objects with "severity" and "message".
    /// </summary>
    public class CollectingLogger : LoggerBase
    {
        private readonly List<JsonObject> _messages = new List<JsonObject>();

        public CollectingLogger() : base(() => OutputMode.Debug) { }

        public List<JsonObject> Messages
        {
            get
            {
                lock (_messages)
                {
                    return _messages.ToList();
                }
            }
        }

        public bool HasErrors
        {
            get
            {
                lock (_messages)
                {
                    return _hasErrors;
                }
            }
        }
        private bool _hasErrors;

        public override void Log(Severity severity, string message)
        {
            if (severity == Severity.Info)
                return;

            lock (_messages)
            {
                _messages.Add(new JsonObject { { "severity", severity }, { "message", message } });
                _hasErrors |= severity == Severity.Error;
            }
        }
    }
}
