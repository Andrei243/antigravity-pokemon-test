using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.UI;

/// <summary>How one scene gives way to the next.</summary>
public enum TransitionKind
{
    /// <summary>A plain fade through the dark: doors, menus, the end of a battle.</summary>
    Fade,
    /// <summary>Into a battle with a wild Pokémon: two flashes, a pinwheel closing, an iris opening.</summary>
    Wild,
    /// <summary>A wild Pokémon of a higher level than the player's first: the picture breaks into shards.</summary>
    WildStrong,
    /// <summary>Into a battle with a trainer: two flashes, shutters from both sides.</summary>
    Trainer,
    /// <summary>A trainer whose first Pokémon is of a higher level: shutters from the sides and from above and below.</summary>
    TrainerStrong,

    // The ways by place and by whom (plan 09 · L13; style guide, "Into battle", "By place and by whom")

    /// <summary>A wild Pokémon met on water: the picture wavers and sinks into a dark that rises from its foot.</summary>
    WildWater,
    /// <summary>A stronger one on water: it wavers further, and the dark comes from the foot and the head at once.</summary>
    WildWaterStrong,
    /// <summary>A wild Pokémon in a cave: the dark closes in a ring on the player.</summary>
    WildCave,
    /// <summary>A stronger one in a cave: the ring is toothed.</summary>
    WildCaveStrong,
    /// <summary>A trainer on water: a Poké Ball spins in at the middle and the dark wells out from under it.</summary>
    TrainerWater,
    /// <summary>A stronger trainer on water: three balls rise from the foot, each drawing a third of the picture dark behind it.</summary>
    TrainerWaterStrong,
    /// <summary>A trainer in a cave: a ball falls to the middle and the dark shuts on it from above and below.</summary>
    TrainerCave,
    /// <summary>A stronger trainer in a cave: three balls fall, each tearing a strip of dark that widens.</summary>
    TrainerCaveStrong,
    /// <summary>Two at once: four pieces slide in from the corners.</summary>
    Double,
    /// <summary>The Battle Frontier, a Frontier Brain, a link battle: the Frontier's mark comes up and a five-bladed iris closes on it.</summary>
    Frontier,
    /// <summary>A Team Galactic grunt: six shards of the Galactic mark fly in, each with a sixth of the dark behind it.</summary>
    Grunt,
    /// <summary>A Commander or the boss: the Galactic mark rises out of the dimming picture and its dark core swallows it.</summary>
    GalacticBoss,

    // The bands (plan 09 · L15; style guide, "Into battle", "Bands")

    /// <summary>A Gym Leader: a band opens across the picture, their portrait slides in as a silhouette and lights up beside their name, and all goes to white.</summary>
    LeaderBand,
    /// <summary>One of the Elite Four or the Champion: the band comes in as two halves, the player's portrait on the one and theirs on the other, and the white tears the picture between them.</summary>
    EliteBand,

    // The camera's ways (plan 09 · L14; style guide, "Into battle", "The legendary's and the mythical's")

    /// <summary>A legendary Pokémon (Dialga, Palkia, Uxie, Azelf, Arceus): the lens widens, then rushes in, and a disc of white grows from the middle.</summary>
    Legendary,
    /// <summary>A mythical Pokémon (Shaymin, Giratina, Regigigas, Heatran, Darkrai, Rotom, the Regis): the camera jolts through sixteen held lenses, and the white closes in from the edges.</summary>
    Mythical
}

/// <summary>
/// The shapes that close over one scene and open on the next (style guide, "Into battle"). What is covered at
/// any moment is a function of the kind, the phase and the point (<see cref="Covers"/>), which is what tests
/// check; <see cref="Draw"/> draws the same shapes. The ways by place and by whom (plan 09 · L13) are in
/// <c>SceneTransition.Ways.cs</c>: their shapes, and what rides over the picture as they close
/// (<see cref="Pieces"/>, <see cref="Ripple"/>, <see cref="Veil"/>); the bands of the Gym Leaders, the Elite Four
/// and the Champion (plan 09 · L15) in <c>SceneTransition.Bands.cs</c> (<see cref="BandAt"/>, <see cref="Bands"/>);
/// the legendary's and the mythical's (plan 09 · L14), which are the field's camera, in <c>SceneTransition.Lenses.cs</c>
/// (<see cref="LensAt"/>).
/// </summary>
public static partial class SceneTransition
{
    /// <summary>The dark a scene closes to: never pure black.</summary>
    public static readonly Color Dark = new(14, 14, 22, 255);

