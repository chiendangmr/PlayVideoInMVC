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

    [HubName("capLowresHub")]

    public class CapLowresHub : Hub
    {
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _Channel = ConfigurationManager.AppSettings["ChannelId"];        

        List<CaptureLowres> lstCapItems = new List<CaptureLowres>();        

        [HubMethodName("sendCaptureLowresItems")]
        public string SendCaptureLowresItems()
        {

            using (var connection = new SqlConnection(_connectionString))

            {

                string query = "SELECT CaptureId,ChannelId, ProgramName, FileName, StartTime, EndTime, Status, StatusMessage, Deleted  FROM [dbo].[CaptureLowres] WHERE ChannelId=" + _Channel + " and Deleted=0";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))

                {

                    command.Notification = null;

                    DataTable dt = new DataTable();

                    SqlDependency dependency = new SqlDependency(command);

                    dependency.OnChange += new OnChangeEventHandler(CaptureLowres_OnChange);

                    if (connection.State == ConnectionState.Closed)

                        connection.Open();

                    var reader = command.ExecuteReader();

                    dt.Load(reader);

                    if (dt.Rows.Count > 0)

                    {
                        dt.AsEnumerable().ToList().ForEach(
                            i => lstCapItems.Add(new CaptureLowres()
                            {
                                CaptureId = (long)i["CaptureId"],
                                ChannelId = (int)i["ChannelId"],
                                ProgramName = (string)i["ProgramName"],
                                FileName = i["FileName"] == DBNull.Value?"None": (string)i["FileName"],
                                StartTime = (DateTime)i["StartTime"],
                                EndTime = (DateTime)i["EndTime"],
                                Status = (int)i["Status"],
                                StatusMessage = i["StatusMessage"] == DBNull.Value ? "None" : (string)i["StatusMessage"],
                                Deleted = (bool)i["Deleted"]
                            })
                       );
                    }

                }

            }

            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CapLowresHub>();

            return (string)context.Clients.All.ReceiveCapItems(lstCapItems).Result;

        }        
        private void CaptureLowres_OnChange(object sender, SqlNotificationEventArgs e)

        {

            if (e.Type == SqlNotificationType.Change)

            {

                CapLowresHub nHub = new CapLowresHub();                
                nHub.SendCaptureLowresItems();

            }

        }

    }

}