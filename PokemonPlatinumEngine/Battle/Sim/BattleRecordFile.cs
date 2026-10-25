using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Battle.Sim.Ai;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>Why a recorded battle's file is not played (plan 08 · P10): what its reader says, with a notice for each.</summary>
public enum ReplayRefusal
{
    /// <summary>Nothing: it is played.</summary>
    None,
    /// <summary>It can't be read: not a file of this kind, or cut short.</summary>
    Damaged,
    /// <summary>A file written by a later game, in a shape this one doesn't know.</summary>
    OtherFileVersion,
    /// <summary>
    /// Written by a game whose species, moves, abilities, items or trainers were other than these
    /// (<see cref="GameDataFiles.Version"/>): played on, it would be another battle (plan 08's risk "A replay goes stale").
    /// </summary>
    OtherDataVersion
}

/// <summary>What reading a recorded battle's file came to: the file, or why there is none.</summary>
public sealed record RecordFileReading(BattleRecordFile? File, ReplayRefusal Refused);

/// <summary>
/// A battle kept on the Vs. Recorder (plan 08 · P10; design "Recorded battles"): everything the rules need to fight it
/// again with no screen, as plain data. Both sides as they were before the first line (<see cref="SavedPokemonData"/>,
/// with the two counters a save doesn't keep), the trainers, the format, the rules' preset, the
/// <see cref="BattleConditions"/>, the player's name, the day, the version of the game's data it was fought under, and
/// the <see cref="BattleRecord"/> (the seed and every answer given from outside). A replay rebuilds its Pokémon from the
/// file, so the events of the log name the replay's own: this is the wire format plan 28 · I1 sends, and the battle
/// plan 12 · Q25 keeps as a battle begins.
/// <para>
/// GPU-free and file-free but for <see cref="WriteTo"/>: <see cref="Opening"/> is taken from a battle's setup before the
/// rules have done anything, <see cref="Closing"/> adds what the battle came to, <see cref="Serialize"/> and
/// <see cref="Read"/> are its text, and <see cref="Replay"/> fights it again.
/// </para>
/// </summary>
public sealed class BattleRecordFile
{
    /// <summary>The shape of the file this game writes; a later one is refused (<see cref="ReplayRefusal.OtherFileVersion"/>).</summary>
    public const int CurrentFileVersion = 1;

    /// <summary>A recorded battle's file ends so.</summary>
    public const string Extension = "json";

    /// <summary>The key item that keeps battles: no recording is kept without it.</summary>
    public const string RecorderItem = "Vs. Recorder";

    public int FileVersion { get; set; } = CurrentFileVersion;

    /// <summary>The game's data it was fought under (<see cref="GameDataFiles.Version"/>).</summary>
    public string DataVersion { get; set; } = "";

    /// <summary>The rules' preset it was fought by.</summary>
    public RulesPreset Rules { get; set; }

    public BattleFormat Format { get; set; }
    public BattleKind Kind { get; set; }

    /// <summary>The name the battle's lines give the player.</summary>
    public string PlayerName { get; set; } = "";

    /// <summary>The day it was fought on (<see cref="GameClock.Today"/>); null until <see cref="Closing"/>.</summary>
    public DateTime? Day { get; set; }

    /// <summary>The player's team as it went in, in its order.</summary>
    public List<RecordedPokemon> Player { get; set; } = new();

    /// <summary>The wild Pokémon met; empty in a trainer's battle.</summary>
    public List<RecordedPokemon> Wild { get; set; } = new();

    /// <summary>The trainers across the field, each with their team as it went in.</summary>
    public List<RecordedTrainer> Trainers { get; set; } = new();

    /// <summary>The first trainer's first Pokémon by its place in their team, where it isn't the first; −1 for the first.</summary>
    public int FirstTrainerIndex { get; set; } = -1;

    /// <summary>The trainer beside the player in a tag battle.</summary>
    public RecordedTrainer? Partner { get; set; }

    public bool CannotFlee { get; set; }
    public bool FirstBattle { get; set; }
    public int SpecialBalls { get; set; }
    public bool Frontier { get; set; }
    public bool NoBattleItems { get; set; }
    public bool OffersSwitch { get; set; }
    public bool MegaMaxDen { get; set; }

