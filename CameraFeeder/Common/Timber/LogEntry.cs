using System;

namespace Feeder.Common.Timber
{
    public class LogEntry
    {
        public DateTime DateTime { get; set; }

        public string DateTimeString { get { return DateTime.ToShortTimeString(); } }

        public string Module { get; set; }

        public string Message { get; set; }
    }
}
