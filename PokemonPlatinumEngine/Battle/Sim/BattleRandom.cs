using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// What a roll decides. Rolls are asked for by kind (<see cref="Rolls.Roll"/>), so a test can fix one kind
/// (<see cref="BattleRandom.Force"/>) without counting how many rolls came before it.
/// </summary>
public enum RollKind
{
    Other,
    /// <summary>Whether a hit is critical, out of the stage's odds: 0 is a critical hit.</summary>
    Critical,
    /// <summary>The spread of damage in sixteen steps: 0 is the weakest hit (85%), 15 the strongest (100%).</summary>
    Damage,
    /// <summary>Out of 100: the move hits when the roll is below its chance.</summary>
    Accuracy,
    /// <summary>Out of 100: a move's side effect happens when the roll is below its chance.</summary>
    SideEffect,
    /// <summary>Out of 100: a paralysed Pokémon can't move when the roll is below 25.</summary>
    FullParalysis,
    /// <summary>Out of 100: a frozen Pokémon thaws when the roll is below 20.</summary>
    Thaw,
    /// <summary>Out of the rules' odds: a confused Pokémon hurts itself on the last face.</summary>
    ConfusionSelfHit,
    /// <summary>How long a sleep lasts, from the shortest (0) up.</summary>
    SleepTurns,
    /// <summary>How long confusion lasts, from the shortest (0) up.</summary>
    ConfusionTurns,
    /// <summary>Who goes first among Pokémon of equal Speed: the lower roll.</summary>
    SpeedTie,
    /// <summary>Out of 100: a Quick Claw works when the roll is below 20.</summary>
    QuickClaw,
    /// <summary>Which foe a move that picks its own target goes for.</summary>
    Target,
    /// <summary>Out of 65,536: the ball holds through a shake when the roll is below the shake rate.</summary>
    CatchShake,
    /// <summary>Out of 100: an ability's chance (Static, Shed Skin).</summary>
    AbilityChance,
    /// <summary>Which of several things an ability does (Effect Spore's three conditions).</summary>
    AbilityPick,
    /// <summary>Out of 100: a held item's chance (King's Rock, Focus Band).</summary>
    ItemChance,
    /// <summary>The spread a trainer's choice of move is given.</summary>
    AiChoice
}

/// <summary>
/// A battle's own random numbers: the generator Platinum's battles use (<c>BattleSystem_RandNext</c>: a linear
/// congruential step, of which the upper sixteen bits are the number), so a seed and a list of choices replay a
/// battle the same on every machine and every version of .NET, which <see cref="Random"/> does not promise. Its
/// whole state is one number (<see cref="State"/>), which is what a saved or resumed battle keeps.
/// <para>
/// It is a <see cref="Random"/>, so everything that takes one can be handed it (<see cref="BattleSetup.Random"/>).
/// The game's own battles still roll with <see cref="Core.Dice"/> until plan 06 · R2 moves them onto the new core.
/// </para>
/// </summary>
public sealed class BattleRandom : Random
{
    private const uint Multiplier = 1103515245, Increment = 24691;

    private readonly Dictionary<RollKind, Queue<int>> forced = new();
    private uint state;

    public BattleRandom(uint seed)
    {
        Seed = seed;
        state = seed;
    }

    /// <summary>The number the battle began from.</summary>
    public uint Seed { get; }

    /// <summary>Where the generator stands now; <see cref="Resume"/> carries on from it.</summary>
    public uint State => state;

    /// <summary>How many numbers have been drawn, forced rolls not counted.</summary>
    public long Drawn { get; private set; }

    /// <summary>A generator that carries on where another stood (a battle read back from a save or resynchronised).</summary>
    public static BattleRandom Resume(uint seed, uint state, long drawn = 0) => new(seed) { state = state, Drawn = drawn };

    /// <summary>The next number, 0 to 65,535: Platinum's own step.</summary>
    public int Next16()
    {
        state = unchecked(state * Multiplier + Increment);
        Drawn++;
        return (int)(state >> 16);
    }

