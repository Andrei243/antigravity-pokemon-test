using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One Pokémon of a Remix team (plan 13 · V15): written whole, nothing left to chance. Its IVs and effort are six
/// numbers each in the stats' order (HP, Attack, Defense, Sp. Atk, Sp. Def, Speed); IVs left out are
/// <see cref="Remix.DefaultIv"/> each, effort left out is none.
/// </summary>
public sealed class RemixPokemon
{
    public string Species { get; set; } = "";
    public string? Form { get; set; }
    public int Level { get; set; }
    public List<string> Moves { get; set; } = new();
    public string? Item { get; set; }
    public string? Ability { get; set; }
    public Nature Nature { get; set; } = Nature.Hardy;
    public Gender? Gender { get; set; }
    public int[]? Ivs { get; set; }
    public int[]? Evs { get; set; }
}

/// <summary>
/// A trainer's team under Remix (plan 13 · V15), by the trainer's id in <c>trainers.json</c>: up to six
/// <see cref="RemixPokemon"/>, the items the trainer uses in battle and how it thinks (null for the table's own), and a
/// note of what the team is built round, for whoever reads the file.
/// </summary>
public sealed class RemixTeam
{
    public string Id { get; set; } = "";

    /// <summary>The session that wrote it (V15 to V19), which the coverage table goes by.</summary>
    public string Batch { get; set; } = "";
    public string? Note { get; set; }
    public List<string>? Ai { get; set; }
    public List<string>? Items { get; set; }
    public List<RemixPokemon> Party { get; set; } = new();
}

/// <summary><c>Data/remix/&lt;region&gt;/trainers.json</c>: the teams in the file's order, which is what each team's Pokémon are seeded by.</summary>
public sealed class RemixTrainersFile
{
    public List<RemixTeam> Trainers { get; set; } = new();
}

/// <summary>An area's tables under Remix, in the area files' own shape and slots (<see cref="WorldAreaFile"/>).</summary>
public sealed class RemixArea
{
    /// <summary>The session that wrote it (V15 to V19), which the coverage table goes by.</summary>
    public string Batch { get; set; } = "";

    public List<AreaEncounter>? Land { get; set; }
    public List<AreaEncounter>? Water { get; set; }
    public List<AreaEncounter>? OldRod { get; set; }
    public List<AreaEncounter>? GoodRod { get; set; }
    public List<AreaEncounter>? SuperRod { get; set; }
}

/// <summary><c>Data/remix/&lt;region&gt;/wild.json</c>: the areas' tables by area key, and the batch each area was written in.</summary>
public sealed class RemixWildFile
{
    public Dictionary<string, RemixArea> Areas { get; set; } = new();
}

/// <summary>
/// Remix (plan 13 · V15 to V19; <c>docs/mechanics/rulings.md</c>, "Remix"): a second take on Sinnoh's trainers and wild
/// Pokémon, written by us and chosen at NEW GAME (<see cref="RunRules.Remix"/>). GPU-free. Its files are apart from the
/// game's (<c>Data/remix/sinnoh/</c>) and read only in a Remix run: <see cref="For"/> is null under any other, and nothing
/// of Platinum's data is patched. A trainer is changed on a copy (<see cref="ForTrainer"/>, before the randomiser and the
/// difficulty, which lay themselves on what Remix wrote), and the wild tables are laid over the maps' own through
/// <see cref="Map.Tables"/>, the one place they are read.
/// </summary>
public sealed class Remix : IWildTables
{
    /// <summary>The folder of a region's Remix files under the data folder.</summary>
    public const string Folder = "remix";

    public const string TrainersFile = "trainers.json";
    public const string WildFile = "wild.json";

    /// <summary>What each IV of a Remix Pokémon is when its file names none.</summary>
    public const int DefaultIv = 20;

    private static readonly Lazy<Remix> sinnoh = new(() => Load("sinnoh"));

    /// <summary>Sinnoh's Remix, read the first time it is asked for.</summary>
    public static Remix Sinnoh => sinnoh.Value;

    /// <summary>The Remix a run plays with: Sinnoh's in a Remix run, null in any other (which changes nothing).</summary>
    public static Remix? For(RunRules? run) => run is { Remix: true } ? Sinnoh : null;

    /// <summary>A trainer as a run meets them: Remix's copy where the run has Remix and the file has a team for them, the very same trainer otherwise.</summary>
    public static Trainer Prepare(Trainer trainer, RunRules? run) => For(run)?.ForTrainer(trainer) ?? trainer;

    public RemixTrainersFile Trainers { get; }
    public RemixWildFile Wild { get; }

    private readonly Dictionary<string, (int Index, RemixTeam Team)> teams = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Tables> areas = new(StringComparer.OrdinalIgnoreCase);

    public Remix(RemixTrainersFile trainers, RemixWildFile wild)
    {
        Trainers = trainers;
        Wild = wild;
        for (int i = 0; i < trainers.Trainers.Count; i++) teams.TryAdd(trainers.Trainers[i].Id, (i, trainers.Trainers[i]));
        foreach (var (key, area) in wild.Areas) areas[key] = new Tables(area);
    }

    /// <summary>A region's Remix from its folder through <see cref="GameDataFiles"/>.</summary>
    public static Remix Load(string region) => new(
        GameDataFiles.Load<RemixTrainersFile>($"{Folder}/{region}/{TrainersFile}"),
        GameDataFiles.Load<RemixWildFile>($"{Folder}/{region}/{WildFile}"));

    /// <summary>The team written for a trainer id; null for a trainer Remix leaves as the table has them.</summary>
    public RemixTeam? TeamOf(string id) => teams.TryGetValue(id, out var t) ? t.Team : null;

