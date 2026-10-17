using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The sign a pad's button is drawn as on a key hint or the Controls page (plan 12 · Q5): our own drawn icons, lettered
/// as an Xbox pad is (the face buttons by where they lie). GPU-free: <see cref="PadGlyphs"/> says which sign a button is,
/// <c>PadGlyphArt</c> draws it.
/// </summary>
public enum PadGlyph { A, B, X, Y, Start, Select, L, R, ZL, ZR, DPad, DPadUp, DPadDown, DPadLeft, DPadRight, DPadHorizontal, DPadVertical }

public static class PadGlyphs
{
    /// <summary>The sign of a pad button; null for a button the game draws no sign for.</summary>
    public static PadGlyph? Of(GamepadButton button) => button switch
    {
        GamepadButton.RightFaceDown => PadGlyph.A,
        GamepadButton.RightFaceRight => PadGlyph.B,
        GamepadButton.RightFaceLeft => PadGlyph.X,
        GamepadButton.RightFaceUp => PadGlyph.Y,
        GamepadButton.MiddleRight => PadGlyph.Start,
        GamepadButton.MiddleLeft => PadGlyph.Select,
        GamepadButton.LeftTrigger1 => PadGlyph.L,
        GamepadButton.RightTrigger1 => PadGlyph.R,
        GamepadButton.LeftTrigger2 => PadGlyph.ZL,
        GamepadButton.RightTrigger2 => PadGlyph.ZR,
        GamepadButton.LeftFaceUp => PadGlyph.DPadUp,
        GamepadButton.LeftFaceDown => PadGlyph.DPadDown,
        GamepadButton.LeftFaceLeft => PadGlyph.DPadLeft,
        GamepadButton.LeftFaceRight => PadGlyph.DPadRight,
        _ => null
    };

    /// <summary>The word the sign stands for (the same as the text name, in the language of the moment).</summary>
    public static string Name(PadGlyph glyph) => glyph switch
    {
        PadGlyph.A => Text.Of("padButtons.bottom"),
        PadGlyph.B => Text.Of("padButtons.right"),
        PadGlyph.X => Text.Of("padButtons.left"),
        PadGlyph.Y => Text.Of("padButtons.top"),
        PadGlyph.Start => Text.Of("padButtons.start"),
        PadGlyph.Select => Text.Of("padButtons.select"),
        PadGlyph.L => Text.Of("padButtons.l1"),
        PadGlyph.R => Text.Of("padButtons.r1"),
        PadGlyph.ZL => Text.Of("padButtons.l2"),
        PadGlyph.ZR => Text.Of("padButtons.r2"),
        PadGlyph.DPadUp => Text.Of("padButtons.dpadUp"),
        PadGlyph.DPadDown => Text.Of("padButtons.dpadDown"),
        PadGlyph.DPadLeft => Text.Of("padButtons.dpadLeft"),
        PadGlyph.DPadRight => Text.Of("padButtons.dpadRight"),
        _ => Text.Of("padButtons.dpad")
    };
}
