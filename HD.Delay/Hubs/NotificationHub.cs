using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace HD.Delay.Hubs

{

    [HubName("notificationHub")]

    public class NotificationHub : Hub

    {

        string expectedDelay = "";

        string realDelay = "";       



        [HubMethodName("sendNotifications")]

        public string SendNotifications()

        {

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))

            {

                string query = "SELECT DelayExpected, RealisticDelay FROM [dbo].[Channel] WHERE ChannelId=" + "1";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))

                {

                    command.Notification = null;

                    DataTable dt = new DataTable();

                    SqlDependency dependency = new SqlDependency(command);

                    dependency.OnChange += new OnChangeEventHandler(dependency_OnChange);

                    if (connection.State == ConnectionState.Closed)

                        connection.Open();

                    var reader = command.ExecuteReader();

                    dt.Load(reader);

                    if (dt.Rows.Count > 0)

                    {

                        var temp1 = int.Parse(dt.Rows[0]["DelayExpected"].ToString());
                        expectedDelay = (new TimeSpan(0, 0, 0, 0, temp1)).ToString(@"hh\:mm\:ss");

                        var temp2 = int.Parse(dt.Rows[0]["RealisticDelay"].ToString());
                        realDelay = (new TimeSpan(0, 0, 0, 0, temp2)).ToString(@"hh\:mm\:ss");
                    }

                }

            }

            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            return (string)context.Clients.All.RecieveNotification(expectedDelay, realDelay).Result;

        }

        private void dependency_OnChange(object sender, SqlNotificationEventArgs e)

        {

            if (e.Type == SqlNotificationType.Change)

            {

                NotificationHub nHub = new NotificationHub();

                nHub.SendNotifications();

            }

        }

    }

}