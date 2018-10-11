using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Scenarios.Model;

namespace Scenarios.Controllers
{
    public class BigJsonInputController : Controller
    {
        [HttpPost("/big-json-input-1")]
        public IActionResult BigJsonSynchronousInput()
        {
            var json = new StreamReader(Request.Body).ReadToEnd();

            var rootobject = JsonConvert.DeserializeObject<PokemonData>(json);

            GC.KeepAlive(rootobject);

            return Accepted();
        }

        [HttpPost("/big-json-input-2")]
        public IActionResult BigJsonInput([FromBody]PokemonData rootobject)
        {
            GC.KeepAlive(rootobject);

            return Accepted();
        }

        [HttpPost("/big-json-input-3")]
        public async Task<IActionResult> BigContentBad()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            var rootobject = JsonConvert.DeserializeObject<PokemonData>(json);

            GC.KeepAlive(rootobject);

            return Accepted();
        }

        [HttpPost("/big-json-input-4")]
        public async Task<IActionResult> BigContentManualGood()
        {
            // Enable efficient buffering
            Request.EnableBuffering();

            // Read the entire request body into this stream
            // It's optimized to buffer in memory up to a certain threshold
            await Request.Body.DrainAsync(HttpContext.RequestAborted);

            // Reset the body
            Request.Body.Seek(0, SeekOrigin.Begin);

            var streamReader = new HttpRequestStreamReader(Request.Body, Encoding.UTF8);

            var jsonReader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            var rootobject = serializer.Deserialize<PokemonData>(jsonReader);

            GC.KeepAlive(rootobject);

            return Accepted();
        }
    }
}
