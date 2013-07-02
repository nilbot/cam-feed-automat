using System.Collections.Generic;

namespace Feeder.Common.Timber
{
    public class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }
    }
}