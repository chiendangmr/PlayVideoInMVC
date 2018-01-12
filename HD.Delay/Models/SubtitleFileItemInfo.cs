using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HD.Delay.Models
{
    public class SubtitleFileItemInfo
    {
        public string SubFileDuration { get; set; }
        public string SubFileStartTime { get; set; }
        public List<SubtitleFileItem> LstSubFileItems { get; set; }       
        
    }
}
