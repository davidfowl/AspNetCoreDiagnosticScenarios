namespace Scenarios.Model
{
    public class PokemonData
    {
        public Pokemon[] pokemon { get; set; }
    }

    public class Pokemon
    {
        public int id { get; set; }
        public string num { get; set; }
        public string name { get; set; }
        public string img { get; set; }
        public string[] type { get; set; }
        public string height { get; set; }
        public string weight { get; set; }
        public string candy { get; set; }
        public int candy_count { get; set; }
        public string egg { get; set; }
        public float spawn_chance { get; set; }
        public float avg_spawns { get; set; }
        public string spawn_time { get; set; }
        public float[] multipliers { get; set; }
        public string[] weaknesses { get; set; }
        public Next_Evolution[] next_evolution { get; set; }
        public Prev_Evolution[] prev_evolution { get; set; }
    }

    public class Next_Evolution
    {
        public string num { get; set; }
        public string name { get; set; }
    }

    public class Prev_Evolution
    {
        public string num { get; set; }
        public string name { get; set; }
    }
}
