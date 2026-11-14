using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The open world's field beside the grid's (plan 32 · OW1): the game keeps one field at a time, the grid's
/// (<c>flow.Map</c>, <c>flow.Player</c>) or an open map's (<c>flow.Open</c>), and the field's update goes to whichever
/// is in force. Nothing of a region of today reaches the open field: no region's file says <c>open</c> and no open map
/// is loaded by the game, so <c>flow.Open</c> is null in every game and every line here is passed by. A tool or a test
/// lays an open map in (<see cref="AddOpenMap"/>) and puts the player there through the <c>Driver</c>.
/// <para>
/// What the open field does is count its ticks, walk the capsule (OW3) by the stick, and cross its triggers and doors
/// and run its scripts (OW5, <c>GameEngine.OpenPlaces.cs</c>); the picture (OW8) comes later, so the field opens no menu
/// and draws what the grid's field drew.
/// </para>
/// </summary>
public partial class GameEngine
{
    private readonly Dictionary<string, OpenMap> openMaps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The region's word on what its outdoors are walked on, where the player stands (plan 32 · OW1).</summary>
    public FieldKind FieldHere => Here.Field;

    /// <summary>Whether the player is in an open map.</summary>
    public bool InOpenField => flow.Open != null;

    /// <summary>The open map of that name the game knows, or null (every name, today).</summary>
    internal OpenMap? OpenMapNamed(string name) => openMaps.GetValueOrDefault(name);

    /// <summary>
    /// An open map the game is to know by name (plan 32 · OW1: a tool's or a test's; OW2 on, the regions' baked maps).
    /// A name a grid map has is refused, since a save and a warp name a map by its name alone.
    /// </summary>
    internal void AddOpenMap(OpenMap map)
    {
        if (MapDatabase.TryGet(map.Name) != null) throw new ArgumentException($"{map.Name} is a grid map's name; an open map's must be its own");
        openMaps[map.Name] = map;
        // The Town Map of an open region is painted from its bake (plan 32 · OW10)
        Graphics.RegionMapPictures.OpenMapOf = OpenMapNamed;
    }

    /// <summary>Puts the player in an open map at a place, facing a way, with the field's clock begun afresh; the grid's place is kept beside it.</summary>
    internal void EnterOpenField(OpenMap map, OpenPos pos, int yaw)
    {
        if (OpenMapNamed(map.Name) != map) AddOpenMap(map);
        flow.Open = new OpenField(map, pos, yaw);
        flow.State = GameState.Overworld;
        // What the player is put down in is no crossing (plan 32 · OW5)
        openCrossings.Settle(map, pos);
        openWalk = null;
        // A raid and a Let's Go don't outlast being put down elsewhere (plan 34 · PD5)
        starRaid = null;
        letsGoShow = null;
        // Everyone back where they were put, and whoever travels along behind the player (OW6)
        BeginTheOpenPeople(flow.Open);
        // The orbit camera begun behind them (plan 32 · OW8)
        BeginTheOpenCamera(yaw);
        // The area come to (plan 34 · PD3's second session)
        openAreaKey = null;
        ComeToTheOpenArea(flow.Open);
    }

