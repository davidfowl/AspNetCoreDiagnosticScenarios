using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scenarios.Model;

namespace Scenarios.Controllers
{
    public class BigJsonInputController : Controller
    {
        [HttpPost("/big-json-input-1")]
        public IActionResult BigJsonSynchronousInput()
        {
            // This synchronously reads the entire http request body into memory and it has several problems:
            // 1. If the request is large it could lead to out of memory problems which can result in a Denial Of Service.
            // 2. If the client is slowly uploading, we're doing sync over async because Kestrel does *NOT* support synchronous reads.
            var json = new StreamReader(Request.Body).ReadToEnd();

            var rootobject = JsonConvert.DeserializeObject<PokemonData>(json);

            return Accepted();
        }

        /// <summary>
        /// This uses MVC's built in model binding to create the PokemonData object. This is the most preferred approach as it handles all of the 
        /// correct buffering on your behalf.
        /// </summary>
        [HttpPost("/big-json-input-2")]
        public IActionResult BigJsonInput([FromBody]PokemonData rootobject)
        {   
            return Accepted();
        }

        [HttpPost("/big-json-input-3")]
        public async Task<IActionResult> BigContentBad()
        {
            // This asynchronously reads the entire http request body into memory. It still suffers from the Denial Of Service
            // issue if the request body is too large but there's no threading issue.
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            var rootobject = JsonConvert.DeserializeObject<PokemonData>(json);

            return Accepted();
        }

        [HttpPost("/big-json-input-4")]
        public async Task<IActionResult> BigContentManualGood()
        {
            var streamReader = new HttpRequestStreamReader(Request.Body, Encoding.UTF8);

            var jsonReader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();

            // This asynchronously reads the entire payload into a JObject then turns it into the real object.
            var obj = await JToken.ReadFromAsync(jsonReader);
            var rootobject = obj.ToObject<PokemonData>(serializer);

            return Accepted();
        }
    }
}
