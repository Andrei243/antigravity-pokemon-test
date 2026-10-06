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
    /// <summary>What a hit loses to chance, in sixteen steps: 0 is the strongest hit (all of it), 15 the weakest (85%).</summary>
    Damage,
    /// <summary>Out of 100: the move hits when the roll is below its chance.</summary>
    Accuracy,
    /// <summary>Out of 100: a move's side effect happens when the roll is below its chance.</summary>
    SideEffect,
    /// <summary>Out of 4: a paralysed Pokémon can't move on a 0.</summary>
    FullParalysis,
    /// <summary>Out of 5: a frozen Pokémon thaws on a 0.</summary>
    Thaw,
    /// <summary>Out of the rules' odds: a confused Pokémon hurts itself on a 0.</summary>
    ConfusionSelfHit,
    /// <summary>How long a sleep lasts, from the shortest (0) up.</summary>
    SleepTurns,
    /// <summary>How long confusion lasts, from the shortest (0) up.</summary>
    ConfusionTurns,
    /// <summary>The number each place on the field draws at the start of a turn, out of 65,536: a Quick Claw works on one in five of them.</summary>
    Speed,
    /// <summary>The coin between two Pokémon of equal Speed: on a 1 the second goes first.</summary>
    SpeedTie,
    /// <summary>Out of 256: a Pokémon slower than the foe gets away when the roll is below its escape number.</summary>
    Escape,
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
    /// <summary>Out of 2: a Pokémon in love can't bring itself to move on a 0.</summary>
    Infatuation,
    /// <summary>Out of 65,536: Protect, Detect and Endure work when the number is no greater than the rules' rate.</summary>
    Protect,
    /// <summary>How long something lasts that the rules give a range for (a taunt, an encore, a binding), from the shortest (0) up.</summary>
    Duration,
    /// <summary>Which of the party a forced switch drags out.</summary>
    DraggedOut,
    /// <summary>Out of 256: Roar and Whirlwind against a foe of a higher level.</summary>
    Whirlwind,
    /// <summary>The spread a trainer's choice of move is given.</summary>
    AiChoice,
    /// <summary>How many times a move of two to five hits does (two draws of 4 in Platinum: the first picks 2 or 3 below 2, the second any; one of 100 by the modern rules).</summary>
    HitCount,
    /// <summary>A power or an amount of damage that chance decides: Psywave (of 11), Magnitude (of 100), Present (of 256).</summary>
    Power,
    /// <summary>Which of several things a move picks: Tri Attack's condition (a burn, a freeze, paralysis), Acupressure's stat.</summary>
    Pick,
    /// <summary>After a battle won: out of 10, a Pokémon with Pickup finds something on a 0, then out of 100 which; out of 100, Honey Gather finds honey below its level's chance.</summary>
    Pickup,
    /// <summary>The Great Marsh: out of 10 for bait and mud (0 is the strong reaction), out of 255 against the Pokémon's flee rate.</summary>
    Safari,
    /// <summary>
    /// A Pokémon of someone else's, over the level the badges command (plan 06 · R10): out of 256 against its level
    /// and the cap, out of 4 for the move it uses instead and for what it says when it does nothing.
    /// </summary>
    Obedience
}

/// <summary>
/// A battle's own random numbers: the generator Platinum's battles use (<c>BattleSystem_RandNext</c>: a linear
/// congruential step, of which the upper sixteen bits are the number), so a seed and a list of choices replay a
/// battle the same on every machine and every version of .NET, which <see cref="Random"/> does not promise. Its
/// whole state is one number (<see cref="State"/>), which is what a saved or resumed battle keeps.
/// <para>
/// It is a <see cref="Random"/>, so everything that takes one can be handed it (<see cref="BattleSetup.Random"/>).
/// A battle that is given no generator makes one of these, seeded from <see cref="Core.Dice"/>.
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
    /// strongest hit" is <c>Force(RollKind.Critical, 1).Force(RollKind.Damage, 0)</c>.
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
/// Rolls by kind on any generator, so a battle can be handed a plain <see cref="Random"/> too. Only a
/// <see cref="BattleRandom"/> can have a kind of roll fixed, and only it replays the same everywhere.
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
