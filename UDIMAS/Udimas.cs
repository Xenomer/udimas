using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// static class containing utility information about UDIMAS and helper methods
    /// </summary>
    public static class Udimas
    {
        /// <summary>
        /// Gets the time UDIMAS started
        /// </summary>
        internal static DateTime StartTime;

        /// <summary>
        /// Raises when UDIMAS is shutting down
        /// </summary>
        public static event Action ShuttingDown;
        internal static void OnShuttingDown()
        {
            ShuttingDown?.Invoke();
        }

        /// <summary>
        /// Raises when boot has completed
        /// </summary>
        public static event Action BootComplete;
        internal static void OnBootComplete()
        {
            BootComplete?.Invoke();
        }

        /// <summary>
        /// Delegate for <see cref="LogHappened"/>
        /// </summary>
        /// <param name="args"></param>
        public delegate void LogEventHandler(LogEventArgs args);

        /// <summary>
        /// Raises when a log raises
        /// </summary>
        public static event LogEventHandler LogHappened;
        internal static void OnLogHappened(LogEventArgs args)
        {
            LogHappened?.Invoke(args);
        }

        /// <summary>
        /// Raises when a log in a plugin raises
        /// </summary>
        public static event LogEventHandler PluginLogHappened;
        internal static void OnPluginLogHappened(LogEventArgs args)
        {
            PluginLogHappened?.Invoke(args);
        }

        /// <summary>
        /// Gets the system uptime
        /// </summary>
        public static TimeSpan SystemUptime { get { return DateTime.Now - StartTime; } }

        /// <summary>
        /// Gets the booting time
        /// </summary>
        public static TimeSpan BootTime { get; internal set; }

        /// <summary>
        /// Gets the directory UDIMAS executable is in
        /// </summary>
        public static string SystemDirectory { get { return new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName; } }

        /// <summary>
        /// Exits UDIMAS asynchronously
        /// </summary>
        public static void Exit()
        {
            Task.Run(() => Core.Exit());
        }

        /// <summary>
        /// Restarts UDIMAS asynchronously
        /// </summary>
        public static void Restart()
        {
            Task.Run(() => Core.Restart());
        }

        /// <summary>
        /// gets whether the operating system is currently a Linux distribution
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        /// <summary>
        /// Gets global settings. these are automatically saved to settings.json and loaded at startup
        /// </summary>
        public static readonly dynamic Settings = new Settings();

        /// <summary>
        /// Executes <paramref name="func"/>. Returns when either <paramref name="func"/> returns or timeout occurs.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">Function</param>
        /// <param name="timeout">Specified timeout</param>
        /// <param name="result">return value of <paramref name="func"/></param>
        /// <returns>true if function returned, false if timeout occurred</returns>
        public static bool TryExecute<T>(Func<T> func, int timeout, out T result)
        {
            var t = default(T);
            var thread = new System.Threading.Thread(() => t = func());
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed) thread.Abort();
            result = t;
            return completed;
        }

        /// <summary>
        /// Executes <paramref name="func"/>. Returns when either <paramref name="func"/> returns or timeout occurs.
        /// </summary>
        /// <param name="func">Action</param>
        /// <param name="timeout">Specified timeout</param>
        /// <returns>true if function returned, false if timeout occurred</returns>
        public static bool TryExecute(Action func, int timeout)
        {
            var thread = new System.Threading.Thread(() => func());
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed) thread.Abort();
            return completed;
        }

        /// <summary>
        /// Gets whether the system is currently exiting
        /// </summary>
        public static bool IsExiting { get; internal set; }

        /// <summary>
        /// Gets commands ignored by CmdInterpreter
        /// </summary>
        internal static string[] IgnoredCommands = new string[0];

        /// <summary>
        /// Checks if the <see cref="Console"/> is present and available to use
        /// </summary>
        public static bool ConsoleExists()
        {
            bool consoleExists = true;
            // check if console exists
            try
            {
                Console.WindowWidth.ToString();
            }
            catch
            {
                consoleExists = false;
            }
            return consoleExists;
        }
    }
}
