using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What first brings a tip: a line of a battle, a step of the game's own in the field, or a script's <c>tip</c>.</summary>
public enum TipTrigger { Battle, Field, Script }

/// <summary>One trainer tip (plan 12 · Q23): an id, what first brings it, and its words in <c>ui.json</c>'s <c>tips</c> section.</summary>
public sealed record Tip(string Id, TipTrigger Trigger)
{
    public string Title => Text.Of($"tips.{Id}.title");
    public string Body => Text.Of($"tips.{Id}.text");
}

/// <summary>
/// The trainer tips (plan 12 · Q23; <c>docs/mechanics/rulings.md</c>, "Trainer tips"): a few words of our own on something
/// the first time the game shows it. GPU-free: which tip a battle's line brings (<see cref="ForBattle"/>), and the list the
/// tests hold to its triggers (every tip named by a trigger, every trigger naming a tip). Type matchups are left to the
/// move hints (plan 12 · Q10). The game keeps which were shown in a <see cref="TipBook"/>.
/// </summary>
public static class Tips
{
    // The tips the game's own code brings in the field (GameEngine.Tips.cs) and from a script's gift (ScriptRunner)
    public const string Ledge = "ledge", Evolution = "evolution", TeamFull = "teamFull";

    /// <summary>Every tip, in the order the TIPS page lists them.</summary>
    public static IReadOnlyList<Tip> All { get; } = new Tip[]
    {
        new("wild", TipTrigger.Battle),
        new("trainer", TipTrigger.Battle),
        new("fainted", TipTrigger.Battle),
        new("poison", TipTrigger.Battle),
        new("burn", TipTrigger.Battle),
        new("paralysis", TipTrigger.Battle),
        new("sleep", TipTrigger.Battle),
        new("freeze", TipTrigger.Battle),
        new("stats", TipTrigger.Battle),
        new("weather", TipTrigger.Battle),
        new("field", TipTrigger.Battle),
        new("caught", TipTrigger.Battle),
        new("toBox", TipTrigger.Battle),
        new("levelUp", TipTrigger.Battle),
        new("newMove", TipTrigger.Battle),
        new(Evolution, TipTrigger.Field),
        new(Ledge, TipTrigger.Field),
        new(TeamFull, TipTrigger.Field),
        new("nurse", TipTrigger.Script),
        new("pc", TipTrigger.Script),
        new("clerk", TipTrigger.Script),
        new("cutTree", TipTrigger.Script),
        new("rockSmash", TipTrigger.Script),
        new("strength", TipTrigger.Script),
        new("surf", TipTrigger.Script),
        new("rockClimb", TipTrigger.Script),
        new("waterfall", TipTrigger.Script),
    };

    private static readonly Dictionary<string, Tip> ById = All.ToDictionary(t => t.Id, StringComparer.Ordinal);

    public static Tip? Find(string id) => ById.GetValueOrDefault(id);

    /// <summary>
    /// The tip a line of a battle brings, as <c>BattleEngine.Show</c> meets it: a wild Pokémon coming out, a trainer standing,
    /// one of the player's fainting, each status condition, a stat's stage, a weather begun, something laid over a side, a
    /// catch (and one sent to the PC), a level, a fifth move wanted. Null for every other line.
    /// </summary>
    public static string? ForBattle(BattleEvent happening) => happening switch
    {
        Entered { Place.Side: BattleSide.Enemy, FromBall: false } => "wild",
        TrainersStand => "trainer",
        Fainted { Place.Side: BattleSide.Player } => "fainted",
        StatusChanged { Status: StatusCondition.Poison or StatusCondition.Toxic } => "poison",
        StatusChanged { Status: StatusCondition.Burn } => "burn",
        StatusChanged { Status: StatusCondition.Paralyze } => "paralysis",
        StatusChanged { Status: StatusCondition.Sleep } => "sleep",
        StatusChanged { Status: StatusCondition.Freeze } => "freeze",
        StageChanged => "stats",
        WeatherChanged { Weather: not BattleWeather.None } => "weather",
        SideConditionChanged { Layers: > 0 } => "field",
        Caught { ToBox: true } => "toBox",
        Caught => "caught",
        LevelRose => "levelUp",
        MoveWanted => "newMove",
        _ => null
    };
}

/// <summary>
/// Which tips a game has shown and which wait for a quiet moment (plan 12 · Q23). A tip is raised once: raised again
/// while it waits, or after it was shown, it is passed over. Only the shown are saved (<see cref="SaveData.TipsSeen"/>).
/// </summary>
public sealed class TipBook
{
    private readonly HashSet<string> seen = new(StringComparer.Ordinal);
    private readonly List<string> waiting = new();

    /// <summary>The tips shown, in the order <see cref="Tips.All"/> lists them.</summary>
    public IReadOnlyList<Tip> Seen => Tips.All.Where(t => seen.Contains(t.Id)).ToList();

    public bool HasSeen(string id) => seen.Contains(id);

    /// <summary>The tips raised and not yet shown, the first raised first.</summary>
    public IReadOnlyList<string> Waiting => waiting;

    /// <summary>Raises a tip: true if it now waits to be shown, false if it was shown already, waits already or is no tip.</summary>
    public bool Raise(string id)
    {
        if (Tips.Find(id) == null || seen.Contains(id) || waiting.Contains(id)) return false;
        waiting.Add(id);
        return true;
    }

    /// <summary>The next tip waiting, now shown: it is seen from here on. Null when none waits.</summary>
    public Tip? TakeNext()
    {
        if (waiting.Count == 0) return null;
        string id = waiting[0];
        waiting.RemoveAt(0);
        seen.Add(id);
        return Tips.Find(id);
    }

    /// <summary>Forgets what waits, keeping what was shown (a battle watched, a game left).</summary>
    public void DropWaiting() => waiting.Clear();

    public void Clear()
    {
        seen.Clear();
        waiting.Clear();
    }

    /// <summary>A save's shown tips, as they were kept; an id that is no tip any more is passed over.</summary>
    public void Load(IEnumerable<string>? ids)
    {
        Clear();
        if (ids == null) return;
        foreach (string id in ids)
            if (Tips.Find(id) != null) seen.Add(id);
    }

    /// <summary>The shown tips for the save, in the list's order; null when none is.</summary>
    public List<string>? ToSave() => seen.Count == 0 ? null : Tips.All.Where(t => seen.Contains(t.Id)).Select(t => t.Id).ToList();
}
