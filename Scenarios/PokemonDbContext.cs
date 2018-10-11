using System.IO;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Scenarios.Model;

namespace Scenarios
{
    public class PokemonDbContext : DbContext
    {
        public PokemonDbContext(DbContextOptions<PokemonDbContext> options)
            : base(options)
        {

        }

        public DbSet<Pokemon> Pokemon { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            PokemonData pokemonData;
            using (var stream = File.OpenRead("pokemon.json"))
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                pokemonData = serializer.Deserialize<PokemonData>(reader);
            }

            modelBuilder.Entity<Pokemon>()
                .HasData(pokemonData.pokemon);

            modelBuilder.Entity<Pokemon>()
                        .Ignore(p => p.next_evolution)
                        .Ignore(p => p.multipliers)
                        .Ignore(p => p.prev_evolution)
                        .Ignore(p => p.weaknesses)
                        .Ignore(p => p.type);

            base.OnModelCreating(modelBuilder);
        }
    }
}
