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
    
    public partial class RecordHighre
    {
        public long RecordId { get; set; }
        public int ChannelId { get; set; }
        public System.DateTime RecordTime { get; set; }
        public string FileName { get; set; }
        public Nullable<long> Duration { get; set; }
        public bool Deleted { get; set; }       
    }
}