    /// <summary>The difficulty the battle was fought under (plan 13 · V2): Easy gives more EXP, so a replay needs it. A file without it is Normal.</summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    /// <summary>The level EXP stopped at under the run's level cap (plan 13 · V3); 0 for none, as a file without it.</summary>
    public int RunLevelCap { get; set; }
    public bool KeyStone { get; set; }
    public bool ZRing { get; set; }
    public bool DynamaxBand { get; set; }
    public bool MegaMaxBand { get; set; }
    public bool TeraOrb { get; set; }

    public RecordedConditions Conditions { get; set; } = new();

    /// <summary>Where the battle's chance began (<see cref="BattleRecord.Seed"/>).</summary>
    public uint Seed { get; set; }

    /// <summary>Every answer given from outside, a list to each time the rules asked (<see cref="BattleRecord.Answers"/>).</summary>
    public List<List<RecordedChoice>> Answers { get; set; } = new();

    /// <summary>How it ended, and after how many turns: what a list of recorded battles shows (plan 08 · P11).</summary>
    public BattleResult Result { get; set; }
    public int Turns { get; set; }

    // ------------------------------------------------------------------ taking it

    /// <summary>
    /// A battle as its rules are about to begin it: the setup the core is handed, before it has changed anything. The
    /// rules are those the core will fight by (the setup's, or the game's in force).
    /// </summary>
    public static BattleRecordFile Opening(CoreSetup setup)
    {
        var rules = setup.Rules ?? Ruleset.Current;
        var first = setup.Trainers.Count > 0 && setup.FirstTrainerPokemon != null
            ? setup.Trainers[0].Party.Members.IndexOf(setup.FirstTrainerPokemon) : -1;
        var conditions = setup.Conditions;
        return new BattleRecordFile
        {
            DataVersion = GameDataFiles.Version,
            Rules = rules.Preset,
            Format = setup.Format,
            Kind = setup.Kind,
            PlayerName = setup.PlayerName,
            Player = setup.PlayerParty.Members.Select(RecordedPokemon.Of).ToList(),
            Wild = setup.WildPokemon.Select(RecordedPokemon.Of).ToList(),
            Trainers = setup.Trainers.Select(RecordedTrainer.Of).ToList(),
            FirstTrainerIndex = first,
            Partner = setup.Partner == null ? null : RecordedTrainer.Of(setup.Partner),
            CannotFlee = setup.CannotFlee,
            FirstBattle = setup.FirstBattle,
            SpecialBalls = setup.SpecialBalls,
            Frontier = setup.Frontier,
            NoBattleItems = setup.NoBattleItems,
            OffersSwitch = setup.OffersSwitch,
            MegaMaxDen = setup.MegaMaxDen,
            Difficulty = setup.Difficulty,
            RunLevelCap = setup.RunLevelCap,
            KeyStone = setup.KeyStone,
            ZRing = setup.ZRing,
            DynamaxBand = setup.DynamaxBand,
            MegaMaxBand = setup.MegaMaxBand,
            TeraOrb = setup.TeraOrb,
            Conditions = new RecordedConditions
            {
                Terrain = conditions.Terrain,
                Night = conditions.Night,
                Weather = conditions.Weather,
                TrickRoom = conditions.TrickRoom,
                Badges = conditions.Badges,
                Money = conditions.Money,
                Player = conditions.Player,
                Inverse = conditions.Inverse,
                Fishing = conditions.Fishing,
                OnlyBall = conditions.OnlyBall,
                // The only species a battle asks about having caught is the wild one's (the Repeat Ball)
                Caught = setup.WildPokemon.Select(p => p.Species).Distinct().Where(conditions.HasCaught).Select(s => s.DexNumber).ToList()
            }
        };
    }

    /// <summary>
    /// The opening with what the battle came to: its record, its end and the day. Null when the battle's chance was
    /// no <see cref="BattleRandom"/> (a test's own generator): nothing else comes out the same again.
    /// </summary>
    public BattleRecordFile? Closing(BattleCore core, DateTime day)
    {
        if (core.Record is not { } record) return null;
        var file = (BattleRecordFile)MemberwiseClone();
        file.Seed = record.Seed;
        file.Answers = record.Answers.Select(turn => turn.Select(RecordedChoice.Of).ToList()).ToList();
        file.Result = core.Result;
        file.Turns = core.Turn;
        file.Day = day.Date;
        return file;
    }

