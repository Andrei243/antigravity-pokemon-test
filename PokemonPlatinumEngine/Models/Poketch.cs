using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>The Pokétch's apps, in the original's order (<c>generated/poketch_apps.txt</c>).</summary>
public enum PoketchApp
{
    DigitalWatch, Calculator, MemoPad, Pedometer, PartyStatus, FriendshipChecker, DowsingMachine, BerrySearcher,
    DayCareChecker, PokemonHistory, Counter, AnalogWatch, MarkingMap, LinkSearcher, CoinToss, MoveTester, Calendar,
    DotArt, Roulette, TrainerCounter, KitchenTimer, ColorChanger, MatchupChecker, Stopwatch, AlarmClock
}

/// <summary>
/// The Pokétch (plan 02 · S2): whether the player has it, the apps put on it in the order they came, the one
/// showing, and the pedometer's count. Saved with the game. The story gives it and its apps (<c>poketch on</c>,
/// <c>poketchapp</c>); the side button goes from one app to the next. Only the apps this game runs so far are shown
/// (<see cref="Runs"/>); the others are kept for the session that builds them.
/// </summary>
public sealed class Poketch
{
    private readonly List<PoketchApp> apps = new();
    private int current;

    /// <summary>The most steps the pedometer counts: five figures.</summary>
    public const int MostSteps = 99999;

    public bool Enabled { get; set; }

    /// <summary>Every app the player has been given, in the order they came.</summary>
    public IReadOnlyList<PoketchApp> Apps => apps;

    /// <summary>Steps taken since the pedometer was last reset.</summary>
    public int Steps { get; private set; }

    /// <summary>The apps this game runs so far: the clock, the pedometer and the team (S2's "clock and party apps first").</summary>
    public static bool Runs(PoketchApp app) => app is PoketchApp.DigitalWatch or PoketchApp.Pedometer or PoketchApp.PartyStatus;

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

    /// <summary>Puts an app on the Pokétch; false if it was there already.</summary>
    public bool Register(PoketchApp app)
    {
        if (apps.Contains(app)) return false;
        apps.Add(app);
        return true;
    }

    /// <summary>The side button: the next app, round again after the last.</summary>
    public void Next()
    {
        int count = Shown.Count;
        if (count > 0) current = (current + 1) % count;
    }

    /// <summary>A step taken with the Pokétch on the wrist.</summary>
    public void Step()
    {
        if (Enabled) Steps = Math.Min(MostSteps, Steps + 1);
    }

    public void ResetSteps() => Steps = 0;

    public void Clear()
    {
        Enabled = false;
        apps.Clear();
        current = 0;
        Steps = 0;
    }

    public PoketchSave Save() => new() { Enabled = Enabled, Apps = apps.Select(a => a.ToString()).ToList(), Current = current, Steps = Steps };

    public void Load(PoketchSave? saved)
    {
        Clear();
        if (saved == null) return;
        Enabled = saved.Enabled;
        foreach (string name in saved.Apps)
            if (Enum.TryParse<PoketchApp>(name, out var app)) Register(app);
        current = Math.Max(0, saved.Current);
        Steps = Math.Clamp(saved.Steps, 0, MostSteps);
    }
}

/// <summary>The Pokétch as a save keeps it.</summary>
public sealed class PoketchSave
{
    public bool Enabled { get; set; }
    public List<string> Apps { get; set; } = new();
    public int Current { get; set; }
    public int Steps { get; set; }
}
