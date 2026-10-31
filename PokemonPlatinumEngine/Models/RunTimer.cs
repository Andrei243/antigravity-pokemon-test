using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The options' RUN TIMER row (plan 13 · V10): the corner hidden, or shown with the splits of one category. Every split
/// is taken whatever the row says; the category only chooses which of them the corner and <c>splits.json</c> count.
/// </summary>
public enum TimerCategory
{
    /// <summary>No corner and no file (the default).</summary>
    Off,
    /// <summary>The Hall of Fame alone: the time from the first step to the credits.</summary>
    HallOfFame,
    /// <summary>Each Badge and the Hall of Fame.</summary>
    Badges,
    /// <summary>Each Badge, the story's turns (each of Team Galactic's commanders beaten, Spear Pillar) and the Hall of Fame.</summary>
    Story
}

/// <summary>What a split stands for.</summary>
public enum SplitKind { Badge, Story, HallOfFame }

/// <summary>
/// A moment a split is taken at: a region's Badge (<see cref="Key"/> its name in the region's list), a story flag of
/// the region (<see cref="Key"/> the flag) or the region's Hall of Fame. <see cref="Id"/> is what the save and
/// <c>splits.json</c> keep: <c>badge.sinnoh.Coal</c>, <c>story.sinnoh.headquarters</c>, <c>hof.sinnoh</c>.
/// </summary>
public sealed record SplitPoint(string Id, SplitKind Kind, string Region, string Key, string? Flag = null)
{
    /// <summary>Whether the story has reached it. Only badges and flags that are never taken back are asked.</summary>
    public bool Reached(StoryState story) => Kind switch
    {
        SplitKind.Badge => story.HasBadge(Region, Key),
        SplitKind.HallOfFame => story.IsRegionComplete(Region),
        _ => story.Has(Flag!)
    };

    /// <summary>Whether a category counts it.</summary>
    public bool CountsIn(TimerCategory category) => category switch
    {
        TimerCategory.HallOfFame => Kind == SplitKind.HallOfFame,
        TimerCategory.Badges => Kind != SplitKind.Story,
        TimerCategory.Story => true,
        _ => false
    };

    /// <summary>Its name in the language chosen: the Badge's, the story's turn's, or the region's Hall of Fame.</summary>
    public string Shown => Kind switch
    {
        SplitKind.Badge => Text.Of("timer.split.badge", ("badge", Words.Badge(Key))),
        SplitKind.HallOfFame => Text.Of("timer.split.hallOfFame", ("region", RegionName(Region))),
        _ => Text.Of($"timer.split.{Key}")
    };

    private static string RegionName(string id) =>
        RegionDatabase.Get(id) is { } r ? Text.Of($"dexText.region.{r.Generation}") : id;
}

/// <summary>One split as it was taken: the point's id, and the real time and the game's time the timer read then.</summary>
public sealed class TimerSplit
{
    public string Id { get; set; } = "";
    public double RealSeconds { get; set; }
    public double GameSeconds { get; set; }
}

/// <summary>
/// The run timer (plan 13 · V10; GPU-free, kept in <see cref="RunState.Timer"/>). Two times side by side: the real time,
/// what the wall clock (<see cref="WallClock.UtcNow"/>, never the system's) has moved on by between the frames the timer
/// runs in, and the game's own time, the sum of the frames' <c>dt</c>. Neither runs on the title, in the options or
/// while the game waits behind another window (<see cref="Pause"/> forgets the last reading, so the gap is never
/// counted). Splits are taken by themselves, by <see cref="Take"/>, at every Badge, at the story's turns and at each
/// region's Hall of Fame; the first Hall of Fame finishes the run, and the corner then holds the final time.
/// </summary>
public sealed class RunTimer
{
    /// <summary>The longest step of real time one frame may add: a machine that slept, or a clock set forward, adds no more.</summary>
    public const double LongestStep = 10.0;

    public double RealSeconds { get; set; }
    public double GameSeconds { get; set; }

