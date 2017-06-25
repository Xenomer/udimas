using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// UDIMAS external (.NET dll/exe) plugin. Put this into your plugin and throw it into /plugins/ folder
    /// </summary>
    [IronPython.Runtime.PythonHidden]
    public abstract class UdimasExternalPlugin
    {
        /// <summary>
        /// Instantiates Udimas plugin instance
        /// </summary>
        public UdimasExternalPlugin() { }

        /// <summary>
        /// Gets the name of this external plugin
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Whether to automatically register plugin to <see cref="PluginHub"/> database
        /// </summary>
        protected virtual bool RegisterPlugin { get { return true; } }

        /// <summary>
        /// Properties used when registering plugin
        /// </summary>
        protected virtual Dictionary<string, dynamic> PluginProperties { get { return new Dictionary<string, dynamic>(); } }

        /// <summary>
        /// Method that can be used for logging
        /// </summary>
        protected Action<LogLevel, string> Log;

        /// <summary>
        /// Runs when this plugin is initialized
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Runs when this plugin is stopping
        /// </summary>
        public virtual void Stop() { }

        /// <summary>
        /// Executes internal plugin initialization
        /// </summary>
        internal void Initialize()
        {
            if (RegisterPlugin)
            {
                var plugin = new Plugin(Name, PluginProperties);
                Log = PluginHub.RegisterPlugin(plugin);
                PluginHub.extPlugins.Add(this);
            }
        }
    }
}
