using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>The options' STREAM FEED row (plan 13 · V24): nothing written, a file for an OBS browser source, or that and a LiveSplit splits file.</summary>
public enum StreamFeedMode { Off, File, FileAndSplits }

/// <summary>A Pokémon in the feed: who it is and how it stands. No ID, no personality, no trainer.</summary>
public sealed record FeedMon(string Species, string? Form, string? Nickname, int Level, int Hp, int MaxHp, string Status, bool Shiny);

/// <summary>A fallen Pokémon of a Nuzlocke run's graveyard, and where it fell.</summary>
public sealed record FeedFallen(string Species, string? Nickname, int Level, string Place);

/// <summary>A split taken, as the feed tells it (times in seconds to the thousandth).</summary>
public sealed record FeedSplit(string Id, string Name, double RealSeconds, double GameSeconds);

/// <summary>The run timer as the feed tells it: the time at the moment of writing, the last split of the chosen category and whether the run is finished.</summary>
public sealed record FeedTimer(double RealSeconds, double GameSeconds, bool Finished, FeedSplit? Last);

/// <summary>The battle as the screen shows it: the two sides' Pokémon on the field.</summary>
public sealed record FeedBattle(string Kind, string? Foe, IReadOnlyList<FeedMon> Player, IReadOnlyList<FeedMon> Enemy);

/// <summary>The run's rules: its preset's key and the names of the rules that are on.</summary>
public sealed record FeedRules(string Preset, IReadOnlyList<string> On);

/// <summary>
/// What <c>stream/state.json</c> holds (plan 13 · V24; its shape is written out in <c>docs/stream-feed.md</c>): the party,
/// the place, the badges, the run's rules, the graveyard and areas met, the timer and in a battle both sides. Plain values
/// only. It never carries a trainer ID, a friend code or a path, whatever the options say.
/// </summary>
public sealed record FeedState
{
    public int Format { get; init; } = LiveFeed.CurrentFormat;
    public string Game { get; init; } = LiveFeed.GameName;
    /// <summary>When it was written (UTC, <see cref="WallClock"/>); left out of what decides whether anything changed.</summary>
    public string? Updated { get; init; }
    public string Place { get; init; } = "";
    public string Region { get; init; } = "";
    public IReadOnlyList<string> Badges { get; init; } = Array.Empty<string>();
    public FeedRules Rules { get; init; } = new("normal", Array.Empty<string>());
    public IReadOnlyList<FeedMon> Party { get; init; } = Array.Empty<FeedMon>();
    public IReadOnlyList<FeedFallen> Graveyard { get; init; } = Array.Empty<FeedFallen>();
    public int AreasMet { get; init; }
    public FeedTimer? Timer { get; init; }
    public FeedBattle? Battle { get; init; }
}

/// <summary>
/// The live feed (plan 13 · V24; rulings, "The live feed, the overlay and the splits file"): a JSON file of what the run is
/// doing, <c>stream/state.json</c> beside the save, for an OBS browser source to read (<c>stream/overlay.html</c>, our own,
/// copied beside it), and a LiveSplit splits file (<see cref="LiveSplitFile"/>). All of it is files: no socket is opened,
/// nothing is sent, and nothing at all is written with the row off. A write is whole (to a file beside it, moved over the old
/// one) so a reader never sees half a file; it happens when something the feed holds has changed, at most four times a
/// second of the game's own time, and once a second while the timer runs and its corner is shown. Nothing here draws from
/// chance, reads the system's clock but through <see cref="WallClock"/>, or touches a frame's result. GPU-free.
/// </summary>
public sealed class LiveFeed
{
    public const int CurrentFormat = 1;
    public const string GameName = "Pokémon Platinum (a fan remake)";

    /// <summary>The least game time between two writes, and how often the running timer refreshes the file.</summary>
    public const double MinGap = 0.25, TimerRefresh = 1.0;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // A name with an accent is written as it is: the file is UTF-8 and a reader of it is a browser
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private string? lastKey;
    private double lastWrite = double.NegativeInfinity;

