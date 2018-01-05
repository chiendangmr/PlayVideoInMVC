using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class SubtitleCategory
    {
        public string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public int ChannelId { get; set; }
        public int? CategoryParrentId { get; set; }
    }
}
