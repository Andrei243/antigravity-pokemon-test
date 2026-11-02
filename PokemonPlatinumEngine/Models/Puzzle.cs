using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a puzzle asks: win within its turns, still stand after them, or catch the wild Pokémon within them.</summary>
public enum PuzzleGoal { Win, Survive, Catch }

/// <summary>One Pokémon of a puzzle: a built Pokémon (<see cref="BuiltPokemon"/>'s fields) with the HP and condition it starts at.</summary>
public sealed class PuzzlePokemon
{
    public string Species { get; set; } = "";
    public string? Form { get; set; }
    public int Level { get; set; } = 50;
    public Nature Nature { get; set; } = Nature.Hardy;
    public string? Ability { get; set; }
    public string? Item { get; set; }
    public List<string> Moves { get; set; } = new();

    /// <summary>The HP it starts with; null for all of it.</summary>
    public int? Hp { get; set; }

    /// <summary>The condition it starts with.</summary>
    public StatusCondition Status { get; set; } = StatusCondition.None;

    public BuiltPokemon Built => new() { Species = Species, Form = Form, Level = Level, Nature = Nature, Ability = Ability, Item = Item, Moves = Moves.ToList() };
}

/// <summary>A kind of roll a puzzle fixes, and the values it comes out as in turn (the last for every roll after it).</summary>
public sealed class PuzzleRoll
{
    public RollKind Kind { get; set; }
    public List<int> Values { get; set; } = new();
}

/// <summary>
/// One answer of a puzzle's solution or decoy, given to whatever the battle asks next: a move of the Pokémon in front
/// by its name, a switch to a party member by its place, or an item from the bag by its name.
/// </summary>
public sealed class PuzzleAnswer
{
    public string? Move { get; set; }
    public int? Switch { get; set; }
    public string? Item { get; set; }
}

/// <summary>
/// A battle puzzle (plan 13 · V26; <c>Data/puzzles/*.json</c>, <c>docs/data-files.md</c>, "Battle puzzles"): a position
/// with a goal, played on the real core. Both sides as built Pokémon with their HP and condition, the weather and Trick
/// Room, the rolls fixed by kind, the rules it is written for, the foe's moves turn by turn, the goal, and the solution
/// and a decoy (a plausible wrong answer). Its words are its title and its hint, keys of the table
/// (<c>puzzles.&lt;id&gt;.title</c>, <c>.hint</c>): the file itself carries no text that is shown.
/// </summary>
public sealed class PuzzleFile
{
    public string Id { get; set; } = "";

    /// <summary>The rules it is written for, by the preset's name: "Platinum" or "Modern".</summary>
    public string Rules { get; set; } = "Modern";

    public PuzzleGoal Goal { get; set; } = PuzzleGoal.Win;

    /// <summary>How many turns the goal is held to: won within them, survived through them, caught within them.</summary>
    public int Turns { get; set; } = 1;

    /// <summary>The battle's seed; whatever no fixed roll decides draws from it, the same every time.</summary>
    public uint Seed { get; set; } = 1;

    public List<PuzzlePokemon> Player { get; set; } = new();
    public List<PuzzlePokemon> Foe { get; set; } = new();

    /// <summary>The foe is a wild Pokémon (one), not a trainer.</summary>
    public bool Wild { get; set; }

    /// <summary>The foe trainer's name, class and look (a name of the data's, like a trainer's of <c>trainers.json</c>).</summary>
    public string FoeName { get; set; } = "Lumi";
    public string FoeClass { get; set; } = "Ace Trainer";
    public string FoeLook { get; set; } = "ace_trainer_f";

    /// <summary>What the foe's Pokémon in front uses, turn by turn, by its name (the last for every turn after); a move it can't use gives way to its first that it can.</summary>
    public List<string> FoeMoves { get; set; } = new();

    public BattleWeather Weather { get; set; }
    public bool TrickRoom { get; set; }

    /// <summary>Poké Balls in the bag, for a catch.</summary>
    public int Balls { get; set; }

    public List<PuzzleRoll> Rolls { get; set; } = new();
    public List<PuzzleAnswer> Solution { get; set; } = new();
    public List<PuzzleAnswer> Decoy { get; set; } = new();

    public Ruleset RulesSet => Rules.Equals("Platinum", StringComparison.OrdinalIgnoreCase) ? Ruleset.Platinum : Ruleset.Modern;

    public string TitleKey => $"puzzles.{Id}.title";
    public string HintKey => $"puzzles.{Id}.hint";
}

/// <summary>
/// The foe's mind in a puzzle: the move the file names for the turn, or its first usable move where that can't be
/// used, and the next Pokémon able to come in after a faint. Nothing drawn, so nothing moves the battle's rolls.
/// </summary>
public sealed class PuzzleFoe : IBattleController
{
    private readonly IReadOnlyList<string> moves;

    public PuzzleFoe(IReadOnlyList<string> moves) => this.moves = moves;

