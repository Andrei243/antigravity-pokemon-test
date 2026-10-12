using System;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Which frame a face of a scene's art shows (<see cref="ArtSheet.PaintFrames(string, int, int, int, float, Action{PixelCanvas, int})"/>):
/// one that moves follows the clock at its rate (a fountain's water); one that is held, of no rate, shows the frame
/// it was last told to (plan 09 · L12: a machine's tray, a PC's screen). No GPU calls: it only says when the face's
/// rectangle of the sheet has to be redrawn, which is when its frame has changed and never otherwise.
/// </summary>
internal sealed class FaceFrames
{
    /// <summary>The face's key in its sheet, by which a held one is told its frame.</summary>
    public string Key { get; }
    public int Count { get; }

    /// <summary>Frames a second; nought for a held face.</summary>
    public float Rate { get; }

    /// <summary>The frame the sheet's texture shows now: the first until the face is first turned.</summary>
    public int Shown { get; private set; }

    private int held;

    public FaceFrames(string key, int count, float rate)
    {
        Key = key;
        Count = Math.Max(1, count);
        Rate = rate;
    }

    public bool IsHeld => Rate <= 0f;

    /// <summary>A held face is told the frame to show from now on; one out of its range is kept within it. A moving face takes no telling.</summary>
    public void Hold(int frame)
    {
        if (IsHeld) held = Math.Clamp(frame, 0, Count - 1);
    }

    /// <summary>The frame the face should show at a time of the field's clock.</summary>
    public int At(double time) => IsHeld ? held : (int)((long)(time * Rate) % Count);

    /// <summary>
    /// The frame to redraw the face with, or null when it shows the right one already: what whoever owns the texture
    /// asks once a frame before drawing.
    /// </summary>
    public int? Turn(double time)
    {
        int frame = At(time);
        if (frame == Shown) return null;
        Shown = frame;
        return frame;
    }
}
