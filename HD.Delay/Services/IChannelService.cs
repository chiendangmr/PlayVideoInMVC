using HD.Delay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HD.Delay.Services
{
    public interface IChannelService
    {
        List<Channel> GetDelayNumber(int channelId, string connectionString);
    }
}