using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mma.grpc.client
{
    public class Logger
    {
        public string Error(string msg)
        {
            return $"ERROR: {msg}";
        }

        public string Info(string msg)
        {
            return $"Information: {msg}";
        }


        public string SystemMsg(string msg)
        {
            return $"System: {msg}";
        }
    }
}
