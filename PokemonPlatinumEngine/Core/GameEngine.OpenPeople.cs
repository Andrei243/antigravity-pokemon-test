using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The people of an open map at work (plan 32 · OW6): each made a person of the grid's kind for the scripts (their
/// own script or what they are, a trainer's team from the data, their flags), put on the map or taken off it by the
/// story, spoken to from in front, and a trainer whose cone sees the player calling out, walking up the navigation
/// mesh (two together where two see the player at once) and challenging the player by the grid's own scripts
/// (<c>common.Trainer</c>, <c>common.TrainerPair</c>) and battle, which comes back to the open map. Scripts walk,
/// place and turn them by name (<c>walkto</c>, <c>placeat</c>, <c>faceto</c>), and whoever travels with the player
/// follows along the player's trail. Nothing of a region of today reaches any of it: no open map is loaded.
/// </summary>
public partial class GameEngine
{
    // The person of the grid's kind each of an open map's people is to the scripts, made the first time asked for
    private readonly Dictionary<OpenPerson, NPC> openNpcs = new();

    // A trainer walking up, and the second of two who saw the player at once
    private OpenApproach? openApproach, openPairApproach;

    // The story's revision and the map the people's presence was last laid out for
    private int openPresenceRevision = -1;
    private OpenMap? openPresenceMap;

    /// <summary>How near the player on the ground, in millimetres, someone is spoken to from, and how far to either side of the player's way (an eighth of a turn and a half: 67.5°).</summary>
    public const int OpenTalkReach = 1600, OpenTalkAngle = OpenYaw.Turn * 3 / 16;

    /// <summary>
    /// The person of the grid's kind someone of an open map is to the scripts (plan 32 · OW6): their name, key, own
    /// script in their place's file, lines and flags; a trainer of the data (the map's region first) with the team the
    /// original builds. Made once and kept, so a trainer beaten stays beaten for as long as the game runs.
    /// </summary>
    internal NPC NpcOf(OpenMap map, OpenPerson p)
    {
        if (openNpcs.TryGetValue(p, out var npc)) return npc;
        npc = new NPC
        {
            Name = p.ShownName ?? p.Key,
            Key = p.Key,
            NpcType = p.Look ?? "Trainer",
            Script = p.Script,
            ScriptFile = p.Place,
            DialogLines = p.Lines.ToList(),
            HiddenBy = p.HiddenBy,
            ShownBy = p.ShownBy,
            Facing = OpenYaw.ToDirection(p.Yaw)
        };
        if (p.TrainerId != null && (TrainerDatabase.Get(p.TrainerId, map.Region) ?? TrainerDatabase.Get(p.TrainerId)) is { } record)
        {
            var trainer = new Trainer { Id = record.Id };
            TrainerDatabase.Fill(trainer, record);
            // A trainer's lines in a place's file are theirs before the battle and once beaten, as an overlay's trainer's
            // two lines are (plan 33 · GL1: Galar's trainers, the first of the data's open places with lines)
            if (p.Lines.Count > 0) trainer.DialogueBefore = p.Lines[0];
            if (p.Lines.Count > 1) trainer.DialogueAfter = p.Lines[1];
            npc.IsTrainer = true;
            npc.TrainerData = trainer;
            if (p.ShownName == null) npc.Name = trainer.Name;
            npc.HasBattled = flow.Story.HasDefeated(record.Id);
        }
        openNpcs[p] = npc;
        return npc;
    }

    /// <summary>Whether a trainer of an open map still waits to battle: a trainer of the data, not beaten.</summary>
    private bool WaitsToBattle(OpenMap map, OpenPerson p)
    {
        var npc = NpcOf(map, p);
        return npc.TrainerData is { } trainer && !npc.HasBattled && !(trainer.Id.Length > 0 && flow.Story.HasDefeated(trainer.Id));
    }

    /// <summary>The map's people put on it or taken off it by their flags, whenever the story has moved or the map is another.</summary>
    private void KeepTheOpenPeopleThere(OpenField field)
    {
        if (openPresenceMap == field.Map && openPresenceRevision == flow.Story.Revision) return;
        openPresenceMap = field.Map;
        openPresenceRevision = flow.Story.Revision;
        foreach (var p in field.Map.People) p.Present = NpcOf(field.Map, p).IsPresent(flow.Story.Has);
    }

