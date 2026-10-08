partial class Harness
{
    // ---------------------------------------------------------------- contact sheets
    public void SheetsMode()
    {
        // Every species: front sprite, back sprite and menu icon, as baked from the 3D models
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).Where(PixelArtGenerator.HasOwnModel).ToArray();
        // Only the preloaded ones are baked at start-up: ask for the rest and wait for them
        foreach (var sp in species.Take(24)) PokemonSprites.Request(sp);
        PokemonSprites.Flush(game.RenderContext);
        var sheet = Raylib.LoadRenderTexture(1920, 1080);
        Raylib.BeginTextureMode(sheet);
        Raylib.ClearBackground(new Color(200, 220, 240, 255));
        for (int i = 0; i < species.Length && i < 24; i++)
        {
            int x = i % 8 * 240, y = i / 8 * 360;
            Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(species[i], false), new Vector2(x, y), 0, 1.5f, Color.White);
            Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(species[i], true), new Vector2(x + 40, y + 180), 0, 1f, Color.White);
            Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonIcon(species[i]), new Vector2(x + 180, y + 190), 0, 1f, Color.White);
            Raylib.DrawText(species[i], x + 4, y + 330, 20, Color.Black);
        }
        Raylib.EndTextureMode();
        var img = Raylib.LoadImageFromTexture(sheet.Texture);
        Raylib.ImageFlipVertical(ref img);
        Save(img, "90_pokemon_sheet");
        Raylib.UnloadRenderTexture(sheet);

        // On its own, the mode goes on through every species in Pokédex order, sixty to a page: the front sprite with the
        // icon beside it (plan 03 · D5). The first run bakes them all (and meshes their models), later runs read the cache
        if (mode == "sheets")
        {
            var context = game.RenderContext;
            var everyone = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToArray();
            const int Cols = 12, Rows = 5, W = 160, H = 216;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int page = 0; page * Cols * Rows < everyone.Length; page++)
            {
                string name = $"90_pokemon_sheet_{page + 1:00}";
                if (!Wanted(name)) continue;
                var onPage = everyone.Skip(page * Cols * Rows).Take(Cols * Rows).ToArray();
                foreach (var sp in onPage) PokemonSprites.Request(sp.Name);
                PokemonSprites.Flush(context);
                var target = Raylib.LoadRenderTexture(Cols * W, Rows * H);
                Raylib.BeginTextureMode(target);
                Raylib.ClearBackground(new Color(200, 220, 240, 255));
                for (int i = 0; i < onPage.Length; i++)
                {
                    int x = i % Cols * W, y = i / Cols * H;
                    Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(onPage[i].Name, false), new Vector2(x + 4, y + 4), 0, 1f, Color.White);
                    Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonIcon(onPage[i].Name), new Vector2(x + 108, y + 140), 0, 1f, Color.White);
                    Raylib.DrawText($"{onPage[i].DexNumber} {onPage[i].Name}", x + 4, y + 192, 10, Color.Black);
                }
                Raylib.EndTextureMode();
                var pageImage = Raylib.LoadImageFromTexture(target.Texture);
                Raylib.ImageFlipVertical(ref pageImage);
                Save(pageImage, name);
                Raylib.UnloadRenderTexture(target);
                foreach (var sp in onPage) PokemonModels.Release(sp.Name);
                Console.WriteLine($"  {page * Cols * Rows + onPage.Length} of {everyone.Length} species in {watch.Elapsed.TotalSeconds:F0} s");
            }
        }
    }
}
