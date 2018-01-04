//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PlayVideoInMVC.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SubtitleCategory
    {
        public SubtitleCategory()
        {
            this.SubtitleCategory1 = new HashSet<SubtitleCategory>();
            this.SubtitleFiles = new HashSet<SubtitleFile>();
        }
    
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ChannelId { get; set; }
        public Nullable<int> CategoryParrentId { get; set; }
    
        public virtual Channel Channel { get; set; }
        public virtual ICollection<SubtitleCategory> SubtitleCategory1 { get; set; }
        public virtual SubtitleCategory SubtitleCategory2 { get; set; }
        public virtual ICollection<SubtitleFile> SubtitleFiles { get; set; }
    }
}