    /// <summary>Every team's id, in the file's order.</summary>
    public IEnumerable<string> TeamIds => Trainers.Trainers.Select(t => t.Id);

    /// <summary>
    /// The trainer with Remix's team: a copy (the trainer of the map keeps the table's), its team built from the file,
    /// its AI's flags and battle items the file's where it names them and the table's where it doesn't. The trainer
    /// itself where the file has no team for them.
    /// </summary>
    public Trainer ForTrainer(Trainer trainer)
    {
        if (!teams.TryGetValue(trainer.Id, out var found)) return trainer;
        var copy = trainer.Copied();
        copy.Remixed = true;
        copy.Party.Clear();
        foreach (var p in BuildTeam(found.Team, found.Index)) copy.Party.Add(p);
        if (found.Team.Ai != null) copy.Ai = AiFlagNames.Parse(found.Team.Ai);
        if (found.Team.Items != null) copy.Items = found.Team.Items.ToList();
        return copy;
    }

    /// <summary>A team's Pokémon, each the same every time it is built.</summary>
    public IReadOnlyList<Pokemon> BuildTeam(RemixTeam team) =>
        BuildTeam(team, teams.TryGetValue(team.Id, out var t) ? t.Index : 0);

    private static List<Pokemon> BuildTeam(RemixTeam team, int index) =>
        team.Party.Select((m, place) => Build(m, index * Party.MaxSize + place)).ToList();

    /// <summary>
    /// A Remix Pokémon as <see cref="TrainerDatabase.Build"/> makes the table's: never shiny, its nature, ability,
    /// item, moves, IVs and effort the file's, its personality (and so its gender where the file gives none) drawn
    /// from a generator seeded by its <paramref name="place"/> in the file, never by a string's hash.
    /// </summary>
    public static Pokemon Build(RemixPokemon m, int place)
    {
        var species = PokemonDatabase.Get(m.Species) ?? throw new InvalidOperationException($"No species called {m.Species}");
        var rng = new Random(0x5E3A + place);
        var p = m.Form != null ? Pokemon.InForm(species, m.Form, m.Level, rng) : new Pokemon(species, m.Level, rng, m.Gender, m.Nature);
        p.IsShiny = false;
        p.Nature = m.Nature;
        if (m.Gender is { } gender) p.Gender = gender;
        var ivs = m.Ivs ?? Enumerable.Repeat(DefaultIv, 6).ToArray();
        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed) = (ivs[0], ivs[1], ivs[2], ivs[3], ivs[4], ivs[5]);
        var evs = m.Evs ?? new int[6];
        (p.EvHP, p.EvAttack, p.EvDefense, p.EvSpAttack, p.EvSpDefense, p.EvSpeed) = (evs[0], evs[1], evs[2], evs[3], evs[4], evs[5]);
        if (m.Ability != null) p.AbilityName = m.Ability;
        p.HeldItem = m.Item == null ? null : ItemDatabase.Get(m.Item);
        if (m.Moves.Count > 0)
        {
            p.Moves.Clear();
            foreach (string move in m.Moves) p.Moves.Add(MoveDatabase.Create(move));
        }
        p.RecalculateStats();
        p.CurrentHP = p.MaxHP;
        return p;
    }

    // ------------------------------------------------------------------ the wild tables

    /// <summary>Whether Remix writes an area's tables (every table of it, an empty one where the file leaves one out).</summary>
    public bool HasArea(string key) => areas.ContainsKey(key);

    public IReadOnlyList<WildEncounterEntry>? Land(string area) => areas.TryGetValue(area, out var t) ? t.Land : null;
    public IReadOnlyList<WildEncounterEntry>? Water(string area) => areas.TryGetValue(area, out var t) ? t.Water : null;
    public IReadOnlyList<WildEncounterEntry>? Rod(string area, FishingRod rod) => areas.TryGetValue(area, out var t) ? t.Rods[(int)rod] : null;

    /// <summary>Every species a Remix table has anywhere, by area.</summary>
    public IEnumerable<(string Area, string Species)> SpeciesMet() =>
        Wild.Areas.SelectMany(a => new[] { a.Value.Land, a.Value.Water, a.Value.OldRod, a.Value.GoodRod, a.Value.SuperRod }
            .Where(t => t != null).SelectMany(t => t!).Select(e => (a.Key, e.Species)));

    /// <summary>An area's tables as rows the field meets, each slot weighted by its place as the area files' are.</summary>
    private sealed class Tables
    {
        public readonly IReadOnlyList<WildEncounterEntry> Land, Water;
        public readonly IReadOnlyList<WildEncounterEntry>[] Rods;

        public Tables(RemixArea area)
        {
            Land = Rows(area.Land, WorldAreaFile.LandSlotWeights);
            Water = Rows(area.Water, WorldAreaFile.WaterSlotWeights);
            Rods = new[]
            {
                Rows(area.OldRod, WorldAreaFile.WaterSlotWeights),
                Rows(area.GoodRod, WorldAreaFile.RodSlotWeights),
                Rows(area.SuperRod, WorldAreaFile.RodSlotWeights)
            };
        }

        private static IReadOnlyList<WildEncounterEntry> Rows(List<AreaEncounter>? slots, int[] weights) =>
            (slots ?? new()).Select((wild, slot) => new WildEncounterEntry
            {
                SpeciesName = wild.Species,
                MinLevel = wild.Level,
                MaxLevel = wild.MaxLevel ?? wild.Level,
                Weight = wild.Weight ?? (slot < weights.Length ? weights[slot] : 1)
            }).ToList();
    }
}
