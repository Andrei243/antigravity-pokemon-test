using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The winter masks on screen (plan 46 · CP13; style guide, "Battle panels", "The MASK button"): the move menu's MASK
// button above the item and TALE buttons, the mask it would put on and its key. The mask on the body is CP14's.
internal static partial class ModernUi
{
    /// <summary>The MASK button's colours: the carved mask's red wood, its deep and its horns' pale (our own, from the style guide).</summary>
    private static readonly Color MaskRed = new(196, 58, 52, 255), MaskDeep = new(110, 30, 28, 255), MaskHorn = new(232, 220, 196, 255);

    /// <summary>The mask the MASK button offers and whether it is armed; null where no mask can be put on.</summary>
    public static (string Mask, bool Armed)? MaskButtonOf(BattleEngine battle) =>
        battle.CanWearMaskNow && battle.MaskOffered is { } mask ? (mask, battle.MaskArmed != null) : null;

    /// <summary>
    /// The move menu's MASK button: of the item button's shape, a row above the highest of the item and TALE buttons
    /// shown, a carved mask's mark, the mask it would put on and its key; lit in the mask's red while armed.
    /// </summary>
    private static void MaskButton(BattleEngine battle)
    {
        if (MaskButtonOf(battle) is not var (mask, armed)) return;
        float top = 772;
        if (LaterButtonOf(battle).Mark != LaterMark.None) top -= 72;
        if (TaleButtonOf(battle) != null) top -= 72;
        string word = WinterMasks.NameOf(mask).ToUpperInvariant();
        string key = Text.Of("shared.keys.pageLeft");
        float wordWidth = UiFonts.Measure(word, 24, UiWeight.Black);
        float keyWidth = Math.Max(34f, UiFonts.Measure(key, 22, UiWeight.Black) + 18f);
        float width = 72 + wordWidth + 18 + keyWidth + 14;
        var r = new Rectangle(1216 - width, top, width, 62);
        if (armed)
        {
            UiShapes.Shadow(r, 31, 24, Vector2.Zero, MaskHorn with { A = 170 });
            UiShapes.Shape(r, 31, MaskRed, MaskDeep, Darker(MaskDeep, 0.3f), 3f);
        }
        else
        {
            UiShapes.Shadow(r, 31, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 31, PanelTop, PanelBottom, Frame, 3f);
        }
        var disc = new Vector2(r.X + 37, r.Y + 31);
        UiShapes.Circle(disc, 25, armed ? Color.White : new Color(232, 236, 246, 255));
        // A carved mask: two pale horns, a face of red wood, a white stripe and two dark eyeholes
        UiShapes.Triangle(disc + new Vector2(-13, -8), disc + new Vector2(-17, -20), disc + new Vector2(-7, -11), MaskHorn);
        UiShapes.Triangle(disc + new Vector2(13, -8), disc + new Vector2(7, -11), disc + new Vector2(17, -20), MaskHorn);
        UiShapes.Circle(disc + new Vector2(0, 2), 15, MaskRed);
        UiShapes.Circle(disc + new Vector2(-6, -1), 3.5f, MaskDeep);
        UiShapes.Circle(disc + new Vector2(6, -1), 3.5f, MaskDeep);
        UiShapes.Shape(new Rectangle(disc.X - 9, disc.Y + 8, 18, 3), 1.5f, Color.White, Color.White);
        UiFonts.DrawCentered(word, r.X + 72, r.Y + 31, 24, armed ? Color.White : Ink, UiWeight.Black);
        var pill = new Rectangle(r.X + r.Width - 14 - keyWidth, r.Y + 14, keyWidth, 34);
        UiShapes.Shape(pill, 17, armed ? Darker(MaskDeep, 0.2f) : Frame, armed ? Darker(MaskDeep, 0.35f) : Darker(Frame, 0.15f));
        float kw = UiFonts.Measure(key, 22, UiWeight.Black);
        UiFonts.DrawCentered(key, pill.X + (pill.Width - kw) / 2f, pill.Y + 17, 22, Color.White, UiWeight.Black);
    }
}
