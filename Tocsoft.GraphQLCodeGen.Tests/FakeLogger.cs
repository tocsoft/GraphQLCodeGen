using System;
using System.Collections.Generic;
using System.Linq;
using Tocsoft.GraphQLCodeGen.Cli;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class FakeLogger : ILogger
    {
        public bool HasErrors => logs?.Any(c => c.IsError) == true;
        public bool HasMessages => logs?.Any() == true;

        private List<LogEntry> logs = new List<LogEntry>();

        public IEnumerable<string> ErrorMessages => logs?.Where(x => x.IsError).Select(x => x.Message).ToArray() ?? Array.Empty<string>();

        public void Error(string str)
        {
            logs = logs ?? new List<LogEntry>();
            var msg = new LogEntry(str, true);
            logs.Add(msg);
        }

        public void Message(string str)
        {
            logs = logs ?? new List<LogEntry>();
            logs.Add(new LogEntry(str, false));
        }

        public struct LogEntry
        {
            public LogEntry(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
            public string Message { get; }
            public bool IsError { get; }
        }
    }
}
