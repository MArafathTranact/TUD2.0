using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class Camera
    {
        public string camera_name { get; set; }
        public string device_name { get; set; }
        public int IsNetCam { get; set; }
        public int camera_type { get; set; }
        public string ip_address { get; set; }
        public int? port_nbr { get; set; }
        public string username { get; set; }
        public string pwd { get; set; }
        public string yardid { get; set; }
        public string URL { get; set; }
        public string videoURL { get; set; }
        public int? contract_id { get; set; }
        public string contract_text { get; set; }
    }
}