    /// <summary>
    /// An open map's people as the player comes to it (the field begun, a save taken up): everyone back where they were
    /// put, nobody walked or held, and whoever travels with the player behind them on the player's own feet.
    /// </summary>
    private void BeginTheOpenPeople(OpenField field)
    {
        openApproach = openPairApproach = null;
        openPresenceMap = null;
        foreach (var p in field.Map.People)
        {
            p.SendHome();
            p.Held = false;
            field.Map.Moved(p);
        }
        KeepTheOpenPeopleThere(field);
        field.Follower = flow.Along is { } along
            ? new OpenFollower(along.Pokemon?.LookName ?? along.Who.Key ?? along.Who.Name, field.Player.Pos, field.Player.Yaw, along.TrainerId)
            : null;
    }

    /// <summary>Lets go of whoever a script or a walk up held, once neither has the field.</summary>
    private void LetTheOpenPeopleGo(OpenField field)
    {
        foreach (var p in field.Map.People)
            if (p.Held && p.Walk == null) p.Held = false;
    }

    /// <summary>
    /// After a tick of the stick: a trainer whose cone sees the player calls out (the "!") and comes over; a second
    /// who sees the player too comes with them, where the player has two Pokémon able to fight or someone beside them,
    /// as the grid's do. Ends the frame's walking (true).
    /// </summary>
    private bool LookForOpenTrainers(OpenField field)
    {
        if (!flow.Party.HasUsablePokemon) return false;
        var map = field.Map;
        var at = field.Player.Pos;
        if (OpenApproach.FindSpotter(map, at, p => WaitsToBattle(map, p)) is not { } trainer) return false;
        openApproach = new OpenApproach(trainer);
        openPairApproach = null;
        if (OpenApproach.FindSpotter(map, at, p => WaitsToBattle(map, p), except: trainer) is { } second
            && (partner != null || flow.Party.Members.Count(p => !p.IsFainted) >= 2))
            openPairApproach = new OpenApproach(second, turnPlayer: false);
        AudioManager.PlaySound("exclaim");
        // Their eye theme, by their class, while they walk up and talk, until the battle theme cuts in
        AudioManager.Region = map.Region;
        if (MusicDirector.EyeSong(NpcOf(map, trainer).TrainerData?.TrainerClass, AudioManager.Region, MusicLibrary.Exists) is { } eyeSong)
            AudioManager.PlayMusic(eyeSong, immediate: true);
        eyeThemePlaying = true;
        return true;
    }

    /// <summary>
    /// A frame of the walk up: the player at rest, the trainers walked by the field and watched after every tick; once
    /// both stand face to face with the player, the challenge, by the grid's own scripts.
    /// </summary>
    private void WalkTheTrainersUp(OpenField field, float dt)
    {
        var first = openApproach!;
        var pair = openPairApproach;
        field.PeopleMayWander = false;
        field.Run(dt, _ => OpenInput.None, f =>
        {
            first.Watch(f.Map, f.Player);
            pair?.Watch(f.Map, f.Player);
            return first.IsDone && pair?.IsDone != false;
        });
        if (!first.IsDone || pair?.IsDone == false) return;
        openApproach = openPairApproach = null;
        var npc = NpcOf(field.Map, first.Trainer);
        npc.Facing = OpenYaw.ToDirection(first.Trainer.Yaw);
        if (pair != null) StartScript(FieldScripts.TrainerPair, npc, pair: NpcOf(field.Map, pair.Trainer));
        else ChallengeTrainer(npc);
    }

    /// <summary>
    /// The confirm button in an open map: the nearest of the people there within <see cref="OpenTalkReach"/> and in
    /// front of the player turns to them and says their piece (their own script, or what they are: a trainer's
    /// challenge, their lines). False where nobody is.
    /// </summary>
    private bool TalkInTheOpen(OpenField field)
    {
        var at = field.Player.Pos;
        OpenPerson? nearest = null;
        long best = long.MaxValue;
        foreach (var p in field.PresentNear(at, OpenTalkReach))
        {
            if (p.Walk != null) continue;
            long d2 = p.Pos.GroundDistanceSquared(at);
            if (d2 == 0 || Math.Abs(p.Pos.Y - at.Y) > 1500) continue;
            if (Math.Abs(OpenYaw.Between(field.Player.Yaw, OpenTriangles.YawOf(p.Pos.X - at.X, p.Pos.Z - at.Z))) > OpenTalkAngle) continue;
            if (d2 < best) { best = d2; nearest = p; }
        }
        if (nearest == null) return false;
        var npc = NpcOf(field.Map, nearest);
        if (FieldScripts.For(npc) is not { } script) return false;
        nearest.Body.Face(OpenTriangles.YawOf(at.X - nearest.Pos.X, at.Z - nearest.Pos.Z));
        npc.Facing = OpenYaw.ToDirection(nearest.Yaw);
        nearest.Held = true;
        return StartScript(script, npc);
    }