    /// <summary>The record the rules replay: the seed and the answers, with the two flags a record keeps.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public BattleRecord Record => new(Seed, Answers.Select(turn => (IReadOnlyList<BattleChoice>)turn.Select(c => c.ToChoice()).ToList()).ToList(),
        Conditions.Inverse, OffersSwitch, Difficulty, RunLevelCap);

    /// <summary>Whom the battle was against: the first trainer's class and name, or the first wild Pokémon's species.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Foe => Trainers.Count > 0 ? Trainer.ClassName(Trainers[0].TrainerClass) + " " + Trainers[0].Name
        : Wild.Count > 0 ? Wild[0].Pokemon.SpeciesName : "";

    // ------------------------------------------------------------------ fighting it again

    /// <summary>
    /// What the rules start from, rebuilt from the file: new Pokémon, new trainers, a generator of the record's seed,
    /// the trainers' own minds and the player's choices left to the record.
    /// </summary>
    public CoreSetup ToSetup()
    {
        var rules = Ruleset.Of(Rules);
        var party = new Party();
        foreach (var p in Player) party.Add(p.ToPokemon(rules));
        var trainers = Trainers.Select(t => t.ToTrainer(rules)).ToList();
        Pokemon? first = trainers.Count > 0 && FirstTrainerIndex >= 0 && FirstTrainerIndex < trainers[0].Party.Count
            ? trainers[0].Party.Members[FirstTrainerIndex] : null;
        var caught = Conditions.Caught.ToHashSet();
        return new CoreSetup
        {
            PlayerParty = party,
            WildPokemon = Wild.Select(p => p.ToPokemon(rules)).ToList(),
            Trainers = trainers,
            FirstTrainerPokemon = first,
            Format = Format,
            Random = new BattleRandom(Seed),
            Rules = rules,
            Conditions = new BattleConditions
            {
                Terrain = Conditions.Terrain,
                Night = Conditions.Night,
                HasCaught = species => caught.Contains(species.DexNumber),
                Weather = Conditions.Weather,
                TrickRoom = Conditions.TrickRoom,
                Badges = Conditions.Badges,
                Money = Conditions.Money,
                Player = Conditions.Player,
                Inverse = Conditions.Inverse,
                Fishing = Conditions.Fishing,
                OnlyBall = Conditions.OnlyBall
            },
            PlayerName = PlayerName,
            PlayerController = null,
            Kind = Kind,
            Partner = Partner?.ToTrainer(rules),
            CannotFlee = CannotFlee,
            FirstBattle = FirstBattle,
            SpecialBalls = SpecialBalls,
            Frontier = Frontier,
            NoBattleItems = NoBattleItems,
            OffersSwitch = OffersSwitch,
            MegaMaxDen = MegaMaxDen,
            Difficulty = Difficulty,
            RunLevelCap = RunLevelCap,
            KeyStone = KeyStone,
            ZRing = ZRing,
            DynamaxBand = DynamaxBand,
            MegaMaxBand = MegaMaxBand,
            TeraOrb = TeraOrb
        };
    }

    /// <summary>
    /// Fights the battle again with no screen, from the file alone, to its end: the same log, line for line, and the
    /// same <see cref="BattleResult"/>. The game's own rules must be the ones in force when it was fought (a species'
    /// values are swapped in place by <see cref="Ruleset.Use"/>), which a game that plays it is.
    /// </summary>
    public BattleCore Replay()
    {
        var core = new BattleCore(ToSetup());
        core.Replay(Record);
        return core;
    }

    // ------------------------------------------------------------------ its text and its file

    public string Serialize() => GameDataFiles.Serialize(this);

    /// <summary>
    /// Reads a file's text: the file, or why it is refused. A file of another shape, or written under data other than
    /// <paramref name="dataVersion"/> (left out, the game's own, <see cref="GameDataFiles.Version"/>), is refused rather
    /// than played wrong.
    /// </summary>
    public static RecordFileReading Read(string text, string? dataVersion = null)
    {
        BattleRecordFile? file;
        try
        {
            file = JsonSerializer.Deserialize<BattleRecordFile>(text, GameDataFiles.Json);
        }
        catch (JsonException)
        {
            return new RecordFileReading(null, ReplayRefusal.Damaged);
        }
        if (file == null || file.Player.Count == 0 && file.Wild.Count == 0) return new RecordFileReading(null, ReplayRefusal.Damaged);
        if (file.FileVersion > CurrentFileVersion || file.FileVersion < 1) return new RecordFileReading(null, ReplayRefusal.OtherFileVersion);
        if (file.DataVersion != (dataVersion ?? GameDataFiles.Version)) return new RecordFileReading(null, ReplayRefusal.OtherDataVersion);
        return new RecordFileReading(file, ReplayRefusal.None);
    }

    /// <summary>Reads a file from the disk (<see cref="Read(string, string?)"/>); one that can't be opened is damaged.</summary>
    public static RecordFileReading ReadFile(string path)
    {
        try
        {
            return Read(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return new RecordFileReading(null, ReplayRefusal.Damaged);
        }
        catch (UnauthorizedAccessException)
        {
            return new RecordFileReading(null, ReplayRefusal.Damaged);
        }
    }

    /// <summary>What a list of recorded battles says of a file it won't play (plan 08 · P11).</summary>
    public static string Notice(ReplayRefusal refused) => refused switch
    {
        ReplayRefusal.OtherDataVersion => Text.Of("engine.recordBattle.otherVersion"),
        ReplayRefusal.OtherFileVersion => Text.Of("engine.recordBattle.otherFile"),
        ReplayRefusal.Damaged => Text.Of("engine.recordBattle.damaged"),
        _ => ""
    };

    /// <summary>
    /// The file's name in its folder, by the day and the foe (<c>20261020-YoungsterTristan</c>), with a number after it
    /// where that name is taken.
    /// </summary>
    public static string NameFor(DateTime day, string foe, Func<string, bool> taken)
    {
        string who = Trainer.KeyOf(foe);
        if (who.Length == 0) who = nameof(Battle);
        string stem = $"{day:yyyyMMdd}-{who}";
        string name = $"{stem}.{Extension}";
        for (int n = 2; taken(name); n++) name = $"{stem}-{n}.{Extension}";
        return name;
    }

    /// <summary>
    /// Writes the file into a folder (the game's is <see cref="SavePaths.Replays"/>), made if it isn't there; returns
    /// where. Written beside itself first and moved into place, so a file cut short is never left under its name.
    /// </summary>
    public string WriteTo(string folder)
    {
        Directory.CreateDirectory(folder);
        string name = NameFor(Day ?? DateTime.MinValue, Foe, n => File.Exists(Path.Combine(folder, n)));
        string path = Path.Combine(folder, name);
        string temporary = SaveManager.TempOf(path);
        File.WriteAllText(temporary, Serialize());
        File.Move(temporary, path, overwrite: true);
        return path;
    }
}

