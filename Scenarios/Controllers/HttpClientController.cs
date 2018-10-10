using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Scenarios.Controllers
{
    public class HttpClientController : Controller
    {
        private readonly string _url = "https://raw.githubusercontent.com/Biuni/PokemonGO-Pokedex/master/pokedex.json";

        [Route("/httpclient-1")]
        public async Task<string> OutgoingWorse()
        {
            var client = new HttpClient();
            return await client.GetStringAsync(_url);
        }

        [Route("/httpclient-2")]
        public async Task<string> OutgoingBad()
        {
            using (var client = new HttpClient())
            {
                return await client.GetStringAsync(_url);
            }
        }

        [Route("/httpclient-3")]
        public async Task<Stream> OutgoingGood([FromServices]IHttpClientFactory clientFactory)
        {
            using (var client = clientFactory.CreateClient())
            {
                return await client.GetStreamAsync(_url);
            }
        }
    }
}
