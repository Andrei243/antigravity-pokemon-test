using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The day's outbreaks as the game plays them (plan 38 · EX1; rules in <see cref="Overworld.Outbreaks"/>): what the region has
/// today, whether the player stands in the place of one, the count of the species beaten or caught there and the shiny
/// rolls it earns. Nothing here draws from the dice: the day's outbreaks are worked out from the day's number, and a roll is
/// made only for a Pokémon of the outbreak's species in its place once thirty have been met.
/// </summary>
public partial class GameEngine
{
    /// <summary>The day's outbreaks in the region the player is in; null before its Hall of Fame is entered.</summary>
    private IReadOnlyList<Outbreak>? OutbreaksNow()
    {
        if (!Overworld.Outbreaks.Open(Here, flow.Story)) return null;
        return Overworld.Outbreaks.Today(Here.Id, flow.Encounters.DailyNumber, Swarms.PlaceToday(flow.Encounters, Here.Id)?.Area);
    }

    /// <summary>The day's outbreak in the place the player stands in; null where there is none.</summary>
    internal Outbreak? OutbreakHere() => Overworld.Outbreaks.At(OutbreaksNow(), flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key);

    /// <summary>
    /// The shiny rolls a Pokémon of this species met in this place is given beyond its own: those of the count of the
    /// outbreak's species beaten or caught there today, and none for anything else.
    /// </summary>
    internal int OutbreakRolls(MapArea? area, string species)
    {
        if (area == null || OutbreaksNow() is not { } day || Overworld.Outbreaks.At(day, area.Key) is not { } outbreak || outbreak.Species != species) return 0;
        return Overworld.Outbreaks.ExtraRolls(flow.Encounters.OutbreakMetIn(area.Key));
    }

    /// <summary>
    /// A wild battle met in the field is won or the Pokémon caught: if it is of the day's outbreak's species in the outbreak's
    /// place it is counted, and the count that earns a roll more is told.
    /// </summary>
    private void CountOutbreak(Pokemon foe) => CountOutbreakIn(foe, flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key);

    /// <summary><see cref="CountOutbreak"/> for a Pokémon beaten or caught in the place with this key.</summary>
    internal void CountOutbreakIn(Pokemon foe, string? area)
    {
        if (Overworld.Outbreaks.At(OutbreaksNow(), area) is not { } outbreak || foe.Species.Name != outbreak.Species) return;
        int met = flow.Encounters.CountOutbreak(outbreak.Area);
        if (met != Overworld.Outbreaks.FirstBonusAt && met != Overworld.Outbreaks.SecondBonusAt) return;
        ShowNotification(Text.Of(met == Overworld.Outbreaks.FirstBonusAt ? "outbreakToast.bonusOne" : "outbreakToast.bonusTwo",
            ("name", foe.Species.DisplayName), ("count", met), ("place", flow.Map.ShownNameAt(flow.Player.GridX, flow.Player.GridY))));
    }
}
