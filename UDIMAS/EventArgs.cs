using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// Arguments for events about logs
    /// </summary>
    public class LogEventArgs
    {
        /// <summary>
        /// Data of the log
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// Source of the log (the name of logger that sent the log)
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Level of this log
        /// </summary>
        public LogLevel Level { get; private set; }

        /// <summary>
        /// Time this log was raised
        /// </summary>
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
