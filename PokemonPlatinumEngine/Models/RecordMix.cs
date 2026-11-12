using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>A decoration standing in a mixed secret base: its kind by the data's id, and its corner's tile.</summary>
public sealed class MixedDecoration
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// Another save's secret base as it was mixed (plan 40 · GE10): its spot, the tile its door leads back to and what stood
/// in it, by kind and tile, never by the other save's decoration PC, which is that save's own.
/// </summary>
public sealed class MixedBase
{
    public int Id { get; set; }
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int OutX { get; set; }
    public int OutY { get; set; }
    public List<MixedDecoration> Standing { get; set; } = new();

    public SecretBaseLayout? Layout => SecretBaseLayouts.Of(Id);
}

/// <summary>
/// What one other save gave when records were mixed (plan 40 · GE10): who its player is, their team as the battle room
/// builds teams (plain choices, so a change of the data never makes the record unreadable: the trainer's file is made
/// afresh each time it stands), their Hoenn secret base and their answer to Jubilife City's reporters. Kept by the game
/// that mixed, keyed by the other game (its trainer's number and the day it began), so mixing again with the same game
/// replaces what it gave before.
/// </summary>
public sealed class MixedRecord
{
    public int TrainerId { get; set; }
    public DateTime? Started { get; set; }
    public string Name { get; set; } = "";
    public PlayerLook Look { get; set; }

    /// <summary>The team its trainer battles with; null when nothing of the other team could battle.</summary>
    public List<BuiltPokemon>? Team { get; set; }

    /// <summary>Its secret base; null with none.</summary>
    public MixedBase? Base { get; set; }

    /// <summary>The two words of its interview, as keys; null with none.</summary>
    public List<string>? Interview { get; set; }

    /// <summary>The day it was mixed.</summary>
    public DateTime Mixed { get; set; }

    /// <summary>The other game, as a key: its trainer's number and the day it began.</summary>
    public string Key => KeyOf(TrainerId, Started);

    public static string KeyOf(int trainerId, DateTime? started) => trainerId + ":" + (started?.ToString("o") ?? "-");
}

/// <summary>What a save keeps of records mixed (<see cref="SaveData.MixedRecords"/>): the records and the friend's base being visited.</summary>
public sealed class RecordMixSave
{
    public List<MixedRecord> Records { get; set; } = new();

    /// <summary>The key of the record whose base the player stands in (<see cref="MixedRecord.Key"/>); null elsewhere.</summary>
    public string? Visiting { get; set; }
}

/// <summary>The records a game has mixed in, newest first, and the friend's base being visited.</summary>
public sealed class RecordMixState
{
    public List<MixedRecord> Records { get; } = new();

    /// <summary>The record whose secret base the player is visiting; null while in none.</summary>
    public MixedRecord? Visiting { get; set; }

    public void Clear()
    {
        Records.Clear();
        Visiting = null;
    }

    public void Load(RecordMixSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var record in save.Records.Take(RecordMix.Most)) Records.Add(record);
        Visiting = save.Visiting == null ? null : Records.FirstOrDefault(r => r.Key == save.Visiting && r.Base != null);
    }

    public RecordMixSave? ToSave() => Records.Count == 0 ? null : new RecordMixSave { Records = Records.ToList(), Visiting = Visiting?.Key };
}

/// <summary>
/// Records mixed between one's own saves (plan 40 · GE10, decided by the user 2026-11-12; rulings, "Records mixed between
/// one's own saves"): the record desk upstairs in a Pokémon Center reads the player's other slots, never writing them, and
/// keeps from each readable one (<see cref="SaveLoad.Ok"/> or recovered from its backup; a damaged slot is passed over,
/// and so is the game in play under any slot) its trainer, its Hoenn secret base and its Jubilife TV interview. The
/// friends' hall stands the mixed trainers after the folder's files, Hoenn's REGISTRY lists the mixed bases, which are
/// visited at their spots, and the television tells of the interviews. GPU-free; nothing drawn by chance.
/// </summary>
public static class RecordMix
{
    /// <summary>The most records kept: one for each other game, and at most this many.</summary>
    public const int Most = 8;

