using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Scenarios.Controllers
{
    public class CancelAsyncOperationController : Controller
    {
        private readonly string _url = "https://raw.githubusercontent.com/Biuni/PokemonGO-Pokedex/master/pokedex.json";

        [HttpGet("/cancellation-1")]
        public async Task<Stream> HttpClientAsyncWithCancellationBad([FromServices]IHttpClientFactory clientFactory)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using (var client = clientFactory.CreateClient())
            {
                var response = await client.GetAsync(_url, cts.Token);
                return await response.Content.ReadAsStreamAsync();
            }
        }

        [HttpGet("/cancellation-2")]
        public async Task<Stream> HttpClientAsyncWithCancellationBetter([FromServices]IHttpClientFactory clientFactory)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                using (var client = clientFactory.CreateClient())
                {
                    var response = await client.GetAsync(_url, cts.Token);
                    return await response.Content.ReadAsStreamAsync();
                }
            }
        }

        [HttpGet("/cancellation-3")]
        public async Task<Stream> HttpClientAsyncWithCancellationBest([FromServices]IHttpClientFactory clientFactory)
        {
            // This has the timeout configured in Startup
            using (var client = clientFactory.CreateClient("timeout"))
            {
                return await client.GetStreamAsync(_url);
            }
        }
    }
}
