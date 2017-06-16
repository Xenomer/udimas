using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    class PluginLogger : AppenderSkeleton
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        bool error = false;

        protected override void Append(LoggingEvent loggingEvent)
        {
            string plugin = loggingEvent.LoggerName.Split('.')[1],
                level = loggingEvent.Level.ToString();
            Udimas.OnPluginLogHappened(
                new LogEventArgs(
                    loggingEvent.RenderedMessage,
                    plugin,
                    (LogLevel)Enum.Parse(typeof(LogLevel), loggingEvent.Level.Name),
                    loggingEvent.TimeStamp)
                    );

            if (error) return;
            try
            {
                File.AppendAllText($"./log/{plugin}.txt",
                    $"{loggingEvent.TimeStamp.ToString("yyyy\\-MM\\-dd HH\\:mm\\:ss\\,fff")} [{loggingEvent.ThreadName}]" +
                    $" {level} - {loggingEvent.RenderedMessage}{Environment.NewLine}");
            }
            catch(Exception e)
            {
                error = true;
                log.Error($"Error on plugin '{plugin}': {e.Message}");
            }
        }
    }

    class EventLogger : AppenderSkeleton
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override void Append(LoggingEvent loggingEvent)
        {
            Udimas.OnLogHappened(
                new LogEventArgs(
                    loggingEvent.RenderedMessage,
                    loggingEvent.LoggerName,
                    (LogLevel)Enum.Parse(typeof(LogLevel), loggingEvent.Level.Name),
                    loggingEvent.TimeStamp)
                    );
        }
    }
}
