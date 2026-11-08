using System;
using System.IO;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// An open map's places at work (plan 32 · OW5): the regions' open maps loaded with their places, a trigger crossed
/// into running its script, a door going into its grid room and the room's way out landing on the door's marker, a
/// script's walk to a marker, its warp to one and its camera sent to one, and an arrival's own script. Nothing of a
/// region of today reaches any of it: no region's file says <c>open</c>, so no open map is loaded and every line here
/// is passed by in every game; a tool or a test lays an open map in through the <c>Driver</c>.
/// </summary>
public partial class GameEngine
{
    // Which triggers and doors the capsule's centre is in, tick by tick
    private readonly OpenCrossings openCrossings = new();

    // A script's walk to a marker under way, or null
    private OpenMarkerWalk? openWalk;

    /// <summary>The marker a script's <c>camera look</c> sent the open map's camera to last (OW8's orbit camera reads it); null for the player.</summary>
    internal string? OpenLook { get; private set; }

    /// <summary>
    /// Every open region's maps, read from <c>Data/world3d/&lt;region&gt;/</c> with their places and their baked land
    /// (<see cref="OpenPlaceFiles.LoadRegion"/>), as the game loads its data. Only a region whose file says
    /// <c>"field": "open"</c> is read: none does yet.
    /// </summary>
    private void LoadOpenRegions()
    {
        foreach (var region in RegionDatabase.All)
        {
            if (region.Field != FieldKind.Open) continue;
            string world = GameDataFiles.PathOf($"world3d/{region.Folder}/world.json");
            if (!File.Exists(world))
            {
                Log.Warn($"{region.Name} is open, and has no world3d/{region.Folder}/world.json: it has no open maps.");
                continue;
            }
            foreach (var map in OpenPlaceFiles.LoadRegion(Path.GetDirectoryName(world)!)) AddOpenMap(map);
        }
    }

    // Rooms a tool or a test laid in for an open map's doors (the open test world's houses), found before the game's
    private readonly System.Collections.Generic.Dictionary<string, Map> roomsOfTools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A grid room an open map's door may lead into, by its name (a tool's or a test's: the game's own rooms are the data's).</summary>
    internal void AddRoom(Map room) => roomsOfTools[room.Name] = room;

    private Map RoomNamed(string name) => roomsOfTools.GetValueOrDefault(name) ?? MapDatabase.Get(name);

    // ------------------------------------------------------------------ crossings