    /// <summary>Someone of the open map the player is in, by name, or a script gone wrong.</summary>
    private OpenPerson PersonHere(string who, string command)
    {
        var open = flow.Open ?? throw new ScriptException($"{command} {who}: the player is in no open map here");
        return open.Map.Person(who) ?? throw new ScriptException($"{command} {who}: the open map {open.Map.Name} has nobody called '{who}'");
    }

    /// <summary>Where a script's <c>faceto</c> target stands: the player, someone of the map, or a marker.</summary>
    private OpenPos PlaceOfTarget(string target)
    {
        var open = flow.Open ?? throw new ScriptException($"faceto … {target}: the player is in no open map here");
        if (target == "player") return open.Player.Pos;
        if (open.Map.Person(target) is { } person) return person.Pos;
        return MarkerHere(target, "faceto").Pos;
    }

    /// <summary>Whether a script's walk to a marker is under way, the player's or anyone's (what the script waits for).</summary>
    private bool OpenWalksUnderWay => openWalk != null || (flow.Open?.Map.People.Any(p => p.Walk is { Kind: OpenWalkKind.Script, Done: false }) ?? false);

    /// <summary>A hurry's end of everyone's walk to a marker: each put on their marker, unseen (plan 12 · Q20's rule).</summary>
    private void FinishTheOpenPeoplesWalks()
    {
        if (flow.Open is not { } field) return;
        foreach (var p in field.Map.People)
            if (p.Walk is { Kind: OpenWalkKind.Script } walk)
            {
                walk.Finish(p);
                p.Walk = null;
                field.Map.Moved(p);
            }
    }

    private void WalkSomeoneTo(string who, string marker, bool fast)
    {
        var p = PersonHere(who, "walkto");
        var target = MarkerHere(marker, "walkto");
        var field = flow.Open!;
        p.Held = true;
        p.Walk = OpenPathWalk.Over(field.Map, p.Pos, target.Pos, OpenWalkKind.Script, fast, target, straight: true);
    }

    private void PlaceSomeoneAt(string who, string marker)
    {
        var p = PersonHere(who, "placeat");
        var at = MarkerHere(marker, "placeat");
        p.Walk = null;
        p.PutAt(at.Pos, at.Yaw);
        flow.Open!.Map.Moved(p);
    }

    private void FaceSomeoneTo(string who, string target)
    {
        var field = flow.Open ?? throw new ScriptException($"faceto {who} {target}: the player is in no open map here");
        var to = PlaceOfTarget(target);
        if (who == "player")
        {
            var pos = field.Player.Pos;
            if (pos.X != to.X || pos.Z != to.Z) field.Player.Body.Face(OpenTriangles.YawOf(to.X - pos.X, to.Z - pos.Z));
            return;
        }
        var p = PersonHere(who, "faceto");
        if (p.Pos.X != to.X || p.Pos.Z != to.Z) p.Body.Face(OpenTriangles.YawOf(to.X - p.Pos.X, to.Z - p.Pos.Z));
        if (openNpcs.TryGetValue(p, out var npc)) npc.Facing = OpenYaw.ToDirection(p.Yaw);
    }

    internal sealed partial class Driver
    {
        /// <summary>The trainer of an open map walking up to the player (plan 32 · OW6), and the second of a pair; null when none.</summary>
        public OpenApproach? OpenApproach => game.openApproach;
        public OpenApproach? OpenPairApproach => game.openPairApproach;

        /// <summary>Whoever walks behind the player in the open map, or null.</summary>
        public OpenFollower? OpenFollower => game.flow.Open?.Follower;

        /// <summary>The person of the grid's kind someone of the open map is to the scripts.</summary>
        public NPC OpenNpc(OpenPerson p) => game.NpcOf(game.flow.Open?.Map ?? throw new InvalidOperationException("the player is in no open map"), p);
    }
}
