using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Whose band a battle opens on, and the colours it wears (style guide, "Into battle", "Bands"): the trainer's
/// class and name as <c>trainers.json</c> has them, the band's own colour, a deep one for its lines, its mark's
/// drop and the name's plate, and a light one for its lines and the mark's echoes. The card behind a portrait is
/// the light colour too (<see cref="Backing"/>), but for a person whose hair is that colour, who has one of their own.
/// </summary>
public readonly record struct BattleBand(string Class, string Name, Color Base, Color Deep, Color Light, Color? Card = null)
{
    /// <summary>The colour of the card a portrait stands before.</summary>
    public Color Backing => Card ?? Light;
}

/// <summary>
/// The moments of a band, in seconds after the flashes (the style guide's table): when it is open, when its mark
/// begins to stamp in and how long that takes, when their portrait slides in and for how long (with the band's
/// halves, in the League's), the white flash from its first step to its last with the moment at its height when
/// the silhouettes light, when the name stands, when the League's portraits begin to tremble, and the two the
/// transition itself goes by: the whole lead, and the white's closing after it.
/// </summary>
public readonly record struct BandBeats(float Opens, float MarkFrom, float MarkTakes, float SlideFrom, float SlideTakes,
    float FlashFrom, float LitAt, float FlashEnds, float NameFrom, float TrembleFrom, float Lead, float Shut);

/// <summary>
/// What a band shows at one moment, on a screen of the size asked about (plan 09 · L15): nothing
/// (<see cref="Shown"/> false) during the flashes and once the picture is shut. The rows the band lies between
/// and how far it has opened (a Leader's wiped in from the right, the League's two halves slid in from the sides);
/// how far its speed lines have run, in whole units; whether the player's portrait is there too, and the middle of
/// each portrait's foot; whether they are lit or still silhouettes; how dark the picture round the band is (the
/// alpha of the dark laid over it); where the name's plate is anchored (its right end for a Leader's, its middle
/// for the League's) and how opaque it is; where the mark between stands and how far it has stamped in; how white
/// the flash over everything is; and how far the white has closed.
/// </summary>
public readonly record struct BandLook(bool Shown, float Top, float Bottom, float Open, int Scroll, bool Two, Vector2 Theirs, Vector2 Own,
    bool Lit, int Dim, Vector2 Name, int NameAlpha, Vector2 Mark, float MarkCome, float Flash, float White);

/// <summary>What a band is drawn with: whose it is, and the portraits rendered for it so far (theirs, and the player's in the League's).</summary>
public readonly record struct BandPicture(BattleBand Band, Texture2D? Theirs, Texture2D? Own);

// The bands of the Gym Leaders, the Elite Four and the Champion (plan 09 · L15; style guide, "Life", "Into battle",
// "Bands"): our own after the beats of the original's EncounterEffect_GymLeader and EncounterEffect_EliteFourChampion
// (src/overlay005/encounter_effect_core.c), whose banners, portraits, mark and palettes are art and are not read.
// What a band shows is one look, a function of the kind and the time (BandAt); drawing only draws it.
public static partial class SceneTransition
{
    /// <summary>The white a band closes to: never pure white.</summary>
    public static readonly Color White = new(250, 250, 252, 255);

    /// <summary>A portrait that isn't lit yet: its shape in this one colour, and the plain bust that stands in for one.</summary>
    public static readonly Color Silhouette = new(32, 32, 40, 255);

    /// <summary>The band's height, a portrait's side, the mark's and the name's type sizes, in layout units.</summary>
    public const float BandHeight = 300f, PortraitSize = 440f, MarkSize = 150f, NameSize = 52f;

    /// <summary>How wide the card behind a portrait is.</summary>
    public const float CardWide = 380f;

    /// <summary>Where across the picture their portrait stands, the player's, and a Leader's mark.</summary>
    public const float TheirPlace = 0.78f, OwnPlace = 0.22f, LeaderMarkPlace = 0.28f;

    /// <summary>How fast the fastest speed line would run, in units a second, and how dark the picture round a band lies.</summary>
    public const float BandPace = 1500f;
    public const int BandDim = 200, BandHalfDim = 130;

