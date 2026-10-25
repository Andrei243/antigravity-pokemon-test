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
        // --shiny: every model in its shiny colours (plan 09 · L1), boards 94_dex_shiny_NN
        bool shiny = args.Skip(2).Contains("--shiny");
        // --females: every species with a female look (plan 09 · L6), each male with its female beside it, boards
        // 94_dex_females_NN (or only the species named, each with its female where it has one)
        bool females = args.Skip(2).Contains("--females");
        var named = args.Skip(2).Where(a => a != "--back" && a != "--shiny" && a != "--females" && a != "--prints").ToArray();
        // --prints: one board of every species' footprint (plan 09 · L9), 94_prints, and no boards of models
        if (args.Skip(2).Contains("--prints"))
        {
            Prints(named.Length > 0 ? named : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).ToArray());
            return;
        }
        var list = named.Length > 0 ? named : females ? PokemonModels.Females.ToArray() : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).ToArray();
        if (females) list = list.SelectMany(s => PokemonLook.HasFemale(s) ? new[] { s, s + PokemonLook.FemaleSuffix } : new[] { s }).ToArray();
        const int Cols = 10, Rows = 6, Box = 192;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int page = 0; page * Cols * Rows < list.Length; page++)
        {
            string kind = (back ? "back_" : "") + (shiny ? "shiny_" : "");
            string name = females ? $"94_dex_females_{kind}{page + 1:00}"
                : (back, shiny) switch { (true, true) => $"94_dex_shiny_back_{page + 1:00}", (true, false) => $"94_dex_back_{page + 1:00}", (false, true) => $"94_dex_shiny_{page + 1:00}", _ => $"94_dex_{page + 1:00}" };
            if (!Wanted(name)) continue;
            var board = Raylib.GenImageColor(Cols * Box, Rows * Box, new Color(206, 218, 232, 255));
            for (int i = 0; i < Cols * Rows && page * Cols * Rows + i < list.Length; i++)
            {
                string species = shiny ? PokemonLook.Shiny(list[page * Cols * Rows + i]) : list[page * Cols * Rows + i];
                var view = PokemonStudio.Strip(context, species, "idle", new[] { 0.4f }, new[] { back ? 2.6f : -0.55f }, Box, Box, 1.3f, 0.5f, false);
                int x = i % Cols * Box, y = i / Cols * Box;
                Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, Box, Box), new Rectangle(x, y, Box, Box), Color.White);
                Raylib.UnloadImage(view);
                string label = PokemonDatabase.Get(PokemonLook.Base(species)) is { } data ? $"{data.DexNumber} {species}" : species;
                Raylib.ImageDrawText(ref board, label, x + 4, y + Box - 16, 10, new Color(30, 30, 40, 255));
                PokemonModels.Release(species);
            }
            Save(board, name);
            Console.WriteLine($"  {Math.Min(list.Length, (page + 1) * Cols * Rows)} of {list.Length} species in {watch.Elapsed.TotalSeconds:F0} s");
        }
    }

    // Every species' footprint on one board (plan 09 · L9), forty to a row in Pokédex order: each print at 2× in ink on
    // a pale cell with its number under it, a cell left plain for a species that leaves none. Each model is built,
    // printed and let go; it prints how many species leave one and how many inked texels the smallest print has
    void Prints(string[] list)
    {
        const string name = "94_prints";
        if (!Wanted(name)) return;
        const int Cols = 40, Cell = 48, Scale = 2;
        int rows = (list.Length + Cols - 1) / Cols;
        var board = Raylib.GenImageColor(Cols * Cell, rows * Cell, new Color(206, 218, 232, 255));
        var ink = new Color(36, 44, 68, 255);
        int leave = 0, smallest = int.MaxValue;
        string smallestName = "";
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < list.Length; i++)
        {
            int x = i % Cols * Cell, y = i / Cols * Cell;
            Raylib.ImageDrawRectangle(ref board, x + 2, y + 2, Cell - 4, Cell - 4, new Color(246, 248, 252, 255));
            if (Footprints.Now(list[i]) is { } print)
            {
                leave++;
                if (print.Count < smallest) { smallest = print.Count; smallestName = list[i]; }
                int left = x + (Cell - Footprint.Size * Scale) / 2, top = y + 2;
                for (int py = 0; py < Footprint.Size; py++)
                    for (int px = 0; px < Footprint.Size; px++)
                        if (print[px, py]) Raylib.ImageDrawRectangle(ref board, left + px * Scale, top + py * Scale, Scale, Scale, ink);
            }
            string label = PokemonDatabase.Get(list[i]) is { } data ? data.DexNumber.ToString() : list[i];
            Raylib.ImageDrawText(ref board, label, x + 4, y + Cell - 12, 10, new Color(110, 120, 148, 255));
            if ((i + 1) % 100 == 0) Console.WriteLine($"  {i + 1} of {list.Length} prints in {watch.Elapsed.TotalSeconds:F0} s");
        }
        Save(board, name);
        Console.WriteLine($"  {leave} of {list.Length} species leave a footprint; the smallest is {smallestName}'s, {smallest} texels");
    }
}
