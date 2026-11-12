using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a mission of the Week's Work asks (plan 41 · EG6); its words are <c>missions.kind.&lt;kind&gt;</c>.</summary>
public enum MissionKind
{
    /// <summary>Catch Pokémon of a type in a region.</summary>
    CatchType,
    /// <summary>Catch Pokémon in one place of a region.</summary>
    CatchInArea,
    /// <summary>Beat trainers in a region.</summary>
    BeatTrainers,
    /// <summary>Win a rematch (a League's second team, a Leader's, the Vs. Seeker's) in a region.</summary>
    WinRematch,
    /// <summary>Hatch Eggs in a region.</summary>
    HatchEggs,
    /// <summary>Come to a landmark of a region.</summary>
    Landmark,
    /// <summary>Win battles at a battle facility of a region.</summary>
    WinFacility,
    /// <summary>Win a contest in a region.</summary>
    WinContest
}

/// <summary>How a kind of mission is drawn and paid (<c>Data/missions.json</c>'s <c>kinds</c>).</summary>
public sealed class MissionKindRow
{
    [DataRequired] public MissionKind Kind { get; set; }
    /// <summary>How often it is drawn against the others.</summary>
    [DataRequired] public int Weight { get; set; }
    /// <summary>The fewest and most it asks for; a timed one asks for the fewest.</summary>
    [DataRequired] public int Least { get; set; }
    [DataRequired] public int Most { get; set; }
    /// <summary>The pennants it pays for each thing asked for, and those it pays on top.</summary>
    [DataRequired] public int PerOne { get; set; }
    public int Bonus { get; set; }
    /// <summary>Whether it may be drawn as a timed mission, and with how many minutes.</summary>
    public bool Timed { get; set; }
    public int Minutes { get; set; }
    /// <summary>The regions it may be drawn in (every finished one where it is empty): where the game has such places.</summary>
    public List<string> Regions { get; set; } = new();
}

/// <summary>A region's places for the missions that name one (<c>Data/missions.json</c>'s <c>places</c>).</summary>
public sealed class MissionPlaces
{
    /// <summary>Areas with wild Pokémon in their grass, by key.</summary>
    public List<string> Catch { get; set; } = new();
    /// <summary>Landmarks to come to, by area key.</summary>
    public List<string> Landmarks { get; set; } = new();
}

/// <summary>The file <c>Data/missions.json</c> (ours, written by hand, read strictly).</summary>
public sealed class MissionsFile
{
    /// <summary>How many missions a week posts with no clock, and how many timed.</summary>
    [DataRequired] public int Plain { get; set; }
    [DataRequired] public int TimedCount { get; set; }
    [DataRequired] public List<MissionKindRow> Kinds { get; set; } = new();
    /// <summary>The types a catch of a type may ask for.</summary>
    [DataRequired] public List<PokemonType> Types { get; set; } = new();
    /// <summary>Each region's places, by the region's id.</summary>
    public Dictionary<string, MissionPlaces> Places { get; set; } = new();
    /// <summary>The items a mission may pay beside its pennants, one of them drawn for each; none for an empty list.</summary>
    public List<string> Items { get; set; } = new();
    /// <summary>One mission in this many pays an item as well.</summary>
    public int ItemOneIn { get; set; } = 2;
    /// <summary>The pennants a timed mission pays on top of its row's, for the clock.</summary>
    public int TimedBonus { get; set; }
}

/// <summary>
/// One mission of the week: what it asks, where, how many, what it pays, and for a timed one its minutes; with what has
/// been done of it and whether its reward has been claimed.
/// </summary>
public sealed class Mission
{
    public MissionKind Kind { get; set; }
    public string Region { get; set; } = "";
    /// <summary>The area a place's mission names, by key; empty for the rest.</summary>
    public string Area { get; set; } = "";
    /// <summary>The type a catch of a type asks for.</summary>
    public PokemonType? Type { get; set; }
    public int Goal { get; set; }
    public int Reward { get; set; }
    /// <summary>The item it pays beside its pennants, or none.</summary>
    public string? Item { get; set; }
    /// <summary>Minutes of play a timed mission runs for; 0 for one with no clock.</summary>
    public int Minutes { get; set; }
    public int Progress { get; set; }
    public bool Claimed { get; set; }

    public bool IsTimed => Minutes > 0;
    public bool IsDone => Progress >= Goal;
}

