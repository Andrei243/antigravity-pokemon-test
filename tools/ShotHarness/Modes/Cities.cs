partial class Harness
{
    // ---------------------------------------------------------------- every town of Sinnoh (plan 01 · M4)

    // The whole of Sinnoh as the importer last wrote it (tools/MapImporter/out/world: run the importer first), with
    // every area open, to look at the buildings and landmarks of towns the game hasn't reached yet. For each town:
    // a shot before each of its different buildings and the things that stand about, and a sheet of them all.
    public void CitiesMode()
    {
        string? repo = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null && repo == null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) repo = dir.FullName;
        string import = Path.Combine(repo ?? startDir, "tools", "MapImporter", "out", "world");
        if (!Directory.Exists(Path.Combine(import, "chunks")))
        {
            Console.WriteLine($"No import at {import}: run `dotnet run --project tools/MapImporter` first.");
            return;
        }

        var index = new WorldIndexFile { Region = "Sinnoh" };
        index.Maps.Add(new WorldMapEntry { Name = "SinnohWhole", Matrix = 0, Trees = TreeStyle.Pine });
        foreach (string file in Directory.GetFiles(Path.Combine(import, "areas"), "*.json")) index.Areas.Add(Path.GetFileNameWithoutExtension(file));
        var built = System.Diagnostics.Stopwatch.StartNew();
        var whole = WorldMapBuilder.Build(World.Open(import, index), index.Maps[0]);
        var all = MapStructures.BuildingsOf(whole);
        Console.WriteLine($"all of Sinnoh: {all.Count} blocks of buildings, {whole.Props.Count} props, made in {built.Elapsed.TotalSeconds:F1} s");

        string[] towns = args.Length > 2 ? args[2..] : new[]
        {
            "twinleaf_town", "sandgem_town", "jubilife_city", "oreburgh_city", "floaroma_town", "valley_windworks_outside", "eterna_city",
            "hearthome_city", "solaceon_town", "veilstone_city", "pastoria_city", "celestic_town", "canalave_city", "snowpoint_city",
            "sunyshore_city", "pokemon_league", "route_209", "route_212_north", "route_213", "valor_lakefront", "route_221",
            "fuego_ironworks_outside", "fight_area", "survival_area", "resort_area"
        };
        var walker = game.Player;
        void Look(string name, int x, int y)
        {
            game.Map = whole;
            walker.SetPosition(Math.Clamp(x, 0, whole.Width - 1), Math.Clamp(y, 0, whole.Height - 1), Direction.Up);
            game.State = GameState.Overworld;
            Frames(3);
            Shot(name);
        }

        foreach (string town in towns)
        {
            if (whole.FindArea(town) == null) { Console.WriteLine($"{town}: no such area on the overworld"); continue; }
            bool Here(int x, int y) => whole.AreaAt(x, y)?.Key == town;
            var shots = new List<string>();

            // One of each model, the town's own buildings before its houses and blocks of flats
            var seen = new HashSet<string>();
            foreach (var b in all.Where(b => !b.Annex && Here(b.X0, b.Y0))
                         .OrderBy(b => b.Kind is BuildingKind.House or BuildingKind.Apartments ? 1 : 0).ThenBy(b => b.Y0).ThenBy(b => b.X0))
            {
                if (!seen.Add(b.Model + (b.Kind == BuildingKind.Gym ? "" : "")) || shots.Count >= 12) continue;
                int x = b.Doors.Count > 0 ? b.Doors[0].X : (b.X0 + b.X1) / 2;
                // Far enough back to see a tall block whole
                int back = 3 + (b.Porch.Count > 0 ? 1 : 0) + (int)MathF.Min(4f, MathF.Max(0f, b.Height - 6f) / 2.5f);
                string name = $"c_{town}_{shots.Count + 1}_{b.Model}";
                Look(name, x, b.Y1 + back);
                shots.Add(name);
                // A tall block's top is out of the frame from the street: look at it from over its roof as well
                if (b.Height > 8f && shots.Count < 12)
                {
                    name = $"c_{town}_{shots.Count + 1}_{b.Model}_top";
                    Look(name, (b.X0 + b.X1) / 2, b.Y0 + 1);
                    shots.Add(name);
                }
            }
            var things = new HashSet<PropType>();
            foreach (var prop in whole.Props.Where(p => p.Model.Length > 0 && Here(p.X, p.Y)).OrderBy(p => p.Y).ThenBy(p => p.X))
            {
                if (!things.Add(prop.Type) || shots.Count >= 15) continue;
                string name = $"c_{town}_{shots.Count + 1}_{prop.Model}";
                Look(name, prop.X + prop.Width / 2, prop.Y + prop.Depth + 2);
                shots.Add(name);
            }
            if (shots.Count == 0) { Console.WriteLine($"{town}: nothing stands there"); continue; }

            // The sheet: three shots to a row at a third of their size
            const int cellW = 640, cellH = 360, gap = 6;
            int rows = (shots.Count + 2) / 3;
            var sheet = Raylib.GenImageColor(3 * cellW + 4 * gap, rows * (cellH + gap) + gap, new Color(24, 26, 34, 255));
            for (int i = 0; i < shots.Count; i++)
            {
                string path = Path.Combine(outDir, shots[i] + ".png");
                if (!File.Exists(path)) continue;
                var img = Raylib.LoadImage(path);
                Raylib.ImageResize(ref img, cellW, cellH);
                Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, cellW, cellH),
                    new Rectangle(gap + i % 3 * (cellW + gap), gap + i / 3 * (cellH + gap), cellW, cellH), Color.White);
                Raylib.UnloadImage(img);
            }
            Save(sheet, "city_" + town);
        }

        // Jubilife City is the densest place there is: its middle by day and after dark
        Look("c_jubilife_middle", 150, 768);
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Look("c_jubilife_night", 150, 768);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        walker.SetPosition(150, 768, Direction.Up);
        Timing("jubilife city (imported)");
    }
}
