using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerBrokerService
{
    public class logger
    {

        public string path { get; set; }
        public string data { get; set; }
        public logger(string _path, string _data)
        {
            path = _path;
            data = _data;
        }
    }
}
