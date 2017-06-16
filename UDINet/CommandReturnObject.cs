using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    [Serializable]
    class CommandReturnObject
    {
        public CommandReturnObject(int code, string msg)
        {
            Code = code;
            Message = msg;
        }
        public CommandReturnObject((int code, string msg) value) : this(value.code, value.msg) { }
        public string Message { get; private set; }
        public int Code { get; private set; }
    }
}