/// <summary>Something the engine tells the board of (plan 41 · EG6), where it counted it already.</summary>
public enum MissionEventKind { Caught, BeatTrainer, BeatRematch, Hatched, Arrived, FacilityWin, ContestWin }

/// <summary>What happened, where: the region, the area or map, and a Pokémon's types where one was caught.</summary>
public readonly record struct MissionEvent(MissionEventKind Kind, string Region, string Area = "", PokemonType? Type1 = null, PokemonType? Type2 = null);

/// <summary>A reward taken from the board: pennants and, maybe, an item.</summary>
public readonly record struct MissionReward(int Pennants, string? Item);

/// <summary>
/// The Week's Work's draw (plan 41 · EG6; <c>docs/mechanics/rulings.md</c>, "The Week's Work"), GPU-free: a week's
/// missions from the regions finished, on <see cref="SplitMix"/> by the week's Monday and the data's version
/// (<see cref="GameDataFiles.Version"/>), never <see cref="Dice"/>, so the board is the same however often it is read.
/// </summary>
public static class MissionWeek
{
    private static readonly Lazy<MissionsFile> file = new(() => GameDataFiles.Load<MissionsFile>("missions.json", strict: true));

    public static MissionsFile File => file.Value;

    /// <summary>The Monday of a date's week.</summary>
    public static DateTime MondayOf(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>A week as the board keeps it: its Monday as a day of <see cref="Plaza.DayOf"/>.</summary>
    public static int WeekOf(DateTime date) => Plaza.DayOf(MondayOf(date));

    /// <summary>The week's seed, from its Monday and the data's version.</summary>
    public static uint SeedFor(DateTime date, string version)
    {
        var monday = MondayOf(date);
        return SplitMix.Mix(SplitMix.Fnv(version), (uint)(monday.Year * 10000 + monday.Month * 100 + monday.Day), 0x5745454Bu);
    }

    /// <summary>Whether a kind can be drawn in a region: its regions, and the places it needs.</summary>
    public static bool Fits(MissionKindRow row, string region)
    {
        if (row.Regions.Count > 0 && !row.Regions.Contains(region, StringComparer.OrdinalIgnoreCase)) return false;
        var places = File.Places.TryGetValue(region, out var p) ? p : null;
        return row.Kind switch
        {
            MissionKind.CatchInArea => places is { Catch.Count: > 0 },
            MissionKind.Landmark => places is { Landmarks.Count: > 0 },
            _ => true
        };
    }

    /// <summary>
    /// The week's missions: <see cref="MissionsFile.Plain"/> with no clock, then <see cref="MissionsFile.TimedCount"/> timed,
    /// each in a region of <paramref name="regions"/> (the finished ones), drawn by weight. Empty with no region finished.
    /// </summary>
    public static List<Mission> Draw(DateTime date, IReadOnlyList<string> regions, string? version = null)
    {
        var missions = new List<Mission>();
        if (regions.Count == 0) return missions;
        var rng = new SplitMix(SeedFor(date, version ?? GameDataFiles.Version));
        // Each mission in a region of its own while there are regions enough, then round again, from a region drawn once
        // (drawn afresh for each, two regions could take all five)
        int first = rng.Int(regions.Count);
        for (int n = 0; n < File.Plain + File.TimedCount; n++)
        {
            bool timed = n >= File.Plain;
            string region = regions[(first + n) % regions.Count];
            var rows = File.Kinds.Where(k => (!timed || k.Timed) && Fits(k, region)).ToList();
            if (rows.Count == 0) continue;
            var row = Weighted(rows, rng);
            var mission = new Mission { Kind = row.Kind, Region = region };
            mission.Goal = timed ? row.Least : row.Least + rng.Int(row.Most - row.Least + 1);
            var places = File.Places.TryGetValue(region, out var p) ? p : new MissionPlaces();
            if (row.Kind == MissionKind.CatchInArea) mission.Area = places.Catch[rng.Int(places.Catch.Count)];
            if (row.Kind == MissionKind.Landmark) mission.Area = places.Landmarks[rng.Int(places.Landmarks.Count)];
            if (row.Kind == MissionKind.CatchType) mission.Type = File.Types[rng.Int(File.Types.Count)];
            mission.Reward = row.PerOne * mission.Goal + row.Bonus + (timed ? File.TimedBonus : 0);
            if (timed) mission.Minutes = Math.Max(1, row.Minutes);
            if (File.Items.Count > 0 && File.ItemOneIn > 0 && rng.Int(File.ItemOneIn) == 0) mission.Item = File.Items[rng.Int(File.Items.Count)];
            missions.Add(mission);
        }
        return missions;
    }

    private static MissionKindRow Weighted(List<MissionKindRow> rows, SplitMix rng)
    {
        int total = rows.Sum(r => Math.Max(1, r.Weight));
        int at = rng.Int(total);
        foreach (var row in rows)
        {
            at -= Math.Max(1, row.Weight);
            if (at < 0) return row;
        }
        return rows[^1];
    }

    /// <summary>Whether an event counts toward a mission: its kind, its region, and its place or type where it names one.</summary>
    public static bool Counts(Mission mission, MissionEvent e)
    {
        if (!string.Equals(mission.Region, e.Region, StringComparison.OrdinalIgnoreCase)) return false;
        return mission.Kind switch
        {
            MissionKind.CatchType => e.Kind == MissionEventKind.Caught && mission.Type is { } t && (e.Type1 == t || e.Type2 == t),
            MissionKind.CatchInArea => e.Kind == MissionEventKind.Caught && e.Area == mission.Area,
            MissionKind.BeatTrainers => e.Kind is MissionEventKind.BeatTrainer or MissionEventKind.BeatRematch,
            MissionKind.WinRematch => e.Kind == MissionEventKind.BeatRematch,
            MissionKind.HatchEggs => e.Kind == MissionEventKind.Hatched,
            MissionKind.Landmark => e.Kind == MissionEventKind.Arrived && e.Area == mission.Area,
            MissionKind.WinFacility => e.Kind == MissionEventKind.FacilityWin,
            MissionKind.WinContest => e.Kind == MissionEventKind.ContestWin,
            _ => false
        };
    }

    /// <summary>The name an area is shown by: its area file's own, in the language chosen.</summary>
    public static string AreaName(string region, string area)
    {
        if (area.Length == 0) return "";
        string folder = RegionDatabase.Get(region)?.Folder ?? region.ToLowerInvariant();
        string path = $"world/{folder}/areas/{area}.json";
        string english = System.IO.File.Exists(GameDataFiles.PathOf(path)) ? GameDataFiles.Load<WorldAreaFile>(path).Name : area;
        return LocalNames.PlaceShown(area, english);
    }

    /// <summary>What a mission asks, in a line, in the language chosen.</summary>
    public static string Describe(Mission m)
    {
        string region = RegionDatabase.Get(m.Region)?.Name ?? m.Region;
        string kind = m.Kind.ToString();
        return Text.Of($"hubMissions.kind.{kind}", ("n", m.Goal), ("region", region), ("place", AreaName(m.Region, m.Area)),
            ("type", m.Type is { } t ? Words.Type(t) : ""));
    }
}

/// <summary>A reward a past week left unclaimed, waiting at the board.</summary>
public sealed class MissionWaiting
{
    public int Pennants { get; set; }
    public string? Item { get; set; }
}

/// <summary>The board as the save keeps it: <c>SaveData.Missions</c>, null until the board is first read.</summary>
public sealed class MissionsSave
{
    public int Week { get; set; }
    public List<Mission> Missions { get; set; } = new();
    public int Active { get; set; } = -1;
    public float SecondsLeft { get; set; }
    public List<MissionWaiting> Waiting { get; set; } = new();
}

/// <summary>
/// A game's Week's Work (plan 41 · EG6): the week drawn and kept, what has been done of each mission, the timed mission
/// under way and its seconds left, and the rewards of past weeks waiting to be claimed. Plain data, no window.
/// </summary>
public sealed class MissionBoard
{
    /// <summary>Whether the board has been read: until then nothing is drawn or saved.</summary>
    public bool Opened { get; private set; }

