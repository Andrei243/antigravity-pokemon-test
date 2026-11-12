using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// Locked hearts on screen (plan 44 · SD2; style guide, "A locked heart"): the lock after a name, the gauge's five bars,
// and the haze over the opening's figure. Our own.
internal static partial class ModernUi
{
    /// <summary>The lock's grey and its darker rim (style guide, "A locked heart").</summary>
    public static readonly Color LockGrey = new(118, 116, 128, 255), LockRim = new(64, 60, 76, 255);

    /// <summary>A padlock after a name ending at <paramref name="x"/>; returns the width it takes.</summary>
    public static float LockAfter(float x, float centerY, float size)
    {
        LockMark(new Vector2(x + size * 0.5f, centerY), size * 0.7f);
        return size * 0.9f;
    }

    /// <summary>A padlock <paramref name="size"/> tall, centred on <paramref name="c"/>: a shackle over a body with a keyhole.</summary>
    public static void LockMark(Vector2 c, float size)
    {
        float bodyW = size * 0.78f, bodyH = size * 0.56f;
        var body = new Rectangle(c.X - bodyW / 2f, c.Y - bodyH / 2f + size * 0.18f, bodyW, bodyH);
        float shackleR = bodyW * 0.32f;
        var shackle = new Vector2(c.X, body.Y);
        UiShapes.Ring(shackle, shackleR, size * 0.12f, LockRim);
        UiShapes.Fill(new Rectangle(body.X - 2f, body.Y - 2f, body.Width + 4f, body.Height + 4f), size * 0.12f, LockRim);
        UiShapes.Fill(body, size * 0.1f, LockGrey);
        UiShapes.Circle(new Vector2(c.X, body.Y + bodyH * 0.42f), size * 0.08f, LockRim);
    }

    /// <summary>
    /// The lock's gauge (the summary's): five bars side by side in <paramref name="r"/>, full ones grey, emptied ones a
    /// hollow outline; the part of the last that is left drawn as a share of its bar.
    /// </summary>
    public static void LockGauge(Rectangle r, HeartLock locked)
    {
        const int bars = HeartLock.Full / HeartLock.BarSize;
        float gap = r.Height * 0.3f;
        float w = (r.Width - gap * (bars - 1)) / bars;
        for (int i = 0; i < bars; i++)
        {
            var bar = new Rectangle(r.X + i * (w + gap), r.Y, w, r.Height);
            UiShapes.Fill(bar, r.Height / 2f, new Color(220, 218, 226, 255));
            int left = Math.Clamp(locked.Gauge - i * HeartLock.BarSize, 0, HeartLock.BarSize);
            if (left > 0) UiShapes.Fill(bar with { Width = Math.Max(r.Height, w * left / HeartLock.BarSize) }, r.Height / 2f, LockGrey);
        }
    }

    /// <summary>The grey haze over the opening's figure, <paramref name="haze"/> of it there: drifting wisps round it.</summary>
    public static void HeartHaze(Vector2 c, float size, float haze, float time)
    {
        if (haze <= 0.01f || size < 1f) return;
        for (int i = 0; i < 6; i++)
        {
            float a = time * 0.4f + i * MathF.Tau / 6f;
            var at = c + new Vector2(MathF.Cos(a) * size * 0.32f, MathF.Sin(a * 0.7f) * size * 0.22f + size * 0.05f);
            SoftLight(at, size * 0.5f, size * 0.36f, Lit(LockGrey, 0.42f * haze), a * 30f);
        }
    }
}
