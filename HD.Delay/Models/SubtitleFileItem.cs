using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class SubtitleFileItem
    {
        public int ItemId { get; set; }
        public string Text { get; set; }
        public int Align { get; set; }
        public int Duration { get; set; }
        public string DurationStr
        {
            get
            {
                return (new TimeSpan(0, 0, 0, 0, Duration)).ToString(@"hh\:mm\:ss");
            }
        }
        public int Position { get; set; }
        public double StartDateTime { get; set; }
        public long StartTime { get; set; }
        public string StartTimeStr
        {
            get
            {
                return (new TimeSpan(0, 0, 0, 0, (int)StartTime)).ToString(@"hh\:mm\:ss");
            }
        }
        public string EndTime
        {
            get
            {
                return (new TimeSpan(0, 0, 0, 0, (int)(StartTime + Duration))).ToString(@"hh\:mm\:ss");
            }

        }
    }
}