/// <summary>A Pokémon of a recorded battle as it went in: what a save keeps of it, and the two counters a save doesn't.</summary>
public sealed class RecordedPokemon
{
    public SavedPokemonData Pokemon { get; set; } = new();

    /// <summary>The turns of sleep it has left, which a battle goes on counting (<see cref="Models.Pokemon.SleepTurns"/>).</summary>
    public int SleepTurns { get; set; }

    /// <summary>How far a bad poisoning has gone (<see cref="Models.Pokemon.ToxicCounter"/>).</summary>
    public int ToxicCounter { get; set; }

    public static RecordedPokemon Of(Pokemon p) => new()
    {
        Pokemon = SavedPokemonData.FromPokemon(p),
        SleepTurns = p.SleepTurns,
        ToxicCounter = p.ToxicCounter
    };

    public Pokemon ToPokemon(Ruleset rules)
    {
        var p = Pokemon.ToPokemon(rules);
        p.SleepTurns = SleepTurns;
        p.ToxicCounter = ToxicCounter;
        return p;
    }
}

/// <summary>A trainer of a recorded battle: who they are, how they think, what they carry and their team as it went in.</summary>
public sealed class RecordedTrainer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TrainerClass { get; set; } = "";
    public int PrizeMoney { get; set; }
    public bool DoubleBattle { get; set; }
    public AiFlags Ai { get; set; }
    public List<string> Items { get; set; } = new();
    public bool KeyStone { get; set; }
    public bool ZRing { get; set; }
    public bool DynamaxBand { get; set; }
    public bool MegaMaxBand { get; set; }
    public bool TeraOrb { get; set; }

    /// <summary>The look the person who carries them is drawn in, for a replay's screen; the rules never read it.</summary>
    public string? Look { get; set; }

    public List<RecordedPokemon> Party { get; set; } = new();

    public static RecordedTrainer Of(Trainer t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        TrainerClass = t.TrainerClass,
        PrizeMoney = t.PrizeMoney,
        DoubleBattle = t.DoubleBattle,
        Ai = t.Ai,
        Items = t.Items.ToList(),
        KeyStone = t.KeyStone,
        ZRing = t.ZRing,
        DynamaxBand = t.DynamaxBand,
        MegaMaxBand = t.MegaMaxBand,
        TeraOrb = t.TeraOrb,
        Look = t.Look,
        Party = t.Party.Members.Select(RecordedPokemon.Of).ToList()
    };

    public Trainer ToTrainer(Ruleset rules)
    {
        var trainer = new Trainer
        {
            Id = Id,
            Name = Name,
            TrainerClass = TrainerClass,
            PrizeMoney = PrizeMoney,
            DoubleBattle = DoubleBattle,
            Ai = Ai,
            Items = Items.ToList(),
            KeyStone = KeyStone,
            ZRing = ZRing,
            DynamaxBand = DynamaxBand,
            MegaMaxBand = MegaMaxBand,
            TeraOrb = TeraOrb,
            Look = Look
        };
        foreach (var p in Party) trainer.Party.Add(p.ToPokemon(rules));
        return trainer;
    }
}

