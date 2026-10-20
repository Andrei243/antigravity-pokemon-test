using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// How big one Pokémon is beside another of its species (plan 01 · M11, part c), by the original's
/// <c>overlay005/size_contest.c</c>: the fisherman of Route 222 measures the Remoraid he is shown and keeps the
/// biggest. A Pokémon's size is its species' height scaled by a factor worked out of its personality and its IVs,
/// which a table of sixteen intervals bends from an even spread into a bell: most are near the Pokédex's height, a
/// rare one is under a third of it or over half as much again. No drawing and no input.
/// </summary>
public static class SizeContest
{
    /// <summary>The story's variable that holds the record, as a factor (the original's <c>VAR_SIZE_CONTEST_RECORD</c>).</summary>
    public const string RecordVariable = "VAR_SIZE_CONTEST_RECORD";

    /// <summary>
    /// The record a game begins with (<c>ScrCmd_InitSizeContestRecord</c>, which a new game's script calls): a factor
    /// just past the middle, so a Remoraid of 60.1 cm.
    /// </summary>
    public const int FirstRecord = 33280;

    /// <summary>What a Pokémon shown comes to beside the record (<c>SIZE_CONTEST_SMALLER</c>, <c>_SAME_SIZE</c>, <c>_LARGER</c>).</summary>
    public enum Outcome
    {
        Smaller = 0,
        Same = 1,
        Larger = 2
    }

    // sUniformToNormalIntervals: the multiplier's base, what the factor past the interval's start is divided by, and
    // where the interval starts
    private static readonly (int Base, int Divisor, int Start)[] Intervals =
    {
        (290, 1, 0), (300, 1, 10), (400, 2, 110), (500, 4, 310), (600, 20, 710), (700, 50, 2710), (800, 100, 7710), (900, 150, 17710),
        (1000, 150, 32710), (1100, 100, 47710), (1200, 50, 57710), (1300, 20, 62710), (1400, 5, 64710), (1500, 2, 65210), (1600, 1, 65410), (1700, 1, 65510),
    };

    /// <summary>
    /// A Pokémon's size factor, 0 to 65,535 (<c>CalcSizeFactor</c>): the low half of its personality with two bytes
    /// made of the low four bits of its IVs worked into it.
    /// </summary>
    public static int Factor(Pokemon pokemon) => Factor(pokemon.Personality, pokemon.IvHP, pokemon.IvAttack, pokemon.IvDefense, pokemon.IvSpeed, pokemon.IvSpAttack, pokemon.IvSpDefense);

    public static int Factor(uint personality, int hp, int attack, int defense, int speed, int spAttack, int spDefense)
    {
        int low16 = (int)(personality & 0xFFFF);
        hp &= 0xF; attack &= 0xF; defense &= 0xF; speed &= 0xF; spAttack &= 0xF; spDefense &= 0xF;
        // Both halves are 16-bit in the original, and their sum is cut to 16 bits where it is kept (a vu16)
        int high = (((attack ^ defense) * hp) ^ (low16 & 0xFF)) & 0xFFFF;
        int low = (((spAttack ^ spDefense) * speed) ^ (low16 >> 8)) & 0xFFFF;
        return (high * 256 + low) & 0xFFFF;
    }

    /// <summary>
    /// The thousandths of its species' height a factor comes to, 290 to 1,725 (<c>CalcMultFromSizeFactor</c>). The
    /// search for the interval is the original's own, which never lands on the last but one (<c>GetApproxIntervalIdx</c>
    /// stops its loop one short and returns the last): a factor from 65,410 on is read on the last interval's line,
    /// which starts at 65,510, in unsigned arithmetic that wraps. Both lines climb one for one, so it comes to the same.
    /// </summary>
    public static long Multiplier(int factor)
    {
        factor &= 0xFFFF;
        int index = Intervals.Length - 1;
        for (int i = 1; i < Intervals.Length - 1; i++)
            if (factor < Intervals[i].Start) { index = i - 1; break; }
        var (multiplierBase, divisor, start) = Intervals[index];
        ulong past = unchecked((ulong)(factor - start));
        return unchecked((long)((ulong)multiplierBase + past / (ulong)divisor));
    }

    /// <summary>A size in millimetres (<c>CalcMillimeterSize</c>): the species' height in tenths of a metre, scaled, cut to 32 bits.</summary>
    public static long Millimetres(float heightInMetres, int factor)
    {
        long decimetres = (long)MathF.Round(heightInMetres * 10f);
        return unchecked((uint)(decimetres * Multiplier(factor) / 10));
    }

    public static long Millimetres(Pokemon pokemon) => Millimetres(pokemon.Height, Factor(pokemon));

    /// <summary>
    /// The variable a measurer keeps its record in (plan 19 · K9, part c): its own where it names one, as Kanto's
    /// three do (FireRed's <c>VAR_HERACROSS_SIZE_RECORD</c> and its kin), and the story's own where it names none.
    /// </summary>
    public static string VariableFor(string kept) => kept.Length > 0 ? kept : RecordVariable;

    /// <summary>
    /// The record as the story has it: the factor kept, or, for Route 222's fisherman, the one every game begins with
    /// (<c>ScrCmd_InitSizeContestRecord</c>). A measurer with a variable of its own begins with nothing (FireRed's
    /// <c>DEFAULT_MAX_SIZE</c>), so the first Pokémon shown beats a record of nought and the script says as much.
    /// </summary>
    public static int Record(StoryState story, string kept = "") =>
        story.Var(VariableFor(kept)) is var held && held != 0 ? held : kept.Length > 0 ? 0 : FirstRecord;

    /// <summary>
    /// A Pokémon beside the record, both as its own species (<c>SizeContest_CalcResultForPartyMon</c>). The original
    /// compares what it shows, and so does this: sizes here are told in centimetres to the millimetre, where the
    /// English original tells them in tenths of an inch (docs/mechanics/rulings.md).
    /// </summary>
    public static Outcome Compare(Pokemon pokemon, int recordFactor)
    {
        long size = Millimetres(pokemon), record = Millimetres(pokemon.Height, recordFactor);
        return size == record ? Outcome.Same : size > record ? Outcome.Larger : Outcome.Smaller;
    }

    /// <summary>A size as it is told: centimetres to one place.</summary>
    public static string Told(long millimetres) => $"{millimetres / 10}.{millimetres % 10} cm";
}
