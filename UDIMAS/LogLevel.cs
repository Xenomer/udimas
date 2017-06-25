using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// Logging level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug logging level
        /// </summary>
        DEBUG,

        /// <summary>
        /// Info logging level
        /// </summary>
        INFO,

        /// <summary>
        /// Warning logging level
        /// </summary>
        WARN,

        /// <summary>
        /// Error logging level
        /// </summary>
        ERROR,

        /// <summary>
        /// Fatal logging level
        /// </summary>
        FATAL,
    }
}
