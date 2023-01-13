using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class ServiceConfiguration
    {

        public static string GetFileLocation(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }


    }
}
