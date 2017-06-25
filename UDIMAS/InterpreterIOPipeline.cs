using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// Replaces <see cref="Console"/> in IO operations to support custom implementations. Used in <see cref="CmdInterpreter"/>
    /// </summary>
    public abstract class InterpreterIOPipeline
    {
        public abstract bool SupportsInput { get; }
        public abstract void Write(string value);
        public void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }
        public void WriteLine()
        {
            Write(Environment.NewLine);
        }
        public abstract string ReadLine();
    }

    public class ConsoleIOPipeline : InterpreterIOPipeline
    {
        public override bool SupportsInput => true;

        public override string ReadLine()
        {
            return Console.ReadLine();
        }

        public override void Write(string value)
        {
            Console.Write(value);
        }
    }
}
