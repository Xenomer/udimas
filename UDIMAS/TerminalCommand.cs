using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// CmdInterpreter command
    /// </summary>
    public struct TerminalCommand
    {
        /// <summary>
        /// String that this <see cref="TerminalCommand"/> instance is linked to
        /// </summary>
        public readonly string Command;

        /// <summary>
        /// Whether this command requires user input
        /// </summary>
        public readonly bool UsesInput;

        /// <summary>
        /// Method that is executed when this command is invoked
        /// </summary>
        public readonly Func<InterpreterIOPipeline, string[], (int, string)> Method;

        /// <summary>
        /// Instantiates this TerminalCommand instance
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="method"></param>
        /// <param name="usesInput"></param>
        public TerminalCommand(string cmd, Func<InterpreterIOPipeline, string[], (int, string)> method, bool usesInput = false)
        {
            Command = cmd;
            Method = method;
            UsesInput = usesInput;
        }
    }
}
