#nullable enable
using PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// What a release is built with (plan 16 · T2), made before any game is: no shots of the game, and neither mode starts
/// one. <c>icon</c> draws the game's icon, our own lettering on the title's void, and writes it as <c>icon.ico</c> into
/// the output folder and into <c>PokemonPlatinumEngine/Assets</c> of the repository the harness was started in, which
/// the project file makes the executable's icon. <c>warm</c> meshes what a first start asks for before its first
/// screens into <c>&lt;out dir&gt;/cache/models</c>, which <c>.github/workflows/release.yml</c> ships in every zip.
/// </summary>
static class ReleaseModes
{
    /// <summary>The sizes the icon holds: Explorer's large icons, the taskbar's, a list's and a title bar's.</summary>
    static readonly int[] IconSizes = { 256, 64, 48, 32, 16 };

    public static void Icon(string outDir, string startDir)
    {
        const int big = 1024;
        // The mark as the title draws its lettering, at a size every icon is cut down from
        var target = Raylib.LoadRenderTexture(big, big);
        float scale = UiShapes.PixelScale;
        UiShapes.PixelScale = 1f;
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        // The title's void: violet at the top deepening to crimson, on a rounded square
        var square = new Rectangle(24, 24, big - 48, big - 48);
        UiShapes.Shape(square, 190, new Color(44, 20, 84, 255), new Color(120, 18, 44, 255), new Color(214, 222, 240, 255), 14f);
        UiShapes.Glow(new Vector2(big / 2f, big * 0.42f), big * 0.42f, big * 0.36f, new Color(170, 110, 255, 90));
        // The letters change the colour and leave the square's alpha as it is: a glyph's soft edge must not let the
        // icon show through
        Rlgl.DrawRenderBatchActive();
        Rlgl.ColorMask(true, true, true, false);
        const string letter = "P";
        const float size = 900f;
        float width = UiFonts.MeasureDisplay(letter, size, 0f);
        float x = (big - width) / 2f + 10f, y = big / 2f - size * 0.56f;
        UiFonts.DrawDisplay(letter, x + 18f, y + 26f, size, 0f, new Color(10, 6, 24, 200));
        UiFonts.DrawDisplayGradient(letter, x, y, size, 0f, new Color(255, 255, 255, 255), new Color(150, 162, 196, 255));
        Rlgl.DrawRenderBatchActive();
        Rlgl.ColorMask(true, true, true, true);
        Raylib.EndTextureMode();
        UiShapes.PixelScale = scale;

        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        Raylib.UnloadRenderTexture(target);
        var pictures = new List<(int Size, byte[] Png)>();
        foreach (int s in IconSizes)
        {
            var copy = Raylib.ImageCopy(image);
            Raylib.ImageResize(ref copy, s, s);
            string png = Path.Combine(outDir, $"icon_{s}.png");
            Raylib.ExportImage(copy, png);
            Raylib.UnloadImage(copy);
            pictures.Add((s, File.ReadAllBytes(png)));
            Console.WriteLine($"wrote icon_{s}");
        }
        Raylib.ExportImage(image, Path.Combine(outDir, "icon_1024.png"));
        Raylib.UnloadImage(image);

        byte[] icon = IconFile.Write(pictures);
        File.WriteAllBytes(Path.Combine(outDir, "icon.ico"), icon);
        Console.WriteLine($"wrote icon.ico ({icon.Length:N0} bytes: {string.Join(", ", IconFile.SizesIn(icon))})");
        if (RepositoryFrom(startDir) is { } repository)
        {
            string assets = Path.Combine(repository, "PokemonPlatinumEngine", "Assets", "icon.ico");
            File.WriteAllBytes(assets, icon);
            Console.WriteLine($"wrote {assets}");
        }
        else Console.WriteLine("!! no repository above the folder the harness was started in: Assets/icon.ico was not written");
    }

    public static void Warm(string outDir)
    {
        string folder = Path.Combine(outDir, "cache", "models");
        Directory.CreateDirectory(folder);
        SdfCache.Folder = folder;
        var clock = System.Diagnostics.Stopwatch.StartNew();

        // The Pokémon the start asks for (GameEngine.Initialize): the stand-in, the title's and the introduction's,
        // and the opening's twenty-four, whose menu sprites are baked from their models at the first start
        var species = new[] { PokemonSprites.Fallback }.Concat(PokemonModels.ShownAtOnce).Concat(PokemonModels.Preloaded)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (string name in species) PokemonModels.Get(name);
        Console.WriteLine($"  {species.Count} Pokémon: {string.Join(", ", species)}");

        // The people of the introduction and of the first map, the player's room and the house under it, both looks
        MapDatabase.Initialize();
        var looks = new List<string> { "PLAYER", "DAWN", "ROWAN" };
        foreach (string map in new[] { "PlayerHouse2F", "PlayerHouse" })
            looks.AddRange(MapDatabase.Get(map).Everyone.Where(n => !n.IsThing && !n.IsPokemon).Select(n => PlayerIdentity.CharacterFor(n.NpcType)));
        looks = looks.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (string look in looks) CharacterModels.Build(look);
        Console.WriteLine($"  {looks.Count} people: {string.Join(", ", looks)}");

        // The balls the battles throw
        string[] balls = { "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball" };
        foreach (string ball in balls) BattleBall.Get(ball);
        Console.WriteLine($"  {balls.Length} balls");

        var files = Directory.GetFiles(folder, "*.mesh");
        long bytes = files.Sum(f => new FileInfo(f).Length);
        Console.WriteLine($"warm: {files.Length} meshes, {bytes / 1048576.0:0.0} MB in {folder}, {clock.Elapsed.TotalSeconds:0.0} s");
    }

    /// <summary>The repository the harness was started in or under: the first folder upward with the solution.</summary>
    static string? RepositoryFrom(string startDir)
    {
        for (var dir = new DirectoryInfo(startDir); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        return null;
    }
}