    public BattleChoice ChooseAction(BattleCore battle, Battler mine)
    {
        var usable = battle.UsableMoves(mine);
        if (usable.Count == 0) return BattleChoice.Fight(mine.Place, -1);
        var known = mine.Pokemon!.Moves;
        string? wanted = moves.Count == 0 ? null : moves[Math.Min(battle.Turn, moves.Count - 1)];
        var pick = usable.FirstOrDefault(m => m.Name == wanted) ?? usable[0];
        return BattleChoice.Fight(mine.Place, known.IndexOf(pick));
    }

    public int ChooseReplacement(BattleCore battle, Battler place) =>
        Enumerable.Range(0, place.Roster?.Count ?? 0).FirstOrDefault(i => battle.CanSendIn(place, i), -1);
}

/// <summary>
/// Battle puzzles (plan 13 · V26): reading them, making a puzzle's battle (<see cref="Setup"/>), answering it
/// (<see cref="Answer"/>), playing a list of answers to the end (<see cref="Play"/>) and judging the goal from the core
/// (<see cref="Judge"/>). Each puzzle is played by the <see cref="Ruleset"/> it names, handed to its battle and never
/// through <see cref="Ruleset.Use"/>. GPU-free.
/// </summary>
public static class Puzzles
{
    public const string Folder = "puzzles";

    private static IReadOnlyList<PuzzleFile>? all;

    /// <summary>Every puzzle of <c>Data/puzzles</c> (and of the packs in use), in the order of their files' names (<c>01-priority.json</c> first).</summary>
    public static IReadOnlyList<PuzzleFile> All => all ??= Read(DataLayers.FilesIn(Folder, "*.json").OrderBy(f => System.IO.Path.GetFileName(f.Path), StringComparer.Ordinal).Select(f => f.Path));

    /// <summary>The puzzles of these files; a file that can't be read is left out (<see cref="Problems"/> names it).</summary>
    public static IReadOnlyList<PuzzleFile> Read(IEnumerable<string> paths)
    {
        var list = new List<PuzzleFile>();
        foreach (string path in paths)
        {
            try
            {
                if (JsonSerializer.Deserialize<PuzzleFile>(File.ReadAllText(path), GameDataFiles.Json) is { Id.Length: > 0 } puzzle) list.Add(puzzle);
            }
            catch (JsonException) { }
        }
        return list;
    }

    /// <summary>What is wrong with a puzzle, a line each: nothing for one the core can play as written.</summary>
    public static List<string> Problems(PuzzleFile puzzle)
    {
        var problems = new List<string>();
        if (puzzle.Player.Count is < 1 or > Party.MaxSize) problems.Add($"{puzzle.Id}: the player has {puzzle.Player.Count} Pokémon");
        if (puzzle.Foe.Count is < 1 or > Party.MaxSize || puzzle.Wild && puzzle.Foe.Count != 1) problems.Add($"{puzzle.Id}: the foe has {puzzle.Foe.Count} Pokémon");
        if (puzzle.Turns < 1) problems.Add($"{puzzle.Id}: a goal of {puzzle.Turns} turns");
        if (puzzle.Goal == PuzzleGoal.Catch && (!puzzle.Wild || puzzle.Balls < 1)) problems.Add($"{puzzle.Id}: a catch wants a wild Pokémon and a ball");
        foreach (var p in puzzle.Player.Concat(puzzle.Foe))
        {
            if (BuiltTeams.WhyNot(p.Built) is { } why) problems.Add($"{puzzle.Id}: {p.Species} {why.Key} {why.Value}");
            else if (p.Hp is { } hp && (hp < 1 || hp > BuiltTeams.Make(p.Built).MaxHP)) problems.Add($"{puzzle.Id}: {p.Species} starts at {hp} HP, outside 1 to its most");
        }
        foreach (string move in puzzle.FoeMoves)
            if (!MoveDatabase.Has(move)) problems.Add($"{puzzle.Id}: the foe's move {move} is no move");
        if (puzzle.Solution.Count == 0) problems.Add($"{puzzle.Id}: no solution");
        if (puzzle.Decoy.Count == 0) problems.Add($"{puzzle.Id}: no decoy");
        return problems;
    }

    /// <summary>A Pokémon of the puzzle, made as a built one is and brought to its HP and condition.</summary>
    public static Pokemon Make(PuzzlePokemon spec, int place)
    {
        var p = BuiltTeams.Make(spec.Built, place);
        if (spec.Hp is { } hp) p.CurrentHP = Math.Clamp(hp, 1, p.MaxHP);
        p.Status = spec.Status;
        if (spec.Status == StatusCondition.Sleep) p.SleepTurns = 3;
        return p;
    }

    public static Party PlayerParty(PuzzleFile puzzle)
    {
        var party = new Party();
        for (int i = 0; i < puzzle.Player.Count; i++) party.Add(Make(puzzle.Player[i], i));
        return party;
    }

    /// <summary>The foe as a trainer (none for a wild Pokémon).</summary>
    public static Trainer? FoeTrainer(PuzzleFile puzzle)
    {
        if (puzzle.Wild) return null;
        var trainer = new Trainer { Id = "puzzle_" + puzzle.Id, Name = puzzle.FoeName, TrainerClass = puzzle.FoeClass, PrizeMoney = 0, Look = puzzle.FoeLook, Ai = Battle.Sim.Ai.AiFlags.Basic };
        for (int i = 0; i < puzzle.Foe.Count; i++) trainer.Party.Add(Make(puzzle.Foe[i], 10 + i));
        return trainer;
    }

