using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iep.Controllers;
using Microsoft.AspNet.SignalR;

namespace iep.Hubs
{
    public class AuctionHub : Hub
    {
        public void Hello() { 
        
            Clients.All.hello();
        }

        public static void AuctionUpdate(int idAuction, int tokensNum, string fullName) { 
        
            var hub = GlobalHost.ConnectionManager.GetHubContext<AuctionHub>();
            hub.Clients.All.AuctionUpdate(idAuction, tokensNum, fullName);
        }

        public void CloseAuction(int idAuction) { 
        
            ManageController manageController = new ManageController();
            manageController.AuctionClose(idAuction);
            
        }


    }
}