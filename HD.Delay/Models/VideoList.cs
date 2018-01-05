using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HD.Delay.Models
{
    public class VideoList
    {
        public virtual long RecordId { get; set; }
        public virtual int ChannelId { get; set; }
        public virtual DateTime RecordTime { get; set; }
        public virtual string FileName { get; set; }
        public virtual long Duration { get; set; }
        public virtual int Deleted { get; set; }
    }
}