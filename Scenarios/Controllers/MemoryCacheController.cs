using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Scenarios.Model;
using Scenarios.Services;

namespace Scenarios.Controllers
{
    public class MemoryCacheController
    {
        private readonly IMemoryCache _memoryCache;
        private readonly PokemonService _pokemonService;
        private static readonly object _key = new();

        public MemoryCacheController(IMemoryCache memoryCache, PokemonService pokemonService)
        {
            _memoryCache = memoryCache;
            _pokemonService = pokemonService;
        }

        [HttpGet("/cache-1")]
        public Task<PokemonData> GetCachedPokemon()
        {
            // This uses the build in GetOrCreateAsync helper extensions method
            return _memoryCache.GetOrCreateAsync(_key, entry =>
            {
                entry.SetAbsoluteExpiration(DateTime.UtcNow + TimeSpan.FromSeconds(5));
                return _pokemonService.GetPokemonAsyncSlow();
            });
        }
    }
}
