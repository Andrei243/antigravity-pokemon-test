using System;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Pokégear in the game (plan 20 · J3): where the rules of <see cref="Pokegear"/>, <see cref="PhoneRematch"/>,
/// <see cref="PhoneRinger"/> and <see cref="RadioDial"/> meet the field. The start menu opens its screen; a call,
/// made or taken, plays its contact's script of <c>phone.txt</c> over the field; the map card opens the region's
/// map; the radio's song plays over the field until it is turned off, a door is gone through or a battle begins.
/// </summary>
public partial class GameEngine
{
    /// <summary>The script file of the calls.</summary>
    public const string PhoneScripts = "phone";

    // When a contact rings on their own, the song the radio plays over the field, and the station it was tuned to
    private readonly PhoneRinger phoneRinger = new();
    private RadioSong radioSong;
    private RadioStation? radioStation;
    private float stationClock;

    // Whether the map of the region was opened from the Pokégear, which it goes back to
    private bool townMapFromPokegear;

    /// <summary>The seconds a talk station keeps a line before its next.</summary>
    public const float StationLineSeconds = 6f;

    /// <summary>Whether the Pokégear is the player's where they are.</summary>
    private bool HasPokegear => Pokegear.Has(flow.Story, Here.Id);

    private void OpenPokegear(PokegearCard? on = null)
    {
        var screen = flow.PokegearScreen;
        screen.Now = GameClock.Moment;
        screen.RegionName = Here.Id;
        // A contact's place as the language names it (plan 17 · N6f)
        screen.PlaceOf = area => flow.Map.Areas.FirstOrDefault(a => a.Key == area)?.DisplayName is { Length: > 0 } name
            ? PlayerIdentity.Fill(LocalNames.PlaceShown(area, name))
            : MapDatabase.MapNames.Select(MapDatabase.Get).SelectMany(m => m.Areas).FirstOrDefault(a => a.Key == area)?.DisplayName is { Length: > 0 } other
                ? PlayerIdentity.Fill(LocalNames.PlaceShown(area, other)) : "";
        screen.Waiting = c => flow.Story.Has(PhoneRematch.WaitingFlag(c.Id));
        screen.PlayerCell = RegionMapNow is { } region ? RegionCellNow(region) : null;
        screen.PlaceHere = PlaceName();
        screen.Dial.Signal = RadioDial.SignalAt(PlaceKey(), flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave);
        screen.Open(Pokegear.Cards(flow.Story), Pokegear.Contacts(flow.Story), on);
        flow.State = GameState.Pokegear;
        TuneTheRadio();
    }

    private void UpdatePokegear(float dt)
    {
        var screen = flow.PokegearScreen;
        screen.Now = GameClock.Moment;
        screen.Update(dt);
        if (screen.IsActive && screen.Card == PokegearCard.Radio)
        {
            stationClock += dt;
            TuneTheRadio();
        }
        if (screen.IsActive) return;
        switch (screen.TakeRequest())
        {
            case PokegearRequest.Call when screen.Called is { } contact:
                flow.State = GameState.Overworld;
                PlaceCall(contact, incoming: false);
                break;
            case PokegearRequest.Map:
                townMapFromPokegear = true;
                if (!OpenTownMap(TownMapMode.Card, fromBag: false))
                {
                    townMapFromPokegear = false;
                    OpenPokegear(PokegearCard.Map);
                }
                break;
            default:
                flow.State = GameState.Overworld;
                break;
        }
    }

    /// <summary>
    /// The station under the radio's cursor plays or talks (<c>RadioShow_BeginSegment</c>): a music station's song over
    /// the field, a talk station's line in the screen's strip, nothing (and the area's theme back) where nothing comes in.
    /// </summary>
    private void TuneTheRadio()
    {
        var screen = flow.PokegearScreen;
        if (screen.Card != PokegearCard.Radio) return;
        RadioStation? station = screen.Dial.Tuned() is { } tuned ? RadioDial.StationOf(tuned.Spot.Slot, GameClock.Moment.Hour) : null;
        screen.Clear = screen.Dial.Tuned() is { Clear: true };
        if (station != radioStation || stationClock >= StationLineSeconds)
        {
            if (station != radioStation) SetRadioSong(station is { } s ? RadioDial.SongOf(s, GameClock.Today.DayOfWeek, flow.Pokedex.NationalUnlocked) : RadioSong.None);
            radioStation = station;
            stationClock = 0f;
            screen.StationLine = station is { } on ? StationLine(on) : "";
        }
        screen.Station = station;
        screen.Playing = radioSong;
    }

