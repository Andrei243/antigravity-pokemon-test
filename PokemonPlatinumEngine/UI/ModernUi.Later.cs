using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Which mark the move menu's item button carries (plan 25 · Y14).</summary>
internal enum LaterMark { None, Mega, ZPower, UltraBurst, Dynamax, Tera, MegaDynamax }

// The move menu's item button (plan 06 · R20, R21; plan 25 · Y14; style guide, "Battle panels", "The item button in
// the move menu"): where the Pokémon whose moves are chosen can Mega Evolve, use a Z-Move or Ultra Burst, a button over
// the move panel's top left with its mark, its word and its key, lit while armed; and with the Z-Power armed each move
// card names its Z-Move.
internal static partial class ModernUi
{
    private static readonly Color MegaFrom = new(176, 92, 236, 255), MegaTo = new(236, 104, 176, 255);
    private static readonly Color ZFrom = new(240, 176, 56, 255), ZTo = new(255, 214, 110, 255);
    private static readonly Color BurstDark = new(70, 40, 120, 255);

    /// <summary>The mark the item button carries now and whether it is armed (GPU-free: the screen's own state).</summary>
    public static (LaterMark Mark, bool Armed) LaterButtonOf(BattleEngine battle)
    {
        // A Mega Dynamax armed with the Mega Evolution, its second step (plan 06 · R20c)
        if (battle.CanMegaEvolveNow) return battle.MegaDynamaxArmed ? (LaterMark.MegaDynamax, true) : (LaterMark.Mega, battle.MegaArmed);
        if (battle.CanZMoveNow) return (battle.CanUltraBurstNow ? LaterMark.UltraBurst : LaterMark.ZPower, battle.ZArmed);
        // Dynamax and Terastallization (plan 25 · Y15), in the order the button goes by; a Mega's Dynamax is a Mega Dynamax
        if (battle.CanDynamaxNow) return (battle.MenuIsMega ? LaterMark.MegaDynamax : LaterMark.Dynamax, battle.DynamaxArmed);
        if (battle.CanTerastallizeNow) return (LaterMark.Tera, battle.TeraArmed);
        return (LaterMark.None, false);
    }

    /// <summary>
    /// What a move's card says: its own name; with the Z-Power armed its Z-Move's (a status move's as Z- and its name),
    /// null where it has none; with the Dynamax armed or the Pokémon Dynamaxed its Max Move's (plan 25 · Y15).
    /// </summary>
    public static string? CardName(BattleEngine battle, Move move, int index)
    {
        if (!battle.ZArmed) return battle.MaxMoveAt(index)?.DisplayName ?? move.DisplayName;
        var z = battle.ZMoveAt(index);
        if (z == null) return null;
        return z == move.Data ? Text.Of("battle.zMoveName", ("move", move.DisplayName)) : z.DisplayName;
    }

    private static void LaterButton(BattleEngine battle)
    {
        var (mark, armed) = LaterButtonOf(battle);
        if (mark == LaterMark.None) return;
        string word = (mark switch
        {
            LaterMark.Mega => Text.Of("battleMenu.later.mega"),
            LaterMark.UltraBurst => Text.Of("battleMenu.later.ultraBurst"),
            LaterMark.Dynamax => Text.Of("battleMenu.later.dynamax"),
            LaterMark.MegaDynamax => Text.Of("battleMenu.later.megaDynamax"),
            LaterMark.Tera => Text.Of("battleMenu.later.tera"),
            _ => Text.Of("battleMenu.later.zPower")
        }).ToUpperInvariant();
        var tera = battle.MenuBattler.Pokemon?.TeraType ?? TeraType.Of(PokemonType.Normal);
        string key = Text.Of("shared.keys.c");
        float wordWidth = UiFonts.Measure(word, 26, UiWeight.Black);
        float keyWidth = Math.Max(34f, UiFonts.Measure(key, 22, UiWeight.Black) + 18f);
        float width = 72 + wordWidth + 18 + keyWidth + 14;
        // Over the field, its right edge 24 short of the player's HP box and its foot 14 over the move panels
        var r = new Rectangle(1216 - width, 772, width, 62);
        var (lit, deep) = mark switch
        {
            LaterMark.Mega => (MegaFrom, MegaTo),
            LaterMark.UltraBurst => (ZTo, BurstDark),
            LaterMark.Dynamax => (DynamaxTo, DynamaxFrom),
            LaterMark.MegaDynamax => (MegaTo, DynamaxFrom),
            LaterMark.Tera => TeraButtonColours(tera),
            _ => (ZTo, ZFrom)
        };
        if (armed)
        {
            UiShapes.Shadow(r, 31, 24, Vector2.Zero, lit with { A = 170 });
            UiShapes.Shape(r, 31, lit, deep, Darker(deep, 0.3f), 3f);
        }
        else
        {
            UiShapes.Shadow(r, 31, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 31, PanelTop, PanelBottom, Frame, 3f);
        }
        var disc = new Vector2(r.X + 37, r.Y + 31);
        if (armed) UiShapes.Glow(disc, 34, 34, Color.White with { A = 120 });
        UiShapes.Circle(disc, 25, armed ? Color.White : new Color(232, 236, 246, 255));
        DrawLaterMark(mark, disc, tera);
        UiFonts.DrawCentered(word, r.X + 72, r.Y + 31, 26, armed ? Color.White : Ink, UiWeight.Black);
        var pill = new Rectangle(r.X + r.Width - 14 - keyWidth, r.Y + 14, keyWidth, 34);
        UiShapes.Shape(pill, 17, armed ? Darker(deep, 0.35f) : Frame, armed ? Darker(deep, 0.45f) : Darker(Frame, 0.15f));
        float kw = UiFonts.Measure(key, 22, UiWeight.Black);
        UiFonts.DrawCentered(key, pill.X + (pill.Width - kw) / 2f, pill.Y + 17, 22, Color.White, UiWeight.Black);
    }

