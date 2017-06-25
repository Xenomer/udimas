using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UDIMAS
{
    /// <summary>
    /// Allows communication and method/property calling between plugins
    /// </summary>
    public static class PluginHub
    {
        internal static Dictionary<string, Plugin> plugins = new Dictionary<string, Plugin>();
        internal static List<UdimasExternalPlugin> extPlugins = new List<UdimasExternalPlugin>();

        /// <summary>
        /// Registers a new plugin into <see cref="PluginHub"/>
        /// </summary>
        /// <param name="plugin">plugin that is registered</param>
        /// <returns>Logging delegate thats logs are directed into its own file</returns>
        public static Action<LogLevel, string> RegisterPlugin(Plugin plugin)
        {
            if ((new string((from c in plugin.Name where char.IsLetterOrDigit(c) select c).ToArray())) != plugin.Name)
                throw new ArgumentException("plugin.Name contains illegal characters.", "plugin.Name");

            plugins.Add(plugin.Name, plugin);
            return (l, s) => {
                var logger = log4net.LogManager.GetLogger("plugin." + plugin.Name);
                switch (l)
                {
                    case LogLevel.DEBUG:
                        logger.Debug(s);
                        break;
                    case LogLevel.INFO:
                        logger.Info(s);
                        break;
                    case LogLevel.WARN:
                        logger.Warn(s);
                        break;
                    case LogLevel.ERROR:
                        logger.Error(s);
                        break;
                    case LogLevel.FATAL:
                        logger.Fatal(s);
                        break;
                }
            };
        }

        /// <summary>
        /// Checks if a plugin with a specific name exists
        /// </summary>
        /// <param name="accessor">name of the plugin</param>
        /// <returns>True if plugin is registered, otherwise false</returns>
        public static bool PluginExists(string accessor)
        {
            return plugins.ContainsKey(accessor);
        }

        /// <summary>
        /// Gets a plugin registered to <see cref="PluginHub"/>
        /// </summary>
        /// <param name="accessor">name of the plugin</param>
        /// <returns>Plugin retrieved</returns>
        public static Plugin GetPlugin(string accessor)
        {
            if (!PluginExists(accessor)) throw new KeyNotFoundException("accessor does not exist.");
            return plugins[accessor];
        }
    }

    /// <summary>
    /// Plugin used by <see cref="PluginHub"/>
    /// </summary>
    public class Plugin
    {
        /// <summary>
        /// Name of this <see cref="Plugin"/> instance
        /// </summary>
        public string Name;

        /// <summary>
        /// Dictionary containing dynamic user-defined objects
        /// </summary>
        public readonly ReadOnlyDictionary<string, dynamic> Properties;

        /// <summary>
        /// Instantiates this <see cref="Plugin"/> instance with dictionary of user-defined properties
        /// </summary>
        /// <param name="name">Name of this instance</param>
        /// <param name="props">Dictionary containing dynamic user-defined objects</param>
        public Plugin(string name, Dictionary<string, dynamic> props)
        {
            Name = name;
            Properties = new ReadOnlyDictionary<string, dynamic>(props);
        }

        /// <summary>
        /// Instantiates this <see cref="Plugin"/> instance with no user-defined properties
        /// </summary>
        /// <param name="name">Name of this instance</param>
        public Plugin(string name) : this(name, new Dictionary<string, dynamic>()) { }
    }
}
