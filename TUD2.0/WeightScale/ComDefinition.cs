using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.WeightScale
{
    public class ComDefinition
    {
        public ComDefinition()
        {
            BaudRate = 0;
            DataBits = 0;
            StopBits = StopBits.None;
            Parity = Parity.None;
            PortName = string.Empty;
            ReadTimeout = SerialPort.InfiniteTimeout;
            Handshake = Handshake.None;
            DtrEnable = false;
            ReceivedBytesThreshold = 1;
        }

        public int? BaudRate { get; set; }

        public int DataBits { get; set; }

        public StopBits StopBits { get; set; }

        public Parity Parity { get; set; }

        public string PortName { get; set; }

        public int ReadTimeout { get; set; }

        public Handshake Handshake { get; set; }
        public bool DtrEnable { get; set; }

        public int ReceivedBytesThreshold { get; set; }
    }
}