    /// <summary>How far the League's portraits are thrown at the end of their trembling, in units.</summary>
    public const float TrembleReach = 15f;

    /// <summary>
    /// Where a Leader's name plate lies under the band's middle row and how far its end is tucked in behind the
    /// portrait's picture, and how far under the band's foot the middle of the League's hangs.
    /// </summary>
    public const float NameLow = 84f, NameTucked = 96f, NameHangs = 28f;

    // The toothed edge a Leader's band wipes in behind, and the band's lines
    private const float ToothTall = 50f, ToothDeep = 120f, InnerLine = 6f, OuterLine = 4f;
    private const int LineRows = 9;
    private const float LineRowGap = 30f;

    // The name's plate, and the class over it
    private const float PlateTall = 76f, PlateRadius = 12f, PlateSkew = 0.2f, PlatePad = 48f, ClassSize = 24f;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>
    /// The thirteen bands (the style guide's table): the eight Gym Leaders, the Elite Four and the Champion, each
    /// after its Gym's type or its room. Keyed by class and name, as the original has a way in to each
    /// (<c>ENCEFF_CUTIN_LEADER_ROARK</c> to <c>ENCEFF_CUTIN_CHAMPION_CYNTHIA</c>).
    /// </summary>
    public static IReadOnlyList<BattleBand> Bands { get; } = new[]
    {
        new BattleBand("Leader", "Roark", Rgb(172, 136, 92), Rgb(104, 76, 48), Rgb(232, 204, 152)),
        new BattleBand("Leader", "Gardenia", Rgb(88, 168, 88), Rgb(40, 104, 60), Rgb(184, 228, 136)),
        new BattleBand("Leader", "Wake", Rgb(60, 132, 216), Rgb(28, 72, 148), Rgb(148, 208, 248)),
        new BattleBand("Leader", "Maylene", Rgb(216, 96, 80), Rgb(136, 44, 48), Rgb(252, 176, 140)),
        new BattleBand("Leader", "Fantina", Rgb(124, 88, 176), Rgb(64, 44, 116), Rgb(196, 168, 236)),
        new BattleBand("Leader", "Candice", Rgb(104, 192, 216), Rgb(44, 116, 152), Rgb(208, 240, 248)),
        new BattleBand("Leader", "Byron", Rgb(128, 144, 168), Rgb(68, 80, 104), Rgb(208, 216, 232)),
        // (His hair is the pale gold of his band's light colour: his card is the pale blue of a spark)
        new BattleBand("Leader", "Volkner", Rgb(236, 184, 44), Rgb(152, 100, 20), Rgb(252, 236, 148), Rgb(196, 224, 252)),
        new BattleBand("Elite Four", "Aaron", Rgb(144, 176, 60), Rgb(80, 108, 32), Rgb(216, 232, 132)),
        new BattleBand("Elite Four", "Bertha", Rgb(208, 148, 76), Rgb(128, 80, 36), Rgb(244, 208, 144)),
        new BattleBand("Elite Four", "Flint", Rgb(232, 100, 48), Rgb(148, 44, 28), Rgb(252, 188, 104)),
        new BattleBand("Elite Four", "Lucian", Rgb(212, 84, 148), Rgb(128, 36, 92), Rgb(248, 172, 208)),
        // (Her hair is her band's gold and her coat its night: her card is a moonlit blue between the two)
        new BattleBand("Champion", "Cynthia", Rgb(52, 56, 84), Rgb(22, 24, 40), Rgb(236, 200, 108), Rgb(132, 140, 196))
    };

