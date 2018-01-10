using HD.Delay.Hubs;
using HD.Delay.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace HD.Delay.Business
{
    public class ChannelRepository
    {
        public static List<Channel> GetDelayNumber(int channelId, string connectionString)
        {
            List<Channel> channel = new List<Channel>();

            using (var db = new SqlConnection(connectionString))
            {
                db.Open();
                var sqlCommandText = @"Select DelayExpected, RealisticDelay From Channel Where ChannelId = " + channelId;
                using (var sqlCommand = new SqlCommand(sqlCommandText, db))
                {                    
                    AddSQLDependency(sqlCommand);

                    if (db.State == ConnectionState.Closed)
                        db.Open();

                    var reader = sqlCommand.ExecuteReader();
                    channel = GetChannelRecords(reader);
                }
            }
            return channel;

        }
        /// <summary>
        /// Adds SQLDependency for change notification and passes the information to Student Hub for broadcasting
        /// </summary>
        /// <param name="sqlCommand"></param>
        private static void AddSQLDependency(SqlCommand sqlCommand)
        {
            sqlCommand.Notification = null;

            var dependency = new SqlDependency(sqlCommand);

            dependency.OnChange += (sender, sqlNotificationEvents) =>
            {
                if (sqlNotificationEvents.Type == SqlNotificationType.Change)
                {
                    GetInfoHub.SendUptodateInformation(sqlNotificationEvents.Info.ToString());
                }
            };
        }

        private static List<Channel> GetChannelRecords(SqlDataReader reader)
        {
            var lstChannelRecords = new List<Channel>();
            var dt = new DataTable();
            dt.Load(reader);
            dt
                .AsEnumerable()
                .ToList()
                .ForEach
                (
                    i => lstChannelRecords.Add(new Channel()
                    {
                        DelayExpected = (int)i["DelayExpected"]
                            ,
                        RealisticDelay = (int)i["RealisticDelay"]

                    })
                );
            return lstChannelRecords;
        }
    }
}