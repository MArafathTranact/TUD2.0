using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.WeightScale
{
    public static class ComObject
    {
        static ComObject()
        {

        }

        public static List<SerialPort> ComPorts { get; set; } = GetCommPorts();

        public static SerialPort InitializeCom(ComDefinition comDefinition)
        {
            var comPort = ComPorts.FirstOrDefault(c => c.PortName == comDefinition.PortName);

            if (comPort == null)
                return default;

            if (comPort.IsOpen)
                return comPort;

            if (comDefinition.BaudRate != null)
                comPort.BaudRate = (int)comDefinition.BaudRate;

            comPort.DataBits = comDefinition.DataBits;
            comPort.StopBits = comDefinition.StopBits;
            comPort.Parity = comDefinition.Parity;
            comPort.ReadTimeout = comDefinition.ReadTimeout;
            comPort.Handshake = comDefinition.Handshake;
            comPort.DtrEnable = comDefinition.DtrEnable;
            comPort.ReceivedBytesThreshold = comDefinition.ReceivedBytesThreshold;

            return comPort;
        }

        private static List<SerialPort> GetCommPorts()
        {
            var query = SerialPort.GetPortNames().ToList();
            return query.ToList().Select(item => new SerialPort
            {
                PortName = item
            }).ToList();
        }
    }
}
