using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Dynamic resolution (plan 16 · T23; style guide, "Quality presets", "Dynamic resolution"): the share of the preset's
/// width and height the scene is drawn at, a step of <see cref="Steps"/>, decided from samples of the frame's work.
/// GPU-free: whoever measures feeds it (<see cref="Feed"/>, with the time of the sample on a clock of their own).
/// A step down when the median of the last <see cref="Window"/> samples is over the frame the cap allows less a
/// quarter; a step up only after <see cref="CalmSeconds"/> of samples under <see cref="CalmShare"/> of that frame;
/// never two steps within <see cref="StepGap"/>. A step forgets the samples taken at the step before.
/// </summary>
public sealed class ResolutionScale
{
    /// <summary>The scene's share of the preset's width and height at each step, in percent.</summary>
    public static IReadOnlyList<int> Steps => new[] { 100, 90, 80, 70, 60, 50 };

    /// <summary>How many of the last samples the median is taken over.</summary>
    public const int Window = 20;

    /// <summary>The fewest samples a step down is decided on, so one long frame after a step moves nothing.</summary>
    public const int Fewest = 8;

    /// <summary>How long every sample must stay calm before a step up, in seconds.</summary>
    public const double CalmSeconds = 3;

    /// <summary>The share of the frame a calm sample stays under.</summary>
    public const double CalmShare = 0.6;

    /// <summary>The least time between two steps, in seconds.</summary>
    public const double StepGap = 1;

    /// <summary>One frame in this many is a sample (it waits for the card; the others wait for nothing).</summary>
    public const int SampleEvery = 15;

    private readonly Queue<double> samples = new();
    private double lastStep = double.NegativeInfinity;
    private double? calmSince;

    /// <summary>Which of <see cref="Steps"/> the scene is drawn at: 0 is the whole preset.</summary>
    public int Step { get; private set; }

    /// <summary>The scene's share of the preset's width and height now.</summary>
    public float Scale => Steps[Step] / 100f;

    /// <summary>The frame the cap allows, in milliseconds: no cap (nought) counts as T3's 60.</summary>
    public static double FrameOf(int frameCap) => 1000.0 / (frameCap > 0 ? frameCap : 60);

    /// <summary>The frame's work over which a step is taken down: the frame the cap allows less a quarter.</summary>
    public static double DownOver(int frameCap) => FrameOf(frameCap) * 0.75;

    /// <summary>
    /// A sample: what a frame's work took in milliseconds, at <paramref name="now"/> seconds, under a cap of
    /// <paramref name="frameCap"/> frames a second. Returns true when the step changed.
    /// </summary>
    public bool Feed(double milliseconds, double now, int frameCap)
    {
        samples.Enqueue(milliseconds);
        while (samples.Count > Window) samples.Dequeue();
        double frame = FrameOf(frameCap);
        if (milliseconds < frame * CalmShare) calmSince ??= now;
        else calmSince = null;
        if (now - lastStep < StepGap) return false;

        if (samples.Count >= Fewest && QualityProbe.Median(samples) > DownOver(frameCap) && Step < Steps.Count - 1)
            return Move(+1, now);
        if (calmSince is { } calm && now - calm >= CalmSeconds && Step > 0)
            return Move(-1, now);
        return false;
    }

    /// <summary>Back to the whole preset, with nothing remembered (the row turned off, a preset changed).</summary>
    public void Reset()
    {
        Step = 0;
        samples.Clear();
        calmSince = null;
        lastStep = double.NegativeInfinity;
    }

    private bool Move(int by, double now)
    {
        Step = Math.Clamp(Step + by, 0, Steps.Count - 1);
        lastStep = now;
        samples.Clear();
        calmSince = null;
        return true;
    }
}
