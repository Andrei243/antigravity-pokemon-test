using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What the first run's measuring came to (plan 16 · T23): the preset, whether dynamic resolution comes on with it, and each preset's median.</summary>
public sealed record QualityChoice(GraphicsQuality Quality, bool DynamicResolution, IReadOnlyDictionary<GraphicsQuality, double> Medians);

/// <summary>
/// The preset a machine can keep (plan 16 · T23; style guide, "Quality presets", "The first run's measurement"). GPU-free:
/// it is handed frame times and decides. <see cref="Choose"/> gives the highest preset whose median frame is under
/// <see cref="Budget"/>, the heaviest town's frame on High on the GTX 970, so a battle's extra millisecond still fits the
/// 8 ms budget; Low when none is, and then (plan 16's decision 12) dynamic resolution comes on with it.
/// <see cref="Measurement"/> is the measuring itself, frame by frame: from High down, <see cref="SettleFrames"/> frames
/// to settle and <see cref="TimedFrames"/> timed for each, stopping at the first preset that fits.
/// </summary>
public static class QualityProbe
{
    /// <summary>The most a preset's median frame may take, in milliseconds.</summary>
    public const double Budget = 6.5;

    /// <summary>Frames rendered after a preset is put in force and not timed: its targets are made and the driver settles.</summary>
    public const int SettleFrames = 10;

    /// <summary>Frames timed for each preset.</summary>
    public const int TimedFrames = 30;

    /// <summary>The presets in the order they are tried: the highest first.</summary>
    public static IReadOnlyList<GraphicsQuality> Order => new[] { GraphicsQuality.High, GraphicsQuality.Medium, GraphicsQuality.Low };

    /// <summary>The middle of a set of times (the mean of the two middle ones for an even count); nought for none.</summary>
    public static double Median(IEnumerable<double> times)
    {
        var sorted = times.OrderBy(t => t).ToArray();
        if (sorted.Length == 0) return 0;
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    /// <summary>
    /// The highest preset whose median is under <see cref="Budget"/>, from each preset's frame times (a preset not
    /// measured counts as missing it); Low with dynamic resolution when none fits.
    /// </summary>
    public static QualityChoice Choose(IReadOnlyDictionary<GraphicsQuality, IReadOnlyList<double>> timings)
    {
        var medians = timings.Where(t => t.Value.Count > 0).ToDictionary(t => t.Key, t => Median(t.Value));
        foreach (var quality in Order)
            if (medians.TryGetValue(quality, out double median) && median < Budget)
                return new QualityChoice(quality, false, medians);
        return new QualityChoice(GraphicsQuality.Low, true, medians);
    }

    /// <summary>The measuring, a frame at a time: which preset to render next, and what each timed frame took.</summary>
    public sealed class Measurement
    {
        private readonly Dictionary<GraphicsQuality, List<double>> timings = new();
        private int tried, frame;

        /// <summary>The preset the next frame is rendered at.</summary>
        public GraphicsQuality Current => Order[Math.Min(tried, Order.Count - 1)];

        /// <summary>The frames rendered at <see cref="Current"/> so far, settling ones included: which of the shots to render.</summary>
        public int Frame => frame;

        /// <summary>Whether the frame about to be rendered is timed (the settling ones are not).</summary>
        public bool Timing => frame >= SettleFrames;

        /// <summary>Whether the measuring has ended: <see cref="Result"/> is then what it came to.</summary>
        public bool Done => Result != null;

        public QualityChoice? Result { get; private set; }

        /// <summary>Every preset's timed frames so far.</summary>
        public IReadOnlyDictionary<GraphicsQuality, IReadOnlyList<double>> Timings =>
            timings.ToDictionary(t => t.Key, t => (IReadOnlyList<double>)t.Value);

        /// <summary>
        /// A frame rendered at <see cref="Current"/> and what it took in milliseconds (ignored while it settles). After
        /// the last timed frame of a preset that fits, or of Low, the measuring is done.
        /// </summary>
        public void Record(double milliseconds)
        {
            if (Done) return;
            if (frame++ < SettleFrames) return;
            if (!timings.TryGetValue(Current, out var list)) timings[Current] = list = new List<double>();
            list.Add(milliseconds);
            if (list.Count < TimedFrames) return;
            if (Median(list) < Budget || tried == Order.Count - 1)
            {
                Result = Choose(Timings);
                return;
            }
            tried++;
            frame = 0;
        }

        /// <summary>Ends the measuring at once with what it has, as when the machine can't wait for its card.</summary>
        public void Stop() => Result ??= Choose(Timings);
    }
}