    public const float FadeSeconds = 0.4f, FlashSeconds = 0.25f, CloseSeconds = 0.9f, OpenSeconds = 0.5f;
    private const int Blades = 8, Bars = 12, Uprights = 16;

    /// <summary>The plain fade and the four ways of plan 04 · G9, which lead in with nothing and take <see cref="CloseSeconds"/>: every later way has a pace of its own.</summary>
    private static bool Plain(TransitionKind kind) => kind <= TransitionKind.TrainerStrong;

    // The shards: a grid of points pulled out of line, two triangles to each of its squares
    private const int ShardColumns = 10, ShardRows = 6;
    private const float ShardGrows = 0.3f;

    /// <summary>
    /// The way into a battle, as Platinum picks it (<c>EncEffects_CutInEffect</c> of <c>src/enc_effects.c</c>): by
    /// whom the meeting is with (<see cref="BattleMeeting.Row"/>, the original's <c>EncEffects_GetEffectPair</c>),
    /// and for everyone with no way of their own by the ground and by whether the first Pokémon they send out is of
    /// a higher level than the player's first (<see cref="Local"/>). The rows with a way of their own are
    /// <c>sEncEffectsTable</c>'s: a band for each Gym Leader, each of the Elite Four and the Champion (plan 09 ·
    /// L15; <see cref="BandFor"/> says whose), Team Galactic's two, the Frontier's for its battles, its Brains and a
    /// link battle, the double's for any two at once, a Leader's too, and the legendary's and the mythical's for the
    /// wild species with a row of their own (plan 09 · L14).
    /// </summary>
    public static TransitionKind ForBattle(BattleMeeting meeting) => meeting.Row switch
    {
        // ENCEFF_CUTIN_LEADER_…, ENCEFF_CUTIN_ELITE_FOUR_… and ENCEFF_CUTIN_CHAMPION_CYNTHIA
        MeetingRow.Leader => TransitionKind.LeaderBand,
        MeetingRow.EliteFour or MeetingRow.Champion => TransitionKind.EliteBand,
        MeetingRow.GalacticGrunt => TransitionKind.Grunt,
        MeetingRow.GalacticCommander or MeetingRow.GalacticBoss => TransitionKind.GalacticBoss,
        MeetingRow.Frontier or MeetingRow.Link or MeetingRow.FrontierBrain => TransitionKind.Frontier,
        MeetingRow.DoubleBattle or MeetingRow.DoubleWild or MeetingRow.DoubleLeader => TransitionKind.Double,
        // ENCEFF_CUTIN_LEGENDARY and ENCEFF_CUTIN_MYTHICAL (the table's rows 89-98)
        MeetingRow.DialgaPalkia or MeetingRow.UxieAzelf or MeetingRow.Arceus => TransitionKind.Legendary,
        MeetingRow.Shaymin or MeetingRow.MinorLegendary or MeetingRow.Giratina or MeetingRow.RegiTrio => TransitionKind.Mythical,
        // ENCEFF_CUTIN_USE_LOCAL: the rival, any other trainer, a wild Pokémon, and Mesprit, Cresselia and the birds
        _ => Local(meeting)
    };

    /// <summary>
    /// The way in by place (<c>CutInEffects_ForBattle</c> of <c>src/overlay005/encounter_effect.c</c>): a trainer's
    /// or a wild Pokémon's, on water, in a cave or on any other ground, and the stronger one of each when their
    /// first Pokémon is of a higher level than the player's first.
    /// </summary>
    public static TransitionKind Local(BattleMeeting meeting) => (meeting.WithTrainers, meeting.Ground, meeting.Stronger) switch
    {
        (false, MeetingGround.Grass, false) => TransitionKind.Wild,
        (false, MeetingGround.Grass, true) => TransitionKind.WildStrong,
        (false, MeetingGround.Water, false) => TransitionKind.WildWater,
        (false, MeetingGround.Water, true) => TransitionKind.WildWaterStrong,
        (false, MeetingGround.Cave, false) => TransitionKind.WildCave,
        (false, MeetingGround.Cave, true) => TransitionKind.WildCaveStrong,
        (true, MeetingGround.Grass, false) => TransitionKind.Trainer,
        (true, MeetingGround.Grass, true) => TransitionKind.TrainerStrong,
        (true, MeetingGround.Water, false) => TransitionKind.TrainerWater,
        (true, MeetingGround.Water, true) => TransitionKind.TrainerWaterStrong,
        (true, MeetingGround.Cave, false) => TransitionKind.TrainerCave,
        _ => TransitionKind.TrainerCaveStrong
    };

