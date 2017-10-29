using IronPython.Hosting;
using log4net.Core;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log.config", Watch = true)]
namespace UDIMAS
{
    internal static class Boot
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Default console interpreter
        /// </summary>
        private static CmdInterpreter ConsoleInterpreter;

        /// <summary>
        /// Additional user-specified path for plugins
        /// </summary>
        private static string PluginPath = null;

        /// <summary>
        /// User-specified ignored plugin names
        /// </summary>
        private static List<string> IgnoredPlugins = new List<string>();

        /// <summary>
        /// User-specified bool whether to not start <see cref="ConsoleInterpreter"/>
        /// </summary>
        private static bool NoInterpreter = false;

        /// <summary>
        /// Entry point for UDIMAS executable
        /// </summary>
        private static void Main()
        {
            Core.SetTitle("Booting...");

            //if debugger is not attached, catch unhandled exceptions
            if (!Debugger.IsAttached)
                AppDomain.CurrentDomain.UnhandledException += (s, a) =>
                {
                    Exception e = (Exception)a.ExceptionObject;
                    string parentClass = e.TargetSite.DeclaringType.Name;
                    string method = e.TargetSite.Name;
                    string message = e.Message;
                    string stackTrace = e.StackTrace.ToString();

                    log.Fatal($"Fault happened in {parentClass}.{method}");
                    log.Fatal($" * Exception message: '{message}'");
                    log.Fatal($" * Stack trace:\r\n{stackTrace}");
                    System.Threading.Thread.Sleep(2000);

                    log.Info($"Trying to shutdown normally...");
                    System.Threading.Thread.Sleep(500);

                    bool success = true;
                    try
                    {
                        if (!Udimas.TryExecute(Core.Exit, 2000)) success = false;
                    }
                    catch
                    {
                        success = false;
                    }
                    if (!success)
                    {
                        log.Error("Normal shutdown failed. Force shutdowning.");
                        System.Threading.Thread.Sleep(1000);
                        Process proc = Process.GetCurrentProcess();
                        proc.Kill();
                    }
                };

            Udimas.IsExiting = false;
            LoadArguments();

            log.Warn("Booting UDIMAS..");

            var bootTime = new Stopwatch();
            bootTime.Start();

            //set handler to handle unhandled shutdown
            if (!Udimas.IsLinux)
            {
                Core._handler += (s) => { Core.Exit(); return false; };
                Core.SetConsoleCtrlHandler(Core._handler, true);
                log.Debug("Succesfully set handler for unattended exit");
            }
            else log.Warn("Could not set handler for unattended exit because OS is a Linux distribution");

            LoadInterpreter();
            LoadPlugins();

            Udimas.StartTime = DateTime.Now;
            bootTime.Stop();
            Udimas.BootTime = bootTime.Elapsed;

            log.Info("Boot complete. Time: " + Udimas.BootTime.ToString());
            Core.SetTitle("");

            Udimas.OnBootComplete();

            StartConsole();
        }

        private static void LoadArguments()
        {
            string ParameterQuery = "\\-(?<key>\\w+)(\\s*=\\s*(\"(?<value>[^\"]*)\"|(?<value>[^\\-]*))|)\\s*";
            Dictionary<string, string> ParseString(string value)
            {
                Dictionary<string, string> items = new Dictionary<string, string>();
                var regex = new Regex(ParameterQuery);
                foreach (Match m in regex.Matches(value).Cast<Match>())
                    if (!items.ContainsKey(m.Groups["key"].Value))
                        items.Add(m.Groups["key"].Value, m.Groups["value"].Value);
                return items;
            }
            var args = new Dictionary<string, string>(ParseString(Environment.CommandLine), StringComparer.InvariantCultureIgnoreCase);

            

            // parameters here
            if (args.ContainsKey("PluginDir")) PluginPath = args["PluginDir"].Trim();
            if (args.ContainsKey("PluginIgnore")) IgnoredPlugins.AddRange(args["PluginIgnore"].Trim().Split(';'));
            if (args.ContainsKey("NoInterpreter")) NoInterpreter = true;
            if (args.ContainsKey("LogLevel"))
            {
                Level level = Level.Emergency;
                switch (args["LogLevel"].Trim().ToUpper())
                {
                    case "ALL":
                        level = Level.All;
                        break;
                    case "DEBUG":
                        level = Level.Debug;
                        break;
                    case "INFO":
                        level = Level.Info;
                        break;
                    case "WARN":
                        level = Level.Warn;
                        break;
                    case "ERROR":
                        level = Level.Error;
                        break;
                    case "FATAL":
                        level = Level.Fatal;
                        break;
                    case "OFF":
                        level = Level.Off;
                        break;
                }
                if (level != Level.Emergency) {
                    Core.SetLogLevel(level);
                }
            }
            if (args.ContainsKey("CmdDisable"))
            {
                Udimas.IgnoredCommands = args["CmdDisable"].Trim().Split(';');
            }
        }

        private static void LoadInterpreter()
        {
            log.Info("Initializing UDIMAS interpreter builtin commands..");

            CmdInterpreter.RegisterCommand(new TerminalCommand("echo", (tw, a) => { tw.WriteLine(string.Join(" ", a)); return (0, ""); }));
            CmdInterpreter.RegisterCommand(new TerminalCommand("exit", (tw, a) => { Udimas.Exit(); return (0, ""); }));
            CmdInterpreter.RegisterCommand(new TerminalCommand("restart", (tw, a) => { Udimas.Restart(); return (0, ""); }));
            CmdInterpreter.RegisterCommand(new TerminalCommand("udimas", InterpreterCommands.Udimas));
            CmdInterpreter.RegisterCommand(new TerminalCommand("help", InterpreterCommands.Help));
            CmdInterpreter.RegisterCommand(new TerminalCommand("plugin", InterpreterCommands.Plugins));
            CmdInterpreter.RegisterCommand(new TerminalCommand("settings", InterpreterCommands.Settings));

            log.Debug("Done initializing UDIMAS Interpreter commands");
        }

