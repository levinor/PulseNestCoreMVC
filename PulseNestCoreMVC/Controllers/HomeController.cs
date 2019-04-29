using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PulseNestCoreMVC.Models;

namespace PulseNestCoreMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "PulseNest";
            return View();
        }


        //public async Task<ActionResult> PulseNest()
        public IActionResult PulseNest()
        {
            ViewData["Title"] = "PulseNest";
            return View();
        }

        public void beginListen()
        {
            nestListener nl = new nestListener();
            nl.startListening();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
