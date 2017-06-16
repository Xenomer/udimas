using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    internal static class InterpreterCommands
    {
        public static (int, string) Help(TextWriter tw, string[] args)
        {
            tw.Write("[ ");
            tw.Write(string.Join(", ", CmdInterpreter._commands.Keys));
            tw.WriteLine(" ]");
            return (0, "");
        }
        public static (int, string) Plugins(TextWriter tw, string[] args)
        {
            if (CmdInterpreter.IsWellFormatterArguments(args, "-l|--list"))
            {
                foreach(Plugin plugin in PluginHub.plugins.Values)
                {
                    tw.WriteLine(plugin.Name);
                }
            }
            else if (CmdInterpreter.IsWellFormatterArguments(args, "-f|--full"))
            {
                foreach (Plugin plugin in PluginHub.plugins.Values)
                {
                    tw.WriteLine(plugin.Name);
                    foreach(KeyValuePair<string, dynamic> kvp in plugin.Properties)
                    {
                        tw.WriteLine($"  {kvp.Key} ({(kvp.Value?.GetType()).ToString()})");
                    }
                }
            }
            else if(CmdInterpreter.IsWellFormatterArguments(args, "-p|--plugin", "\\w+"))
            {
                string plugin = args[1];
                if (!PluginHub.PluginExists(plugin)) return (1, "Plugin does not exist.");

                Plugin p = PluginHub.GetPlugin(plugin);
                tw.WriteLine("Plugin name: " + p.Name);
                tw.WriteLine("Plugin properties");
                foreach (KeyValuePair<string, dynamic> kvp in p.Properties)
                {
                    tw.WriteLine($"  {kvp.Key} ({(kvp.Value?.GetType()).ToString()})");
                }
            }
            else if(CmdInterpreter.IsWellFormatterArguments(args, "-h|--help"))
            {
                tw.WriteLine("Shows information or lists installed plugins.");
                tw.WriteLine("Usage:");
                tw.WriteLine(" -l/--list\tlist all installed plugins");
                tw.WriteLine(" -f/--full\tlist all installed plugins and their properties");
                tw.WriteLine(" -p/--plugin [plugin]\tshow information about a specified plugin");
            }
            else
            {
                return (1, "-h or --help for info");
            }
            return (0, "");
        }
        public static (int, string) Udimas(TextWriter tw, string[] args)
        {
            string[] data =
            {
                $"UDIMAS v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}, made by Xenomer",
                "System time: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"),
                "",
                "Online time: " + UDIMAS.Udimas.SystemUptime.ToString("hh\\:mm\\:ss"),
                "System boot time: " + UDIMAS.Udimas.BootTime.ToString(),
                "",
                $"Plugins loaded ({PluginHub.plugins.Count} registered): {Core.ExtPluginsLoaded} external, {Core.ScriptPluginsLoaded} scripts",

            };
            tw.WriteLine(string.Join(tw.NewLine, data));

            return (0, "");
        }
        public static (int, string) Settings(TextWriter tw, string[] args)
        {
            if (CmdInterpreter.IsWellFormatterArguments(args, "-l|--list"))
            {
                foreach (KeyValuePair<string, object> obj in (UDIMAS.Udimas.Settings as Settings).dictionary)
                {
                    tw.WriteLine($"{obj.Key}={(obj.Value is string ? $"\"{obj.Value}\"" : obj.Value.ToString())}");
                }
            }
            else if (CmdInterpreter.IsWellFormatterArguments(args, "-s", @"\w+", @"\S+"))
            {
                object obj = args[2];
                if (args[2].StartsWith("bool|") && bool.TryParse(args[2].Substring(5), out bool bres))
                    obj = bres;
                else if (args[2].StartsWith("int|") && int.TryParse(args[2].Substring(4), out int ires))
                    obj = ires;

                var settings = UDIMAS.Udimas.Settings as Settings;
                settings.dictionary[args[1]] = obj;
                settings.Save();
            }
            else if (CmdInterpreter.IsWellFormatterArguments(args, "-h|--help"))
            {
                CmdInterpreter.PrintLines(tw, new string[] {
                    "Lists or edits UDIMAS settings",
                    "Usage:",
                    "  settings -l\tlists all settings",
                    "  settings -s\tsets a value to a setting",
                    "",
                    "",
                    "When setting a value to a setting, you can use bool and int values by prefixing actual value with 'bool|' or 'int|'."
                });
            }
            else
            {
                return (CmdInterpreter.INVALIDARGUMENTS, "-h for help");
            }
            return (0, "");
        }
    }
}
