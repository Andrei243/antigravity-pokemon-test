partial class Harness
{
    // ---------------------------------------------------------------- every species' model (plan 03 · D5)

    // Boards of sixty models each in Pokédex order, seen from three-quarters in front, or only the species named after
    // the mode. Not part of "all": the first run meshes every species (cached afterwards in cache/models)
    public void DexMode()
    {
        var context = game.RenderContext;
        // --back: from behind, as the player's own Pokémon is seen in battle
        bool back = args.Skip(2).Contains("--back");
        var named = args.Skip(2).Where(a => a != "--back").ToArray();
        var list = named.Length > 0 ? named : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).ToArray();
        const int Cols = 10, Rows = 6, Box = 192;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int page = 0; page * Cols * Rows < list.Length; page++)
        {
            string name = back ? $"94_dex_back_{page + 1:00}" : $"94_dex_{page + 1:00}";
            if (!Wanted(name)) continue;
            var board = Raylib.GenImageColor(Cols * Box, Rows * Box, new Color(206, 218, 232, 255));
            for (int i = 0; i < Cols * Rows && page * Cols * Rows + i < list.Length; i++)
            {
                string species = list[page * Cols * Rows + i];
                var view = PokemonStudio.Strip(context, species, "idle", new[] { 0.4f }, new[] { back ? 2.6f : -0.55f }, Box, Box, 1.3f, 0.5f, false);
                int x = i % Cols * Box, y = i / Cols * Box;
                Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, Box, Box), new Rectangle(x, y, Box, Box), Color.White);
                Raylib.UnloadImage(view);
                string label = PokemonDatabase.Get(species) is { } data ? $"{data.DexNumber} {species}" : species;
                Raylib.ImageDrawText(ref board, label, x + 4, y + Box - 16, 10, new Color(30, 30, 40, 255));
                PokemonModels.Release(species);
            }
            Save(board, name);
            Console.WriteLine($"  {Math.Min(list.Length, (page + 1) * Cols * Rows)} of {list.Length} species in {watch.Elapsed.TotalSeconds:F0} s");
        }
    }
}
