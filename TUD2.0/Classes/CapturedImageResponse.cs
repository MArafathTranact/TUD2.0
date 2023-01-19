using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class CapturedImageResponse
    {
        public long capture_seq_nbr { get; set; }
        public string thumbnail_url { get; set; }
        public string url { get; set; }
        public string lineItemBase64Image { get; set; }
    }
}
