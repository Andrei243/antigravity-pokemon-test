using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>A famous trainer of the partners' tree (<c>Data/tree-cast.json</c>, written by hand).</summary>
public sealed class TreeCastRecord
{
    /// <summary>The trainer of the tables whose team they bring (a rematch team where the region has one).</summary>
    [DataRequired] public string Trainer { get; set; } = "";

    /// <summary>The look they are drawn in (a row of <c>characters.json</c>).</summary>
    [DataRequired] public string Look { get; set; } = "";

    /// <summary>The region whose Hall of Fame lets them into the tree (its story flag is <c>&lt;Region&gt;HallOfFame</c>).</summary>
    [DataRequired] public string Region { get; set; } = "";

    /// <summary><c>champion</c>, <c>elite</c>, <c>leader</c> or <c>kahuna</c>: only a champion stands at a hard room's 50th battle.</summary>
    [DataRequired] public string Role { get; set; } = "";
}

/// <summary>The partners' tree's cast file.</summary>
public sealed class TreeCastFile
{
    [DataRequired] public List<TreeCastRecord> Cast { get; set; } = new();
}

/// <summary>
/// The partners' tree's famous trainers (plan 37 · FC3; <c>docs/mechanics/rulings.md</c>, "The partners' tree"): the
/// game's own Champions, Elite Four, Leaders and kahunas, each region's once its Hall of Fame is set (decision 3). One stands
/// in the way at every tenth battle of a hard room (the tree's Super rooms) that is not its host's, bringing three of their
/// own team at 50 (<see cref="Tournament.ThreeOf"/>); a hard room's 50th battle is a Champion of the cast drawn from the
/// regions finished, in place of the Tree Master's hard team. One beaten may be asked to partner the Multi room, bringing
/// two of their team clear of the player's species and items (<see cref="BattleSubway.MakeFriend"/>'s rule). Every draw is
/// on the challenge's own generator. No drawing or input.
/// </summary>
public static class PartnersTree
{
    public const string FileName = "tree-cast.json";

    /// <summary>The battles of a hard room at which a famous trainer stands in the way: every tenth.</summary>
    public const int FamousEvery = 10;

    /// <summary>How many a famous partner brings into the Multi room.</summary>
    public const int PartnerBrings = 2;

    private static readonly Lazy<TreeCastFile> file = new(() => GameDataFiles.Load<TreeCastFile>(FileName, strict: true));

    /// <summary>The whole cast, in the file's order (a member's number is their place here from 1, which the scripts' lines go by).</summary>
    public static IReadOnlyList<TreeCastRecord> Cast => file.Value.Cast;

    /// <summary>A member's number from 1 by their trainer id, or 0 for none.</summary>
    public static int NumberOf(string? trainer)
    {
        if (string.IsNullOrEmpty(trainer)) return 0;
        for (int n = 0; n < Cast.Count; n++)
            if (Cast[n].Trainer == trainer) return n + 1;
        return 0;
    }

    /// <summary>The story flag that lets a region's cast in.</summary>
    public static string HallOfFameFlag(string region) => region + "HallOfFame";

    /// <summary>The cast whose region's Hall of Fame is set, in the file's order.</summary>
    public static List<TreeCastRecord> Eligible(Func<string, bool> has) => Cast.Where(c => has(HallOfFameFlag(c.Region))).ToList();

    /// <summary>Whether a battle of a hard room (counted from 1) brings a famous trainer: every tenth that isn't the host's.</summary>
    public static bool FamousComes(int battle, int hostAt) => battle > 0 && battle % FamousEvery == 0 && battle != hostAt;

    /// <summary>One of <paramref name="from"/> drawn on the caller's sixteen-bit draw (one draw), or null when there are none.</summary>
    public static TreeCastRecord? Draw(IReadOnlyList<TreeCastRecord> from, Func<int> next16) =>
        from.Count == 0 ? null : from[(next16() & 0xFFFF) % from.Count];

    /// <summary>A member's trainer record (the tables of every region), or null where it is gone.</summary>
    public static TrainerRecord? RecordOf(TreeCastRecord member) => TrainerDatabase.Get(member.Trainer);

    /// <summary>A famous trainer standing in the way: three of their own team at 50, dressed in their own look.</summary>
    public static FrontierOpponent Foe(TreeCastRecord member)
    {
        var record = RecordOf(member) ?? throw new InvalidOperationException($"The tree's cast names no trainer {member.Trainer}");
        return new FrontierOpponent
        {
            Number = 0, Pokemon = Tournament.ThreeOf(record),
            Dress = new FrontierDress(record.Id, record.Class, record.Name, member.Look)
        };
    }

    /// <summary>
    /// A famous trainer beside the player in the Multi room: two of their own team at 50, the highest levels first (the later
    /// among equals), passing over any of the species or the held items of those <paramref name="entered"/>, and of each other.
    /// </summary>
    public static FrontierOpponent Partner(TreeCastRecord member, IReadOnlyCollection<string> enteredSpecies, IReadOnlyCollection<string> enteredItems)
    {
        var record = RecordOf(member) ?? throw new InvalidOperationException($"The tree's cast names no trainer {member.Trainer}");
        var team = TrainerDatabase.PartyOf(record).Members.ToList();
        var species = enteredSpecies.ToHashSet(StringComparer.Ordinal);
        var items = enteredItems.ToHashSet(StringComparer.Ordinal);
        var picked = new List<(Pokemon p, int i)>();
        foreach (var x in team.Select((p, i) => (p, i)).OrderByDescending(x => x.p.Level).ThenByDescending(x => x.i))
        {
            if (species.Contains(x.p.Species.Name) || x.p.HeldItem != null && items.Contains(x.p.HeldItem.Name)) continue;
            picked.Add(x);
            species.Add(x.p.Species.Name);
            if (x.p.HeldItem != null) items.Add(x.p.HeldItem.Name);
            if (picked.Count == PartnerBrings) break;
        }
        return new FrontierOpponent
        {
            Number = 0, Pokemon = picked.OrderBy(x => x.i).Select(x => LevelScaling.At(x.p, FrontierRules.Level)).ToList(),
            Dress = new FrontierDress(record.Id, record.Class, record.Name, member.Look)
        };
    }
}
