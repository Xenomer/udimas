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
        public static bool PluginExists(string accessor)
        {
            return plugins.ContainsKey(accessor);
        }
        public static Plugin GetPlugin(string accessor)
        {
            return plugins[accessor];
        }
    }

    /// <summary>
    /// Plugin used by <see cref="PluginHub"/>
    /// </summary>
    public class Plugin
    {
        public string Name;
        public readonly ReadOnlyDictionary<string, dynamic> Properties;
        public Plugin(string name, Dictionary<string, dynamic> props)
        {
            Name = name;
            Properties = new ReadOnlyDictionary<string, dynamic>(props);
        }
        public Plugin(string name) : this(name, new Dictionary<string, dynamic>()) { }
    }
}