    /// <summary>
    /// After a tick of the stick: the triggers and doors the capsule's centre crossed into. A trigger the story is
    /// waiting at starts its script (looked for in its place's file first), a door the story has opened goes through
    /// into its room; either ends the frame's walking (true). One crossed while the story isn't waiting for it is
    /// crossed all the same, and nothing happens until it is left and come into again.
    /// </summary>
    private bool CrossTheVolumes(OpenField field)
    {
        foreach (var crossing in openCrossings.Cross(field.Map, field.Player.Pos))
        {
            if (crossing.Trigger is { } trigger && trigger.Waits(flow.Story.Var))
            {
                if (StartScript(trigger.Script, file: trigger.Place)) return true;
            }
            else if (crossing.Door is { } door && door.Open(flow.Story.Has))
            {
                GoThroughADoor(field, door);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Through an open map's door into its grid room (plan 32 · decision 6): the door's sound, the room's music as the
    /// picture closes, and in the dark the room's tile, its arrival and its name, as a grid door's warp has them.
    /// </summary>
    private void GoThroughADoor(OpenField field, OpenDoor door)
    {
        var room = RoomNamed(door.Room);
        AudioManager.PlaySound("door_open");
        PlayAreaMusic(room, door.RoomX, door.RoomY);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Open = null;
            openWalk = null;
            flow.Map = room;
            flow.Player.SetPosition(door.RoomX, door.RoomY, door.RoomFacing);
            ArriveOnMap();
            flow.SteppedOutOfWarp = true;
            AnnounceLocation();
            world.Life.Clear();
        });
    }

    /// <summary>
    /// A grid room's way out to an open map (plan 32 · OW5): the door of that map that leads into this room (the one
    /// nearest the way out, where two do) is come out of, onto its <see cref="OpenDoor.Out"/> marker facing as the
    /// marker does. A room with no door of the map leading back in has no way there, which the content check tells of;
    /// the game says so and stays.
    /// </summary>
    private void ComeOutOfARoom(OpenMap open, Warp warp)
    {
        var door = open.DoorBackFrom(flow.Map.Name, warp.SourceX, warp.SourceY);
        if (door == null || open.Marker(door.Out) is not { } marker)
        {
            Log.Error($"{flow.Map.Name}'s way out at {warp.SourceX},{warp.SourceY} leads to the open map {open.Name}, where no door leads back in{(door != null ? $" ({door.Name}'s marker {door.Out} isn't there)" : "")}.");
            return;
        }
        AudioManager.PlaySound("stairs");
        StartTransition(GameState.Overworld, () => ArriveInTheOpen(open, marker));
    }

    /// <summary>
    /// The player put in an open map on a marker (a door come out of, a script's <c>warpto</c>): the field begun on it
    /// (what the centre stands in is no crossing), and the place's own <c>OnEnter</c> started, as an arrival on the grid
    /// starts its own.
    /// </summary>
    private void ArriveInTheOpen(OpenMap open, OpenMarker marker)
    {
        EnterOpenField(open, marker.Pos, marker.Yaw);
        OpenLook = null;
        world.Life.Clear();
        string file = open.ScriptFileAt(marker.Pos);
        if (flow.Scripts.In(file, ScriptLibrary.OnEnter) is { } script) flow.Runner.Start(script);
    }

    // ------------------------------------------------------------------ under a script

    /// <summary>
    /// The open field's frame while a script has it: the frame's ticks, the player steered by the script's walk to a
    /// marker if one is under way (at rest otherwise: standing, or falling and sliding as the ground has it). Nothing is
    /// crossed under a script; what the walk ends in counts as stood in already, as a grid's scripted walk starts no
    /// trigger.
    /// </summary>
    private void StepTheOpenFieldUnderAScript(float dt)
    {
        var field = flow.Open!;
        var walk = openWalk;
        field.Run(dt, f => walk?.InputFor(f.Player) ?? OpenInput.None, f =>
        {
            walk?.After(f.Player);
            return false;
        });
        openCrossings.Settle(field.Map, field.Player.Pos);
        if (walk is { Done: true } && ReferenceEquals(openWalk, walk)) openWalk = null;
    }

    /// <summary>A hurry's end of the open walk under way: taken to its end unseen, onto its marker (plan 12 · Q20's rule, plan 32 · OW5).</summary>
    private void FinishTheOpenWalk()
    {
        // Everyone's walks to markers too (plan 32 · OW6)
        FinishTheOpenPeoplesWalks();
        if (openWalk is not { } walk) return;
        if (flow.Open is { } field)
        {
            field.Finish(walk);
            openCrossings.Settle(field.Map, field.Player.Pos);
        }
        openWalk = null;
    }

    /// <summary>The marker of that name in the open map the player is in, or a script gone wrong.</summary>
    private OpenMarker MarkerHere(string name, string command)
    {
        var open = flow.Open ?? throw new ScriptException($"{command} {name}: the player is in no open map here, and a grid map has no markers");
        return open.Map.Marker(name) ?? throw new ScriptException($"{command}: there is no marker '{name}' in the open map {open.Map.Name}");
    }

    internal sealed partial class Driver
    {
        /// <summary>A grid room an open map's door may lead into (plan 32 · OW5: the open test world's houses).</summary>
        public void AddRoom(Map room) => game.AddRoom(room);

        /// <summary>Which of the open map's triggers and doors the capsule's centre is in (<c>t:name</c>, <c>d:name</c>).</summary>
        public System.Collections.Generic.IReadOnlyCollection<string> OpenInside => game.openCrossings.Inside;

        /// <summary>The marker a script's <c>camera look</c> sent the open map's camera to last, or null.</summary>
        public string? OpenLook => game.OpenLook;

        /// <summary>Whether a script's walk to a marker is under way.</summary>
        public bool OpenWalking => game.openWalk != null;

        /// <summary>
        /// The game's scripts made these (a test's open places, whose files no data folder has), with a runner afresh,
        /// as the console's reload makes one.
        /// </summary>
        public void UseScripts(ScriptLibrary library)
        {
            game.flow.Scripts = library;
            game.flow.Runner = new ScriptRunner(library, new FieldHost(game));
        }
    }

    private sealed partial class FieldHost
    {
        public void WalkTo(string who, string marker, bool fast)
        {
            // Someone of the map walks there over the navigation mesh (plan 32 · OW6)
            if (who != "player")
            {
                game.WalkSomeoneTo(who, marker, fast);
                return;
            }
            var target = game.MarkerHere(marker, "walkto");
            game.openWalk = new OpenMarkerWalk(who, target, fast);
            // In a hurry it is walked to its end where the scene waits for it (Arrive), as the grid's walks are
        }

        public void PlaceAt(string who, string marker)
        {
            if (who != "player")
            {
                game.PlaceSomeoneAt(who, marker);
                return;
            }
            var at = game.MarkerHere(marker, "placeat");
            var field = game.flow.Open!;
            field.Player.Place(at.Pos, at.Yaw);
            game.openCrossings.Settle(field.Map, at.Pos);
        }

        // The player or someone of the map turned toward the player, someone or a marker (plan 32 · OW6)
        public void FaceTo(string who, string target) => game.FaceSomeoneTo(who, target);

        public void WarpTo(string map, string marker)
        {
            var open = game.OpenMapNamed(map) ?? throw new ScriptException($"warpto \"{map}\" {marker}: there is no open map called {map} (a grid map is gone to by 'warp')");
            var at = open.Marker(marker) ?? throw new ScriptException($"warpto \"{map}\" {marker}: {map} has no marker {marker}");
            game.openWalk = null;
            // In a hurry the player arrives at once, behind the skip's dark, as at the middle of the warp's own fade
            if (Skipping) game.ArriveInTheOpen(open, at);
            else game.StartTransition(GameState.Overworld, () => game.ArriveInTheOpen(open, at));
        }

        public float LookAt(string marker, float seconds)
        {
            game.MarkerHere(marker, nameof(Op.LookAt));
            game.OpenLook = marker;
            // (A hurry waits for nothing: the camera is where it was sent at once)
            return Skipping ? 0f : seconds;
        }
    }
}
