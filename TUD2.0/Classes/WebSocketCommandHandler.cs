using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TUD2._0.Interface;

namespace TUD2._0.Classes
{
    internal class WebSocketCommandHandler : IHandleTUDCommand
    {
        private string WorkStationIp = "";

        private readonly IHandleTUDCommand handleTUDCommand;
        public WebSocketCommandHandler(IHandleTUDCommand tudCommand)
        {
            this.handleTUDCommand = tudCommand;
        }

        public async Task HandleCommand(string command)
        {
            try
            {

            }
            catch { }
        }

        public void ProcessCommandHandle(Camera camera)
        {
            handleTUDCommand.ProcessCommandHandle(camera);
        }
    }
}
