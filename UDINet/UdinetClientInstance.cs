using Hik.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    class UdinetClientInstance : ScsService, IUdinetClientService
    {
        public void Write(string data)
        {
            Console.Write(data);
        }
        public string ReadLine()
        {
            return Console.ReadLine();
        }
    }
}
