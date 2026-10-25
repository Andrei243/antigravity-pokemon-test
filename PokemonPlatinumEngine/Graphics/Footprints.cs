using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A species' footprint (plan 09 · L9; style guide, "Menu screens (G10)", the Pokédex): a square of
/// <see cref="Size"/> by <see cref="Size"/> texels of ink, each one inked or not. Plain data, with no drawing.
/// </summary>
internal sealed class Footprint : IEquatable<Footprint>
{
    public const int Size = 16;

    private readonly bool[] ink;

    public Footprint(bool[] ink)
    {
        if (ink.Length != Size * Size) throw new ArgumentException($"a footprint is {Size} by {Size}", nameof(ink));
        this.ink = ink;
    }

    /// <summary>Whether the texel at column <paramref name="x"/>, row <paramref name="y"/> (row 0 the toes' end) is inked.</summary>
    public bool this[int x, int y] => x >= 0 && y >= 0 && x < Size && y < Size && ink[y * Size + x];

    /// <summary>How many texels are inked.</summary>
    public int Count
    {
        get
        {
            int n = 0;
            foreach (bool b in ink) if (b) n++;
            return n;
        }
    }

    /// <summary>Sixteen rows of <c>#</c> and <c>.</c>, a line each: what the cache keeps and the tests compare.</summary>
    public string Encode()
    {
        var sb = new StringBuilder(Size * (Size + 1));
        for (int y = 0; y < Size; y++)
        {
            if (y > 0) sb.Append('\n');
            for (int x = 0; x < Size; x++) sb.Append(ink[y * Size + x] ? '#' : '.');
        }
        return sb.ToString();
    }

    /// <summary>The footprint <see cref="Encode"/> wrote; null for anything else.</summary>
    public static Footprint? Decode(string text)
    {
        var rows = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length != Size) return null;
        var ink = new bool[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            if (rows[y].Length != Size) return null;
            for (int x = 0; x < Size; x++)
            {
                char c = rows[y][x];
                if (c is not ('#' or '.')) return null;
                ink[y * Size + x] = c == '#';
            }
        }
        return new Footprint(ink);
    }

    /// <summary>The print as a picture: white where inked and clear elsewhere, tinted as it is drawn.</summary>
    public PixelCanvas ToCanvas()
    {
        var canvas = new PixelCanvas(Size, Size);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (ink[y * Size + x]) canvas.SetRaw(x, y, new Color(255, 255, 255, 255));
        return canvas;
    }

    public bool Equals(Footprint? other) => other != null && ink.AsSpan().SequenceEqual(other.ink);
    public override bool Equals(object? obj) => Equals(obj as Footprint);
    public override int GetHashCode()
    {
        int h = 17;
        for (int i = 0; i < ink.Length; i++) if (ink[i]) h = h * 31 + i;
        return h;
    }
}

/// <summary>
/// Footprints (plan 09 · L9; style guide, "Menu screens (G10)", the Pokédex). A species' print is made from its own
/// model, never taken from the games: the triangles of one foot that lie within a band of its lowest point, seen from
/// below and laid into a <see cref="Footprint"/> with the toes at the top (<see cref="Make"/>, GPU-free). A
/// <see cref="BodyPlan.Biped"/>, <see cref="BodyPlan.Quadruped"/> or <see cref="BodyPlan.Bird"/> always leaves one
/// of at least <see cref="Least"/> texels (the body's own underside where no leg carries enough, a plain paw where
/// even that comes to too little); a fish, a floater and a serpent leave none, as Platinum shows none for those that
/// don't walk. Kept in <c>cache/sprites</c> beside the menu sprites under the model's <see cref="PokemonModels.Signature"/>,
/// so a model is made for a print only once; the Pokédex's list shows only prints already made (<see cref="HeldIcon"/>)
/// and the opened entry asks for one (<see cref="Icon"/>).
/// </summary>
internal static class Footprints
{
    /// <summary>Raise when the way a print is made changes, so cached prints are made again.</summary>
    public const int Version = 1;

    /// <summary>The fewest texels a walking species' print has.</summary>
    public const int Least = 8;

    /// <summary>Texels across the print's longer side: the rest of the square is its margin.</summary>
    public const float Across = 12f;

    /// <summary>The weight a vertex gives a leg's bone to be part of its foot (the decals' rule).</summary>
    public const float LegWeight = 0.3f;

    /// <summary>Whether a body plan walks, and so leaves a print.</summary>
    public static bool LeavesOne(BodyPlan plan) => plan is BodyPlan.Biped or BodyPlan.Quadruped or BodyPlan.Bird;

    // ------------------------------------------------------------------ making one (GPU-free)

