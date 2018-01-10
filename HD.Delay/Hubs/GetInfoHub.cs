using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace HD.Delay.Hubs
{
    public class GetInfoHub : Hub
    {
        [HubMethodName("sendUptodateInformation")]
        public static void SendUptodateInformation(string action)
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<GetInfoHub>();

            // the updateStudentInformation method will update the connected client about any recent changes in the server data
            context.Clients.All.updateChannelInformation(action);
        }
    }
}