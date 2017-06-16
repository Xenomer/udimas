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
        public readonly string Command;

        /// <summary>
        /// Whether this command requires user input
        /// </summary>
        public readonly bool UsesInput;

        public readonly Func<System.IO.TextWriter, string[], (int, string)> Method;

        public TerminalCommand(string cmd, Func<System.IO.TextWriter, string[], (int, string)> method, bool usesInput = false)
        {
            Command = cmd;
            Method = method;
            UsesInput = usesInput;
        }
    }
}