    /// <summary>A model's footprint, or null for a body that leaves none. The same model always gives the same print.</summary>
    public static Footprint? Make(PokeModel model)
    {
        if (!LeavesOne(model.Plan)) return null;
        var mesh = model.Mesh;
        if (mesh.VertexCount == 0 || mesh.TriangleCount == 0 || model.Bones.Count == 0) return Paw();
        int foot = FootBone(model);
        Footprint? sole = foot >= 0 ? Sole(model, v => mesh.WeightOf(v, foot) >= LegWeight) : null;
        // A body that stands on itself (no leg reaching the ground, or one too small to show): its own underside
        if (sole == null || sole.Count < Least) sole = Sole(model, _ => true);
        if (sole == null || sole.Count < Least) return Paw();
        var print = Style(model.Plan, sole);
        return print.Count >= Least ? print : sole;
    }

    /// <summary>
    /// The sole drawn as a print of its kind, inside its own bounds: a bird's three toes forward and one back as
    /// lines; a quadruped's pad with three toes (four on a broad paw) parted from it by a row; a biped's sole with its
    /// toes parted the same way, three of them. A sole too small to part is left whole.
    /// </summary>
    internal static Footprint Style(BodyPlan plan, Footprint sole)
    {
        const int n = Footprint.Size;
        int top = n, bottom = -1, left = n, right = -1;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if (sole[x, y])
                {
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                    left = Math.Min(left, x); right = Math.Max(right, x);
                }
        int height = bottom - top + 1, width = right - left + 1;
        if (height < 6 || width < 5) return sole;
        var ink = new bool[n * n];
        if (plan == BodyPlan.Bird)
        {
            // Three toes from the heel to the sole's front corners and middle, a fourth straight back, a texel thick
            // (two, side by side, ran together into a wedge)
            float cx = MathF.Floor((left + right) / 2f);
            int heel = top + (int)MathF.Round(height * 0.62f);
            const bool thick = false;
            void Line(float x0, float y0, float x1, float y1)
            {
                int steps = (int)MathF.Ceiling(MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0))) * 2 + 1;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    int x = (int)MathF.Floor(x0 + (x1 - x0) * t + 0.5f), y = (int)MathF.Floor(y0 + (y1 - y0) * t + 0.5f);
                    for (int dx = 0; dx <= (thick ? 1 : 0); dx++)
                        if (x + dx >= 0 && x + dx < n && y >= 0 && y < n) ink[y * n + x + dx] = true;
                }
            }
            float mid = thick ? cx - 0.5f : cx;
            Line(mid, heel, left, top);
            Line(mid, heel, mid, top);
            Line(mid, heel, right - (thick ? 1 : 0), top);
            Line(mid, heel, mid, bottom);
            return new Footprint(ink);
        }
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                ink[y * n + x] = sole[x, y];
        // The row that parts the toes from the pad, and the columns that part the toes from one another
        int gap = top + (int)MathF.Round(height * (plan == BodyPlan.Quadruped ? 0.38f : 0.3f));
        for (int x = 0; x < n; x++) ink[gap * n + x] = false;
        int toes = plan == BodyPlan.Quadruped && width >= 10 ? 4 : 3;
        for (int k = 1; k < toes; k++)
        {
            int column = left + (int)MathF.Round(width * k / (float)toes);
            for (int y = top; y < gap; y++) ink[y * n + column] = false;
        }
        return new Footprint(ink);
    }

    /// <summary>
    /// The leg whose foot is printed: the front left one where there is one, else a left one, else one in the
    /// middle, else any; among those, the one that reaches lowest (the first bone where two reach as low).
    /// </summary>
    internal static int FootBone(PokeModel model)
    {
        var mesh = model.Mesh;
        var lowest = new float[model.Bones.Count];
        Array.Fill(lowest, float.MaxValue);
        for (int v = 0; v < mesh.VertexCount; v++)
            for (int k = 0; k < 4; k++)
            {
                int bone = mesh.BoneIndices[v * 4 + k];
                if (bone < lowest.Length && model.Bones[bone].Role == PokeRole.Leg && mesh.BoneWeights[v * 4 + k] >= LegWeight)
                    lowest[bone] = MathF.Min(lowest[bone], mesh.Positions[v].Y);
            }
        int best = -1, bestRank = int.MaxValue;
        float bestLow = float.MaxValue;
        for (int b = 0; b < model.Bones.Count; b++)
        {
            var info = model.Bones[b];
            if (info.Role != PokeRole.Leg || lowest[b] == float.MaxValue) continue;
            int rank = info.Side < 0 && info.Front ? 0 : info.Side < 0 ? 1 : info.Side == 0 ? 2 : 3;
            if (rank < bestRank || (rank == bestRank && lowest[b] < bestLow - 1e-6f))
            {
                best = b;
                bestRank = rank;
                bestLow = lowest[b];
            }
        }
        return best;
    }

    /// <summary>
    /// The sole of the vertices <paramref name="picked"/>: their triangles that lie within a band of their lowest
    /// point, seen from below, scaled so their longer side is <see cref="Across"/> texels and set in the square's middle.
    /// </summary>
    internal static Footprint? Sole(PokeModel model, Func<int, bool> picked)
    {
        var mesh = model.Mesh;
        var take = new bool[mesh.VertexCount];
        float low = float.MaxValue;
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (!picked(v)) continue;
            take[v] = true;
            low = MathF.Min(low, mesh.Positions[v].Y);
        }
        if (low == float.MaxValue) return null;
        float band = MathF.Max(3f * model.Cell, 0.05f * MathF.Max(model.Height, 0.1f));
        var tris = new List<int>();
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        for (int t = 0; t < mesh.TriangleCount; t++)
        {
            int a = mesh.Indices[t * 3], b = mesh.Indices[t * 3 + 1], c = mesh.Indices[t * 3 + 2];
            if (!take[a] || !take[b] || !take[c]) continue;
            var pa = mesh.Positions[a]; var pb = mesh.Positions[b]; var pc = mesh.Positions[c];
            if (pa.Y > low + band || pb.Y > low + band || pc.Y > low + band) continue;
            tris.Add(t);
            minX = MathF.Min(minX, MathF.Min(pa.X, MathF.Min(pb.X, pc.X)));
            maxX = MathF.Max(maxX, MathF.Max(pa.X, MathF.Max(pb.X, pc.X)));
            minZ = MathF.Min(minZ, MathF.Min(pa.Z, MathF.Min(pb.Z, pc.Z)));
            maxZ = MathF.Max(maxZ, MathF.Max(pa.Z, MathF.Max(pb.Z, pc.Z)));
        }
        if (tris.Count == 0) return null;
        float span = MathF.Max(maxX - minX, maxZ - minZ);
        if (span <= 1e-5f) return null;
        float scale = Across / span;
        float x0 = (Footprint.Size - (maxX - minX) * scale) / 2f, y0 = (Footprint.Size - (maxZ - minZ) * scale) / 2f;
        var ink = new bool[Footprint.Size * Footprint.Size];
        foreach (int t in tris)
        {
            // In texels: x to the right, the toes (+Z) at the top
            var p = new System.Numerics.Vector2[3];
            for (int k = 0; k < 3; k++)
            {
                var q = mesh.Positions[mesh.Indices[t * 3 + k]];
                p[k] = new System.Numerics.Vector2(x0 + (q.X - minX) * scale, y0 + (maxZ - q.Z) * scale);
            }
            int left = Math.Max(0, (int)MathF.Floor(MathF.Min(p[0].X, MathF.Min(p[1].X, p[2].X))));
            int right = Math.Min(Footprint.Size - 1, (int)MathF.Ceiling(MathF.Max(p[0].X, MathF.Max(p[1].X, p[2].X))));
            int top = Math.Max(0, (int)MathF.Floor(MathF.Min(p[0].Y, MathF.Min(p[1].Y, p[2].Y))));
            int bottom = Math.Min(Footprint.Size - 1, (int)MathF.Ceiling(MathF.Max(p[0].Y, MathF.Max(p[1].Y, p[2].Y))));
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                    if (Inside(new System.Numerics.Vector2(x + 0.5f, y + 0.5f), p[0], p[1], p[2])) ink[y * Footprint.Size + x] = true;
        }
        return new Footprint(ink);
    }

    private static bool Inside(System.Numerics.Vector2 q, System.Numerics.Vector2 a, System.Numerics.Vector2 b, System.Numerics.Vector2 c)
    {
        static float Cross(System.Numerics.Vector2 o, System.Numerics.Vector2 u, System.Numerics.Vector2 v) => (u.X - o.X) * (v.Y - o.Y) - (u.Y - o.Y) * (v.X - o.X);
        float d1 = Cross(a, b, q), d2 = Cross(b, c, q), d3 = Cross(c, a, q);
        bool negative = d1 < 0 || d2 < 0 || d3 < 0, positive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(negative && positive);
    }

    /// <summary>A plain paw of our own, for a walking body whose model gives too little: a pad and three toes.</summary>
    public static Footprint Paw()
    {
        var ink = new bool[Footprint.Size * Footprint.Size];
        void Disc(float cx, float cy, float rx, float ry)
        {
            for (int y = 0; y < Footprint.Size; y++)
                for (int x = 0; x < Footprint.Size; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) ink[y * Footprint.Size + x] = true;
                }
        }
        Disc(8f, 10.5f, 3.6f, 3.1f);
        Disc(4.2f, 5.6f, 1.6f, 1.8f);
        Disc(8f, 4.2f, 1.6f, 1.8f);
        Disc(11.8f, 5.6f, 1.6f, 1.8f);
        return new Footprint(ink);
    }

    // ------------------------------------------------------------------ keeping them

    /// <summary>Prints known this run by species: null for a species that leaves none.</summary>
    private static readonly ConcurrentDictionary<string, Footprint?> Known = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Species whose print the cache was found not to hold this run (so a list doesn't look again every frame).</summary>
    private static readonly HashSet<string> NotOnDisk = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Prints being made from a model in the background.</summary>
    private static readonly ConcurrentDictionary<string, bool> Working = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Models borrowed for a print, given back once it is made.</summary>
    private static readonly HashSet<string> Borrowing = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Makes an opened entry's print at once, from its model built on the spot, and leaves the cache of earlier runs
    /// unread, so that what a screen shows hangs on this run alone: the harness sets it, so two runs draw the same pictures.
    /// </summary>
    public static bool Patient { get; set; }

    /// <summary>Whether a species' print is known this run, and what it is (null: it leaves none).</summary>
    public static bool TryHeld(string species, out Footprint? print)
    {
        if (Known.TryGetValue(species, out print)) return true;
        if (Patient || NotOnDisk.Contains(species)) return false;
        if (TryLoad(species, out print))
        {
            Known[species] = print;
            return true;
        }
        NotOnDisk.Add(species);
        return false;
    }

    /// <summary>
    /// A species' print now, made from its model at once if need be (the model is built on this thread and let go
    /// after): for a tool, such as the harness's board of every print, never for a frame of the game.
    /// </summary>
    public static Footprint? Now(string species)
    {
        if (TryHeld(species, out var held)) return held;
        bool had = PokemonModels.Holds(species);
        var print = Make(PokemonModels.Get(species));
        Keep(species, print);
        if (!had) PokemonModels.Release(species);
        return print;
    }

    /// <summary>
    /// The print's picture for an entry looked at (main thread): asks for its model in the background when the print
    /// isn't made yet, and is null until it is, or for a species that leaves none.
    /// </summary>
    public static Texture2D? Icon(string species)
    {
        if (Patient) return Now(species) is { } made ? TextureOf(species, made) : null;
        if (TryHeld(species, out var print))
        {
            if (Borrowing.Remove(species)) PokemonModels.Return(species);
            return print == null ? null : TextureOf(species, print);
        }
        if (Working.ContainsKey(species)) return null;
        if (PokemonModels.TryGet(species, out var model))
        {
            Working[species] = true;
            Task.Run(() =>
            {
                try { Keep(species, Make(model)); }
                finally { Working.TryRemove(species, out _); }
            });
        }
        else if (Borrowing.Add(species)) PokemonModels.Borrow(species);
        return null;
    }

    /// <summary>The print's picture if it is made already (here or on an earlier run), never asking for a model: what a list draws.</summary>
    public static Texture2D? HeldIcon(string species) =>
        TryHeld(species, out var print) && print != null ? TextureOf(species, print) : null;

    private static Texture2D TextureOf(string species, Footprint print)
    {
        if (!Textures.TryGetValue(species, out var texture))
        {
            texture = print.ToCanvas().ToTexture();
            Textures[species] = texture;
        }
        return texture;
    }

    private static void Keep(string species, Footprint? print)
    {
        Known[species] = print;
        if (!PokemonSprites.CacheEnabled) return;
        string file = FileOf(species);
        string stem = Path.GetFileName(file)[..^(SignatureOf(species).Length + 4)];
        string text = print?.Encode() ?? "none";
        CacheFolders.Write(file, stream => stream.Write(Encoding.ASCII.GetBytes(text)),
            () => Directory.GetFiles(PokemonSprites.CacheFolder, stem + "*.txt"), CacheFolders.Prunes);
    }

    private static bool TryLoad(string species, out Footprint? print)
    {
        print = null;
        if (!PokemonSprites.CacheEnabled) return false;
        string file = FileOf(species);
        if (!File.Exists(file)) return false;
        string text;
        try { text = File.ReadAllText(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
        if (text.Trim() == "none") return true;
        print = Footprint.Decode(text);
        return print != null;
    }

    private static string SignatureOf(string species) => $"{PokemonModels.Signature(species)}{Version}";

    private static string FileOf(string species)
    {
        var safe = new StringBuilder();
        foreach (char c in species) safe.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToLowerInvariant(c) : '_');
        return Path.Combine(PokemonSprites.CacheFolder, $"{safe}-print-{SignatureOf(species)}.txt");
    }
}
