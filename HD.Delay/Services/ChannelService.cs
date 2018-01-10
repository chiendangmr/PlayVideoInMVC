using Dapper;
using HD.Delay.Business;
using HD.Delay.Hubs;
using HD.Delay.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace HD.Delay.Services
{
    public class ChannelService : IChannelService
    {
        public List<Channel> GetDelayNumber(int channelId, string connectionString)
        {
            return ChannelRepository.GetDelayNumber(channelId, connectionString);
        }
    }
}