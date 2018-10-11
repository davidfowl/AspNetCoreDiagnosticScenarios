using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Scenarios.Model;

namespace Scenarios.Controllers
{
    public class BigJsonOutputController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _url = "https://raw.githubusercontent.com/Biuni/PokemonGO-Pokedex/master/pokedex.json";

        public BigJsonOutputController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("/big-string-content-1")]
        public async Task<IActionResult> BigContentBad()
        {
            var client = _clientFactory.CreateClient();
            var json = await client.GetStringAsync(_url);
            return Ok(json);
        }

        [HttpGet("/big-string-content-2")]
        public async Task<IActionResult> BigContentGood()
        {
            var client = _clientFactory.CreateClient();
            var jsonStream = await client.GetStreamAsync(_url);
            return Ok(jsonStream);
        }

        [HttpGet("/big-json-content-3")]
        public async Task<IActionResult> BigContentJsonBad()
        {
            var client = _clientFactory.CreateClient();
            var json = await client.GetStringAsync(_url);
            var obj = JsonConvert.DeserializeObject<PokemonData>(json);

            return Ok(obj);
        }

        [HttpGet("/big-json-content-4")]
        public async Task<IActionResult> BigContentJsonGood()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(_url);
            // This is a hack to work around the fact that this JSON api returns text/plain
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // Use the built in methods to read the response as JSON
            var obj = await response.Content.ReadAsAsync<PokemonData>();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-5")]
        public async Task<IActionResult> BigContentJsonManualBad()
        {
            var client = _clientFactory.CreateClient();
            // Don't buffer the entire response into memory as a byte[]
            using (var response = await client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does some buffering but we're not double buffering
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                // THIS is a problem, we're doing synchronous IO here
                var obj = serializer.Deserialize<PokemonData>(reader);

                return Ok(obj);
            }
        }

        [HttpGet("/big-json-content-6")]
        public async Task<IActionResult> BigContentJsonManualGood()
        {
            var client = _clientFactory.CreateClient();
            
            // This buffers the response so that we don't end up doing blocking IO when 
            // de-serializing the JSON
            using (var response = await client.GetAsync(_url))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does some buffering but we're not double buffering
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                var obj = serializer.Deserialize<PokemonData>(reader);

                return Ok(obj);
            }
        }
    }
}