    private static readonly Color DynamaxFrom = new(200, 30, 70, 255), DynamaxTo = new(255, 96, 150, 255), DynamaxRed = new(232, 40, 84, 255);
    private static readonly Color StellarGold = new(255, 206, 96, 255), StellarViolet = new(150, 96, 200, 255);

    /// <summary>A Tera Type's colour on the interface: its type's, or a Stellar one's gold.</summary>
    private static Color TeraColour(TeraType tera) => tera.IsStellar ? StellarGold : Palette.GetTypeColor(tera.Type.ToString());

    /// <summary>The TERASTALLIZE button's two colours armed: the Tera Type's, lighter to darker (a Stellar one's gold to violet).</summary>
    private static (Color Lit, Color Deep) TeraButtonColours(TeraType tera) =>
        tera.IsStellar ? (StellarGold, StellarViolet) : (Lighter(TeraColour(tera), 0.15f), Darker(TeraColour(tera), 0.2f));

    /// <summary>
    /// What a Dynamax and a Terastallization add to a Pokémon's HP box (plan 25 · Y15; style guide, "Battle panels"):
    /// the turns its Dynamax has left on a red pill and the crown of its Tera Type, each hung on the box's top edge.
    /// </summary>
    private static void GiantTags(Rectangle box, CombatantView view)
    {
        if (view.Dynamaxed && view.DynamaxTurnsLeft > 0)
        {
            var pill = new Rectangle(box.X + box.Width - 40 - 92, box.Y - 14, 92, 30);
            UiShapes.Shadow(pill, 15, 8, new Vector2(0, 3), ShadowColor);
            UiShapes.Shape(pill, 15, Lighter(DynamaxRed, 0.1f), Darker(DynamaxRed, 0.1f), Darker(DynamaxRed, 0.35f), 2);
            // Three small chevrons rising, and the turns left
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2(pill.X + 18 + i * 13, pill.Y + 19);
                UiShapes.Line(at + new Vector2(-5, 0), at + new Vector2(0, -6), 2.5f, Color.White);
                UiShapes.Line(at + new Vector2(0, -6), at + new Vector2(5, 0), 2.5f, Color.White);
            }
            // The count, ticking down as a menu settles a lower one (plan 25 · YD3): the old falls away, the new drops in
            var (from, to, k) = TickOf(view);
            float rx = pill.X + pill.Width - 16;
            if (k < 1f)
            {
                string old = from.ToString();
                UiFonts.DrawCentered(old, rx - UiFonts.Measure(old, 24, UiWeight.Black), pill.Y + 15 + TickFall * k, 24, Color.White with { A = (byte)(255 * (1f - k)) }, UiWeight.Black);
            }
            string turns = to.ToString();
            UiFonts.DrawCentered(turns, rx - UiFonts.Measure(turns, 24, UiWeight.Black), pill.Y + 15 - TickFall * (1f - k), 24, Color.White with { A = (byte)(255 * k) }, UiWeight.Black);
        }
        if (view.Shown?.Terastallized is { } tera)
        {
            var c = new Vector2(box.X + box.Width - 150 - 19, box.Y + 1);
            UiShapes.Circle(c, 19, Frame);
            UiShapes.Circle(c, 16.5f, Color.White);
            TeraCrown(c, tera, 0.62f);
        }
    }

    // ------------------------------------------------------------------ the boss box (plan 25 · YD6)

    /// <summary>The foe's own box, and the boss box it widens into (the style guide's "The boss box"), layout units.</summary>
    public static readonly Rectangle BossFrom = new(56, 52, 580, 120), BossTo = new(56, 22, 1104, 100);

    /// <summary>
    /// The boss box's right end stops short of the foe's platform as the overview sees it (x 1180), so the giant's head
    /// and crown stand clear of it (plan 25's leftovers; a box along the whole top hid them).
    /// </summary>
    public const float BossRightLimit = 1160f;

    /// <summary>The boss box at a share of its widening (GPU-free): the foe's own box at nought, the whole at one.</summary>
    public static Rectangle BossRect(float share)
    {
        float k = Math.Clamp(share, 0f, 1f);
        return new Rectangle(BossFrom.X + (BossTo.X - BossFrom.X) * k, BossFrom.Y + (BossTo.Y - BossFrom.Y) * k,
            BossFrom.Width + (BossTo.Width - BossFrom.Width) * k, BossFrom.Height + (BossTo.Height - BossFrom.Height) * k);
    }

    /// <summary>
    /// The share of the most HP the boss box's bar holds (GPU-free): exactly the HP box's own, the view's displayed
    /// HP over the screen's Pokémon's most, which <c>TakeRaisedHp</c> keeps as the giant grows and falls.
    /// </summary>
    public static float BossBarShare(CombatantView view) =>
        view.Shown is { } p ? view.DisplayedHp / Math.Max(1, p.MaxHP) : 0f;

    /// <summary>
    /// The boss box's three pips (GPU-free): how many are lit (the turns left), and the alpha of the one going out as
    /// the pill's number would fall (1 when nothing ticks).
    /// </summary>
    public static (int Lit, int Going, float GoingAlpha) BossPips(CombatantView view)
    {
        var (from, to, k) = TickOf(view);
        return from > to ? (to, from, 1f - k) : (to, -1, 1f);
    }

    private static Color Toward(Color a, Color b, float k) => new((byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k), (byte)(a.A + (b.A - a.A) * k));

    /// <summary>The pips' radius and the gap between their middles.</summary>
    public const float BossPipRadius = 13f, BossPipGap = 34f;

    /// <summary>
    /// The boss box: the foe's HP box widened along the picture's top as a Mega Max team's ace Mega Dynamaxes, the
    /// Mega Max mark on a disc, the trainer's class and name over the ace's, a bar across, its level and the three
    /// turns as three pips; what belongs to the widened box fades in with it.
    /// </summary>
    private static void BossBox(CombatantView view, float share, Party? trainerParty, string trainerLine)
    {
        var p = view.Shown!;
        var (jolt, ghost, _) = PunchOf(view);
        var r = BossRect(share);
        r.X += jolt.X;
        r.Y += jolt.Y;
        byte a = (byte)(255 * Math.Clamp((share - 0.4f) / 0.6f, 0f, 1f));

        TrainerTray(r.X, r.Y + r.Height, trainerParty);

        Panel(r, 22, skew: -0.2f, frame: Toward(Frame, DynamaxRed, share * 0.7f));
        // The mark on its disc, coming in with the widening
        float inset = 96f * share;
        float nameY = r.Y + 38 - 4 * share;
        float nameW = NameWithGender(p, r.X + 38 + inset, nameY, 36);
        float pillW = StatusPill(r.X + 38 + inset + nameW + 14, nameY - 16, p.Status, 30);
        if (a > 0)
        {
            var disc = new Vector2(r.X + 28 + 40, r.Y + r.Height / 2f);
            UiShapes.Glow(disc, 50, 50, DynamaxRed with { A = (byte)(a * 0.45f) });
            UiShapes.Circle(disc, 40, DynamaxRed with { A = a });
            UiShapes.Circle(disc, 34, Color.White with { A = a });
            if (a == 255) DrawLaterMark(LaterMark.MegaDynamax, disc);
            // Whose ace it is, after its name on the same row
            float after = r.X + 38 + inset + nameW + 22 + (pillW > 0 ? pillW + 14 : 0);
            UiFonts.DrawCentered(trainerLine.ToUpperInvariant(), after, nameY + 2, 24, Muted with { A = a }, UiWeight.ExtraBold);
        }
        Level(r.X + r.Width - 46, nameY, p.Level, 34);
        float barY = r.Y + 72 - 10 * share;
        float barRight = 52 + (3 * BossPipGap + 6) * share;
        HpBar(r.X + 38 + inset, barY, r.Width - 38 - inset - barRight, 24 + 2 * share, BossBarShare(view), ghost);
        if (a == 0) return;
        // The three turns as three pips, lit for each left; the one going fades out as the pill's number would fall
        var (lit, going, goingAlpha) = BossPips(view);
        for (int i = 0; i < 3; i++)
        {
            var c = new Vector2(r.X + r.Width - 46 - BossPipGap * (2 - i) - BossPipRadius, barY + 12 + share);
            UiShapes.Ring(c, BossPipRadius, 3f, Darker(DynamaxRed, 0.35f) with { A = a });
            if (i < lit) UiShapes.Circle(c, BossPipRadius - 3f, DynamaxRed with { A = a });
            else if (i == going - 1) UiShapes.Circle(c, BossPipRadius - 3f, DynamaxRed with { A = (byte)(a * goingAlpha) });
        }
    }

    /// <summary>How far the pill's numbers fall as it ticks, layout units (plan 25 · YD3).</summary>
    public const float TickFall = 14f;

    /// <summary>The Dynamax pill's tick (plan 25 · YD3; GPU-free): the count it ticks from, the count now, and how far through (1 = settled).</summary>
    public static (int From, int To, float Progress) TickOf(CombatantView view)
    {
        if (view.TickAge < 0f || view.TickFrom <= view.DynamaxTurnsLeft) return (view.DynamaxTurnsLeft, view.DynamaxTurnsLeft, 1f);
        float k = Math.Clamp(view.TickAge / BattleAnimator.TickTime, 0f, 1f);
        return (view.TickFrom, view.DynamaxTurnsLeft, k * k * (3f - 2f * k));
    }

    /// <summary>The box's jolt across and down at most, layout units (plan 25 · YD2).</summary>
    public const float JoltAcross = 6f, JoltDown = 3f;

    /// <summary>
    /// A Max Move's punch on a Pokémon's HP box (plan 25 · YD2; GPU-free): how far the box is jolted now (none under
    /// <see cref="BattleCamera.Motion"/> nought), the HP the bar's white ghost reaches as a share of the most (-1 for
    /// none), and how much the HP number swells.
    /// </summary>
    public static (Vector2 Jolt, float Ghost, float Thump) PunchOf(CombatantView view)
    {
        if (view.PunchAge < 0f || view.Shown is not { } p) return (Vector2.Zero, -1f, 1f);
        float age = view.PunchAge;
        var jolt = Vector2.Zero;
        if (age < BattleAnimator.PunchTime)
        {
            float k = (1f - age / BattleAnimator.PunchTime) * Math.Clamp(BattleCamera.Motion, 0f, 1f);
            jolt = new Vector2(JoltAcross * MathF.Sin(age * MathF.Tau * 11f), JoltDown * MathF.Abs(MathF.Cos(age * MathF.Tau * 11f))) * k;
        }
        float ghost = view.GhostHp > view.DisplayedHp ? view.GhostHp / Math.Max(1, p.MaxHP) : -1f;
        float thump = age < 0.25f ? 1f + 0.25f * MathF.Sin(age / 0.25f * MathF.PI) : 1f;
        return (jolt, ghost, thump);
    }

    /// <summary>A crown of three facets in a Tera Type's colour (a Stellar one's in four), 34 wide at a scale of 1 × 0.62 on the box.</summary>
    private static void TeraCrown(Vector2 c, TeraType tera, float scale)
    {
        Color[] stellar = { new(255, 92, 92, 255), StellarGold, new(90, 220, 240, 255), new(186, 110, 255, 255) };
        var colour = TeraColour(tera);
        for (int i = -1; i <= 1; i++)
        {
            float tall = (i == 0 ? 24f : 17f) * scale;
            float half = 8f * scale;
            var foot = c + new Vector2(i * 15f * scale, 10f * scale);
            var tip = foot + new Vector2(i * 4f * scale, -tall);
            var fill = tera.IsStellar ? stellar[i + 1] : (i == 0 ? Lighter(colour, 0.15f) : colour);
            UiShapes.Triangle(tip, foot + new Vector2(-half, 0), foot + new Vector2(half, 0), fill);
            UiShapes.Triangle(tip, foot + new Vector2(0, 0), foot + new Vector2(half, 0), Darker(fill, 0.2f));
        }
        UiShapes.Line(c + new Vector2(-22f * scale, 11f * scale), c + new Vector2(22f * scale, 11f * scale), 4f * scale, tera.IsStellar ? stellar[3] : Darker(colour, 0.3f));
    }

    /// <summary>A mark of our own on a disc 50 across: the Key Stone, the Z crystal, Ultra Burst's ring of rays, Dynamax's rising chevrons or a Tera gem.</summary>
    private static void DrawLaterMark(LaterMark mark, Vector2 c, TeraType tera = default)
    {
        switch (mark)
        {
            case LaterMark.Dynamax:
            {
                // A red ring, and three chevrons rising inside it
                UiShapes.Ring(c, 19, 4, DynamaxRed);
                for (int i = 0; i < 3; i++)
                {
                    var at = c + new Vector2(0, 9 - i * 8);
                    UiShapes.Line(at + new Vector2(-9, 0), at + new Vector2(0, -7), 4f, i == 2 ? DynamaxTo : DynamaxRed);
                    UiShapes.Line(at + new Vector2(0, -7), at + new Vector2(9, 0), 4f, i == 2 ? DynamaxTo : DynamaxRed);
                }
                break;
            }
            case LaterMark.Tera:
            {
                // A faceted gem: six triangles round its middle, lit and shaded in turn
                var colour = TeraColour(tera);
                for (int i = 0; i < 6; i++)
                {
                    float a0 = i * MathF.Tau / 6f - MathF.PI / 2f, a1 = (i + 1) * MathF.Tau / 6f - MathF.PI / 2f;
                    var p0 = c + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * 20f;
                    var p1 = c + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * 20f;
                    var fill = tera.IsStellar
                        ? (i % 3 == 0 ? StellarGold : i % 3 == 1 ? StellarViolet : new Color(90, 220, 240, 255))
                        : i % 2 == 0 ? Lighter(colour, 0.25f) : Darker(colour, 0.1f);
                    UiShapes.Triangle(c, p0, p1, fill);
                }
                UiShapes.Circle(c + new Vector2(-6, -8), 2.5f, Color.White);
                break;
            }
            case LaterMark.MegaDynamax:
                // The Key Stone in Dynamax's red ring (plan 06 · R20c; plan 25 draws its own look later)
                DrawLaterMark(LaterMark.Mega, c);
                UiShapes.Ring(c, 22, 3.5f, DynamaxRed);
                break;
            case LaterMark.Mega:
            {
                // A round stone with two arcs of colour swirled through it and a glint
                UiShapes.Circle(c, 19, new Color(120, 128, 160, 255));
                UiShapes.Circle(c, 16.5f, new Color(236, 236, 246, 255));
                Arc(c + new Vector2(-3, 2), 9, 200f, 380f, 4f, new Color(186, 96, 255, 255));
                Arc(c + new Vector2(3, -2), 9, 20f, 200f, 4f, new Color(255, 176, 70, 255));
                UiShapes.Circle(c + new Vector2(-8, -8), 3, Color.White);
                break;
            }
            case LaterMark.UltraBurst:
                UiShapes.Ring(c, 11, 4, new Color(232, 168, 40, 255));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.Tau / 8f;
                    var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    UiShapes.Line(c + dir * 15, c + dir * 21, 3.5f, new Color(232, 168, 40, 255));
                }
                UiShapes.Circle(c, 5, BurstDark);
                break;
            default:
            {
                // A crystal (a diamond) with a Z across it
                var gold = new Color(232, 168, 40, 255);
                UiShapes.Triangle(c + new Vector2(0, -20), c + new Vector2(-15, 0), c + new Vector2(15, 0), Lighter(gold, 0.2f));
                UiShapes.Triangle(c + new Vector2(-15, 0), c + new Vector2(0, 20), c + new Vector2(15, 0), gold);
                UiShapes.Line(c + new Vector2(-6, -6), c + new Vector2(6, -6), 3.5f, Color.White);
                UiShapes.Line(c + new Vector2(6, -6), c + new Vector2(-6, 6), 3.5f, Color.White);
                UiShapes.Line(c + new Vector2(-6, 6), c + new Vector2(6, 6), 3.5f, Color.White);
                break;
            }
        }
    }

    /// <summary>An arc of a circle (degrees, clockwise on screen), drawn as short lines.</summary>
    private static void Arc(Vector2 c, float radius, float from, float to, float thickness, Color color)
    {
        const int Steps = 10;
        var last = c + new Vector2(MathF.Cos(from * MathF.PI / 180f), MathF.Sin(from * MathF.PI / 180f)) * radius;
        for (int i = 1; i <= Steps; i++)
        {
            float a = (from + (to - from) * i / Steps) * MathF.PI / 180f;
            var p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            UiShapes.Line(last, p, thickness, color);
            last = p;
        }
    }
}
