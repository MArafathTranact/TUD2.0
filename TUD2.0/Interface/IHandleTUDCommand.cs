using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUD2._0.Classes;

namespace TUD2._0.Interface
{
    public interface IHandleTUDCommand
    {
        void ProcessCommandHandle(Camera camera, TudCommand command);
    }
}
