using Hik.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    [ScsService()]
    interface IUdinetClientService
    {
        void Write(string data);
        string ReadLine();
    }
}
