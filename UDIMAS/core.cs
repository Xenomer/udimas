using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace UDIMAS
{
    internal static class Core
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Raises events and stops plugins
        /// </summary>
        private static void DoInternalShutdown()
        {
            Udimas.IsExiting = true;

            log.Info("Raising events..");
            Udimas.OnShuttingDown();
            log.Debug("Events raised succesfully");

            log.Info("Stopping plugins..");
            PluginHub.extPlugins.ForEach(p => p.Stop());
            log.Debug("Plugins stopped succesfully");
        }

        public static void SetTitle(string title)
        {
            if (Udimas.ConsoleExists())
                Console.Title = 
                    $"UDIMAS v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}{(string.IsNullOrWhiteSpace(title) ? "" : " - " + title)}";
        }

        /// <summary>
        /// Synchronously exits
        /// </summary>
        public static void Exit()
        {
            log.Warn("Exiting UDIMAS..");
            DoInternalShutdown();

            log.Info("Exiting..");
            Environment.Exit(0);
        }

        /// <summary>
        /// Synchronously restarts
        /// </summary>
        public static void Restart()
        {
            log.Warn("Restarting UDIMAS..");

            DoInternalShutdown();

            log.Info("Starting new UDIMAS process..");
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location, Environment.CommandLine);

            log.Info("Exiting..");
            Environment.Exit(0);
        }

        /// <summary>
        /// Gets how many external plugins were loaded
        /// </summary>
        public static int ExtPluginsLoaded = 0;
        
        /// <summary>
        /// Gets how many script plugins were loaded
        /// </summary>
        public static int ScriptPluginsLoaded = 0;

        /// <summary>
        /// sets globally log4net loglevel
        /// </summary>
        public static void SetLogLevel(log4net.Core.Level level)
        {
            Hierarchy hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            var appender = (ColoredConsoleAppender)hierarchy.GetAppenders().Where(a => a is ColoredConsoleAppender && a.Name == "ConsoleAppender").First();
            ((log4net.Filter.LevelRangeFilter)appender.FilterHead).LevelMin = level;
            appender.ActivateOptions();
        }

        //these are for unhandled close handling (eg. ctrl+c, red big button ontop-right)
        #region Console Control Handler stuff
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        public delegate bool EventHandler(CtrlType sig);
        public static EventHandler _handler;
        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        #endregion
    }
}