    /// <summary>How many times the file was written (for whoever checks the rate).</summary>
    public int Writes { get; private set; }

    public static string ToJson(FeedState state) => JsonSerializer.Serialize(state, Json);

    /// <summary>What decides whether anything has changed: the state without the moment it was written and without the timer's running seconds.</summary>
    public static string ChangeKey(FeedState state) =>
        ToJson(state with { Updated = null, Timer = state.Timer == null ? null : state.Timer with { RealSeconds = 0, GameSeconds = 0 } });

    /// <summary>
    /// Offers the state at <paramref name="now"/> (the game's own seconds): written to <paramref name="path"/> if something it
    /// holds has changed since the last write and the gap has passed, or if the timer is shown and runs. True when it wrote.
    /// </summary>
    public bool Offer(FeedState state, double now, string path, bool timerRuns)
    {
        if (now - lastWrite < MinGap) return false;
        string key = ChangeKey(state);
        bool changed = key != lastKey;
        if (!changed && !(timerRuns && now - lastWrite >= TimerRefresh)) return false;
        if (!WriteWhole(path, ToJson(state))) { lastWrite = now; return false; }
        lastKey = key;
        lastWrite = now;
        Writes++;
        return true;
    }

    /// <summary>Forgets what was written: the next offer writes (a game begun or loaded, the row switched on).</summary>
    public void Reset()
    {
        lastKey = null;
        lastWrite = double.NegativeInfinity;
    }

    /// <summary>Writes a file whole: to a file beside it first and then over the old one. False, told once in the log, where it can't be written.</summary>
    public static bool WriteWhole(string path, string text)
    {
        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
            string temp = path + ".tmp";
            File.WriteAllText(temp, text, new System.Text.UTF8Encoding(false));
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!warned) Log.Warn($"The stream feed couldn't be written to {path}: {e.Message}");
            warned = true;
            return false;
        }
    }

    private static bool warned;

    // ------------------------------------------------------------------ making the state

    /// <summary>A Pokémon as the feed tells it.</summary>
    public static FeedMon Mon(Pokemon p) =>
        new(p.Species.Name, p.Form, string.IsNullOrWhiteSpace(p.Nickname) || p.Nickname == p.Species.Name ? null : p.Nickname, p.Level,
            Math.Max(0, p.CurrentHP), p.MaxHP, p.IsEgg ? "egg" : p.Status.ToString(), p.IsShiny);

    /// <summary>The names of the rules a run has on, in the order <see cref="RunRules.RuleNames"/> lists them.</summary>
    public static FeedRules RulesOf(RunRules rules) =>
        new(rules.PresetKey, RunRules.RuleNames.Where(rules.Has).ToList());

    /// <summary>The timer as the feed tells it; null where the game has none (a save from before it is given one as it loads).</summary>
    public static FeedTimer? TimerOf(RunTimer? timer, TimerCategory category)
    {
        if (timer == null) return null;
        var finish = timer.Finish;
        var last = timer.Last(category == TimerCategory.Off ? TimerCategory.Badges : category);
        return new FeedTimer(Math.Round(finish?.RealSeconds ?? timer.RealSeconds, 3), Math.Round(finish?.GameSeconds ?? timer.GameSeconds, 3), finish != null,
            last is { } l ? new FeedSplit(l.Point.Id, l.Point.Shown, Math.Round(l.Split.RealSeconds, 3), Math.Round(l.Split.GameSeconds, 3)) : null);
    }

    /// <summary>The graveyard as the feed tells it.</summary>
    public static IReadOnlyList<FeedFallen> GraveyardOf(RunState state) =>
        state.Graveyard.Select(f => new FeedFallen(f.Pokemon.SpeciesName, string.IsNullOrWhiteSpace(f.Pokemon.Nickname) ? null : f.Pokemon.Nickname, f.Pokemon.Level, f.Place)).ToList();
}
