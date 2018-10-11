using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Scenarios.Model;

namespace Scenarios.Services
{
    public class PokemonService
    {
        private readonly HttpClient _client;
        private readonly string _url = "https://raw.githubusercontent.com/Biuni/PokemonGO-Pokedex/master/pokedex.json";

        public PokemonService(HttpClient client)
        {
            _client = client;
        }

        public async Task<PokemonData> GetPokemonBadAsync()
        {
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

        public async Task<PokemonData> GetPokemonManualBadAsync()
        {
            // Don't buffer the entire response into memory as a byte[]
            using (var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does some buffering but we're not double buffering
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                // THIS is a problem, we're doing synchronous IO here
                return serializer.Deserialize<PokemonData>(reader);
            }
        }

        public async Task<PokemonData> GetPokemonManualGoodAsync()
        {
            // This buffers the response so that we don't end up doing blocking IO when 
            // de-serializing the JSON
            using (var response = await _client.GetAsync(_url))
            {
                // Get the response stream
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Create a StreamReader and JsonTextReader over that
                // This does double buffering...
                var textReader = new StreamReader(responseStream);
                var reader = new JsonTextReader(textReader);

                var serializer = new JsonSerializer();

                return serializer.Deserialize<PokemonData>(reader);
            }
        }
    }
}
