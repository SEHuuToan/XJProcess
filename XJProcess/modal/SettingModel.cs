using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XJProcess.modal
{
    public class SettingModel
    {
        public string DrumId { get; set; } 
        public string IpAddress { get; set; }
        public string WebSocketPort { get; set; } 
        public string ApiUrl { get; set; }
        public string PingInterval { get; set; } 
        public string Timeout { get; set; }

        // Nhóm 2: Vận hành bồn quay
        public string DefaultSpeed { get; set; } 
        public string ReverseDelay { get; set; } 
        public string MotorAccel { get; set; }
        public string StepAngle { get; set; } 
        public string StopDelay { get; set; }
        public string DefaultMode { get; set; } 

        // Nhóm 3: Giới hạn an toàn
        public string MaxTemp { get; set; } 
        public string MaxPressure { get; set; } 
        public string MaxWeight { get; set; }
    }
}