    /// <summary>A line of a station: Professor Oak talks of a Pokémon the player has seen and a place of Johto it lives in.</summary>
    private string StationLine(RadioStation station)
    {
        string? species = null, place = null;
        // The day's swarm, once swarms have begun, is told one time in two (plan 20 · J13, part c)
        if (station == RadioStation.PokemonTalk && Swarms.PlaceToday(flow.Encounters, RegionDatabase.Johto) is { Species.Length: > 0 } swarm && fieldRandom.Next(2) == 0)
            return PlayerIdentity.Fill(RadioShows.SwarmLine(fieldRandom, LocalNames.Species(swarm.Species), LocalNames.PlaceShown(null, swarm.Name)));
        if (station == RadioStation.PokemonTalk && Habitats.For("johto") is { } habitats)
        {
            var seen = flow.Pokedex.SeenSpecies.Order().Select(n => PokemonDatabase.GetByDex(n)?.Name).OfType<string>()
                .Where(n => habitats.Of(n).Count > 0).ToList();
            if (seen.Count > 0)
            {
                species = seen[fieldRandom.Next(seen.Count)];
                var lives = habitats.Of(species);
                place = PlayerIdentity.Fill(LocalNames.PlaceShown(null, lives[fieldRandom.Next(lives.Count)].Name));
            }
        }
        return PlayerIdentity.Fill(RadioShows.Line(station, fieldRandom, species, place));
    }

    /// <summary>The radio plays a song over the field, or stops and gives the field its theme back.</summary>
    private void SetRadioSong(RadioSong song)
    {
        if (song == radioSong) return;
        radioSong = song;
        PlayFieldMusic();
    }

    /// <summary>The radio stops (a door gone through, a battle): the next theme is the place's own.</summary>
    private void SilenceTheRadio()
    {
        radioSong = RadioSong.None;
        radioStation = null;
    }

    // ------------------------------------------------------------------ the phone

    /// <summary>
    /// A call, made by the player or taken from a contact who rang: the contact's script of <c>phone.txt</c> plays
    /// over the field, told what the call is (<see cref="Pokegear.CallVar"/>): a rematch they now ask for (and wait on
    /// their map for), one they asked for already, or a talk.
    /// </summary>
    private void PlaceCall(PhoneContact contact, bool incoming)
    {
        int call = PhoneRematch.Call(contact, flow.Story, GameClock.Today, GameClock.Now, incoming);
        if (call == Pokegear.CallRematch) KeepPhoneRematchesWaiting();
        flow.Story.SetVar(Pokegear.CallVar, call);
        AudioManager.PlaySound("phone_ring");
        if (!StartScript(contact.Script, file: PhoneScripts)) StartScript(Pokegear.NobodysCall, file: PhoneScripts);
    }

    /// <summary>
    /// A step's chance of a contact ringing (<c>ov02_02251F20</c>): only in Johto with the Pokégear, with no script
    /// running and the field quiet, never underground or in a cave, which no call reaches.
    /// </summary>
    private bool PhoneRingsOnAStep()
    {
        if (!HasPokegear || ScriptRunning || flow.State != GameState.Overworld || IsUnderground) return false;
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave) return false;
        if (phoneRinger.TryRing(flow.Story, PlaceKey(), fieldRandom) is not { } caller) return false;
        PlaceCall(caller, incoming: true);
        return true;
    }

    /// <summary>Whoever a call over the phone asked to wait for a rematch spins on their map until fought, as the Vs. Seeker's do.</summary>
    private void KeepPhoneRematchesWaiting()
    {
        if (!PhoneRematch.Waiting(flow.Story).Any()) return;
        foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) PhoneRematch.KeepWaiting(map.Everyone, flow.Story);
    }

    /// <summary>The field's clock runs the phone's (a call can come once ten minutes have passed).</summary>
    private void RunThePhonesClock(float dt)
    {
        if (HasPokegear && flow.State == GameState.Overworld) phoneRinger.Advance(dt);
    }
}
