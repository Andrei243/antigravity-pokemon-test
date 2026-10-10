using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.Models;

/// <summary>The Pokétch's apps, in the original's order (<c>generated/poketch_apps.txt</c>).</summary>
public enum PoketchApp
{
    DigitalWatch, Calculator, MemoPad, Pedometer, PartyStatus, FriendshipChecker, DowsingMachine, BerrySearcher,
    DayCareChecker, PokemonHistory, Counter, AnalogWatch, MarkingMap, LinkSearcher, CoinToss, MoveTester, Calendar,
    DotArt, Roulette, TrainerCounter, KitchenTimer, ColorChanger, MatchupChecker, Stopwatch, AlarmClock
}

/// <summary>
/// The Pokétch (plan 02 · S2, plan 06 · R14b): whether the player has it, the apps put on it in the order they
/// came, the one showing, and what the original keeps of it in the save (<c>include/poketch.h</c>): the
/// pedometer's count, the screen's colour, the last twelve Pokémon caught, and what an app keeps of its own (the
/// dot art, the calendar's marks, the map's markers) in <see cref="Memory"/>. The story gives it and its apps
/// (<c>poketch on</c>, <c>poketchapp</c>); the side button goes from one app to the next.
///
/// An app's state while it is on the screen is a <see cref="PoketchAppState"/>, made afresh each time the app comes
/// up (the original starts an app's task when it is chosen and ends it when another is), so a calculator's sum or
/// a kitchen timer is lost by going to another app, as in the original. No drawing or input here.
/// </summary>
public sealed class Poketch
{
    private readonly List<PoketchApp> apps = new();
    private readonly List<string> history = new();
    private int current;
    private PoketchAppState? state;

    /// <summary>The most steps the pedometer counts: five figures.</summary>
    public const int MostSteps = 99999;

    /// <summary>How many Pokémon the history keeps (<c>POKETCH_POKEMONHISTORY_MAX</c>).</summary>
    public const int HistoryLength = 12;

    /// <summary>The screen's colours (<c>PoketchScreenColor</c>): green, yellow, orange, red, purple, blue, teal, white.</summary>
    public const int ScreenColors = 8;

    public bool Enabled { get; set; }

    /// <summary>Every app the player has been given, in the order they came.</summary>
    public IReadOnlyList<PoketchApp> Apps => apps;

    /// <summary>Steps taken since the pedometer was last reset.</summary>
    public int Steps { get; private set; }

    /// <summary>The screen's colour, 0 (green) to 7 (white), set by the Color Changer.</summary>
    public int ScreenColor { get; set; }

    /// <summary>
    /// The last Pokémon caught or given, oldest first, by their model's name (a form's own name where it has one):
    /// what the Pokémon History shows (<c>Poketch_PokemonHistoryEnqueue</c>, which drops the oldest of twelve).
    /// </summary>
    public IReadOnlyList<string> History => history;

    /// <summary>What each app keeps between one time on the screen and the next, by the app's name.</summary>
    public Dictionary<string, List<int>> Memory { get; } = new();

    /// <summary>
    /// The apps this game runs: all but the Stopwatch and the Alarm Clock, which Platinum gives nobody (no script
    /// registers either; plan 06 · R14b).
    /// </summary>
    public static bool Runs(PoketchApp app) => app is not (PoketchApp.Stopwatch or PoketchApp.AlarmClock);

    /// <summary>The apps that can be shown, in the order they came.</summary>
    public IReadOnlyList<PoketchApp> Shown => apps.Where(Runs).ToList();

    /// <summary>The app on the screen; null when none that runs has been given.</summary>
    public PoketchApp? Current
    {
        get
        {
            var shown = Shown;
            return shown.Count == 0 ? null : shown[Math.Clamp(current, 0, shown.Count - 1)];
        }
    }

    /// <summary>The state of the app on the screen, made as it comes up; null with no app.</summary>
    public PoketchAppState? State
    {
        get
        {
            if (Current is not { } app) return state = null;
            if (state?.App != app)
            {
                state = PoketchAppState.Create(app);
                state.Load(this);
            }
            return state;
        }
    }

    /// <summary>Puts an app on the Pokétch; false if it was there already.</summary>
    public bool Register(PoketchApp app)
    {
        if (apps.Contains(app)) return false;
        apps.Add(app);
        return true;
    }

    public bool Has(PoketchApp app) => apps.Contains(app);

    /// <summary>The side button: the next app, round again after the last.</summary>
    public void Next()
    {
        int count = Shown.Count;
        if (count > 0) current = (current + 1) % count;
    }

    /// <summary>The app before, round again from the first.</summary>
    public void Previous()
    {
        int count = Shown.Count;
        if (count > 0) current = (current + count - 1) % count;
    }

    /// <summary>A step taken with the Pokétch on the wrist.</summary>
    public void Step()
    {
        if (Enabled) Steps = Math.Min(MostSteps, Steps + 1);
    }

    public void ResetSteps() => Steps = 0;

    /// <summary>A Pokémon caught, hatched or given: the newest in the history, the oldest dropped after twelve.</summary>
    public void Remember(Pokemon pokemon)
    {
        history.Add(pokemon.ModelName);
        if (history.Count > HistoryLength) history.RemoveAt(0);
    }

    /// <summary>The app's own memory, or null when it has none yet.</summary>
    public List<int>? Recall(PoketchApp app) => Memory.TryGetValue(app.ToString(), out var kept) ? kept : null;

    public void Keep(PoketchApp app, List<int> values) => Memory[app.ToString()] = values;

    /// <summary>Lets the app on the screen go, so it is made afresh (its own memory kept) the next time it is asked for.</summary>
    public void Close() => state = null;

    public void Clear()
    {
        Enabled = false;
        apps.Clear();
        history.Clear();
        Memory.Clear();
        current = 0;
        Steps = 0;
        ScreenColor = 0;
        state = null;
    }

    public PoketchSave Save() => new()
    {
        Enabled = Enabled,
        Apps = apps.Select(a => a.ToString()).ToList(),
        Current = current,
        Steps = Steps,
        ScreenColor = ScreenColor,
        History = history.ToList(),
        Memory = Memory.ToDictionary(e => e.Key, e => e.Value.ToList())
    };

    public void Load(PoketchSave? saved)
    {
        Clear();
        if (saved == null) return;
        Enabled = saved.Enabled;
        foreach (string name in saved.Apps)
            if (Enum.TryParse<PoketchApp>(name, out var app)) Register(app);
        current = Math.Max(0, saved.Current);
        Steps = Math.Clamp(saved.Steps, 0, MostSteps);
        ScreenColor = Math.Clamp(saved.ScreenColor, 0, ScreenColors - 1);
        if (saved.History != null) history.AddRange(saved.History.TakeLast(HistoryLength));
        if (saved.Memory != null)
            foreach (var (key, values) in saved.Memory)
                if (values != null) Memory[key] = values.ToList();
    }
}

/// <summary>The Pokétch as a save keeps it.</summary>
public sealed class PoketchSave
{
    public bool Enabled { get; set; }
    public List<string> Apps { get; set; } = new();
    public int Current { get; set; }
    public int Steps { get; set; }
    public int ScreenColor { get; set; }
    public List<string>? History { get; set; }
    public Dictionary<string, List<int>>? Memory { get; set; }
}
