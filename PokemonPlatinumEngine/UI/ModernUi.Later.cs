using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Which mark the move menu's item button carries (plan 25 · Y14).</summary>
internal enum LaterMark { None, Mega, ZPower, UltraBurst }

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
        if (battle.CanMegaEvolveNow) return (LaterMark.Mega, battle.MegaArmed);
        if (battle.CanZMoveNow) return (battle.CanUltraBurstNow ? LaterMark.UltraBurst : LaterMark.ZPower, battle.ZArmed);
        return (LaterMark.None, false);
    }

    /// <summary>What a move's card says: its own name, or with the Z-Power armed its Z-Move's (a status move's as Z- and its name); null where it has none.</summary>
    public static string? CardName(BattleEngine battle, Move move, int index)
    {
        if (!battle.ZArmed) return move.DisplayName;
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
            _ => Text.Of("battleMenu.later.zPower")
        }).ToUpperInvariant();
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
        DrawLaterMark(mark, disc);
        UiFonts.DrawCentered(word, r.X + 72, r.Y + 31, 26, armed ? Color.White : Ink, UiWeight.Black);
        var pill = new Rectangle(r.X + r.Width - 14 - keyWidth, r.Y + 14, keyWidth, 34);
        UiShapes.Shape(pill, 17, armed ? Darker(deep, 0.35f) : Frame, armed ? Darker(deep, 0.45f) : Darker(Frame, 0.15f));
        float kw = UiFonts.Measure(key, 22, UiWeight.Black);
        UiFonts.DrawCentered(key, pill.X + (pill.Width - kw) / 2f, pill.Y + 17, 22, Color.White, UiWeight.Black);
    }

    /// <summary>A mark of our own on a disc 50 across: the Key Stone, the Z crystal or Ultra Burst's ring of rays.</summary>
    private static void DrawLaterMark(LaterMark mark, Vector2 c)
    {
        switch (mark)
        {
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
