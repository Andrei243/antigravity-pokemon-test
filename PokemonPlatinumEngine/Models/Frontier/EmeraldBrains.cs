using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// The Brains of Emerald's three facilities that had none here (plan 21, the leftovers of 2026-11-12;
/// <c>docs/mechanics/rulings.md</c>, "The Brains of the Palace, the Arena and the Dome"): Spenser the Palace Maven,
/// Greta the Arena Tycoon and Tucker the Dome Ace. When each comes is Emerald's (<c>sFrontierBrainStreakAppearances</c>
/// in <c>frontier_util.c</c>); their teams are ours (<c>Data/our-trainers.json</c>, on Emerald's species, as the Pike
/// Queen's and the Pyramid King's are). A Brain stands in a round as a dressed <see cref="FrontierOpponent"/>, so the
/// rooms and the battle take them as any trainer of the Frontier. No drawing or input.
/// </summary>
public static class EmeraldBrains
{
    public const string SpenserId = "palace_maven_spenser", SpenserGoldId = "palace_maven_spenser_gold";
    public const string GretaId = "arena_tycoon_greta", GretaGoldId = "arena_tycoon_greta_gold";
    public const string TuckerId = "dome_ace_tucker", TuckerGoldId = "dome_ace_tucker_gold";

    /// <summary>
    /// Which team a Brain brings (Emerald's <c>GetFrontierBrainStatus</c>): 1 at <paramref name="first"/>, 2 at
    /// <paramref name="second"/> and at every <paramref name="every"/> after it, 0 otherwise.
    /// </summary>
    public static int Due(int count, int first, int second, int every) =>
        count == first ? 1 : count == second || (count > second && (count - second) % every == 0) ? 2 : 0;

    /// <summary>The Palace Maven at the 21st battle won in a row and the 42nd and every 21st after (the battle's own number: Emerald's modifier of one).</summary>
    public static int PalaceDue(int battle) => Due(battle, 21, 42, 21);

    /// <summary>The Arena Tycoon at the 28th battle and the 56th and every 28th after.</summary>
    public static int ArenaDue(int battle) => Due(battle, 28, 56, 28);

    /// <summary>The Dome Ace in the tournament after four won in a row, and after nine and every fifth after (Emerald's modifier of nought: the tournaments won before).</summary>
    public static int DomeDue(int tournamentsWonBefore) => Due(tournamentsWonBefore, 4, 9, 5);

    /// <summary>The Brain of a venue for a round's last battle, or 0 when none is due.</summary>
    public static int VenueDue(AnnexVenue venue, int battle) => venue == AnnexVenue.Palace ? PalaceDue(battle) : ArenaDue(battle);

    /// <summary>A Brain's team of our data (silver or gold) as a trainer of the Frontier dressed as them, at level 50.</summary>
    public static FrontierOpponent Opponent(string id, string look)
    {
        var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException($"No Brain's team {id} in our-trainers.json.");
        return new FrontierOpponent
        {
            Number = 0,
            Pokemon = record.Party.Select(m => FrontierRules.ForBattle(TrainerDatabase.Build(m))).ToList(),
            Dress = new FrontierDress(record.Id, record.Class, record.Name, look)
        };
    }

    /// <summary>The venue's Brain with the team the battle calls for.</summary>
    public static FrontierOpponent OfVenue(AnnexVenue venue, int due) => venue == AnnexVenue.Palace
        ? Opponent(due == 2 ? SpenserGoldId : SpenserId, "spenser")
        : Opponent(due == 2 ? GretaGoldId : GretaId, "greta");

    /// <summary>The Dome Ace with the team the tournament calls for.</summary>
    public static FrontierOpponent OfDome(int due) => Opponent(due == 2 ? TuckerGoldId : TuckerId, "tucker");

    /// <summary>Whether a trainer of the Frontier is one of these Brains.</summary>
    public static bool IsBrain(FrontierOpponent? o) => o?.Dress?.Id is { } id &&
        (id.StartsWith("palace_maven_", StringComparison.Ordinal) || id.StartsWith("arena_tycoon_", StringComparison.Ordinal) || id.StartsWith("dome_ace_", StringComparison.Ordinal));
}
