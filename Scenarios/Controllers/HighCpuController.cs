using System;
using System.Collections.Generic;
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
            var spin = new System.Threading.SpinWait();
            for (int i = 0; i < 100000; i++)
            {
                spin.SpinOnce();
            }
            return "Done";
        }
    }
}
