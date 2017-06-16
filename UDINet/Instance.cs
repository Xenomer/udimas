using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    internal class Instance
    {
        public string Name;
        public string Ip;
        public Instance(string ip, string name)
        {
            Name = name;
            Ip = ip;
        }
        public Instance() { }
    }
}
