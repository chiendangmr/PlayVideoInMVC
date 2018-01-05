using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class Channel
    {
        public string ChannelName { get; set; }
        public int DelayExpected { get; set; }
        public string DelayExpectedStr
        {
            get
            {
                return (new TimeSpan(0, 0, 0, 0, DelayExpected)).ToString(@"hh\:mm\:ss");
            }
        }
        public int RealisticDelay { get; set; }
        public string RealisticDelayStr
        {
            get
            {
                return (new TimeSpan(0, 0, 0, 0, RealisticDelay)).ToString(@"hh\:mm\:ss");
            }
        }
        public int StyleId { get; set; }
    }
}
