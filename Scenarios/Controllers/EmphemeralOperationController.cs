using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scenarios.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Scenarios.Controllers
{
    public class EmphemeralOperationController : Controller
    {
        [HttpGet("/timer-leak")]
        public IActionResult Index()
        {
            var operation = new EphemeralOperation();
            return Ok();
        }
    }
}