    /// <summary>
    /// How long a kind takes to close over the scene it leaves: the flashes, its lead and the dark's closing. The
    /// first four ways into a battle take <see cref="CloseSeconds"/>; the ways by place and by whom and the bands
    /// about as long as the original's do (<see cref="Pace"/>).
    /// </summary>
    public static float OutSeconds(TransitionKind kind) => kind == TransitionKind.Fade ? FadeSeconds
        : Plain(kind) ? CloseSeconds
        : FlashSeconds + Pace(kind).Lead + Pace(kind).Shut;

    /// <summary>How long it takes to open on the scene it brings.</summary>
    public static float InSeconds(TransitionKind kind) => kind == TransitionKind.Fade ? FadeSeconds : OpenSeconds;

    /// <summary>
    /// How long a way into a battle leads in after its flashes, before anything is covered: a ball spinning in, a
    /// mark coming up, the picture wavering, the whole of a band's show before its white.
    /// </summary>
    public static float LeadSeconds(TransitionKind kind) => Plain(kind) ? 0f : Pace(kind).Lead;

    /// <summary>What a kind closes to: the dark, or the white of a band, a legendary and a mythical.</summary>
    public static Color CoverOf(TransitionKind kind) => ClosesToWhite(kind) ? White : Dark;

    /// <summary>True for the ways that close to white: the two bands, the legendary's and the mythical's.</summary>
    public static bool ClosesToWhite(TransitionKind kind) => IsBand(kind) || MovesTheCamera(kind);

    /// <summary>
    /// How white the screen is <paramref name="seconds"/> into closing: two flashes in the first quarter of a
    /// second, each lit for half of its eighth. Never more than three quarters white.
    /// </summary>
    public static float Flash(TransitionKind kind, float seconds)
    {
        if (kind == TransitionKind.Fade || seconds < 0f || seconds >= FlashSeconds) return 0f;
        // Under the options' REDUCE FLASHES (plan 12 · Q9) the two flashes are one soft swell to a quarter and back
        if (Core.Accessibility.ReduceFlashes) return Core.Accessibility.FlashCap * MathF.Sin(MathF.PI * seconds / FlashSeconds);
        float beat = seconds / (FlashSeconds / 2f);
        return beat - MathF.Floor(beat) < 0.5f ? 0.75f : 0f;
    }

    /// <summary>How far the shapes have closed, 0 to 1, <paramref name="seconds"/> into closing or opening.</summary>
    public static float Closed(TransitionKind kind, bool closing, float seconds)
    {
        if (kind == TransitionKind.Fade) return Math.Clamp(closing ? seconds / FadeSeconds : 1f - seconds / FadeSeconds, 0f, 1f);
        if (!closing) return Math.Clamp(1f - seconds / OpenSeconds, 0f, 1f);
        // The shapes wait for the flashes and for the way's lead, then close: with an ease, or at an even pace where
        // several things come one after another
        float from = FlashSeconds + LeadSeconds(kind), until = OutSeconds(kind);
        if (seconds >= until) return 1f;
        float t = Math.Clamp((seconds - from) / (until - from), 0f, 1f);
        return EvenPace(kind) ? t : t * t * (3f - 2f * t);
    }

    private static bool OpensAsIris(TransitionKind kind) => kind is TransitionKind.Wild or TransitionKind.WildStrong
        or TransitionKind.WildWater or TransitionKind.WildWaterStrong or TransitionKind.WildCave or TransitionKind.WildCaveStrong;

