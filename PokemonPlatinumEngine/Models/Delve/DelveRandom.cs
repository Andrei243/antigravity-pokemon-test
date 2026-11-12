using System;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// The delve's chance (plan 44 · SD7): a <see cref="Random"/> whose every draw is a <see cref="SplitMix"/>'s, so a seed
/// gives the same floor and the same run on any machine and any version of .NET (a <see cref="Random"/> of its own seed
/// promises neither). A run draws everything from one, a floor's layout and residents from one of the floor's own seed.
/// No drawing or input.
/// </summary>
public sealed class DelveRandom : Random
{
    private readonly SplitMix mix;

    public DelveRandom(uint seed) : base(0) => mix = new SplitMix(seed);

    public override int Next(int maxValue) => mix.Int(maxValue);
    public override int Next() => (int)(mix.Next() >> 1);
    public override int Next(int minValue, int maxValue) => minValue + mix.Int(maxValue - minValue);
    public override double NextDouble() => mix.Float();
    protected override double Sample() => mix.Float();

    public override void NextBytes(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)mix.Int(256);
    }

    public override void NextBytes(Span<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)mix.Int(256);
    }

    /// <summary>A seed made of another and a number, so a run's floors each have their own: the same pair, the same seed.</summary>
    public static uint Mix(uint seed, int n)
    {
        var m = new SplitMix(seed ^ (uint)n * 0x9E3779B9u);
        m.Next();
        return m.Next();
    }
}
