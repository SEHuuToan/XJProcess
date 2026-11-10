using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XJProcess.modal
{
    public class SettingModel
    {
        public string DrumNumber { get; set; } 
        public string IpAddress { get; set; }
        public string WebSocketPort { get; set; } 
        public string ApiUrl { get; set; }
        public string PingInterval { get; set; } 
        public string Timeout { get; set; }
        public short uid { get; set; }
        public string slaveId { get; set; }
        public int ReverseTime { get; set; }
        public bool IsReverse { get; set; }
        public string Language { get; set; } = "vi";

    }
}
