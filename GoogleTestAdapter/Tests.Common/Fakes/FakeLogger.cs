using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Tests.Common.Fakes
{
    public class FakeLogger : LoggerBase
    {
        private readonly bool _timeStampLogMessages;

        public IList<string> Infos => GetMessages(Severity.Info);
        public IList<string> Warnings => GetMessages(Severity.Warning);
        public IList<string> Errors => GetMessages(Severity.Error);

        public IList<string> All => GetMessages(Enum.GetValues(typeof(Severity)).Cast<Severity>().ToArray());

        public FakeLogger() : this(() => OutputMode.Verbose, false) { }

        public FakeLogger(Func<OutputMode> outputMode, bool timestampLogMessages = true)
            : base(outputMode)
        {
            _timeStampLogMessages = timestampLogMessages;
        }

        public override void Log(Severity severity, string message)
        {
            if (_timeStampLogMessages)
            {
                message = $"{Utils.GetTimestamp()} - {message}";
            }

            lock (this)
            {
                ReportFinalLogEntry(new LogEntry {Severity = severity, Message = message});
            }
        }

    }

}