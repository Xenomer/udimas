using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    public class LogEventArgs
    {
        public string Data { get; private set; }
        public string Source { get; private set; }
        public LogLevel Level { get; private set; }
        public DateTime Timestamp { get; private set; }

        internal LogEventArgs(string data, string src, LogLevel level, DateTime timestamp)
        {
            Data = data;
            Source = src;
            Level = level;
            Timestamp = timestamp;
        }
    }
}