    /// <summary>The splits taken, in the order they were.</summary>
    public List<TimerSplit> Splits { get; set; } = new();

    /// <summary>The points the story had already reached when the timer began (a save from before the timer): never taken.</summary>
    public List<string> Before { get; set; } = new();

    /// <summary>The timer began on a game already under way (a save from before it), so its times count from there.</summary>
    public bool BegunLate { get; set; }

    private DateTime? lastReading;

    /// <summary>The run is over: its first Hall of Fame has been entered.</summary>
    [JsonIgnore]
    public TimerSplit? Finish => Splits.FirstOrDefault(s => s.Id.StartsWith("hof.", StringComparison.Ordinal));

    /// <summary>
    /// A frame of the run: the game's time moves on by <paramref name="dt"/> and the real time by what the wall clock
    /// moved on since the last frame (nothing on the first frame after a pause, and nothing when the clock went back).
    /// </summary>
    public void Tick(float dt, DateTime utcNow)
    {
        if (dt > 0) GameSeconds += dt;
        if (lastReading is { } last)
        {
            double step = (utcNow - last).TotalSeconds;
            if (step > 0) RealSeconds += Math.Min(step, LongestStep);
        }
        lastReading = utcNow;
    }

    /// <summary>The timer stops here: the next <see cref="Tick"/> begins again from its own reading.</summary>
    public void Pause() => lastReading = null;

    /// <summary>
    /// Takes a split at every point the story has reached that has none yet, at the times the timer reads now, and
    /// gives back those it took.
    /// </summary>
    public IReadOnlyList<SplitPoint> Take(StoryState story)
    {
        List<SplitPoint>? taken = null;
        foreach (var point in Points)
        {
            if (Has(point.Id) || !point.Reached(story)) continue;
            Splits.Add(new TimerSplit { Id = point.Id, RealSeconds = RealSeconds, GameSeconds = GameSeconds });
            (taken ??= new()).Add(point);
        }
        return (IReadOnlyList<SplitPoint>?)taken ?? Array.Empty<SplitPoint>();
    }

    /// <summary>Notes, without a time, every point the story has already reached: a timer begun on a game under way.</summary>
    public void Know(StoryState story)
    {
        foreach (var point in Points)
            if (!Has(point.Id) && point.Reached(story)) Before.Add(point.Id);
    }

    private bool Has(string id) => Before.Contains(id) || Splits.Any(s => s.Id == id);

    /// <summary>The splits a category counts, in the order they were taken.</summary>
    public IEnumerable<(SplitPoint Point, TimerSplit Split)> In(TimerCategory category) =>
        Splits.Select(s => (Point: PointOf(s.Id), Split: s)).Where(p => p.Point != null && p.Point.CountsIn(category))!;

    /// <summary>The last split a category counts; null before the first.</summary>
    public (SplitPoint Point, TimerSplit Split)? Last(TimerCategory category) =>
        In(category).Select(p => ((SplitPoint, TimerSplit)?)p).LastOrDefault();

