using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The PokéNav in the game (plan 21 · H2, part b): where the rules of <see cref="PokeNav"/> meet the field. The start
/// menu opens its screen; the map page opens the map of Hoenn and comes back to it; a call made from Match Call plays
/// the contact's script of <c>phone.txt</c> over the field, as a Pokégear call does.
/// </summary>
public partial class GameEngine
{
    // Whether the map of the region was opened from the PokéNav, which it goes back to
    private bool townMapFromPokeNav;

    /// <summary>Whether the PokéNav is the player's where they are.</summary>
    private bool HasPokeNav => PokeNav.Has(flow.Story, Here.Id);

    private void OpenPokeNav(PokeNavPage? on = null)
    {
        var screen = flow.PokeNavScreen;
        screen.RegionName = Here.Id;
        screen.PlaceOf = area => MapDatabase.MapNames.Select(MapDatabase.Get).SelectMany(m => m.Areas).FirstOrDefault(a => a.Key == area)?.DisplayName is { Length: > 0 } name
            ? PlayerIdentity.Fill(name) : "";
        screen.PlayerCell = RegionMapNow is { } region ? RegionCellNow(region) : null;
        screen.PlaceHere = PlaceName();
        var team = flow.Party.Members.Where(p => !p.IsEgg).ToList();
        screen.Open(PokeNav.Pages(flow.Story, team), PokeNav.Contacts(flow.Story), team, on);
        flow.State = GameState.PokeNav;
    }

    private void UpdatePokeNav(float dt)
    {
        var screen = flow.PokeNavScreen;
        screen.Update(dt);
        if (screen.IsActive) return;
        switch (screen.TakeRequest())
        {
            case PokeNavRequest.Call when screen.Called is { } contact:
                flow.State = GameState.Overworld;
                PlaceCall(contact, incoming: false);
                break;
            case PokeNavRequest.Map:
                townMapFromPokeNav = true;
                if (!OpenTownMap(TownMapMode.Card, fromBag: false))
                {
                    townMapFromPokeNav = false;
                    OpenPokeNav(PokeNavPage.Map);
                }
                break;
            default:
                flow.State = GameState.Overworld;
                break;
        }
    }
}