    /// <summary>
    /// Kanto's thirteen (plan 19 · K8; the style guide's second table): the eight Leaders, the four of the Elite Four
    /// and the Champion, who is the rival (his entry's name is the placeholder <c>{rival}</c>, and <see cref="BandOf"/>
    /// finds it by whatever name the player gave him). None wears the colours of a Sinnoh band of the same type.
    /// </summary>
    public static IReadOnlyList<BattleBand> KantoBands { get; } = new[]
    {
        new BattleBand("Leader", "Brock", Rgb(150, 132, 112), Rgb(86, 72, 58), Rgb(222, 208, 184)),
        new BattleBand("Leader", "Misty", Rgb(72, 152, 224), Rgb(28, 76, 152), Rgb(160, 216, 248)),
        new BattleBand("Leader", "Lt. Surge", Rgb(200, 172, 48), Rgb(104, 88, 28), Rgb(240, 226, 136)),
        new BattleBand("Leader", "Erika", Rgb(72, 168, 128), Rgb(28, 92, 72), Rgb(176, 232, 196)),
        new BattleBand("Leader", "Koga", Rgb(150, 78, 170), Rgb(78, 38, 104), Rgb(214, 160, 232)),
        new BattleBand("Leader", "Sabrina", Rgb(224, 96, 172), Rgb(132, 40, 104), Rgb(250, 176, 224)),
        new BattleBand("Leader", "Blaine", Rgb(220, 64, 48), Rgb(128, 28, 28), Rgb(250, 152, 104)),
        new BattleBand("Leader", "Giovanni", Rgb(116, 84, 60), Rgb(60, 40, 28), Rgb(204, 168, 128)),
        new BattleBand("Elite Four", "Lorelei", Rgb(136, 168, 232), Rgb(56, 84, 164), Rgb(208, 226, 252)),
        new BattleBand("Elite Four", "Bruno", Rgb(188, 92, 60), Rgb(112, 44, 28), Rgb(240, 184, 140)),
        new BattleBand("Elite Four", "Agatha", Rgb(104, 96, 132), Rgb(50, 44, 76), Rgb(182, 176, 212)),
        new BattleBand("Elite Four", "Lance", Rgb(52, 92, 180), Rgb(24, 44, 108), Rgb(140, 176, 240)),
        new BattleBand("Champion", RivalPlaceholder, Rgb(92, 64, 152), Rgb(44, 28, 92), Rgb(206, 184, 244))
    };

    /// <summary>The name the Champion of Kanto's entry carries: the rival's, whatever the player called him.</summary>
    public const string RivalPlaceholder = "{rival}";

    /// <summary>The band of someone the table doesn't name (another region's Leader, before their band is written): steel grey.</summary>
    public static readonly BattleBand PlainBand = new("", "", Rgb(120, 132, 152), Rgb(64, 72, 92), Rgb(204, 212, 224));

    /// <summary>True for the two ways in that are bands.</summary>
    public static bool IsBand(TransitionKind kind) => kind is TransitionKind.LeaderBand or TransitionKind.EliteBand;

    /// <summary>
    /// The band of a trainer by class and name: Sinnoh's table, Kanto's, or the plain one under their own class and
    /// name. The rival as Kanto's Champion (named <c>{rival}</c> in the data, or by the name the player gave him) wears
    /// the Champion's band under that name.
    /// </summary>
    public static BattleBand BandOf(string trainerClass, string name)
    {
        foreach (var band in Bands)
            if (band.Class == trainerClass && band.Name == name) return band;
        foreach (var band in KantoBands)
            if (band.Class == trainerClass && (band.Name == name || (trainerClass == "Champion" && IsTheRival(name))))
                return band with { Name = name };
        return PlainBand with { Class = trainerClass, Name = name };
    }

    private static bool IsTheRival(string name) => name == RivalPlaceholder || name == PlayerIdentity.RivalName;

    /// <summary>
    /// Whose band a meeting opens on: the first trainer's, as the first trainer's class alone picks the way
    /// (<see cref="ForBattle"/>); null for a meeting that opens any other way.
    /// </summary>
    public static BattleBand? BandFor(BattleMeeting meeting) =>
        IsBand(ForBattle(meeting)) ? BandOf(meeting.Trainers[0].Class, meeting.Trainers[0].Name) : null;

