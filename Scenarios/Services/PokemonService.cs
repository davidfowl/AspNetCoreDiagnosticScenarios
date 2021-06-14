using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scenarios.Model;

namespace Scenarios.Services
{
    /// <summary>
    /// This service shows the various ways to make an outgoing HTTP request to get a JSON payload. It shows the various tradeoffs involved in doing this. It
    /// uses JSON.NET to perform Deserialization. In general there are 3 approaches:
    /// 1. Buffer the response in memory before handing it to the JSON serializer. This could lead to out of memory exceptions which can lead to a Denial Of Service.
    /// 2. Stream the response and synchronously read from the stream. This can lead to thread pool starvation.
    /// 3. Stream the response and asynchronously read from the stream.
    /// </summary>
    public class PokemonService
    {
        private readonly HttpClient _client;
        private readonly string _url = "https://raw.githubusercontent.com/Biuni/PokemonGO-Pokedex/master/pokedex.json";

        public PokemonService(HttpClient client)
        {
            _client = client;
        }

        public async Task<PokemonData> GetPokemonBufferdStringAsync()
        {
            // This service returns the entire JSON payload into memory before converting that into a JSON object
            var json = await _client.GetStringAsync(_url);

            return JsonConvert.DeserializeObject<PokemonData>(json);
        }

        public async Task<PokemonData> GetPokemonAsync()
        {
            var response = await _client.GetAsync(_url);
            // This is a hack to work around the fact that this JSON api returns text/plain
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // Use the built in methods to read the response as JSON
            return await response.Content.ReadAsAsync<PokemonData>();
        }

        public async Task<PokemonData> GetPokemonManualUnbufferedBadAsync()
        {
            // Using HttpCompletionOption.ResponseHeadersRead avoids buffering the entire response body into memory
            using (var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does some buffering but we're not double buffering
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                // *THIS* is a problem, we're doing synchronous IO here over the Stream. If the back end is slow, this can result
                // in thread pool starvation.
                return serializer.Deserialize<PokemonData>(reader);
            }
        }

        public async Task<PokemonData> GetPokemonManualUnbufferedGoodAsync()
        {
            // Using HttpCompletionOption.ResponseHeadersRead avoids buffering the entire response body into memory
            using (var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does double buffering...
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                // This asynchronously reads the JSON object into memory. This does true asynchronous IO. The only downside is that we're
                // converting the object graph to an intermediate DOM before going to the object directly.
                var obj = await JToken.ReadFromAsync(reader);

                // Convert the JToken to an object
                return obj.ToObject<PokemonData>(serializer);
            }
        }

        public async Task<PokemonData> GetPokemonManualBufferedAsync()
        {
            // This buffers the entire response into memory so that we don't end up doing blocking IO when 
            // de-serializing the JSON. If the payload is *HUGE* this could result in large allocations that lead to a Denial Of Service.
            using (var response = await _client.GetAsync(_url))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does double buffering...
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                // Because we're buffering the entire response, we're also avoiding synchronous IO
                return serializer.Deserialize<PokemonData>(reader);
            }
        }

        public async Task<PokemonData> GetPokemonAsyncNewJson()
        {
            using var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead);

            // Get the response stream
            var responseStream = await response.Content.ReadAsStreamAsync();

            return await System.Text.Json.JsonSerializer.DeserializeAsync<PokemonData>(responseStream);
        }
    }
}
