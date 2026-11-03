using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>One player of a link battle as they come to the room: their name and the team they bring, as plain data (what a client sends).</summary>
public sealed record LinkPlayer(string Name, IReadOnlyList<RecordedPokemon> Team);

/// <summary>
/// A link battle's setup (plan 28 · I4): the teams brought made into copies at the room's level (plan 14 · B11's
/// <see cref="LevelScaling"/>, so nothing a battle does reaches anyone's save), the clauses checked as they are brought,
/// the preview each side is shown, and once both have picked their order the <see cref="CoreSetup"/> the server fights:
/// side A the core's player side, side B its other side as a trainer of the player's own name, both answered from
/// outside (no controller), under the Battle Frontier's rules (no EXP, no money, no bag, every Pokémon obeys) and the
/// link's (<see cref="CoreSetup.Link"/>: given up, never run from), by the room's rules. GPU-free.
/// </summary>
public sealed class LinkSetup
{
    /// <summary>The class a player stands as across the field, as Platinum's link battles name them.</summary>
    public const string PlayerClass = "Pokémon Trainer";

    public LinkRoom Room { get; }
    public Ruleset Rules { get; }

    /// <summary>Each side's six as copies at the room's level, in the order brought: the player side's first, then the other's.</summary>
    public IReadOnlyList<Pokemon> TeamA { get; }
    public IReadOnlyList<Pokemon> TeamB { get; }
    public LinkPlayer A { get; }
    public LinkPlayer B { get; }

    private LinkSetup(LinkRoom room, LinkPlayer a, LinkPlayer b)
    {
        Room = room;
        Rules = Ruleset.Of(room.Rules);
        A = a;
        B = b;
        TeamA = Copies(room, a.Team);
        TeamB = Copies(room, b.Team);
    }

    /// <summary>The room's teams, or why one of them can't be brought (a line to each fault, the side named).</summary>
    public static LinkSetup Bring(LinkRoom room, LinkPlayer a, LinkPlayer b)
    {
        var faults = Check(room, a).Select(f => $"{a.Name}: {f}").Concat(Check(room, b).Select(f => $"{b.Name}: {f}")).ToList();
        if (faults.Count > 0) throw new LinkRefusedException(string.Join("\n", faults));
        return new LinkSetup(room, a, b);
    }

    /// <summary>
    /// What a room refuses of a team, as the original's link rooms do: none or more than six, an Egg, a species the
    /// room's rules don't have (Platinum's are the first 493), two of one species (the species clause) and two holding
    /// one item (the item clause); fewer than the room picks.
    /// </summary>
    public static List<string> Check(LinkRoom room, LinkPlayer player)
    {
        var faults = new List<string>();
        var team = player.Team;
        if (team.Count == 0 || team.Count > Party.MaxSize) faults.Add(Text.Of("linkRoom.fault.teamSize", ("count", team.Count)));
        if (team.Count < Math.Min(room.Picks, Party.MaxSize)) faults.Add(Text.Of("linkRoom.fault.tooFew", ("count", room.Picks)));
        var species = new List<int>();
        var items = new List<string>();
        foreach (var recorded in team)
        {
            var saved = recorded.Pokemon;
            if (saved.IsEgg) faults.Add(Text.Of("linkRoom.fault.egg"));
            var kind = PokemonDatabase.Get(saved.SpeciesName);
            if (kind == null) faults.Add(Text.Of("linkRoom.fault.unknown", ("species", saved.SpeciesName)));
            else
            {
                if (kind.IsOurs || room.Rules == RulesPreset.Platinum && kind.DexNumber > 493)
                    faults.Add(Text.Of("linkRoom.fault.notInRules", ("species", kind.DisplayName)));
                species.Add(kind.DexNumber);
            }
            if (!string.IsNullOrEmpty(saved.HeldItem)) items.Add(saved.HeldItem);
        }
        if (room.SpeciesClause && species.GroupBy(n => n).Any(g => g.Count() > 1)) faults.Add(Text.Of("linkRoom.fault.speciesClause"));
        if (room.ItemClause && items.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) faults.Add(Text.Of("linkRoom.fault.itemClause"));
        return faults;
    }

    /// <summary>
    /// A team brought into a room: copies by the room's rules at its level, whole (<see cref="LevelScaling"/>), in the
    /// order brought. The server and each player make them alike from the same plain data.
    /// </summary>
    public static List<Pokemon> Copies(LinkRoom room, IReadOnlyList<RecordedPokemon> team)
    {
        var rules = Ruleset.Of(room.Rules);
        return team.Select(r =>
        {
            var p = r.ToPokemon(rules);
            return LevelScaling.At(p, room.Level switch { LinkLevel.FlatFifty => 50, LinkLevel.FlatHundred => 100, _ => p.Level });
        }).ToList();
    }

    /// <summary>What side A's or side B's foe is shown before the battle: the other's six as the preview has them.</summary>
    public List<PreviewEntry> PreviewFor(BattleSide side) => TeamPreview.Of(side == BattleSide.Player ? TeamB : TeamA).Preview;

    /// <summary>Why an order picked can't be taken; null when it can: as many as the room picks, of the six, none twice.</summary>
    public string? WhyNot(IReadOnlyList<int> order, BattleSide side)
    {
        int count = (side == BattleSide.Player ? TeamA : TeamB).Count;
        int wanted = Math.Min(Room.Picks, count);
        if (order.Count != wanted) return Text.Of("linkRoom.fault.pickCount", ("count", wanted));
        if (order.Any(i => i < 0 || i >= count) || order.Distinct().Count() != order.Count) return Text.Of("linkRoom.fault.pickEach");
        return null;
    }

    /// <summary>
    /// The battle both picks make, on fresh copies (a battle changes the Pokémon it is handed), fought from
    /// <paramref name="seed"/>. The same setup, picks and seed make the same battle.
    /// </summary>
    public CoreSetup Battle(IReadOnlyList<int> orderA, IReadOnlyList<int> orderB, uint seed)
    {
        if (WhyNot(orderA, BattleSide.Player) is { } a) throw new LinkRefusedException($"{A.Name}: {a}");
        if (WhyNot(orderB, BattleSide.Enemy) is { } b) throw new LinkRefusedException($"{B.Name}: {b}");
        var mine = new Party();
        foreach (int i in orderA) mine.Add(TeamA[i].Clone());
        var theirs = new Trainer { Id = "link_" + B.Name, Name = B.Name, TrainerClass = PlayerClass, DoubleBattle = Room.Format == BattleFormat.Double };
        foreach (int i in orderB) theirs.Party.Add(TeamB[i].Clone());
        return new CoreSetup
        {
            PlayerParty = mine,
            Trainers = new List<Trainer> { theirs },
            Format = Room.Format,
            Random = new BattleRandom(seed),
            Rules = Rules,
            PlayerName = A.Name,
            PlayerController = null,
            EnemyController = null,
            Frontier = true,
            Link = true
        };
    }

    /// <summary>What a battle's end comes to as a link's result.</summary>
    public static LinkResult ResultOf(BattleCore core, LinkEnding how) => new(core.Result switch
    {
        BattleResult.PlayerVictory => BattleSide.Player,
        BattleResult.PlayerDefeat => BattleSide.Enemy,
        _ => null
    }, core.Turn, how);
}

/// <summary>A room refuses a team or a pick, with its reasons.</summary>
public sealed class LinkRefusedException : Exception
{
    public LinkRefusedException(string message) : base(message) { }
}
