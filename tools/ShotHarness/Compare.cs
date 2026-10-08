// The tools that read shots already saved, without starting the game: a contact sheet, a crop and a comparison of two runs
static class Compare
{
    // ---------------------------------------------------------------- every shot at a glance
    //
    //   dotnet run --project tools/ShotHarness -- <dir> contact [name prefix]
    //
    // Puts the shots of a folder on sheets of twenty, four to a row with each one's name under it
    // (`contact_01`, `contact_02`, ...): for looking through a whole run when nothing in particular is being looked
    // for. A prefix keeps only the shots whose names begin with it.
    public static void Contact(string[] args, string outDir)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        // (Only for the default font the names are written in)
        Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
        Raylib.InitWindow(320, 200, "contact");
        string prefix = args.Length > 2 ? args[2] : "";
        var names = Directory.GetFiles(outDir, "*.png").Select(Path.GetFileNameWithoutExtension)
            .Where(n => !n.StartsWith("compare_") && !n.StartsWith("diff_") && !n.StartsWith("crop_") && !n.StartsWith("contact_") && n.StartsWith(prefix))
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        foreach (string old in Directory.GetFiles(outDir, "contact_*.png")) File.Delete(old);
        const int columns = 4, rows = 5, thumbW = 480, thumbH = 270, label = 22, gap = 6;
        for (int first = 0, sheetNumber = 1; first < names.Count; first += columns * rows, sheetNumber++)
        {
            var sheet = Raylib.GenImageColor(columns * (thumbW + gap) + gap, rows * (thumbH + label + gap) + gap, new Color(24, 26, 34, 255));
            for (int i = 0; i < columns * rows && first + i < names.Count; i++)
            {
                var img = Raylib.LoadImage(Path.Combine(outDir, names[first + i] + ".png"));
                // A crop keeps its shape: it is fitted into the cell, not stretched over it
                float fit = Math.Min((float)thumbW / img.Width, (float)thumbH / img.Height);
                int w = Math.Max(1, (int)(img.Width * fit)), h = Math.Max(1, (int)(img.Height * fit));
                Raylib.ImageResize(ref img, w, h);
                int x = gap + i % columns * (thumbW + gap), y = gap + i / columns * (thumbH + label + gap);
                Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, w, h), new Rectangle(x + (thumbW - w) / 2, y + (thumbH - h) / 2, w, h), Color.White);
                Raylib.ImageDrawText(ref sheet, names[first + i], x + 2, y + thumbH + 2, 20, Color.White);
                Raylib.UnloadImage(img);
            }
            Raylib.ExportImage(sheet, Path.Combine(outDir, $"contact_{sheetNumber:00}.png"));
            Raylib.UnloadImage(sheet);
            Console.WriteLine($"contact_{sheetNumber:00}: {names[first]} to {names[Math.Min(names.Count, first + columns * rows) - 1]}");
        }
        Raylib.CloseWindow();
        return;
    }

    // ---------------------------------------------------------------- a closer look at a shot
    //
    //   dotnet run --project tools/ShotHarness -- <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]
    //
    // Cuts a rectangle (in the shot's own pixels) out of a saved shot and enlarges it without smoothing, as
    // `crop_<shot>_<x>_<y>` in the first folder: for judging an edge or a texture pixel by pixel. The same shot of
    // the other folders is cut alike and put beside it, in the order the folders are given.
    public static void Crop(string[] args, string startDir)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        if (args.Length < 8) { Console.WriteLine("usage: <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]"); return; }
        string[] folders = new[] { args[0] }.Concat(args.Skip(8)).Select(f => Path.GetFullPath(f, startDir)).ToArray();
        string shot = args[2];
        int cx = int.Parse(args[3]), cy = int.Parse(args[4]), cw = int.Parse(args[5]), ch = int.Parse(args[6]);
        int zoom = int.Parse(args[7]);
        var sheet = Raylib.GenImageColor(folders.Length * (cw * zoom + 10) + 10, ch * zoom + 20, new Color(24, 26, 34, 255));
        for (int i = 0; i < folders.Length; i++)
        {
            var img = Raylib.LoadImage(Path.Combine(folders[i], shot + ".png"));
            Raylib.ImageCrop(ref img, new Rectangle(cx, cy, cw, ch));
            Raylib.ImageResizeNN(ref img, cw * zoom, ch * zoom);
            int x = 10 + i * (cw * zoom + 10);
            Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, cw * zoom, ch * zoom), new Rectangle(x, 10, cw * zoom, ch * zoom), Color.White);
            Raylib.UnloadImage(img);
        }
        string cropPath = Path.Combine(folders[0], $"crop_{shot}_{cx}_{cy}.png");
        Raylib.ExportImage(sheet, cropPath);
        Console.WriteLine("wrote " + cropPath);
        return;
    }

    // ---------------------------------------------------------------- comparing two runs
    //
    //   dotnet run --project tools/ShotHarness -- <dir> diff <other dir>
    //
    // Compares the shots two runs saved under the same names, pixel by pixel, without starting the game. The harness
    // seeds the game's chance and counts its own clock, so two runs of the same code draw the same pictures and any
    // shot listed here was changed by the code. It prints the shots that differ, the most changed first, and writes a
    // board for each (`diff_<name>`: the other run's shot, this one's, and their difference made eight times stronger).
    public static void Diff(string[] args, string outDir, string startDir)
    {
        if (args.Length < 3) { Console.WriteLine("usage: <dir> diff <other dir>"); return; }
        string otherDir = Path.GetFullPath(args[2], startDir);
        static string NameOf(string path) => Path.GetFileNameWithoutExtension(path);
        bool Compared(string name) => !name.StartsWith("compare_") && !name.StartsWith("diff_") && !name.StartsWith("crop_") && !name.StartsWith("contact_");
        var mine = Directory.GetFiles(outDir, "*.png").Select(NameOf).Where(Compared).ToHashSet();
        var theirs = Directory.GetFiles(otherDir, "*.png").Select(NameOf).Where(Compared).ToHashSet();
        foreach (string old in Directory.GetFiles(outDir, "diff_*.png")) File.Delete(old);

        const int tolerance = 6;
        var changed = new List<(string Name, double Share, double Mean, int Max)>();
        int same = 0;
        foreach (string name in mine.Intersect(theirs).OrderBy(n => n, StringComparer.Ordinal))
        {
            var a = Raylib.LoadImage(Path.Combine(otherDir, name + ".png"));
            var b = Raylib.LoadImage(Path.Combine(outDir, name + ".png"));
            Raylib.ImageFormat(ref a, PixelFormat.UncompressedR8G8B8A8);
            Raylib.ImageFormat(ref b, PixelFormat.UncompressedR8G8B8A8);
            if (a.Width != b.Width || a.Height != b.Height)
            {
                changed.Add((name, 1.0, 255.0, 255));
                Raylib.UnloadImage(a); Raylib.UnloadImage(b);
                continue;
            }
            long over = 0, sum = 0;
            int max = 0, count = a.Width * a.Height;
            var delta = Raylib.GenImageColor(a.Width, a.Height, Color.Black);
            Raylib.ImageFormat(ref delta, PixelFormat.UncompressedR8G8B8A8);
            unsafe
            {
                byte* pa = (byte*)a.Data, pb = (byte*)b.Data, pd = (byte*)delta.Data;
                for (int i = 0; i < count; i++)
                {
                    int worst = 0;
                    for (int c = 0; c < 3; c++)
                    {
                        int d = Math.Abs(pa[i * 4 + c] - pb[i * 4 + c]);
                        sum += d;
                        if (d > worst) worst = d;
                        pd[i * 4 + c] = (byte)Math.Min(255, d * 8);
                    }
                    if (worst > tolerance) over++;
                    if (worst > max) max = worst;
                }
            }
            if (over == 0) same++;
            else
            {
                changed.Add((name, (double)over / count, sum / (count * 3.0), max));
                var board = Raylib.GenImageColor(960 * 3 + 40, 540 + 76, new Color(24, 26, 34, 255));
                int col = 0;
                foreach (var (img, label) in new[] { (a, "OTHER"), (b, "THIS"), (delta, "DIFFERENCE x8") })
                {
                    var small = Raylib.ImageCopy(img);
                    Raylib.ImageResize(ref small, 960, 540);
                    int x = 10 + col * 970;
                    Raylib.ImageDraw(ref board, small, new Rectangle(0, 0, 960, 540), new Rectangle(x, 66, 960, 540), Color.White);
                    Raylib.ImageDrawText(ref board, label, x + 4, 18, 40, Color.White);
                    Raylib.UnloadImage(small);
                    col++;
                }
                Raylib.ExportImage(board, Path.Combine(outDir, "diff_" + name + ".png"));
                Raylib.UnloadImage(board);
            }
            Raylib.UnloadImage(delta); Raylib.UnloadImage(a); Raylib.UnloadImage(b);
        }

        foreach (var c in changed.OrderByDescending(c => c.Share))
            Console.WriteLine($"{c.Name}: {c.Share * 100:F2}% of its pixels differ (mean {c.Mean:F2}, most {c.Max})");
        Console.WriteLine($"{same + changed.Count} shots compared: {same} the same, {changed.Count} differ.");
        var onlyMine = mine.Except(theirs).OrderBy(n => n, StringComparer.Ordinal).ToList();
        var onlyTheirs = theirs.Except(mine).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (onlyMine.Count > 0) Console.WriteLine($"only here ({onlyMine.Count}): {string.Join(", ", onlyMine)}");
        if (onlyTheirs.Count > 0) Console.WriteLine($"only in the other ({onlyTheirs.Count}): {string.Join(", ", onlyTheirs)}");
        return;
    }
}
