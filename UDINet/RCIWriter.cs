using Hik.Communication.Scs.Client;
using Hik.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    class RCIWriter : TextWriter
    {
        public IScsServiceClient service;
        public override Encoding Encoding => Encoding.ASCII;
        public override void Write(string value)
        {
            service.GetClientProxy<IUdinetClientService>().Write(value);
        }
        public override void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }
        public override void WriteLine()
        {
            Write(Environment.NewLine);
        }
    }
}
