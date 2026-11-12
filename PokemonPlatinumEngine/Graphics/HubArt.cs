using System;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The furniture of the hub's halls (plan 41 · EG2, EG4; style guide, "Rooms", "The Laurel Gallery"): a plinth of pale
/// stone for each region finished, a laurel wreath in the region's own colour on its face and standing on its slab, built in
/// the rooms' kit like <see cref="PropModels"/>, with no GPU call. Which region a plinth is for is its prop's
/// <see cref="Prop.Model"/> (a region's id, <c>earlier</c> or <c>Circuit</c>), which the gallery is furnished with.
/// </summary>
internal static class HubArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>Whether a prop is built here: a plinth or a banner.</summary>
    public static bool Builds(Prop p) => p.Type is PropType.Plinth or PropType.Banner;

    /// <summary>
    /// A team's banner in Grayfall Keep (plan 41 · EG9; style guide, "The hub's halls"): its field and its band, our own
    /// colours for each team that gathers there, never a mark of theirs. The stage's banner is a curtain of gold on red.
    /// </summary>
    /// <summary>Whose bay of Grayfall Keep a tile's column is (Hub_Keep: six bays of five, one tile between).</summary>
    public static string KeepTeamAt(int x) => (x / 6) switch
    {
        0 => "galactic", 1 => "rocket", 2 => "hoenn", 3 => "stage", 4 => "plasma", 5 => "flare", _ => "skull"
    };

    public static (Color Field, Color Band) BannerColours(string? team) => team?.ToLowerInvariant() switch
    {
        "galactic" => (Rgb(92, 106, 138), Rgb(214, 220, 232)),
        "rocket" => (Rgb(46, 42, 48), Rgb(200, 58, 58)),
        "hoenn" => (Rgb(176, 58, 46), Rgb(52, 98, 176)),
        "plasma" => (Rgb(208, 212, 222), Rgb(54, 84, 156)),
        "flare" => (Rgb(196, 64, 42), Rgb(34, 34, 42)),
        // Alola's bay (plan 41 · EG9's owed row): a slate field with a bone-white band
        "skull" => (Rgb(58, 52, 70), Rgb(226, 224, 232)),
        _ => (Rgb(150, 40, 52), Rgb(232, 196, 84))
    };

    /// <summary>
    /// A region's laurel colour (style guide's table): the colour of its plinth's wreath and slab and of its tab in the
    /// Hall of Fame. Bronze for the earlier teams, gold for the circuit, a stone grey for a region the table doesn't name.
    /// </summary>
    public static Color LaurelColour(string? region) => region?.ToLowerInvariant() switch
    {
        "sinnoh" => Rgb(132, 120, 196),
        "kanto" => Rgb(206, 86, 72),
        "johto" => Rgb(212, 168, 64),
        "hoenn" => Rgb(66, 160, 112),
        "unova" => Rgb(96, 104, 124),
        "kalos" => Rgb(70, 128, 212),
        "alola" => Rgb(236, 140, 64),
        "galar" => Rgb(186, 60, 96),
        "paldea" => Rgb(164, 84, 168),
        "hisui" => Rgb(120, 148, 104),
        "earlier" => Rgb(170, 120, 74),
        "circuit" => Rgb(232, 196, 84),
        _ => Rgb(150, 150, 160)
    };

    public static void Build(KitBuilder kit, Prop p, Map map)
    {
        if (p.Type == PropType.Banner)
        {
            Banner(kit, p);
            return;
        }
        var colour = LaurelColour(p.Model);
        string key = p.Model.ToLowerInvariant();
        var stone = Tone.Of(226, 222, 212, 244, 242, 236, 190, 184, 172);
        var slab = Tone.Of(colour);

        // The base: pale stone, a laurel wreath of the region's colour carved into its face
        var face = kit.Face($"plinth.face.{key}", 22, 16, c =>
        {
            Pix.Raised(c, 0, 0, 22, 16, stone);
            Wreath(c, 11, 8, 5.2f, colour);
        });
        var side = kit.Face("plinth.side", 22, 16, c => Pix.Raised(c, 0, 0, 22, 16, stone));
        var top = kit.Face("plinth.top", 22, 22, c => c.Rect(0, 0, 22, 22, stone.Light));
        kit.Box(5, 27, 6, 28, 0, 16, top, face, side, side);
        // The slab of the region's colour, a little wider
        kit.Block($"plinth.slab.{key}", slab, 4, 28, 5, 29, 16, 19);
        // A wreath standing on the slab, in the colour and a band of gold through it
        var standing = kit.Face($"plinth.wreath.{key}", 16, 16, c => Wreath(c, 7.5f, 7.5f, 6.4f, colour));
        kit.Sprite(standing, 16, 17, 19);
    }

    /// <summary>
    /// A banner hung on the back wall over its tile: a cloth 20 texels wide and 34 long on a rod of dark iron, its field the
    /// team's, a band of the other colour across it two thirds down and a swallowtail cut at its foot. Its top 64 texels
    /// over the floor.
    /// </summary>
    private static void Banner(KitBuilder kit, Prop p)
    {
        // A map file's prop names no team: the keep's bays are the teams' in the order they stand, west to east
        string team = p.Model.Length > 0 ? p.Model : KeepTeamAt(p.X);
        var (field, band) = BannerColours(team);
        string key = team.ToLowerInvariant();
        const int w = 20, h = 34;
        var art = kit.Face($"banner.{key}", w + 4, h + 3, c =>
        {
            var rod = Rgb(54, 50, 58);
            c.Rect(0, 0, w + 4, 2, rod);
            var f = Tone.Of(field);
            for (int y = 2; y < h + 3; y++)
                for (int x = 2; x < w + 2; x++)
                {
                    int fromFoot = h + 2 - y, mid = Math.Abs(x - (w + 3) / 2);
                    // the swallowtail: a notch up the middle of the foot
                    if (fromFoot < 6 - mid / 2 && mid < 6) continue;
                    bool inBand = y >= 2 + h * 2 / 3 - 3 && y < 2 + h * 2 / 3 + 2;
                    var colour = inBand ? band : x == 2 ? f.Light : x == w + 1 ? f.Dark : field;
                    c.Set(x, y, colour);
                }
            Pix.Outline(c);
        });
        float x0 = (p.Width * 32 - art.Width) / 2f;
        kit.Card(x0, x0 + art.Width, 32.5f, 64 - art.Height, 64, art);
    }

    /// <summary>A wreath of leaves round a middle, open at its top, in a colour and its lighter and darker shades, outlined.</summary>
    private static void Wreath(PixelCanvas c, float cx, float cy, float r, Color colour)
    {
        var t = Tone.Of(colour);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x - cx, dy = y - cy, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d < r - 2.2f || d > r + 0.6f) continue;
                // Open at the top, where the two branches don't meet
                if (dy < -r * 0.7f && MathF.Abs(dx) < r * 0.35f) continue;
                // Leaves: lighter on the outside of each, by the angle round the ring
                float a = MathF.Atan2(dy, dx);
                bool leaf = ((int)MathF.Floor((a + MathF.PI) / (MathF.PI / 6f))) % 2 == 0;
                c.Set(x, y, d > r - 0.8f ? (leaf ? t.Light : t.Base) : (leaf ? t.Base : t.Dark));
            }
        // A ribbon tied at the foot
        c.Rect((int)MathF.Round(cx) - 1, (int)MathF.Round(cy + r) - 1, 3, 2, Rgb(236, 206, 96));
    }
}
