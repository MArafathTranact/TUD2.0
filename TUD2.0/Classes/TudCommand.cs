using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUD2._0.Classes
{
    public class TudCommand
    {
        public string ticket_nbr { get; set; }
        public string receipt_nbr { get; set; }
        public string camera_name { get; set; }
        public string location { get; set; }
        public string event_code { get; set; }
        public decimal amount { get; set; }
        public string transaction_type { get; set; }
        public string branch_code { get; set; }
        public string commodity { get; set; }
        public string yardid { get; set; }
        public int tare_seq_nbr { get; set; }

    }
}
