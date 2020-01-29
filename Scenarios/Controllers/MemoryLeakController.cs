using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Scenarios.Controllers
{
    public class MemoryLeakController : ControllerBase
    {
        private static ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();

        [HttpGet("/leak")]
        public string Leak1(string id)
        {
            return _cache.GetOrAdd(id, _ => new string('c', 1024 * 1024 * 5)).Substring(0, 5);
        }
    }
}
