using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The travel phone in the game (plan 43 · LF6): where the rules of <see cref="TravelPhone"/> meet the field. The start
/// menu's PHONE opens its screen where the phone works; a call made from CALLS plays the contact's script of
/// <c>phone.txt</c> over the field, as a Pokégear's does, and its friends ring on a step on the same clock; NEWS shows
/// the day's programme; MAP opens the region's map and comes back to the phone. A save past the scene that gives it
/// is handed it as it loads or crosses (<see cref="OweTheTravelPhone"/>).
/// </summary>
public partial class GameEngine
{
    // Whether the map of the region was opened from the phone, which it goes back to
    private bool townMapFromPhone;

    /// <summary>Whether the travel phone is the player's where they are.</summary>
    private bool HasTravelPhone => TravelPhone.Has(flow.Story, Here.Id);

    private void OpenPhone(PhoneCard? on = null)
    {
        var screen = flow.PhoneScreen;
        screen.RegionName = Here.Id;
        screen.PlaceOf = area => MapDatabase.MapNames.Select(MapDatabase.Get).SelectMany(m => m.Areas).FirstOrDefault(a => a.Key == area)?.DisplayName is { Length: > 0 } name
            ? PlayerIdentity.Fill(LocalNames.PlaceShown(area, name)) : "";
        screen.Waiting = c => flow.Story.Has(PhoneRematch.WaitingFlag(c.Id));
        screen.PlayerCell = RegionMapNow is { } region ? RegionCellNow(region) : null;
        screen.PlaceHere = PlaceName();
        screen.News = TravelPhone.News(new FieldHost(this).Broadcast(), (int)flow.Encounters.DailyNumber);
        screen.Open(TravelPhone.Cards(Here.Id), TravelPhone.Contacts(flow.Story, Here.Id), on);
        flow.State = GameState.Phone;
    }

    private void UpdatePhone(float dt)
    {
        var screen = flow.PhoneScreen;
        screen.Update(dt);
        if (screen.IsActive) return;
        switch (screen.TakeRequest())
        {
            case PhoneRequest.Call when screen.Called is { } contact:
                flow.State = GameState.Overworld;
                PlaceCall(contact, incoming: false);
                break;
            case PhoneRequest.Map:
                townMapFromPhone = true;
                if (!OpenTownMap(TownMapMode.Card, fromBag: false))
                {
                    townMapFromPhone = false;
                    OpenPhone(PhoneCard.Map);
                }
                break;
            default:
                flow.State = GameState.Overworld;
                break;
        }
    }

    /// <summary>
    /// A step's chance of a friend of the travel phone ringing, on the Pokégear's own clock and odds: only where the
    /// phone works, with no script running and the field quiet, never underground or in a cave.
    /// </summary>
    private bool TravelPhoneRingsOnAStep()
    {
        if (!HasTravelPhone || ScriptRunning || flow.State != GameState.Overworld || IsUnderground) return false;
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave) return false;
        if (phoneRinger.TryRing(TravelPhone.Contacts(flow.Story, Here.Id), PlaceKey(), fieldRandom) is not { } caller) return false;
        PlaceCall(caller, incoming: true);
        return true;
    }

    /// <summary>
    /// A save past the scene that gives the phone in a region (Bianca's Pokédex in Unova, Mom's letter in Kalos), or a
    /// Champion who crossed in and never met it, is handed the phone and the region's numbers (no story version raised).
    /// </summary>
    private void OweTheTravelPhone()
    {
        if (TravelPhone.Owed(flow.Story)) flow.Story.Set(TravelPhone.Flag);
        if (!flow.Story.Has(TravelPhone.Flag) || !TravelPhone.NumbersOwed(flow.Story, Here.Id)) return;
        foreach (var contact in PhoneBook.Of(Here.Id)) flow.Story.Set(Pokegear.ContactFlag(contact.Id));
    }
}
