using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The open world's place commands with no screen (plan 32 · OW5): played at once, as the host's walks are. Who stands
/// on which marker is written down (<see cref="OnMarker"/>); with an <see cref="OpenMap"/> the markers are held to it
/// and the player's place follows them (<see cref="OpenPlayer"/>), and with none any marker is taken on trust, which is
/// how a walk of every script plays an open place's without its map.
/// </summary>
public sealed partial class HeadlessScriptHost
{
    /// <summary>The open map the player is in; null for none known (markers are taken on trust).</summary>
    public OpenMap? OpenMap { get; set; }

    /// <summary>Looks an open map up by name for a <c>warpto</c>; left out, a warp only notes where it went.</summary>
    public Func<string, OpenMap?>? OpenMapNamed { get; set; }

    /// <summary>Who stands on which marker, as the open commands left them: <c>player</c> and the people by name.</summary>
    public Dictionary<string, string> OnMarker { get; } = new(StringComparer.Ordinal);

    /// <summary>Who a <c>faceto</c> turned toward what (a marker or someone).</summary>
    public Dictionary<string, string> FacingToward { get; } = new(StringComparer.Ordinal);

    /// <summary>The open map a <c>warpto</c> went to last, by name; null before any.</summary>
    public string? OpenMapName { get; private set; }

    /// <summary>The player's place in the open map, where a marker of a known map put them: millimetres and yaw.</summary>
    public OpenPlace? OpenPlayer { get; private set; }

    // The people of an open map as the scripts know them, made once each (plan 33 · GL1)
    private readonly Dictionary<OpenPerson, NPC> openPeople = new();

    /// <summary>
    /// Someone of an open map by name, as a person of the grid's kind (a scene's <c>battle hop</c>): in the open map
    /// the host is in, or with none the data's open map the script's place is an area of (<see cref="OpenRegions"/>);
    /// null for nobody there.
    /// </summary>
    private NPC? OpenPersonNamed(string name, string? place)
    {
        var map = OpenMap ?? (place != null && OpenRegions.MapOfArea(place) is { } of ? OpenRegions.Shared(of) : null);
        if (map?.Person(name) is not { } person) return null;
        if (!openPeople.TryGetValue(person, out var npc))
            openPeople[person] = npc = new NPC
            {
                Name = person.ShownName ?? person.Key, Key = person.Key, NpcType = person.Look ?? "Trainer",
                ScriptFile = person.Place, HiddenBy = person.HiddenBy, ShownBy = person.ShownBy
            };
        return npc;
    }

    private OpenMarker? Marker(string name)
    {
        if (OpenMap == null) return null;
        var marker = OpenMap.Marker(name);
        if (marker == null) Problems.Add($"there is no marker '{name}' in the open map {OpenMap.Name}");
        return marker;
    }

    /// <summary>Someone a command names: the player, or (with a known map) someone of it (plan 32 · OW6); a name of nobody there is a problem.</summary>
    private void Someone(string who)
    {
        if (who != "player" && OpenMap != null && OpenMap.Person(who) == null) Problems.Add($"there is nobody called '{who}' in the open map {OpenMap.Name}");
    }

    private void Stand(string who, string marker)
    {
        Someone(who);
        OnMarker[who] = marker;
        if (who == "player" && Marker(marker) is { } m && OpenMap != null) OpenPlayer = new OpenPlace(OpenMap.Name, m.Pos, m.Yaw);
    }

    public void WalkTo(string who, string marker, bool fast)
    {
        Shown("walk someone");
        // (A test may hold the script at the walk, as at a grid's; a hurry never waits)
        if (Pauses && !Skipping) Walking = true;
        Stand(who, marker);
        Log.Add($"walkto {who} {marker}" + (fast ? " fast" : ""));
    }

    public void PlaceAt(string who, string marker)
    {
        Shown("move someone");
        Stand(who, marker);
        Log.Add($"placeat {who} {marker}");
    }

    public void FaceTo(string who, string target)
    {
        Someone(who);
        // A target is the player, someone of the map or one of its markers
        if (OpenMap != null && target != "player" && OpenMap.Person(target) == null) Marker(target);
        FacingToward[who] = target;
        Log.Add($"faceto {who} {target}");
    }

    public void WarpTo(string map, string marker)
    {
        Shown("go somewhere else");
        // (In a hurry the player arrives at once, with no fade to see through)
        if (!Skipping) Waits();
        Log.Add($"warpto {map} {marker}");
        OpenMapName = map;
        if (OpenMapNamed != null)
        {
            if (OpenMapNamed(map) is { } open) OpenMap = open;
            else Problems.Add($"there is no open map '{map}'");
        }
        Stand("player", marker);
    }

    public float LookAt(string marker, float seconds)
    {
        Shown("move the camera");
        Marker(marker);
        Log.Add($"camera look {marker}");
        CameraOn = "marker " + marker;
        return seconds;
    }
}