    /// <summary>A time as the corner writes it: <c>1:02:03.4</c>, minutes and seconds alone under the hour (<c>12:03.4</c>).</summary>
    public static string Format(double seconds, bool tenths = true)
    {
        if (seconds < 0 || double.IsNaN(seconds)) seconds = 0;
        long whole = (long)Math.Floor(seconds);
        long h = whole / 3600, m = whole / 60 % 60, s = whole % 60;
        string text = h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m}:{s:D2}";
        return tenths ? $"{text}.{(int)Math.Floor((seconds - whole) * 10)}" : text;
    }

    // ------------------------------------------------------------------ the points

    /// <summary>The story's turns of Sinnoh a split is taken at (the Story category's), by their names and flags.</summary>
    public static IReadOnlyList<(string Name, string Flag)> SinnohStory { get; } = new[]
    {
        ("windworks", "FLAG_HIDE_VALLEY_WINDWORKS_BUILDING_TEAM_GALACTIC"),
        ("eternaBuilding", "FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING"),
        ("valorCavern", "FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN"),
        ("lakeVerity", "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY"),
        ("headquarters", "FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN"),
        ("spearPillar", "FLAG_SPEAR_PILLAR_IS_DISTORTED")
    };

    private static IReadOnlyList<SplitPoint>? points;

    /// <summary>Every point a split may be taken at: each region's Badges in its card's order, Sinnoh's turns, each region's Hall of Fame.</summary>
    public static IReadOnlyList<SplitPoint> Points => points ??= MakePoints();

    private static IReadOnlyList<SplitPoint> MakePoints()
    {
        var list = new List<SplitPoint>();
        foreach (var region in RegionDatabase.All)
        {
            string key = region.Id.ToLowerInvariant();
            foreach (var badge in region.BadgeNames)
                list.Add(new($"badge.{key}.{badge}", SplitKind.Badge, region.Id, badge));
            if (region.Id == RegionDatabase.Sinnoh)
                foreach (var (name, flag) in SinnohStory)
                    list.Add(new($"story.{key}.{name}", SplitKind.Story, region.Id, name, flag));
            list.Add(new(HallOfFameId(region.Id), SplitKind.HallOfFame, region.Id, region.Id));
        }
        return list;
    }

    /// <summary>The id of a region's Hall of Fame split: <c>hof.sinnoh</c>.</summary>
    public static string HallOfFameId(string region) => $"hof.{region.ToLowerInvariant()}";

    private static Dictionary<string, SplitPoint>? byId;

    /// <summary>A point by its id; null for one there is none of (a save of a later game).</summary>
    public static SplitPoint? PointOf(string id) => (byId ??= Points.ToDictionary(p => p.Id, StringComparer.Ordinal)).GetValueOrDefault(id);
}

/// <summary>
/// <c>splits.json</c> (plan 13 · V10; <see cref="SavePaths.Splits"/>): the run as a spreadsheet or an autosplitter reads
/// it. Plain fields, times in seconds to the thousandth; the splits are the category's, in order, each with its
/// segment (the time since the split before it). Written whole each time, never read back by the game.
/// </summary>
public sealed class SplitsFile
{
    public int Format { get; set; } = 1;
    public string Category { get; set; } = "";
    public int Slot { get; set; }
    public string Trainer { get; set; } = "";
    public bool BegunLate { get; set; }
    public bool Finished { get; set; }
    public double RealSeconds { get; set; }
    public double GameSeconds { get; set; }
    public List<SplitsFileRow> Splits { get; set; } = new();

    /// <summary>The file of a timer under a category.</summary>
    public static SplitsFile Of(RunTimer timer, TimerCategory category, int slot, string trainer)
    {
        var file = new SplitsFile
        {
            Category = category.ToString(), Slot = slot, Trainer = trainer, BegunLate = timer.BegunLate,
            Finished = timer.Finish != null,
            RealSeconds = Round(timer.Finish?.RealSeconds ?? timer.RealSeconds),
            GameSeconds = Round(timer.Finish?.GameSeconds ?? timer.GameSeconds)
        };
        double realBefore = 0, gameBefore = 0;
        foreach (var (point, split) in timer.In(category))
        {
            file.Splits.Add(new SplitsFileRow
            {
                Id = point.Id, Name = point.Shown,
                RealSeconds = Round(split.RealSeconds), GameSeconds = Round(split.GameSeconds),
                RealSegment = Round(split.RealSeconds - realBefore), GameSegment = Round(split.GameSeconds - gameBefore)
            });
            realBefore = split.RealSeconds;
            gameBefore = split.GameSeconds;
        }
        return file;
    }

    private static double Round(double seconds) => Math.Round(seconds, 3);

    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string ToJson() => JsonSerializer.Serialize(this, Json);
}

/// <summary>One split as <c>splits.json</c> writes it.</summary>
public sealed class SplitsFileRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double RealSeconds { get; set; }
    public double GameSeconds { get; set; }
    public double RealSegment { get; set; }
    public double GameSegment { get; set; }
}
