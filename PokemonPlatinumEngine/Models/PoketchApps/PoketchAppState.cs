using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// Something on the Pokétch's screen that can be touched, in the LCD's blocks: the screen is
/// <see cref="PoketchAppState.Columns"/> by <see cref="PoketchAppState.Rows"/> blocks of 8 (style guide, "The
/// Pokétch"). <paramref name="Id"/> is the app's own number for it, handed back by <see cref="PoketchAppState.Press"/>.
/// </summary>
public readonly record struct PoketchButton(int Id, int X, int Y, int W, int H)
{
    public float CentreX => X + W / 2f;
    public float CentreY => Y + H / 2f;
}

/// <summary>
/// What an app may read of the game and do to it, filled by the engine each frame (and by tests as they please).
/// Everything may be missing but <see cref="Poketch"/>, <see cref="Party"/> and <see cref="Rng"/>: an app shows what
/// it can without the rest.
/// </summary>
public sealed class PoketchContext
{
    public required Poketch Poketch { get; init; }
    public Party Party { get; set; } = new();

    /// <summary>The time the watch shows (the computer's clock, or a fixed day for tools).</summary>
    public DateTime Now { get; set; } = new(2009, 3, 22, 12, 0, 0);

    /// <summary>The map the player is on, and the tile they stand on.</summary>
    public Map? Map { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>Off the map of Sinnoh, the tile of it the player last went in from (a cave's mouth, a door); null on it or before any.</summary>
    public (int X, int Y)? Outside { get; set; }

    public StoryState? Story { get; set; }
    public Pokedex? Pokedex { get; set; }

    /// <summary>The roamers, the swarms and the rest of the wild Pokémon beyond the tables.</summary>
    public SpecialEncounters? Encounters { get; set; }

    /// <summary>The Poké Radar's chain.</summary>
    public RadarChain? Radar { get; set; }

    /// <summary>Sinnoh's berry patches.</summary>
    public BerryPatches? Berries { get; set; }

    /// <summary>The Pokémon left at the Day Care, none to two (breeding is plan 06 · R15; empty until then).</summary>
    public IReadOnlyList<Pokemon> DayCare { get; set; } = Array.Empty<Pokemon>();

    /// <summary>Whatever an app leaves to chance (a coin, a roulette): the field's generator.</summary>
    public Random Rng { get; set; } = new(0);

    /// <summary>Plays a sound of the bank by name (<c>docs/sound-effects.md</c>); nothing in tests unless they listen.</summary>
    public Action<string> Sound { get; set; } = _ => { };

    /// <summary>Plays a Pokémon's cry.</summary>
    public Action<Pokemon> Cry { get; set; } = _ => { };
}

/// <summary>
/// An app while it is on the Pokétch's screen (plan 06 · R14b): its buttons, what touching one does, and what
/// moves by itself as time passes. One class per app in this folder, each from the original's
/// <c>src/applications/poketch/&lt;app&gt;/main.c</c>; drawing is <c>UI/ModernUi.Poketch*.cs</c>'s. No drawing or
/// input here, so tests touch an app with <see cref="Press"/>.
/// </summary>
public abstract class PoketchAppState
{
    /// <summary>The screen in blocks: 360 by 296 at 8 to the block.</summary>
    public const int Columns = 45, Rows = 37;

    public abstract PoketchApp App { get; }

    /// <summary>The Pokétch the app is on, for its memory (<see cref="Poketch.Recall"/>, <see cref="Poketch.Keep"/>).</summary>
    protected Poketch Owner { get; private set; } = null!;

    /// <summary>What can be touched now; none for an app that only shows.</summary>
    public virtual IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => Array.Empty<PoketchButton>();

    /// <summary>A button touched (its <see cref="PoketchButton.Id"/>).</summary>
    public virtual void Press(int button, PoketchContext context) { }

    /// <summary>Time passing while the app is on the screen (or put away with the Pokétch: it runs on, as the original's does).</summary>
    public virtual void Update(float dt, PoketchContext context) { }

    /// <summary>Called once as the app is made: reads what it keeps of its own from the Pokétch.</summary>
    protected virtual void Opened() { }

    internal void Load(Poketch owner)
    {
        Owner = owner;
        Opened();
    }

    /// <summary>A fresh state of an app.</summary>
    public static PoketchAppState Create(PoketchApp app) => app switch
    {
        PoketchApp.DigitalWatch => new DigitalWatchApp(),
        PoketchApp.Calculator => new CalculatorApp(),
        PoketchApp.MemoPad => new MemoPadApp(),
        PoketchApp.Pedometer => new PedometerApp(),
        PoketchApp.PartyStatus => new PartyStatusApp(),
        PoketchApp.FriendshipChecker => new FriendshipCheckerApp(),
        PoketchApp.DowsingMachine => new DowsingMachineApp(),
        PoketchApp.BerrySearcher => new BerrySearcherApp(),
        PoketchApp.DayCareChecker => new DayCareCheckerApp(),
        PoketchApp.PokemonHistory => new PokemonHistoryApp(),
        PoketchApp.Counter => new CounterApp(),
        PoketchApp.AnalogWatch => new AnalogWatchApp(),
        PoketchApp.MarkingMap => new MarkingMapApp(),
        PoketchApp.LinkSearcher => new LinkSearcherApp(),
        PoketchApp.CoinToss => new CoinTossApp(),
        PoketchApp.MoveTester => new MoveTesterApp(),
        PoketchApp.Calendar => new CalendarApp(),
        PoketchApp.DotArt => new DotArtApp(),
        PoketchApp.Roulette => new RouletteApp(),
        PoketchApp.TrainerCounter => new TrainerCounterApp(),
        PoketchApp.KitchenTimer => new KitchenTimerApp(),
        PoketchApp.ColorChanger => new ColorChangerApp(),
        PoketchApp.MatchupChecker => new MatchupCheckerApp(),
        _ => new UnavailableApp(app)
    };
}

/// <summary>An app Platinum gives nobody (the Stopwatch, the Alarm Clock): kept in the list, never shown.</summary>
public sealed class UnavailableApp(PoketchApp app) : PoketchAppState
{
    public override PoketchApp App => app;
}