    /// <summary>The beats of a band (the style guide's table), in seconds after the flashes; nothing for a kind that is no band.</summary>
    public static BandBeats Beats(TransitionKind kind)
    {
        var (lead, shut) = Pace(kind);
        return kind switch
        {
            // The mark from 0.55 s, the portrait at 1.1 s, lit under the flash at 1.7 s with the name, held to the white
            TransitionKind.LeaderBand => new BandBeats(Opens: 0.2f, MarkFrom: 0.55f, MarkTakes: 0.5f, SlideFrom: 1.1f, SlideTakes: 0.15f,
                FlashFrom: 1.6f, LitAt: 1.7f, FlashEnds: 1.8f, NameFrom: 1.7f, TrembleFrom: lead, Lead: lead, Shut: shut),
            // The portraits with the halves, the mark and the name once they have met, lit at 1.1 s, trembling from 1.55 s to the white
            TransitionKind.EliteBand => new BandBeats(Opens: 0.2f, MarkFrom: 0.2f, MarkTakes: 0.5f, SlideFrom: 0f, SlideTakes: 0.2f,
                FlashFrom: 1f, LitAt: 1.1f, FlashEnds: 1.3f, NameFrom: 0.2f, TrembleFrom: 1.55f, Lead: lead, Shut: shut),
            _ => default
        };
    }

