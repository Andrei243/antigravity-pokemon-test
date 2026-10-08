partial class Harness
{
    // ---------------------------------------------------------------- Pokémon models (plan 04 · G7)
    public void PokemonMode()
    {
        var context = game.RenderContext;
        var handBuilt = PokemonModels.Species;
        var backdrop = new Color(206, 218, 232, 255);
        Image Studio(string species, string clip, float[] times, float[] yaws, int w, int h, float zoom = 1.5f, float lookY = 0.5f, bool blink = false) =>
            PokemonStudio.Strip(context, species, clip, times, yaws, w, h, zoom, lookY, blink);

        // Species named after the mode (pokemon Kricketot Kricketune …): only their turntables, one under another on a
        // board, for a quick look at models being sculpted
        var named = mode == "pokemon" ? args.Skip(2).ToArray() : Array.Empty<string>();
        if (named.Length > 0)
        {
            const int Box = 300;
            var board = Raylib.GenImageColor(4 * Box, named.Length * Box, backdrop);
            for (int i = 0; i < named.Length; i++)
            {
                var row = Studio(named[i], "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, Box, Box);
                Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, i * Box, row.Width, row.Height), Color.White);
                Raylib.UnloadImage(row);
                Raylib.ImageDrawText(ref board, named[i], 6, i * Box + 6, 20, new Color(30, 30, 40, 255));
            }
            Save(board, "91_turntables");
        }

        // Turntables: front, three-quarter, side and back, for every model and each body plan's sample
        foreach (var species in named.Length > 0 ? Array.Empty<string>() : handBuilt.Append("Generic").Concat(new[] { "sample Serpent", "sample Fish", "sample Floating" }))
        {
            string name = "91_turntable_" + species.Replace("sample ", "sample_").ToLowerInvariant();
            if (Wanted(name)) Save(Studio(species, "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), name);
        }

        // Clips: a row per clip, a column per moment, seen from three-quarters in front
        var clips = new (string Clip, float[] Times)[]
        {
            ("idle", new[] { 0f, 0.42f, 0.85f, 1.27f, 1.7f }),
            ("physical", new[] { 0.14f, 0.26f, 0.4f, 0.48f, 0.66f }),
            ("special", new[] { 0.2f, 0.36f, 0.48f, 0.56f, 0.76f }),
            ("status", new[] { 0.1f, 0.3f, 0.45f, 0.6f, 0.86f }),
            ("hit", new[] { 0.04f, 0.12f, 0.3f, 0.55f, 0.8f }),
            ("faint", new[] { 0.15f, 0.35f, 0.55f, 0.75f, 1f }),
            ("entry", new[] { 0.3f, 0.4f, 0.55f, 0.72f, 0.9f })
        };
        foreach (var species in named.Length > 0 ? Array.Empty<string>() : new[] { "Riolu", "Chimchar", "Shinx", "Turtwig", "Starly", "Garchomp", "Giratina", "sample Serpent", "sample Fish", "sample Floating" })
        {
            string name = "92_clips_" + species.Replace("sample ", "").ToLowerInvariant();
            if (!Wanted(name)) continue;
            const int Box = 200;
            var board = Raylib.GenImageColor(5 * Box, clips.Length * Box, backdrop);
            for (int r = 0; r < clips.Length; r++)
            {
                var row = Studio(species, clips[r].Clip, clips[r].Times, new[] { -0.6f }, Box, Box, 1.7f, 0.42f);
                Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, r * Box, row.Width, row.Height), Color.White);
                Raylib.UnloadImage(row);
            }
            Save(board, name);
        }

        // Generated models (plan 03 · D5): the first species of each body kind, in Pokédex order, that isn't hand-built,
        // turned round
        var firstOfKind = new Dictionary<string, string>();
        foreach (var sp in named.Length > 0 ? Array.Empty<string>() : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name))
        {
            if (handBuilt.Contains(sp, StringComparer.OrdinalIgnoreCase) || PokemonGenomes.For(sp) is not { } genome) continue;
            string kind = genome.Kind.ToString();
            firstOfKind.TryAdd(kind, sp);
        }
        foreach (var species in firstOfKind.Values)
        {
            string name = "91_turntable_gen_" + species.ToLowerInvariant();
            if (Wanted(name)) Save(Studio(species, "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), name);
        }

        // A model brought in from a glTF file: Riolu's own model written out, dropped into overrides/models and read back,
        // turned round and playing the clips the file carries
        if (named.Length == 0 && Wanted("imported"))
        {
            string folder = Path.Combine(outDir, "overrides", "models");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "Riolu.glb");
            GltfWriter.Save(PokemonModels.Get("Riolu"), file);
            void Swap()
            {
                ModelOverrides.Refresh();
                PokemonModels.ForgetSignatures();
                PokemonModels.Release("Riolu");
            }
            Swap();
            Save(Studio("Riolu", "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), "91_turntable_imported_riolu");
            const int Box = 200;
            var board = Raylib.GenImageColor(5 * Box, clips.Length * Box, backdrop);
            for (int r = 0; r < clips.Length; r++)
            {
                var row = Studio("Riolu", clips[r].Clip, clips[r].Times, new[] { -0.6f }, Box, Box, 1.7f, 0.42f);
                Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, r * Box, row.Width, row.Height), Color.White);
                Raylib.UnloadImage(row);
            }
            Save(board, "92_clips_imported_riolu");
            File.Delete(file);
            Swap();
        }

        // Eyes up close: open, blinking, squeezed by a hit, fierce in an attack
        foreach (var (species, lookY) in named.Length > 0 ? Array.Empty<(string, float)>() : new[] { ("Piplup", 0.74f), ("Riolu", 0.66f), ("Turtwig", 0.6f), ("Luxray", 0.7f), ("Starly", 0.78f), ("Gible", 0.68f), ("Chimchar", 0.75f), ("Garchomp", 0.84f) })
        {
            string name = "93_eyes_" + species.ToLowerInvariant();
            if (!Wanted(name)) continue;
            const int Box = 260;
            var board = Raylib.GenImageColor(4 * Box, Box, backdrop);
            var frames = new[] { Studio(species, "idle", new[] { 0.4f }, new[] { 0f }, Box, Box, 0.6f, lookY), Studio(species, "idle", new[] { 0.4f }, new[] { 0f }, Box, Box, 0.6f, lookY, true),
                Studio(species, "hit", new[] { 0.025f }, new[] { 0f }, Box, Box, 0.6f, lookY), Studio(species, "physical", new[] { 0.07f }, new[] { 0f }, Box, Box, 0.6f, lookY) };
            for (int i = 0; i < frames.Length; i++)
            {
                Raylib.ImageDraw(ref board, frames[i], new Rectangle(0, 0, Box, Box), new Rectangle(i * Box, 0, Box, Box), Color.White);
                Raylib.UnloadImage(frames[i]);
            }
            Save(board, name);
        }
    }
}