    /// <summary>The week drawn (<see cref="MissionWeek.WeekOf"/>), and its missions.</summary>
    public int Week { get; private set; } = -1;
    public List<Mission> Missions { get; } = new();

    /// <summary>The timed mission under way (its place in <see cref="Missions"/>), -1 for none, and its seconds left.</summary>
    public int Active { get; private set; } = -1;
    public float SecondsLeft { get; private set; }

    /// <summary>Past weeks' rewards for missions done and never claimed: they wait at the board.</summary>
    public List<MissionWaiting> Waiting { get; } = new();

    public Mission? Running => Active >= 0 && Active < Missions.Count ? Missions[Active] : null;

    /// <summary>
    /// The board read on a date with these regions finished: the board opened, and on a new week the week drawn anew (the
    /// last week's undone missions gone, its done and unclaimed rewards kept waiting). True when the week was drawn now.
    /// </summary>
    public bool Refresh(DateTime date, IReadOnlyList<string> regions, string? version = null)
    {
        Opened = true;
        int week = MissionWeek.WeekOf(date);
        if (week == Week && Missions.Count > 0) return false;
        foreach (var m in Missions)
            if (m.IsDone && !m.Claimed) Waiting.Add(new MissionWaiting { Pennants = m.Reward, Item = m.Item });
        Week = week;
        Missions.Clear();
        Missions.AddRange(MissionWeek.Draw(date, regions, version));
        Active = -1;
        SecondsLeft = 0;
        return true;
    }

