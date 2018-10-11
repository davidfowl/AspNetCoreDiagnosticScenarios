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
            var obj = await _pokemonService.GetPokemonBadAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-2")]
        public async Task<IActionResult> BigContentJsonGood()
        {
            var obj = await _pokemonService.GetPokemonAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-3")]
        public async Task<IActionResult> BigContentJsonManualBad()
        {
            var obj = await _pokemonService.GetPokemonManualBadAsync();

            return Ok(obj);
        }

        [HttpGet("/big-json-content-4")]
        public async Task<IActionResult> BigContentJsonManualGood()
        {
            var obj = await _pokemonService.GetPokemonManualGoodAsync();
            return Ok(obj);
        }
    }
}
