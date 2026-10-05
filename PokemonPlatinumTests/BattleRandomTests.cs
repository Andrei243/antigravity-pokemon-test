using System;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumTests;

/// <summary>A battle's own random numbers: Platinum's generator, rolls by kind, rolls fixed for a test.</summary>
public class BattleRandomTests
{
    [Fact]
    public void TheGeneratorIsPlatinums()
    {
        // state × 1103515245 + 24691, of which the upper sixteen bits are the number (BattleSystem_RandNext).
        // From seed 0 the states are 0x00006073, 0xE97E7B6A, 0x52713895, 0x31B0DDE4, 0x8E425287, 0xE2CCA5EE.
        var rng = new BattleRandom(0);
        var drawn = Enumerable.Range(0, 6).Select(_ => rng.Next16()).ToArray();
        Assert.Equal(new[] { 0x0000, 0xE97E, 0x5271, 0x31B0, 0x8E42, 0xE2CC }, drawn);
        Assert.Equal(0xE2CCA5EEu, rng.State);
        Assert.Equal(6, rng.Drawn);
    }

    [Fact]
    public void ARollIsTheNextNumbersRemainder()
    {
        // As in the original: rand % sides
        var rng = new BattleRandom(0);
        rng.Next16();
        Assert.Equal(0xE97E % 100, rng.Roll(RollKind.Accuracy, 100));
        Assert.Equal(0x5271 % 16, rng.Roll(RollKind.Damage, 16));
    }

    [Fact]
    public void TheSameSeedGivesTheSameNumbersAndAStateCarriesOn()
    {
        var a = new BattleRandom(20261004);
        var b = new BattleRandom(20261004);
        var first = Enumerable.Range(0, 50).Select(_ => a.Roll(RollKind.Other, 100)).ToArray();
        Assert.Equal(first, Enumerable.Range(0, 50).Select(_ => b.Roll(RollKind.Other, 100)).ToArray());

        // A battle put away and taken up again rolls on as if it had never stopped
        var resumed = BattleRandom.Resume(a.Seed, a.State, a.Drawn);
        Assert.Equal(Enumerable.Range(0, 20).Select(_ => a.Next16()).ToArray(), Enumerable.Range(0, 20).Select(_ => resumed.Next16()).ToArray());
        Assert.Equal(a.Drawn, resumed.Drawn);

        Assert.NotEqual(first, Enumerable.Range(0, 50).Select(_ => new BattleRandom(7).Roll(RollKind.Other, 100)).ToArray());
    }

    [Fact]
    public void AKindOfRollCanBeFixed()
    {
        var rng = new BattleRandom(99).Force(RollKind.Critical, 0).Force(RollKind.Damage, 3, 9, 15);

        // One value: every roll of the kind. Several: in turn, and the last one stays
        Assert.Equal(new[] { 0, 0, 0 }, new[] { rng.Roll(RollKind.Critical, 16), rng.Roll(RollKind.Critical, 16), rng.Roll(RollKind.Critical, 8) });
        Assert.Equal(new[] { 3, 9, 15, 15, 15 }, Enumerable.Range(0, 5).Select(_ => rng.Roll(RollKind.Damage, 16)).ToArray());

        // A value past the die's last face is its last face, and fixed rolls draw nothing
        Assert.Equal(1, rng.Force(RollKind.ConfusionSelfHit, 99).Roll(RollKind.ConfusionSelfHit, 2));
        Assert.Equal(0, rng.Drawn);

        // The other kinds are still chance, and a freed kind is again
        var plain = new BattleRandom(99);
        Assert.Equal(plain.Roll(RollKind.Accuracy, 100), rng.Roll(RollKind.Accuracy, 100));
        Assert.Equal(plain.Roll(RollKind.Critical, 16), rng.Free(RollKind.Critical).Roll(RollKind.Critical, 16));

        Assert.Throws<ArgumentException>(() => rng.Force(RollKind.Thaw));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Roll(RollKind.Thaw, 0));
    }

    [Fact]
    public void ItStandsInForASystemRandom()
    {
        Random rng = new BattleRandom(4242);
        for (int i = 0; i < 2000; i++)
        {
            Assert.InRange(rng.Next(100), 0, 99);
            Assert.InRange(rng.Next(85, 101), 85, 100);
            Assert.InRange(rng.Next(), 0, int.MaxValue - 1);
            Assert.InRange(rng.NextDouble(), 0.0, 0.9999999999);
            Assert.InRange(rng.NextSingle(), 0f, 0.99999f);
            Assert.InRange(rng.NextInt64(1_000_000_000_000), 0, 999_999_999_999);
            Assert.InRange(rng.NextInt64(-5, 5), -5, 4);
        }
        Assert.Equal(0, rng.Next(0));
        Assert.Equal(0, rng.Next(1));
        Assert.Equal(7, rng.Next(7, 7));
        Assert.Equal(7, rng.Next(7, 8));

        // Every face of a die comes up, and about as often as the others
        var faces = new int[16];
        for (int i = 0; i < 16000; i++) faces[rng.Next(16)]++;
        Assert.All(faces, count => Assert.InRange(count, 800, 1200));

        var bytes = new byte[64];
        rng.NextBytes(bytes);
        Assert.True(bytes.Distinct().Count() > 16);
    }

    [Fact]
    public void RollsByKindDrawWhatTheUnnamedRollsDrew()
    {
        // A battle can still be handed a plain System.Random (older tests do): a roll by kind then draws exactly
        // what the same call without a kind would.
        var named = new Random(31337);
        var plain = new Random(31337);
        for (int i = 0; i < 500; i++)
        {
            Assert.Equal(plain.Next(16), named.Roll(RollKind.Critical, 16));
            Assert.Equal(plain.Next(85, 101), 85 + named.Roll(RollKind.Damage, 16));
            Assert.Equal(plain.Next(100), named.Roll(RollKind.Accuracy, 100));
            Assert.Equal(plain.Next(1, 5), 1 + named.Roll(RollKind.SleepTurns, 4));
            Assert.Equal(plain.Next(2, 6), 2 + named.Roll(RollKind.ConfusionTurns, 4));
            Assert.Equal(plain.Next(2) == 0, named.Roll(RollKind.ConfusionSelfHit, 2) < 1);
            Assert.Equal(plain.Next(), named.RollAny(RollKind.SpeedTie));
            Assert.Equal(plain.NextDouble(), named.RollFraction(RollKind.AiChoice));
            Assert.Equal(plain.Next(65536), named.Roll(RollKind.CatchShake, 65536));
        }
    }
}
