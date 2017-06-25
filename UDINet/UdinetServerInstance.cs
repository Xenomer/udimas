using Hik.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDIMAS;

namespace UDINet
{
    class UdinetServerInstance : ScsService, IUdinetServerService
    {
        public CommandReturnObject Execute(string cmd, string[] args)
        {
            var i = new CmdInterpreter("RCI",  new RCIWriter() { service = CurrentClient });
            return new CommandReturnObject(i.UserInput(cmd, args));
        }
    }
}
