using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class SubtitleTimeLine
    {
        public int TimeLineId { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; }
        public DateTime StartTime { get; set; }
        public string StartTimeString
        {
            get { return StartTime.ToString("dd-MM-yyyy HH:mm:ss"); }

            set { StartTime = DateTime.ParseExact(value, "dd-MM-yyyy HH:mm:ss", null); }
        }
    }
}
