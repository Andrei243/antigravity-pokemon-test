using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>
/// What a later battle place keeps of one of its ways (plan 37 · FC2; <c>docs/mechanics/rulings.md</c>, "The later
/// games' battle places"): a room and a format, normal or hard, with the battles won in a row, the most ever, the last
/// streak that ended, whether the hard room beside it is open, whether a challenge is under way (one walked out on ends
/// the streak the next time one is begun) and a challenge put down to be taken up again. Kept in
/// <see cref="Frontier.FrontierRecords.Facilities"/> by <see cref="KeyOf"/>. No drawing or input.
/// </summary>
public sealed class FacilityRecord
{
    /// <summary>The battles won in a row.</summary>
    public int Streak { get; set; }

    /// <summary>The longest run of battles won in a row.</summary>
    public int Best { get; set; }

    /// <summary>The streak the last challenge ended on (the board's "latest").</summary>
    public int Latest { get; set; }

    /// <summary>The hard room beside this one is open (its host beaten here); kept on the normal way's record.</summary>
    public bool HardOpen { get; set; }

    /// <summary>A challenge has been begun and not finished nor put down: one walked out on ends the streak.</summary>
    public bool InProgress { get; set; }

    /// <summary>The challenge put down between battles, to be taken up again; null when none is.</summary>
    public FacilityPause? Paused { get; set; }

    /// <summary>A place's way as its record is kept: <c>house.single</c>, and <c>house.single.hard</c> for the hard room.</summary>
    public static string KeyOf(string place, string way, bool hard) => $"{place}.{way}{(hard ? ".hard" : "")}";
}

/// <summary>
/// A challenge put down between battles (the Tower's <c>TowerPause</c>, for the later places): the room, the places in
/// the team of those entered with their ids (so a team changed meanwhile is not taken for the one put down), and the
/// challenge's own generator as it stood before the next trainer was drawn, so the same trainer comes when it is taken up.
/// </summary>
public sealed class FacilityPause
{
    public List<int> Slots { get; set; } = new();

    /// <summary>The ids of the Pokémon entered (<see cref="Pokemon.Id"/>), in the order of <see cref="Slots"/>.</summary>
    public List<Guid> Ids { get; set; } = new();

    /// <summary>Their species, in the same order (what a Pokémon given no id yet is told by).</summary>
    public List<string> Species { get; set; } = new();

    /// <summary>The generator's seed, its state before the next trainer's draw, and how many draws it had made.</summary>
    public uint Seed { get; set; }
    public uint State { get; set; }
    public long Drawn { get; set; }
}