    /// <summary>
    /// A roll of a kind: 0 to <paramref name="sides"/> − 1. As in Platinum it is the next number's remainder, so
    /// the faces of a die that doesn't divide 65,536 are a hair's breadth from even, exactly as the original's.
    /// </summary>
    public int Roll(RollKind kind, int sides)
    {
        if (sides <= 0) throw new ArgumentOutOfRangeException(nameof(sides));
        if (forced.TryGetValue(kind, out var values))
        {
            // The last value stays for every roll after it
            int value = values.Count > 1 ? values.Dequeue() : values.Peek();
            return Math.Clamp(value, 0, sides - 1);
        }
        return Draw(sides);
    }

    /// <summary>
    /// Fixes the rolls of a kind: they come out as these values in turn, and the last one for every roll after
    /// it. A value past the die's last face counts as its last face. For tests: "no critical hits and the
    /// strongest hit" is <c>Force(RollKind.Critical, 1).Force(RollKind.Damage, 15)</c>.
    /// </summary>
    public BattleRandom Force(RollKind kind, params int[] values)
    {
        if (values.Length == 0) throw new ArgumentException("Give at least one value", nameof(values));
        forced[kind] = new Queue<int>(values);
        return this;
    }

    /// <summary>Lets a kind of roll be chance again.</summary>
    public BattleRandom Free(RollKind kind)
    {
        forced.Remove(kind);
        return this;
    }

    private int Draw(int sides) => sides <= 65536 ? Next16() % sides : (int)(Next31() % (uint)sides);

    private uint Next31() => (((uint)Next16() << 16) | (uint)Next16()) & 0x7FFFFFFF;

    // ------------------------------------------------------------------ as a System.Random

    public override int Next() => (int)(Next31() % int.MaxValue);

    public override int Next(int maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
        return maxValue <= 1 ? 0 : Draw(maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        long range = (long)maxValue - minValue;
        if (range <= 1) return minValue;
        return range <= int.MaxValue ? minValue + Draw((int)range) : (int)(minValue + NextInt64(range));
    }

    public override long NextInt64() => (long)(((ulong)Next31() << 32) | ((ulong)Next31() << 1) | (uint)(Next16() & 1));

    public override long NextInt64(long maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
        return maxValue <= 1 ? 0 : (long)((ulong)NextInt64() % (ulong)maxValue);
    }

    public override long NextInt64(long minValue, long maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        ulong range = (ulong)(maxValue - minValue);
        return range <= 1 ? minValue : (long)((ulong)minValue + (ulong)NextInt64() % range);
    }

    public override double NextDouble() => Sample();

    public override float NextSingle() => Next16() / 65536f;

    protected override double Sample() => ((ulong)Next16() << 16 | (uint)Next16()) / 4294967296.0;

    public override void NextBytes(byte[] buffer) => NextBytes(buffer.AsSpan());

    public override void NextBytes(Span<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(Next16() >> 8);
    }
}

/// <summary>
/// Rolls by kind on any generator. A <see cref="BattleRandom"/> can have them forced; any other
/// <see cref="Random"/> draws exactly what it drew before the rolls had names, so a battle seeded through
/// <see cref="Core.Dice"/> plays out as it always did.
/// </summary>
public static class Rolls
{
    /// <summary>0 to <paramref name="sides"/> − 1.</summary>
    public static int Roll(this Random rng, RollKind kind, int sides) =>
        rng is BattleRandom battle ? battle.Roll(kind, sides) : rng.Next(sides);

    /// <summary>Any number at all, for putting things in an order that chance decides.</summary>
    public static int RollAny(this Random rng, RollKind kind) =>
        rng is BattleRandom battle ? battle.Roll(kind, int.MaxValue) : rng.Next();

    /// <summary>A fraction from 0 up to, never reaching, 1.</summary>
    public static double RollFraction(this Random rng, RollKind kind) =>
        rng is BattleRandom battle ? battle.Roll(kind, 65536) / 65536.0 : rng.NextDouble();
}
