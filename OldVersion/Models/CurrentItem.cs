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
    
    public partial class CurrentItem
    {
        public int ChannelId { get; set; }
        public int CurrentType { get; set; }
        public Nullable<int> ItemType { get; set; }
        public Nullable<long> ClipID { get; set; }
        public System.DateTime StartTime { get; set; }
        public System.DateTime LastTime { get; set; }       
    }
}
