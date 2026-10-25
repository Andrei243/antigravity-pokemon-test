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
            string turns = view.DynamaxTurnsLeft.ToString();
            float tw = UiFonts.Measure(turns, 24, UiWeight.Black);
            UiFonts.DrawCentered(turns, pill.X + pill.Width - 16 - tw, pill.Y + 15, 24, Color.White, UiWeight.Black);
        }
        if (view.Shown?.Terastallized is { } tera)
        {
            var c = new Vector2(box.X + box.Width - 150 - 19, box.Y + 1);
            UiShapes.Circle(c, 19, Frame);
            UiShapes.Circle(c, 16.5f, Color.White);
            TeraCrown(c, tera, 0.62f);
        }
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
