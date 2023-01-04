using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class SocketMessage
    {
        public string identifier { get; set; }
        public string type { get; set; }
        public Message message { get; set; }
    }


    public class Message
    {
        public int id { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string command { get; set; }

    }
}
