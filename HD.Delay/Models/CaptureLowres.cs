using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class CaptureLowres
    {
        public long CaptureId { get; set; }
        public int ChannelId { get; set; }
        public string ProgramName { get; set; }
        public string FileName { get; set; }
        public DateTime StartTime { get; set; }
        public string StartTimeStr
        {
            get { return StartTime.ToString("dd-MM-yyyy HH:mm:ss"); }
        }
        public DateTime EndTime { get; set; }
        public string EndTimeStr
        {
            get { return EndTime.ToString("dd-MM-yyyy HH:mm:ss"); }
        }
        public int Status { get; set; }
        public string StatusMessage { get; set; }
        public bool Deleted { get; set; }
    }
}
