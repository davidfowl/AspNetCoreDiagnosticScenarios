using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scenarios.Services;

namespace Scenarios.Controllers
{
    public class BigJsonOutputController : Controller
    {
        private readonly PokemonService _pokemonService;
        public BigJsonOutputController(PokemonService pokemonService)
        {
            _pokemonService = pokemonService;
        }

        [HttpGet("/big-json-content-1")]
        public async Task<IActionResult> BigContentJsonBad()
        {
            var obj = await _pokemonService.GetPokemonBufferdStringAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-2")]
        public async Task<IActionResult> BigContentJsonGood()
        {
            var obj = await _pokemonService.GetPokemonAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-3")]
        public async Task<IActionResult> BigContentJsonManualUnbufferedBad()
        {
            var obj = await _pokemonService.GetPokemonManualUnbufferedBadAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-4")]
        public async Task<IActionResult> BigContentJsonManualUnbufferedGood()
        {
            var obj = await _pokemonService.GetPokemonManualUnbufferedGoodAsync();
            return Ok(obj);
        }

        [HttpGet("/big-json-content-5")]
        public async Task<IActionResult> BigContentJsonManualBuffered()
        {
            var obj = await _pokemonService.GetPokemonManualBufferedAsync();
            return Ok(obj);
        }

        [HttpGet("/big-json-content-6")]
        public async Task<IActionResult> BigContentJsonNewJson()
        {
            var obj = await _pokemonService.GetPokemonAsyncNewJson();
            return Ok(obj);
        }
    }
}
