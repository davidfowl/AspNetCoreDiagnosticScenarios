using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Scenarios.Controllers
{
    public class HighCpuController : Controller
    {
        [HttpPost("/cpu-1")]
        public string BurnCpu()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (true)
            {
                watch.Stop();
                if (watch.ElapsedMilliseconds > 1000 * 5)
                    break;
                watch.Start();
            }
            return "Working";
        }
    }
}
