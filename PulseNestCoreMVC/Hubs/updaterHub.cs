using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PulseNestCoreMVC.Models;

namespace PulseNestCoreMVC.Hubs
{
    public class updaterHub : Hub<IUpdateMap>
    {
        public async Task ReceivePoint( mapPoint cords)
        {
            await Clients.All.ReceivePoint(cords);
        }
    }
}
