using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class RecordLowres
    {
        public long RecordId { get; set; }
        public int ChannelId { get; set; }
        public DateTime RecordTime { get; set; }
        public string FileName { get; set; }
        public long? Duration { get; set; }
        public bool Deleted { get; set; }
    }
}