    /// <summary>The most decorations a mixed base keeps standing: a base's own most (<see cref="SecretBases.MostOut"/>).</summary>
    public const int MostStanding = SecretBases.MostOut;

    /// <summary>
    /// Mixes with <paramref name="others"/>: each gives its record, replacing what the same game gave before, and the newest
    /// stand first, at most <see cref="Most"/>. The game in play (<paramref name="ownTrainerId"/>, <paramref name="ownStarted"/>)
    /// gives nothing. The number of saves mixed.
    /// </summary>
    public static int Mix(RecordMixState state, IEnumerable<SaveData> others, int ownTrainerId, DateTime? ownStarted, DateTime today)
    {
        string own = MixedRecord.KeyOf(ownTrainerId, ownStarted);
        int mixed = 0;
        foreach (var other in others)
        {
            if (RecordOf(other, today) is not { } record || record.Key == own) continue;
            state.Records.RemoveAll(r => r.Key == record.Key);
            state.Records.Insert(0, record);
            mixed++;
        }
        if (state.Records.Count > Most) state.Records.RemoveRange(Most, state.Records.Count - Most);
        if (state.Visiting != null && !state.Records.Contains(state.Visiting))
            state.Visiting = state.Records.FirstOrDefault(r => r.Key == state.Visiting.Key && r.Base != null);
        return mixed;
    }

    /// <summary>What another save gives: its player, a team that can battle, its base and its interview.</summary>
    public static MixedRecord RecordOf(SaveData other, DateTime today)
    {
        var team = other.Party.Select(Built).Where(b => b != null).Select(b => b!).Take(Party.MaxSize).ToList();
        var interview = other.StoryPhrases.TryGetValue(StoryState.InterviewPhrase, out var words) && PhraseGroups.IsWhole(words) ? words.ToList() : null;
        return new MixedRecord
        {
            TrainerId = other.TrainerId,
            Started = other.Started,
            Name = PlayerIdentity.Clean(other.PlayerName, TrainerFile.NameLength),
            Look = Enum.IsDefined(other.Look) ? other.Look : PlayerLook.Boy,
            Team = team.Count > 0 ? team : null,
            Base = BaseOf(other),
            Interview = interview,
            Mixed = today.Date
        };
    }

    /// <summary>
    /// A Pokémon of another save as the battle room would build it, or null where it can't battle: an Egg, a species of
    /// ours, one the data doesn't have. Its level is held to 100, a form, an ability or an item the room doesn't allow is
    /// let go, and of its moves only those its species may know stand (the four latest it learns by level where none does).
    /// </summary>
    public static BuiltPokemon? Built(SavedPokemonData saved)
    {
        if (saved.IsEgg || PokemonDatabase.Get(saved.SpeciesName) is not { } species || species.IsOurs) return null;
        var built = new BuiltPokemon
        {
            Species = species.Name,
            Level = Math.Clamp(saved.Level, 1, BuiltTeams.HighestLevel),
            Nature = saved.Nature
        };
        if (saved.Form != null && species.Form(saved.Form) is { } form && BuiltTeams.Choosable(form)) built.Form = form.Name;
        if (saved.Ability != null && BuiltTeams.AbilitiesOf(built).Contains(saved.Ability)) built.Ability = saved.Ability;
        if (saved.HeldItem != null && ItemDatabase.Get(saved.HeldItem) is { } item && !string.IsNullOrEmpty(item.HoldEffect)) built.Item = item.Name;
        var learnable = new HashSet<string>(BuiltTeams.MovesOf(built), StringComparer.OrdinalIgnoreCase);
        built.Moves = saved.Moves.Select(m => m.MoveName).Where(learnable.Contains).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
        if (built.Moves.Count == 0) built.Moves = BuiltTeams.Fresh(species.Name).Moves;
        return BuiltTeams.WhyNot(built) == null ? built : null;
    }

