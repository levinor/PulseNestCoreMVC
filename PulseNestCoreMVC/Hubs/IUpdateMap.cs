using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PulseNestCoreMVC.Models;

namespace PulseNestCoreMVC.Hubs
{
    public interface IUpdateMap
    {
        Task ReceivePoint(mapPoint point);
    }
}
