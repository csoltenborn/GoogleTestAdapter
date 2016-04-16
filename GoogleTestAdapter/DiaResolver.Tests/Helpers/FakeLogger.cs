using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver.Helpers
{
    public class FakeLogger : LoggerBase
    {
        public IList<string> Infos { get; } = new List<string>();
        public IList<string> Warnings { get; } = new List<string>();
        public IList<string> Errors { get; } = new List<string>();

        public IList<string> All => Infos.Concat(Warnings).Concat(Errors).ToList();

        public IList<string> MessagesOfType(params Severity[] severities)
        {
            var messages = new List<string>();
            if (severities.Contains(Severity.Info))
                messages.AddRange(Infos);
            if (severities.Contains(Severity.Warning))
                messages.AddRange(Warnings);
            if (severities.Contains(Severity.Error))
                messages.AddRange(Errors);
            return messages;
        }

        public override void Log(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Info:
                    Infos.Add(message);
                    break;
                case Severity.Warning:
                    Warnings.Add(message);
                    break;
                case Severity.Error:
                    Errors.Add(message);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}