    /// <summary>
    /// Something the engine counted: each mission it counts for goes on by one, a timed one only while it runs. Returns the
    /// missions this made done.
    /// </summary>
    public List<Mission> Tell(MissionEvent e)
    {
        var done = new List<Mission>();
        for (int k = 0; k < Missions.Count; k++)
        {
            var m = Missions[k];
            if (m.IsDone || (m.IsTimed && k != Active) || !MissionWeek.Counts(m, e)) continue;
            m.Progress++;
            if (m.IsDone)
            {
                done.Add(m);
                if (k == Active) { Active = -1; SecondsLeft = 0; }
            }
        }
        return done;
    }

    /// <summary>A timed mission begun: its progress from nought and its minutes in full. False for one with no clock, one done, or with another under way.</summary>
    public bool Begin(int index)
    {
        if (index < 0 || index >= Missions.Count || !Missions[index].IsTimed || Missions[index].IsDone || Running != null) return false;
        Active = index;
        Missions[index].Progress = 0;
        SecondsLeft = Missions[index].Minutes * 60f;
        return true;
    }

    /// <summary>
    /// The timed mission's clock: it runs for <paramref name="dt"/> of play while the player is in its region
    /// (<paramref name="region"/>) and stands still anywhere else. True on the moment its time ran out, which ends it undone.
    /// </summary>
    public bool Tick(float dt, string? region)
    {
        if (Running is not { } m || !string.Equals(m.Region, region, StringComparison.OrdinalIgnoreCase)) return false;
        SecondsLeft -= dt;
        if (SecondsLeft > 0) return false;
        SecondsLeft = 0;
        m.Progress = 0;
        Active = -1;
        return true;
    }

    /// <summary>A mission's reward taken: once, and only for one done. Null otherwise.</summary>
    public MissionReward? Claim(int index)
    {
        if (index < 0 || index >= Missions.Count || !Missions[index].IsDone || Missions[index].Claimed) return null;
        Missions[index].Claimed = true;
        return new MissionReward(Missions[index].Reward, Missions[index].Item);
    }

    /// <summary>Every reward waiting: the past weeks' and this week's done missions, each taken once.</summary>
    public List<MissionReward> ClaimAll()
    {
        var all = Waiting.Select(w => new MissionReward(w.Pennants, w.Item)).ToList();
        Waiting.Clear();
        for (int k = 0; k < Missions.Count; k++)
            if (Claim(k) is { } reward) all.Add(reward);
        return all;
    }

    /// <summary>How many rewards wait: the past weeks' and this week's done and unclaimed.</summary>
    public int Unclaimed => Waiting.Count + Missions.Count(m => m.IsDone && !m.Claimed);

    public void Clear() => Load(null);

    public void Load(MissionsSave? save)
    {
        Opened = save != null;
        Week = save?.Week ?? -1;
        Missions.Clear();
        if (save != null) Missions.AddRange(save.Missions);
        Active = save != null && save.Active >= 0 && save.Active < Missions.Count ? save.Active : -1;
        SecondsLeft = Active >= 0 ? Math.Max(0, save!.SecondsLeft) : 0;
        Waiting.Clear();
        if (save != null) Waiting.AddRange(save.Waiting);
    }

    /// <summary>The board as the save keeps it; null until it has been read.</summary>
    public MissionsSave? ToSave() => !Opened ? null : new MissionsSave
    {
        Week = Week,
        Missions = Missions.ToList(),
        Active = Active,
        SecondsLeft = SecondsLeft,
        Waiting = Waiting.ToList()
    };
}