    /// <summary>
    /// What a band shows <paramref name="seconds"/> into its closing, on a screen of the given size: everything
    /// <see cref="DrawBand"/> draws, as a function of the kind and the time alone.
    /// </summary>
    public static BandLook BandAt(TransitionKind kind, float seconds, float width, float height)
    {
        if (!IsBand(kind) || seconds < FlashSeconds || seconds >= OutSeconds(kind)) return default;
        var beats = Beats(kind);
        bool league = kind == TransitionKind.EliteBand;
        float since = seconds - FlashSeconds, cx = width / 2f, cy = height / 2f;
        float top = cy - BandHeight / 2f, foot = cy + BandHeight / 2f;
        float closed = Closed(kind, true, seconds);
        bool lit = since >= beats.LitAt;

        // A Leader's band wipes in at an even pace; the League's halves slow as they arrive
        float open = Math.Clamp(since / beats.Opens, 0f, 1f);
        if (league) open = EaseOut(open);

        // The flash: three steps up to its height, where the portraits light, and three back
        float flash = 0f;
        if (since >= beats.FlashFrom && since < beats.LitAt)
            flash = 0.25f * (1 + Math.Min(2, (int)((since - beats.FlashFrom) / (beats.LitAt - beats.FlashFrom) * 3f)));
        else if (since >= beats.LitAt && since < beats.FlashEnds)
            flash = 0.25f * (3 - Math.Min(2, (int)((since - beats.LitAt) / (beats.FlashEnds - beats.LitAt) * 3f)));

        Vector2 theirs, own = default, name, mark;
        int nameAlpha;
        if (league)
        {
            // Each portrait rides its half in
            float off = (1f - open) * cx;
            own = new Vector2(OwnPlace * width - off, foot);
            theirs = new Vector2(TheirPlace * width + off, foot);
            // The name hangs under theirs, from the band's foot, until they fly apart
            name = new Vector2(theirs.X, foot + NameHangs);
            nameAlpha = since >= beats.NameFrom && closed <= 0f ? 255 : 0;
            mark = new Vector2(cx, cy);
            if (closed <= 0f && since >= beats.TrembleFrom)
            {
                // Thrown apart and at one another in turn, every fifteenth of a second and further each time; their
                // feet stay on the band's foot (the original's are thrown along the diagonal they will fly off by)
                float share = (since - beats.TrembleFrom) / (beats.Lead - beats.TrembleFrom);
                float thrown = MathF.Round(TrembleReach * share * share) * ((int)((since - beats.TrembleFrom) * 15f) % 2 == 0 ? -1f : 1f);
                own.X += thrown;
                theirs.X -= thrown;
            }
            else if (closed > 0f)
            {
                // They fly apart as the white tears the picture between them: fast at first (the original's 144 of 256
                // pixels in eight frames, on both axes), the player's up and to the left, theirs down and to the right
                float t = Math.Clamp((since - beats.Lead) / beats.Shut, 0f, 1f);
                var flown = new Vector2(0.5625f * width, 0.75f * height) * ((4f * t - t * t) / 3f);
                own -= flown;
                theirs += flown;
            }
        }
        else
        {
            // Theirs slides in from beyond the right edge, slowing as it arrives
            float slid = EaseOut(Math.Clamp((since - beats.SlideFrom) / beats.SlideTakes, 0f, 1f));
            float from = width + PortraitSize / 2f + 20f, to = TheirPlace * width;
            theirs = new Vector2(from + (to - from) * slid, foot);
            // The name stands beside it, low in the band: its plate ends behind the portrait's shoulder
            name = new Vector2(to - PortraitSize / 2f + NameTucked, cy + NameLow);
            nameAlpha = since >= beats.NameFrom ? 255 : 0;
            mark = new Vector2(LeaderMarkPlace * width, cy);
        }

        int dim = lit ? BandDim : league ? BandHalfDim : 0;
        float come = Math.Clamp((since - beats.MarkFrom) / beats.MarkTakes, 0f, 1f);
        return new BandLook(true, top, foot, open, (int)(since * BandPace), league, theirs, own, lit, dim, name, nameAlpha, mark, come, flash, closed);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>
    /// A band as the field closes: the dim round it, the band and its lines, the mark, the name and the portraits,
    /// the flash, and the white closing over all of it. Without a <paramref name="picture"/> the band is the plain
    /// one, with no name and plain busts.
    /// </summary>
    private static void DrawBand(TransitionKind kind, float seconds, float closed, int width, int height, BandPicture? picture)
    {
        if (closed >= 1f)
        {
            Raylib.DrawRectangle(0, 0, width, height, White);
            return;
        }
        var look = BandAt(kind, seconds, width, height);
        if (!look.Shown) return;
        var band = picture?.Band ?? PlainBand;
        bool league = kind == TransitionKind.EliteBand;
        float cx = width / 2f;

        if (look.Dim > 0) Raylib.DrawRectangle(0, 0, width, height, new Color(Dark.R, Dark.G, Dark.B, (byte)look.Dim));

        // The band: two halves from the sides, or one length wiped in from the right behind its teeth
        float off = (1f - look.Open) * cx, edge = width * (1f - look.Open);
        if (league)
        {
            Strip(band, -off, cx - off, look.Top, look.Bottom, look.Scroll, -1);
            Strip(band, cx + off, width + off, look.Top, look.Bottom, look.Scroll, 1);
        }
        else
        {
            Strip(band, edge, width, look.Top, look.Bottom, look.Scroll, 1);
            if (look.Open < 1f)
                for (float y = look.Top - OuterLine; y < look.Bottom + OuterLine; y += ToothTall)
                {
                    float y1 = MathF.Min(look.Bottom + OuterLine, y + ToothTall);
                    Tri(new Vector2(edge + 1f, y), new Vector2(edge - ToothDeep, (y + y1) / 2f), new Vector2(edge + 1f, y1), band.Base);
                }
        }

        // The cards the portraits stand before: part of the band, so the League's ride their halves in and a Leader's
        // is there once the wipe has passed it
        if (league)
        {
            Card(band, OwnPlace * width - off, look.Top, look.Bottom, 1f);
            Card(band, TheirPlace * width + off, look.Top, look.Bottom, -1f);
        }
        else if (edge <= TheirPlace * width - CardWide / 2f - PlateSkew * BandHeight / 2f)
            Card(band, TheirPlace * width, look.Top, look.Bottom, -1f);

        DrawMark(band, look.Mark, look.MarkCome);
        // A Leader's name lies under the edge of the portrait; the League's over the foot of theirs
        if (!league) DrawName(band, look.Name, false, look.NameAlpha);
        if (look.Two) DrawPortrait(picture?.Own, look.Own, look.Lit);
        DrawPortrait(picture?.Theirs, look.Theirs, look.Lit);
        // The band's foot is drawn over the portraits' own, so a bust ends in the band's line
        if (league)
        {
            Foot(band, -off, cx - off, look.Bottom);
            Foot(band, cx + off, width + off, look.Bottom);
            DrawName(band, look.Name, true, look.NameAlpha);
        }
        else Foot(band, edge, width, look.Bottom);

        if (look.Flash > 0f) Raylib.DrawRectangle(0, 0, width, height, new Color(255, 255, 255, (int)(look.Flash * 255)));
        if (closed > 0f) DrawDark(kind, closed, width, height, null);
    }

    /// <summary>
    /// A length of the band between two places across the picture: its colour, its speed lines running one way or
    /// the other, and the lines along its head. The dashes are placed on the picture, not on the length, so a half
    /// that slides in uncovers them.
    /// </summary>
    private static void Strip(BattleBand band, float x0, float x1, float top, float bottom, int scroll, int way)
    {
        if (x1 <= x0) return;
        Raylib.DrawRectangleRec(new Rectangle(x0, top, x1 - x0, bottom - top), band.Base);
        for (int row = 0; row < LineRows; row++)
        {
            // Each row its own dashes, from its number alone: how long, how far apart, how thick, how fast, which colour
            float length = MathF.Round(120f + 280f * Noise(row, 1, 15)), gap = MathF.Round(160f + 320f * Noise(row, 2, 15));
            float thick = 6f + 2f * (int)(4.99f * Noise(row, 3, 15)), pace = 0.6f + 0.8f * Noise(row, 4, 15);
            var colour = Noise(row, 5, 15) < 0.4f ? band.Light : band.Deep;
            float period = length + gap;
            float at = way * MathF.Floor(scroll * pace) + MathF.Round(Noise(row, 6, 15) * period);
            float y = MathF.Round(top + LineRowGap * (row + 1) - thick / 2f);
            float first = x0 + ((at - x0) % period + period) % period - period;
            for (float x = first; x < x1; x += period)
            {
                float a = MathF.Max(x, x0), b = MathF.Min(x + length, x1);
                if (b > a) Raylib.DrawRectangleRec(new Rectangle(a, y, b - a, thick), colour);
            }
        }
        Raylib.DrawRectangleRec(new Rectangle(x0, top - OuterLine, x1 - x0, OuterLine), band.Deep);
        Raylib.DrawRectangleRec(new Rectangle(x0, top, x1 - x0, InnerLine), band.Light);
    }

    /// <summary>
    /// The card a portrait stands before: a slanted panel of the band's light colour between the band's lines, its
    /// head leaning away from the middle of the picture (<paramref name="lean"/>: 1 for the card on the left, -1
    /// for the one on the right, so the League's two open like a V round the mark). A bust wears its Gym's own
    /// colours, which are the band's: on the card it never stands on them, its outline's ink reads all round it,
    /// and a silhouette is at its darkest against it.
    /// </summary>
    private static void Card(BattleBand band, float middle, float top, float bottom, float lean)
    {
        var rect = new Rectangle(middle - CardWide / 2f, top + InnerLine, CardWide, bottom - top - 2f * InnerLine);
        UiShapes.Fill(rect, 0f, band.Backing, skew: -lean * PlateSkew);
    }

    /// <summary>The lines along a length of the band's foot.</summary>
    private static void Foot(BattleBand band, float x0, float x1, float bottom)
    {
        if (x1 <= x0) return;
        Raylib.DrawRectangleRec(new Rectangle(x0, bottom - InnerLine, x1 - x0, InnerLine), band.Light);
        Raylib.DrawRectangleRec(new Rectangle(x0, bottom, x1 - x0, OuterLine), band.Deep);
    }

    /// <summary>
    /// The mark between: three echoes a tenth of a second apart, each shrinking from twice its size to its own in a
    /// fifth of a second, and then the mark itself, white over a drop of the band's deep colour.
    /// </summary>
    private static void DrawMark(BattleBand band, Vector2 at, float come)
    {
        if (come <= 0f) return;
        for (int echo = 0; echo < 3; echo++)
        {
            float shrunk = (come - 0.2f * echo) / 0.4f;
            if (shrunk <= 0f || shrunk >= 1f) continue;
            MarkText(at, MarkSize * (2f - shrunk), band.Light with { A = (byte)(90 + 40 * echo) });
        }
        if (come < 0.6f) return;
        MarkText(at + new Vector2(8f, 8f), MarkSize, band.Deep);
        MarkText(at, MarkSize, White);
    }

    private static void MarkText(Vector2 middle, float size, Color colour)
    {
        const string mark = "VS";
        float wide = UiFonts.MeasureDisplay(mark, size, 0f);
        UiFonts.DrawDisplay(mark, middle.X - wide / 2f, middle.Y - size * 0.56f, size, 0f, colour);
    }

    /// <summary>
    /// The trainer's name on its plate, with their class over it: the plate ends at <paramref name="at"/>, or is
    /// centred on it, and is as wide as the name asks.
    /// </summary>
    private static void DrawName(BattleBand band, Vector2 at, bool centred, int alpha)
    {
        if (alpha <= 0 || band.Name.Length == 0) return;
        // (The rival as Champion carries his placeholder until he is drawn)
        band = band with { Name = PlayerIdentity.Fill(band.Name) };
        float wide = UiFonts.Measure(band.Name, NameSize, UiWeight.Black);
        float plate = MathF.Max(wide + 2f * PlatePad, 240f);
        float x = centred ? at.X - plate / 2f : at.X - plate;
        var rect = new Rectangle(x, at.Y - PlateTall / 2f, plate, PlateTall);
        UiShapes.Fill(rect, PlateRadius, band.Deep with { A = (byte)alpha }, skew: -PlateSkew);
        UiFonts.DrawCentered(band.Name, x + (plate - wide) / 2f, at.Y, NameSize, White with { A = (byte)alpha }, UiWeight.Black);
        if (band.Class.Length == 0) return;
        string title = band.Class.ToUpperInvariant();
        // Over the plate's head end, in the band and in its deep colour; or under the plate where it hangs from the
        // band's foot, on the dimmed picture and in the light one
        float tx = centred ? at.X - UiFonts.Measure(title, ClassSize, UiWeight.ExtraBold) / 2f : x + PlatePad * 0.6f;
        float ty = centred ? rect.Y + PlateTall + 8f : rect.Y - ClassSize - 6f;
        UiFonts.Draw(title, tx, ty, ClassSize, (centred ? band.Light : band.Deep) with { A = (byte)alpha }, UiWeight.ExtraBold);
    }

    // A picture's shape in one flat colour: its alpha alone, in the colour it is drawn with
    private const string FlatFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
out vec4 finalColor;
void main()
{
    finalColor = vec4(fragColor.rgb, texture(texture0, fragTexCoord).a * fragColor.a);
}";

    private static Shader flat;
    private static bool flatLoaded;

    /// <summary>
    /// Makes what a band's drawing needs made once (its silhouettes' shader, the mark's lettering), so that the
    /// frame which first shows them doesn't pay for it: the portraits' renderer calls it in a band's first frame,
    /// which lies under the flash.
    /// </summary>
    internal static void WarmBands()
    {
        if (!flatLoaded)
        {
            flat = Raylib.LoadShaderFromMemory(null, FlatFragment);
            flatLoaded = true;
        }
        _ = UiFonts.Display;
    }

    /// <summary>
    /// A portrait with the middle of its foot at <paramref name="foot"/>: the picture rendered for it, its shape
    /// alone in the silhouette's colour until it is lit, or a plain bust in that colour while there is no picture.
    /// </summary>
    private static void DrawPortrait(Texture2D? picture, Vector2 foot, bool lit)
    {
        if (picture is { } rendered)
        {
            var box = new Rectangle(foot.X - PortraitSize / 2f, foot.Y - PortraitSize, PortraitSize, PortraitSize);
            var whole = new Rectangle(0, 0, rendered.Width, -rendered.Height);
            if (lit)
            {
                Raylib.DrawTexturePro(rendered, whole, box, Vector2.Zero, 0f, Color.White);
                return;
            }
            WarmBands();
            Raylib.BeginShaderMode(flat);
            Raylib.DrawTexturePro(rendered, whole, box, Vector2.Zero, 0f, Silhouette);
            Raylib.EndShaderMode();
            return;
        }
        float head = PortraitSize * 0.24f, shoulders = PortraitSize * 0.3f, chest = PortraitSize * 0.2f;
        UiShapes.Circle(new Vector2(foot.X, foot.Y - PortraitSize * 0.5f), head, Silhouette);
        UiShapes.Fill(new Rectangle(foot.X - shoulders, foot.Y - chest - 40f, 2f * shoulders, 80f), 40f, Silhouette);
        Raylib.DrawRectangleRec(new Rectangle(foot.X - shoulders, foot.Y - chest, 2f * shoulders, chest), Silhouette);
    }
}
