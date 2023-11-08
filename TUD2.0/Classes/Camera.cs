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

        public bool isBasic { get; set; }
        public string workstation_ip { get; set; }
        public string workstation_port { get; set; }

        public string scale_camera_name { get; set; }
        public int? BaudRate { get; set; }
        public string DataStop { get; set; }
        public int? ScaleParity { get; set; }
        public int? ComPort { get; set; }
        public int? BufferSize { get; set; }
        public int? WeightBeginPosition { get; set; }
        public int? WeightEndPosition { get; set; }
        public int? MotionPosition { get; set; }
        public int? UnitsPosition { get; set; }
        public int? ModePosition { get; set; }
        public int? StartOfText { get; set; }
        public int? NoMotionChar { get; set; }
        public int? LbUnitsChar { get; set; }
        public int? GrossModeChar { get; set; }
        public int? MaxCharToRead { get; set; }
        public int? NumberOfMatchingRead { get; set; }
    }
}
