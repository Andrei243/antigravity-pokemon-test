using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The day's report and the visitor as the game plays them (plan 38 · EX2; rules in <see cref="FieldReport"/> and
/// <see cref="Visitors"/>): the report the Town Map turns to, the scan it asks for, the visitor's hour counted while the
/// field or a battle is on screen, and the toast as it leaves. Nothing here draws from the dice.
/// </summary>
public partial class GameEngine
{
    /// <summary>What the region the player is in has today; null before its Hall of Fame is entered.</summary>
    internal FieldReport? FieldReportNow() =>
        FieldReport.Of(Here, flow.Story, flow.Encounters, flow.Pokedex.NationalUnlocked, GameClock.Today, Swarms.PlaceToday(flow.Encounters, Here.Id)?.Area);

    /// <summary>The visitor out in the region the player is in, for the moment's grass; null where none is.</summary>
    private VisitorCall? VisitorNow() =>
        flow.Encounters.Visitor is { } visitor && string.Equals(visitor.Region, Here.Id, System.StringComparison.OrdinalIgnoreCase) ? visitor : null;

    /// <summary>
    /// The report's scan (plan 38 · EX2): the day's visitor called up where <see cref="Visitors.Check"/> allows it, and a toast
    /// that names it and its place. Returns whether one came.
    /// </summary>
    internal bool ScanForVisitor()
    {
        if (Visitors.Scan(Here, flow.Story, flow.Encounters, flow.Pokedex.NationalUnlocked, GameClock.Today.DayOfWeek, OutbreaksNow()) is not { } visitor)
            return false;
        ShowNotification(Text.Of("visitorToast.came", ("name", LocalNames.Species(visitor.Species)), ("place", FieldReport.PlaceName(visitor.Region, visitor.Area))));
        return true;
    }

    /// <summary>The visitor's hour runs down by the game's own time, and the toast tells as it leaves.</summary>
    private void TickVisitor(float dt)
    {
        if (Visitors.Tick(flow.Encounters, dt) is { } gone)
            ShowNotification(Text.Of("visitorToast.gone", ("name", LocalNames.Species(gone.Species)), ("place", FieldReport.PlaceName(gone.Region, gone.Area))));
    }
}