    /// <summary>
    /// The field's frame in an open map (plan 32 · OW3, OW5): while a script has the field, the script's frame (its
    /// walk to a marker and the field's ticks under it, <see cref="StepTheOpenFieldUnderAScript"/>); otherwise the
    /// frame's ticks, each stepping the player's capsule by the stick (the frame's quantised tilt, or the direction
    /// actions as a full tilt: <c>InputManager.GetStick</c>, its y down the screen being south) and the run button
    /// (turned round by the options' Autorun, as the grid's is), and after each the triggers and doors the capsule's
    /// centre crossed into (<see cref="CrossTheVolumes"/>), a trainer who sees the player (OW6) and a wild Pokémon the
    /// player touches (OW7, <see cref="MeetOpenWild"/>), the first of which ends the frame's walking. No menu or life of
    /// the field runs yet.
    /// </summary>
    private void UpdateOpenField(float dt)
    {
        var field = flow.Open!;
        // Who of the map is there (plan 32 · OW6), as the story has them
        KeepTheOpenPeopleThere(field);
        // The zones' tables are read at the game's time of day (plan 32 · OW7)
        field.Wild.Now = GameClock.Now;
        // and under each zone's weather of the day (plan 33 · GL2)
        field.Wild.WeatherOf ??= zone => OpenWeather.OfZone(field.Map, zone, GameClock.Today);
        if (ScriptRunning)
        {
            // A script that begins takes the player off the mount (plan 30 · FM12)
            GetOffTheMount();
            field.PeopleMayWander = false;
            field.WildMayAct = false;
            UpdateScript(dt);
            return;
        }
        // A trainer who saw the player walks up, and nothing else happens meanwhile (OW6)
        if (openApproach != null)
        {
            field.WildMayAct = false;
            WalkTheTrainersUp(field, dt);
            return;
        }
        field.PeopleMayWander = !PeopleStayPut;
        field.WildMayAct = true;
        LetTheOpenPeopleGo(field);
        // A on someone speaks to them; ridden, A with nobody to speak to is the mount's button (plan 30 · FM12)
        bool pressed = InputManager.IsActionPressed(GameAction.Confirm);
        if (pressed && TalkInTheOpen(field)) return;
        if (InputManager.IsActionPressed(GameAction.Mount)) ToggleMount();
        // Let's Go: the lead sent at the wild Pokémon nearest, or a raid's crew (plan 34 · PD5); a real battle met instead ends the frame
        if (InputManager.IsActionPressed(GameAction.LetsGo) && LetsGo(field)) return;
        KeepTheMountsPowers(field);
        // What the story grants the walker (plan 33 · GL5: Galar's bike fitted for the water), derived every frame
        field.Grants = OpenGrants.Of(flow.Story);
        var stick = InputManager.GetStick();
        bool run = InputManager.IsActionDown(GameAction.Run) != Settings.Autorun;
        var input = new OpenInput(stick.X, stick.Y, run);
        // The press is the frame's first tick's alone
        bool jump = pressed && OpenMounted;
        field.Run(dt, _ =>
        {
            var tick = jump ? input with { Jump = true } : input;
            jump = false;
            return tick;
        }, f => RaidTick(f) || CrossTheVolumes(f) || LookForOpenTrainers(f) || starRaid == null && MeetOpenWild(f));
        // The area the player stands in now (plan 34 · PD3's second session): its song and its first arrival
        ComeToTheOpenArea(field);
        // A Let's Go's show played out is let go (plan 34 · PD5)
        if (letsGoShow is { } show && show.Over(field.Wild.Tick)) letsGoShow = null;
        // A wild Pokémon that noticed the player in those ticks cries out (plan 32, after OW10)
        HearTheNoticed(field);
    }

    // The open area the player was last told to be in (its key), so a crossing into another is told once
    private string? openAreaKey;

    /// <summary>
    /// The area of an open map the player stands in (plan 34 · PD3's second session), told once as they cross into it, as
    /// a grid area is (<see cref="NoteArrival"/>): a town of the region's spawns is come to (its first arrival's flag, which
    /// Fly's place waits for, and a line of the Journal the first time), the region's map hears of a first arrival it
    /// waits for, and the area's song plays. A place come to by a door or a save is told the same way.
    /// </summary>
    private void ComeToTheOpenArea(OpenField field)
    {
        if (field.Map.AreaAt(field.Player.Pos) is not { } area || area.Key == openAreaKey) return;
        openAreaKey = area.Key;
        if (SpawnLocations.ArrivedIn(Here.Id, area.Key) is { } town)
        {
            if (!flow.Story.Has(town.ArrivalFlag)) flow.Journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, area.Name));
            flow.Story.Set(town.ArrivalFlag);
        }
        if (RegionMap.ForMap(field.Map.Name) is { } region)
            foreach (string flag in region.ArrivalFlags(area.Key)) flow.Story.Set(flag);
        if (area.Music is { Length: > 0 } song && MusicLibrary.Exists(song))
        {
            AudioManager.Region = Here.Id;
            AudioManager.PlayMusic(song);
        }
    }

    /// <summary>
    /// A loaded save's open place (plan 32 · OW1): the player is put there if the game knows its map, and otherwise
    /// wakes on the save's grid place, which the game has already put them on.
    /// </summary>
    private void TakeUpOpenPlace(SaveData save)
    {
        flow.Open = null;
        openAreaKey = null;
        if (save.OpenPlace is not { } saved) return;
        var place = saved.ToPlace();
        if (OpenMapNamed(place.Map) is { } map)
        {
            flow.Open = new OpenField(map, place.Pos, place.Yaw);
            // Saved riding, and where the mount may be ridden: wakes riding (plan 30 · FM12); anywhere else on foot
            if (saved.Mounted == true && MayWakeMounted()) flow.Open.Player.Body.Mount();
            openCrossings.Settle(map, place.Pos);
            openWalk = null;
            BeginTheOpenPeople(flow.Open);
            BeginTheOpenCamera(place.Yaw);
        }
        else Log.Warn($"The save stood in the open map {place.Map}, which isn't there: it wakes on its grid place, {flow.Map.Name} {flow.Player.GridX},{flow.Player.GridY}.");
    }
}