    private static float Noise(int a, int b, int salt)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + salt * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65536f;
    }

    /// <summary>A point of the shards' grid: on the screen's edge it stays put, inside it is pulled up to a third of a square out of line.</summary>
    private static Vector2 ShardPoint(int col, int row, float width, float height)
    {
        float cw = width / ShardColumns, ch = height / ShardRows;
        float x = col * cw, y = row * ch;
        if (col > 0 && col < ShardColumns) x += (Noise(col, row, 1) - 0.5f) * cw * 0.66f;
        if (row > 0 && row < ShardRows) y += (Noise(col, row, 2) - 0.5f) * ch * 0.66f;
        return new Vector2(x, y);
    }

    /// <summary>One of the two shards of a square of the grid, and how far it has grown from its middle (0 to 1).</summary>
    private static (Vector2 A, Vector2 B, Vector2 C, float Grown) Shard(int col, int row, int half, float closed, float width, float height)
    {
        var nw = ShardPoint(col, row, width, height);
        var ne = ShardPoint(col + 1, row, width, height);
        var sw = ShardPoint(col, row + 1, width, height);
        var se = ShardPoint(col + 1, row + 1, width, height);
        // Squares are cut along one diagonal or the other in turn
        bool falling = (col + row) % 2 == 0;
        var (a, b, c) = falling
            ? (half == 0 ? (nw, sw, se) : (nw, se, ne))
            : (half == 0 ? (nw, sw, ne) : (ne, sw, se));

        // The nearer the middle of the screen, the sooner a shard starts; none starts in step with its neighbour
        var centre = (a + b + c) / 3f;
        var middle = new Vector2(width / 2f, height / 2f);
        float far = Math.Clamp((centre - middle).Length() / middle.Length(), 0f, 1f);
        float start = (1f - ShardGrows) * Math.Clamp(0.8f * far + 0.2f * Noise(col * 2 + half, row, 3), 0f, 1f);
        float grown = Math.Clamp((closed - start) / ShardGrows, 0f, 1f);
        return (centre + (a - centre) * grown, centre + (b - centre) * grown, centre + (c - centre) * grown, grown);
    }

    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);
        float d2 = (p.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (p.Y - c.Y);
        float d3 = (p.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (p.Y - a.Y);
        bool negative = d1 < 0f || d2 < 0f || d3 < 0f, positive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(negative && positive);
    }

    /// <summary>
    /// Whether a point of a screen of the given size is under the dark, the shapes being <paramref name="closed"/>
    /// this far. <paramref name="focus"/> is where on that screen the player stands, for the ways that close on
    /// them (a cave's ring); the screen's middle when none is given.
    /// </summary>
    public static bool Covers(TransitionKind kind, bool closing, float closed, float x, float y, float width, float height, Vector2? focus = null)
    {
        if (closed <= 0f) return false;
        if (closed >= 1f) return true;
        float cx = width / 2f, cy = height / 2f;
        if (!closing && OpensAsIris(kind))
        {
            // An iris: open from the middle outward
            float reach = MathF.Sqrt(cx * cx + cy * cy);
            float dx = x - cx, dy = y - cy;
            return MathF.Sqrt(dx * dx + dy * dy) > (1f - closed) * reach;
        }

        switch (kind)
        {
            case TransitionKind.Wild:
            {
                // Eight blades turning a quarter of a turn as they widen
                float slice = MathF.Tau / Blades;
                float angle = MathF.Atan2(y - cy, x - cx) - closed * MathF.PI / 2f;
                float within = ((angle % slice) + slice) % slice;
                return within < closed * slice;
            }
            case TransitionKind.WildStrong:
            {
                var p = new Vector2(x, y);
                for (int row = 0; row < ShardRows; row++)
                    for (int col = 0; col < ShardColumns; col++)
                        for (int half = 0; half < 2; half++)
                        {
                            var (a, b, c, grown) = Shard(col, row, half, closed, width, height);
                            if (grown > 0f && Inside(p, a, b, c)) return true;
                        }
                return false;
            }
            case TransitionKind.Trainer or TransitionKind.TrainerStrong:
            {
                int bar = Math.Clamp((int)(y / (height / Bars)), 0, Bars - 1);
                if (bar % 2 == 0 ? x < closed * width : x > (1f - closed) * width) return true;
                if (kind == TransitionKind.Trainer) return false;
                int upright = Math.Clamp((int)(x / (width / Uprights)), 0, Uprights - 1);
                return upright % 2 == 0 ? y < closed * height : y > (1f - closed) * height;
            }
            default:
                // The ways by place and by whom, and the bands' white; only as the field closes does a ring close on the player
                return CoversByWay(kind, closed, x, y, width, height, closing ? focus : null);
        }
    }

    /// <summary>
    /// Draws the transition over a screen of the given size, <paramref name="seconds"/> into its closing or opening
    /// half. <paramref name="focus"/> is where on that screen the player stands (<see cref="Covers"/>);
    /// <paramref name="band"/> whose band it is and the portraits rendered for it, for the two kinds that are bands
    /// (a plain band with no name and plain busts when none is given).
    /// </summary>
    public static void Draw(TransitionKind kind, bool closing, float seconds, int width, int height, Vector2? focus = null, BandPicture? band = null)
    {
        float closed = Closed(kind, closing, seconds);
        if (kind == TransitionKind.Fade)
        {
            Raylib.DrawRectangle(0, 0, width, height, new Color(0, 0, 0, (int)(closed * 255)));
            return;
        }

        if (closing && Flash(kind, seconds) is > 0f and var white)
            Raylib.DrawRectangle(0, 0, width, height, new Color(255, 255, 255, (int)(white * 255)));
        // A band is a show of its own as the field closes: the band, the portraits, the mark, the name, then its white
        if (closing && IsBand(kind))
        {
            DrawBand(kind, seconds, closed, width, height, band);
            return;
        }
        // A way by place or by whom has more to show as the field closes than its dark: what leads in and rides over it
        if (closing && !Plain(kind))
        {
            DrawWay(kind, seconds, closed, width, height, focus);
            return;
        }
        if (closed <= 0f) return;
        if (closed >= 1f)
        {
            Raylib.DrawRectangle(0, 0, width, height, CoverOf(kind));
            return;
        }

        var middle = new Vector2(width / 2f, height / 2f);
        float reach = middle.Length() + 4f;
        if (!closing && OpensAsIris(kind))
        {
            Raylib.DrawRing(middle, (1f - closed) * reach, reach + 8f, 0f, 360f, 64, Dark);
            return;
        }

        switch (kind)
        {
            case TransitionKind.Wild:
            {
                float slice = 360f / Blades, turned = closed * 90f;
                for (int i = 0; i < Blades; i++)
                    Raylib.DrawCircleSector(middle, reach, turned + i * slice, turned + i * slice + closed * slice, 12, Dark);
                break;
            }
            case TransitionKind.WildStrong:
            {
                for (int row = 0; row < ShardRows; row++)
                    for (int col = 0; col < ShardColumns; col++)
                        for (int half = 0; half < 2; half++)
                        {
                            var (a, b, c, grown) = Shard(col, row, half, closed, width, height);
                            if (grown <= 0f) continue;
                            // The order raylib wants: counter-clockwise as the screen shows it
                            float turn = (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
                            if (turn < 0f) Raylib.DrawTriangle(a, b, c, Dark);
                            else Raylib.DrawTriangle(a, c, b, Dark);
                        }
                break;
            }
            case TransitionKind.Trainer or TransitionKind.TrainerStrong:
            {
                float bar = height / (float)Bars, w = closed * width;
                for (int i = 0; i < Bars; i++)
                    Raylib.DrawRectangleRec(new Rectangle(i % 2 == 0 ? 0f : width - w, i * bar, w, bar + 1f), Dark);
                if (kind == TransitionKind.Trainer) break;
                float upright = width / (float)Uprights, h = closed * height;
                for (int i = 0; i < Uprights; i++)
                    Raylib.DrawRectangleRec(new Rectangle(i * upright, i % 2 == 0 ? 0f : height - h, upright + 1f, h), Dark);
                break;
            }
            default:
                // A way by place or by whom, opening on the battle: its dark alone, in reverse; a band's white the same
                DrawDark(kind, closed, width, height, null);
                break;
        }
    }
}