/// <summary>What a recorded battle's rules knew of the world outside it (<see cref="BattleConditions"/>, as plain data).</summary>
public sealed class RecordedConditions
{
    public BattleTerrain Terrain { get; set; }
    public bool Night { get; set; }
    public BattleWeather Weather { get; set; }
    public bool TrickRoom { get; set; }
    public int Badges { get; set; }
    public int Money { get; set; }
    public TrainerMark? Player { get; set; }
    public bool Inverse { get; set; }
    public bool Fishing { get; set; }
    public string? OnlyBall { get; set; }

    /// <summary>The wild species met that the player had caught before, by their National Pokédex numbers (the Repeat Ball).</summary>
    public List<int> Caught { get; set; } = new();
}

/// <summary>An answer of a recorded battle (<see cref="BattleChoice"/>, as plain data).</summary>
public sealed class RecordedChoice
{
    public BattleSide Side { get; set; }
    public int Slot { get; set; }
    public ChoiceKind Kind { get; set; }
    public int Move { get; set; } = -1;
    public BattleSide? TargetSide { get; set; }
    public int TargetSlot { get; set; }
    public int SwitchTo { get; set; } = -1;
    public string? Item { get; set; }
    public bool Mega { get; set; }
    public bool ZMove { get; set; }
    public bool UltraBurst { get; set; }
    public bool Dynamax { get; set; }
    public bool Terastallize { get; set; }

    public static RecordedChoice Of(BattleChoice c) => new()
    {
        Side = c.Who.Side,
        Slot = c.Who.Slot,
        Kind = c.Kind,
        Move = c.Move,
        TargetSide = c.Target?.Side,
        TargetSlot = c.Target?.Slot ?? 0,
        SwitchTo = c.SwitchTo,
        Item = c.Item,
        Mega = c.Mega,
        ZMove = c.ZMove,
        UltraBurst = c.UltraBurst,
        Dynamax = c.Dynamax,
        Terastallize = c.Terastallize
    };

    public BattleChoice ToChoice() => new(new Place(Side, Slot), Kind)
    {
        Move = Move,
        Target = TargetSide is { } side ? new Place(side, TargetSlot) : null,
        SwitchTo = SwitchTo,
        Item = Item,
        Mega = Mega,
        ZMove = ZMove,
        UltraBurst = UltraBurst,
        Dynamax = Dynamax,
        Terastallize = Terastallize
    };
}