    /// <summary>Another save's secret base as it stands, or null with none (or one whose room the game doesn't know).</summary>
    public static MixedBase? BaseOf(SaveData other)
    {
        if (other.SecretBase is not { } own || own.Layout == null || string.IsNullOrEmpty(own.Map)) return null;
        var standing = SecretBases.Standing(own, other.Decorations ?? new List<StoredDecoration>())
            .Take(MostStanding).Select(s => new MixedDecoration { Id = s.Decoration.Id, X = s.X, Y = s.Y }).ToList();
        return new MixedBase { Id = own.Id, Map = own.Map, X = own.X, Y = own.Y, OutX = own.OutX, OutY = own.OutY, Standing = standing };
    }

    /// <summary>
    /// A mixed record's trainer as a file (plan 13 · V22), made afresh under the data in force, so the friends' hall and a
    /// friend's base stand it as they stand the folder's; null where it has no team or its name isn't a keyboard's. Its class
    /// and its two lines are worked out from its trainer's number, so the same game's trainer is always the same.
    /// </summary>
    public static TrainerFile? TrainerOf(MixedRecord record)
    {
        if (record.Team is not { Count: > 0 } members || record.Name.Length == 0 || !TrainerFile.OnTheKeyboard(record.Name)) return null;
        var team = new BuiltTeam { Name = record.Name, Members = members.Select(m => m.Copy()).ToList() };
        if (BuiltTeams.WhyNot(team) != null) return null;
        uint n = (uint)record.TrainerId;
        int before = (int)(n % TrainerFile.Phrases);
        int after = (before + 1 + (int)(n / TrainerFile.Phrases % (TrainerFile.Phrases - 1))) % TrainerFile.Phrases;
        string cls = TrainerFile.Classes[(int)(n / 7 % (uint)TrainerFile.Classes.Count)];
        var file = TrainerFile.Of(record.Name, record.Look, cls, before, after, team);
        return TrainerFile.Refuse(file) == null ? file : null;
    }

    /// <summary>The mixed trainers as the folder's entries, newest first, after which the friends' hall stands them.</summary>
    public static IReadOnlyList<TrainerFiles.Entry> Entries(RecordMixState state) =>
        state.Records.Select(r => TrainerOf(r)).Where(f => f != null).Select(f => new TrainerFiles.Entry("mixed:" + f!.Checksum, f, null)).ToList();

    /// <summary>The mixed bases, newest first, a spot once (the newest base at a spot stands there).</summary>
    public static IReadOnlyList<MixedRecord> Bases(RecordMixState state)
    {
        var seen = new HashSet<(string, int, int)>();
        return state.Records.Where(r => r.Base is { Layout: not null } b && seen.Add((b.Map, b.X, b.Y))).ToList();
    }

    /// <summary>The mixed base at a spot of a map; null with none.</summary>
    public static MixedRecord? BaseAt(RecordMixState state, string map, int x, int y) =>
        Bases(state).FirstOrDefault(r => r.Base!.Map == map && r.Base.X == x && r.Base.Y == y);

    /// <summary>The interviews mixed in, newest first: whose, and their two words as keys.</summary>
    public static IReadOnlyList<(string Name, IReadOnlyList<string> Words)> Interviews(RecordMixState state) =>
        state.Records.Where(r => r.Interview != null && PhraseGroups.IsWhole(r.Interview) && r.Name.Length > 0)
            .Select(r => (r.Name, (IReadOnlyList<string>)r.Interview!)).ToList();

    /// <summary>
    /// The tile a friend's trainer stands on in their base: the open floor nearest the PC's front tile that is neither a
    /// tile kept clear (inside the door, before the PC) nor taken by what stands there, the first in reading order of the
    /// nearest; null in a room with none.
    /// </summary>
    public static (int X, int Y)? OwnerTile(BaseFloor floor, SecretBaseLayout layout, Func<int, int, bool> solid)
    {
        var kept = new HashSet<(int, int)>(floor.KeptClear);
        return floor.Floor.Where(t => !kept.Contains(t) && !solid(t.X, t.Y))
            .OrderBy(t => Math.Abs(t.X - layout.PcFrontX) + Math.Abs(t.Y - layout.PcFrontY)).ThenBy(t => t.Y).ThenBy(t => t.X)
            .Select(t => ((int, int)?)t).FirstOrDefault();
    }
}
