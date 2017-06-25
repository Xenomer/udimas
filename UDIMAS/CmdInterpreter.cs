using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NDesk.Options;

namespace UDIMAS
{
    /// <summary>
    /// An UDIMAS command interpreter
    /// </summary>
    public sealed class CmdInterpreter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly log4net.ILog instLog;

        /// <summary>
        /// Command was executed successfully
        /// </summary>
        public const int SUCCESS = 0;

        /// <summary>
        /// Command was executed with invalid arguments
        /// </summary>
        public const int INVALIDARGUMENTS = 1;

        /// <summary>
        /// Command executed required support which was not supported
        /// </summary>
        public const int NOINPUTSUPPORTED = 2;

        /// <summary>
        /// Command executed had an unspecified error
        /// </summary>
        public const int COMMANDERROR = 3;

        /// <summary>
        /// Initializes this <see cref="CmdInterpreter"/> instance with a specified name, command output and whether this instance supports output
        /// </summary>
        /// <param name="name">name of this instance</param>
        /// <param name="output">command output of this instance</param>
        /// <param name="supportsInput">whether this instance supports console input</param>
        public CmdInterpreter(string name, InterpreterIOPipeline output)
        {
            Name = name;
            instLog = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "." + Name);

            Output = output??new ConsoleIOPipeline();
        }

        /// <summary>
        /// Contains all registered commands
        /// </summary>
        internal static Dictionary<string, TerminalCommand> _commands = new Dictionary<string, TerminalCommand>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Contains this instances name
        /// </summary>
        internal string Name;

        /// <summary>
        /// Gets the output <see cref="TextWriter"/>
        /// </summary>
        public readonly InterpreterIOPipeline Output;

        /// <summary>
        /// Globally registers a new command
        /// </summary>
        /// <param name="command">Command to be registered</param>
        public static void RegisterCommand(TerminalCommand command)
        {
            if (Udimas.IgnoredCommands.Contains(command.Command)) return;
            if (_commands.ContainsKey(command.Command))
                return;
            if ((new string((from c in command.Command where char.IsLetterOrDigit(c) select c).ToArray())) != command.Command)
                throw new ArgumentException("command.Command contains illegal characters.", "command.Command");
            else if(command.Command.Length > 15)
                throw new ArgumentException("command.Command is too long.", "command.Command");
            _commands.Add(command.Command, command);
            log.Debug("Registered new command: " + command.Command);
        }

        /// <summary>
        /// Checks whether <paramref name="args"/> matches length of and <paramref name="regexFilters"/>
        /// </summary>
        /// <param name="args">arguments that are checked</param>
        /// <param name="regexFilters">an array containing <see cref="Regex"/> filters. 
        /// Every <paramref name="args"/> item has to match an item in corresponding position in this array</param>
        /// <returns>Whether every <paramref name="args"/> item matches every <paramref name="regexFilters"/>. True if it matches, otherwise false</returns>
        public static bool IsWellFormatterArguments(string[] args, params string[] regexFilters)
        {
            if(args.Length != regexFilters.Length)
            {
                return false;
            }
            else
            {
                for(int i=0; i < args.Length; i++)
                {
                    if(!Regex.IsMatch(args[i], "^" + regexFilters[i] + "$"))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Calls <see cref="Input(string, string[])"/> and prints whether the call was successful.
        /// If the call was not successful, prints error code (with optional message)
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <param name="args">Arguments</param>
        /// <returns>Return code</returns>
        public (int, string) UserInput(string cmd, string[] args)
        {
            (int code, string msg) r = Input(cmd, args);
            Output.WriteLine();
            if (r.code == 0)
            {
                Output.WriteLine("Command completed successfully. (0)");
            }
            else
            {
                string postfix = "";
                if (!string.IsNullOrWhiteSpace(r.msg)) postfix = ": " + r.msg;
                Output.WriteLine($"Command completed with code {r.Item1} ({GetErrorName(r.Item1)}){postfix}");
            }
            return r;
        }

        /// <summary>
        /// Gets userfriendly error message of an error code
        /// </summary>
        /// <returns>string representation</returns>
        public static string GetErrorName(int code)
        {
            switch (code)
            {
                case -1:
                    return "Command not found";
                case 0:
                    return "Successful";
                case 1:
                    return "Invalid arguments";
                case 2:
                    return "Input not supported";
                case 3:
                    return "Runtime error occurred";
            }
            return "";
        }

        /// <summary>
        /// Executes a command with specified arguments
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <param name="args">Arguments</param>
        /// <returns>Return code</returns>
        public (int, string) Input(string cmd, string[] args)
        {
            if (_commands.ContainsKey(cmd))
            {
                instLog.Debug($"Executing command ('{cmd}', '{string.Join(" ", args)}')");
                (int, string) r;
                if (!Output.SupportsInput && _commands[cmd].UsesInput)
                {
                    r = (NOINPUTSUPPORTED, "");
                }
                else r = _commands[cmd].Method(Output, args);
                instLog.Debug($"Command '{cmd}' returned with code {r.Item1}");
                return r;
            }
            else return (-1, "");
        }

        /// <summary>
        /// Prints all lines in <paramref name="lines"/> into <paramref name="output"/>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="lines"></param>
        public static void PrintLines(InterpreterIOPipeline output, params string[] lines)
        {
            output.WriteLine(string.Join(Environment.NewLine, lines));
        }
        /// <summary>
        /// Prints all lines in <paramref name="lines"/> into <paramref name="output"/>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="lines"></param>
        public static void PrintLines(TextWriter output, params string[] lines)
        {
            output.WriteLine(string.Join(Environment.NewLine, lines));
        }
    }
}