        private static void LoadPlugins()
        {
            if (IgnoredPlugins.Count >= 1 &&
                IgnoredPlugins[0] == "*") return;

            log.Info("Loading plugins..");
            string dirPath = Path.Combine(Udimas.SystemDirectory, "plugins");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            LoadPluginsFromDir(dirPath);

            if(PluginPath != null 
                && !string.IsNullOrWhiteSpace(PluginPath))
            {
                if (PluginPath.StartsWith("~")) {
                    string str = Regex.Match(PluginPath, @"^~[/\\]?(.+)$").Groups[1].Value;
                    LoadPluginsFromDir(Path.Combine(Udimas.SystemDirectory, str));
                }
                else LoadPluginsFromDir(PluginPath);
            }
        }

        private static void LoadPluginsFromDir(string pluginDirectory)
        {
            try
            {   // path is not well formed windows folder path
                Path.GetFullPath(pluginDirectory);
                if (!Path.IsPathRooted(pluginDirectory))
                {
                    throw new Exception();
                }

                if (!Directory.Exists(pluginDirectory)) throw new Exception();
            }
            catch { log.Error("Error loading plugins from directory " + pluginDirectory); return; }
            
            string relative = pluginDirectory.StartsWith(Udimas.SystemDirectory) ? 
                pluginDirectory.Substring(Udimas.SystemDirectory.Length+1) : pluginDirectory;
            log.Debug($"Loading external plugins from {relative}..");

            int count = 0;

            //enumerate all executable and dll files in /plugins/
            foreach (string dll in Directory
                                    .EnumerateFiles(pluginDirectory, "*", SearchOption.AllDirectories)
                                    .Where(file => file.ToLower().EndsWith("dll") || file.ToLower().EndsWith("exe")))
            {
                if (IgnoredPlugins.Contains(new FileInfo(dll).Name)) continue;

                //load the assembly
                Assembly a = Assembly.LoadFrom(dll);
                log.Debug($"Loading {a.GetName().Name}..");

                try
                {
                    //enumerate all classes that implements IPlugin interface
                    foreach (TypeInfo module in a.DefinedTypes.Where(
                        ti => ti.IsClass && !ti.IsAbstract && ti.IsSubclassOf(typeof(UdimasExternalPlugin))))
                    {
                        count++;
                        log.Info($"Loading {module.Name}..");

                        var inst = (UdimasExternalPlugin)Activator.CreateInstance(module.AsType());
                        try
                        {
                            inst.Initialize();
                            inst.Run();
                        }
                        catch (Exception e) { log.Error($" * Run() threw: '{e.Message}'"); }
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Exception while enumerating types on {a.GetName().Name}: '{e.Message}'");
                }
            }
            log.Debug($"Loaded {count} external plugins");
            Core.ExtPluginsLoaded += count;

            count = 0;
            var engine = Python.CreateEngine();

            //enumerate all .py files in /plugins/
            foreach (string sfile in Directory
                                    .EnumerateFiles(pluginDirectory, "*", SearchOption.AllDirectories)
                                    .Where(file => file.ToLower().EndsWith(".py")))
            {
                string name = new FileInfo(sfile).Name;
                if (IgnoredPlugins.Contains(name)) { log.Info("Ignoring plugin " + name); continue; }
                count++;
                log.Info($"Loading {name}..");

                ScriptScope scope = engine.CreateScope();
                engine.Execute("import clr; clr.AddReference('UDIMAS'); from UDIMAS import *", scope);

                try
                {
                    engine.ExecuteFile(sfile, scope);
                }
                catch (Microsoft.Scripting.SyntaxErrorException e)
                {
                    log.Error($"Error in {name} (line {e.Line}, column {e.Column}): {e.Message}");
                    log.Error("  " +e.SourceCode.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[e.Line - 1].TrimStart());
                }
                catch(Exception e)
                {
                    log.Error(e.GetType().ToString());
                    log.Error($"Error in {new FileInfo(sfile).Name}: {e.Message}");
                }
            }

            log.Debug($"Loaded {count} script plugins");
            Core.ScriptPluginsLoaded += count;
        }

        private static void StartConsole()
        {
            if (NoInterpreter)
            {
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }

            log.Info("Initializing console interpreter..");

            bool consoleExists = Udimas.ConsoleExists();

            if (consoleExists)
            {
                ConsoleInterpreter = new CmdInterpreter("Console", new ConsoleIOPipeline());
                log.Debug("Console interpreter initialization complete");

                Console.WriteLine($"\nUDIMAS v{Assembly.GetExecutingAssembly().GetName().Version}");
                while (true)
                {
                    Udimas.Settings.interpreterprefix = Udimas.Settings.interpreterprefix ?? "-> ";
                    Console.Write(Udimas.Settings.interpreterprefix);
                    string cmd = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(cmd) && Console.CursorTop > 0)
                    {
                        Console.CursorTop = Console.CursorTop - 1;
                        continue;
                    }
                    ConsoleInterpreter.UserInput(cmd.Split(' ')[0], cmd.Split(' ').Skip(1).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

                    Console.WriteLine();
                }
            }
        }

    }
}
