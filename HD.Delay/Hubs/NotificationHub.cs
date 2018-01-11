using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using HD.Delay.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace HD.Delay.Hubs

{

    [HubName("notificationHub")]

    public class NotificationHub : Hub
    {
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _Channel = ConfigurationManager.AppSettings["ChannelId"];

        int expectedDelay = 0;
        string expectedDelayStr = "";

        string realDelayStr = "";
        int realDelay = 0;        

        [HubMethodName("sendNotifications")]
        public string SendNotifications()
        {

            using (var connection = new SqlConnection(_connectionString))

            {

                string query = "SELECT DelayExpected, RealisticDelay FROM [dbo].[Channel] WHERE ChannelId=" + _Channel;

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))

                {

                    command.Notification = null;

                    DataTable dt = new DataTable();

                    SqlDependency dependency = new SqlDependency(command);

                    dependency.OnChange += new OnChangeEventHandler(Dependency_OnChange);

                    if (connection.State == ConnectionState.Closed)

                        connection.Open();

                    var reader = command.ExecuteReader();

                    dt.Load(reader);

                    if (dt.Rows.Count > 0)

                    {

                        expectedDelay = int.Parse(dt.Rows[0]["DelayExpected"].ToString());
                        expectedDelayStr = (new TimeSpan(0, 0, 0, 0, expectedDelay)).ToString(@"hh\:mm\:ss");

                        realDelay = int.Parse(dt.Rows[0]["RealisticDelay"].ToString());
                        realDelayStr = (new TimeSpan(0, 0, 0, 0, realDelay)).ToString(@"hh\:mm\:ss");
                    }

                }

            }

            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            return (string)context.Clients.All.ReceiveNotification(expectedDelay, expectedDelayStr, realDelay, realDelayStr).Result;

        }        
        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)

            {
                NotificationHub nHub = new NotificationHub();
                nHub.SendNotifications();
            }

        }   

    }

}