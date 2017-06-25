using Hik.Communication.Scs.Client;
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
    class RCIWriter : InterpreterIOPipeline
    {
        public IScsServiceClient service;
        public override bool SupportsInput => false;
        public override string ReadLine()
        {
            return service.GetClientProxy<IUdinetClientService>().ReadLine();
        }

        public override void Write(string value)
        {
            service.GetClientProxy<IUdinetClientService>().Write(value);
        }
    }
}