    public static List<Pokemon> WildPokemon(PuzzleFile puzzle) => puzzle.Wild ? new List<Pokemon> { Make(puzzle.Foe[0], 10) } : new List<Pokemon>();

    /// <summary>The puzzle's generator: its seed, with its rolls fixed.</summary>
    public static BattleRandom Random(PuzzleFile puzzle)
    {
        var rng = new BattleRandom(puzzle.Seed);
        foreach (var roll in puzzle.Rolls)
            if (roll.Values.Count > 0) rng.Force(roll.Kind, roll.Values.ToArray());
        return rng;
    }

    /// <summary>The conditions the battle opens under: the puzzle's weather and Trick Room on plain ground.</summary>
    public static BattleConditions Conditions(PuzzleFile puzzle) => new()
    {
        Terrain = BattleTerrain.Building,
        Weather = puzzle.Weather,
        TrickRoom = puzzle.TrickRoom
    };

    /// <summary>The bag a puzzle's battle has: its Poké Balls, nothing else.</summary>
    public static Inventory Bag(PuzzleFile puzzle)
    {
        var bag = new Inventory();
        if (puzzle.Balls > 0) bag.AddItem(ItemDatabase.Get("Poké Ball")!, puzzle.Balls);
        return bag;
    }

    /// <summary>The rules' half of the puzzle's battle, with no screen: what the tests and <see cref="Play"/> fight.</summary>
    public static CoreSetup Setup(PuzzleFile puzzle)
    {
        var foe = FoeTrainer(puzzle);
        return new CoreSetup
        {
            PlayerParty = PlayerParty(puzzle),
            WildPokemon = WildPokemon(puzzle),
            Trainers = foe == null ? new List<Trainer>() : new List<Trainer> { foe },
            Random = Random(puzzle),
            Rules = puzzle.RulesSet,
            Conditions = Conditions(puzzle),
            PlayerName = "Player",
            EnemyController = new PuzzleFoe(puzzle.FoeMoves),
            NoBattleItems = puzzle.Balls == 0,
            SpecialBalls = 0
        };
    }

    /// <summary>The choice an answer makes for what the core asks now; null where the answer doesn't fit the question.</summary>
    public static BattleChoice? Answer(BattleCore core, PuzzleAnswer answer)
    {
        switch (core.Request)
        {
            case ActionRequest asked:
            {
                var who = asked.Places[0];
                if (answer.Move is { } move)
                {
                    int index = core.At(who).Pokemon!.Moves.FindIndex(m => m.Name == move);
                    return index < 0 ? null : BattleChoice.Fight(who, index);
                }
                if (answer.Switch is { } to) return BattleChoice.Switch(who, to);
                if (answer.Item is { } item) return BattleChoice.UseItem(who, item);
                return null;
            }
            case ReplacementRequest replacement when answer.Switch is { } to:
                return BattleChoice.Switch(replacement.Place, to);
            case SwitchOfferRequest offer:
                return answer.Switch is { } s ? BattleChoice.Switch(offer.Place, s) : BattleChoice.Stay(offer.Place);
            default:
                return null;
        }
    }

    /// <summary>
    /// The puzzle fought with these answers, one to each question in turn, until they run out, a question doesn't take
    /// the answer, or the battle is over. The core comes back as it stands.
    /// </summary>
    public static BattleCore Play(PuzzleFile puzzle, IEnumerable<PuzzleAnswer> answers)
    {
        var core = new BattleCore(Setup(puzzle));
        core.Start();
        foreach (var answer in answers)
        {
            if (core.Request == null || core.Result != BattleResult.None) break;
            if (Answer(core, answer) is not { } choice || core.WhyNot(choice) != null) break;
            core.Submit(choice);
        }
        return core;
    }

    /// <summary>Whether the battle as it stands has met the puzzle's goal.</summary>
    public static bool Judge(PuzzleFile puzzle, BattleResult result, int turnsDone) => puzzle.Goal switch
    {
        // A battle won in the middle of a turn ends with that turn not counted (BattleCore.Turn): it was the turn after those done
        PuzzleGoal.Win => result == BattleResult.PlayerVictory && turnsDone < puzzle.Turns,
        PuzzleGoal.Catch => result == BattleResult.EnemyCaught && turnsDone < puzzle.Turns,
        PuzzleGoal.Survive => result == BattleResult.PlayerVictory || result == BattleResult.None && turnsDone >= puzzle.Turns,
        _ => false
    };

    public static bool Judge(PuzzleFile puzzle, BattleCore core) => Judge(puzzle, core.Result, core.Turn);

    /// <summary>
    /// Whether the battle can no longer meet the goal, so that the game may stop it: lost, run from, over another way, or
    /// past its turns without a win or a catch. A survival is decided only once its turns are done.
    /// </summary>
    public static bool Decided(PuzzleFile puzzle, BattleResult result, int turnsDone) => result != BattleResult.None || turnsDone >= puzzle.Turns;
}
