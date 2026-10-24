using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using Gesture = PokemonPlatinumEngine.Overworld.Gesture;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The story in the field (plan 02 · S1): what starts a script, and the game as a script's host. A script runs
/// over the field, in <see cref="GameState.Overworld"/>, and the player's keys do nothing meanwhile; it goes on
/// only while the game is in that state, so whatever it starts that changes the state (text, a battle, a screen, a
/// fade into another map) is waited for by itself.
/// </summary>
public partial class GameEngine
{
    // Whoever a script's last gesture was given to (plan 11 · C11; null for the player), while the script waits for it
    private (bool Waiting, NPC? Who) actor;

    /// <summary>True while someone plays the gesture a script waits for.</summary>
    private bool ActorBusy => actor.Waiting && (actor.Who is { } who ? who.Gesture : flow.Player.Gesture) != null;

    /// <summary>
    /// Runs the clock of the gesture a script waits for when its actor is not among the people whose clocks run
    /// anyway (those near the player), so a script never waits for one that can't end.
    /// </summary>
    private void KeepTheActorActing(float dt, List<NPC> near)
    {
        if (!actor.Waiting) return;
        if (actor.Who is { } who && !near.Contains(who)) who.TickGesture(dt);
        if (!ActorBusy) actor = default;
    }

    // Boulders sliding on after a push: which, how long they have left, and which way they go
    private readonly List<(NPC Boulder, float Left, float Length, int Dx, int Dy)> slides = new();

    /// <summary>True while a script has the field: the player's keys do nothing.</summary>
    public bool ScriptRunning => flow.Runner != null && flow.Runner.IsRunning;

    /// <summary>The answers on screen for a question a script asked: tests and the harness pick one with its own buttons.</summary>
    public ChoiceBox Choice => flow.Choice;

    private void LoadScripts()
    {
        flow.Scripts = ScriptLibrary.Default;
        flow.Runner = new ScriptRunner(flow.Scripts, new FieldHost(this));
    }

    // ------------------------------------------------------------------ what starts a script

    /// <summary>
    /// Starts a script over the field. Its name is looked for in the file of the place first (the subject's own,
    /// or where the player stands) and among the common ones second. False, with nothing started, when there is
    /// no such script.
    /// </summary>
    /// <param name="item">The item the script's <c>find own</c> gives, when it isn't the subject's (a hidden item).</param>
    /// <param name="flag">The flag its <c>setflag own</c> sets, when it isn't the one that hides the subject.</param>
    /// <param name="pokemon">The Pokémon of the team the script is for: the one whose field move was chosen in the party menu.</param>
    public bool StartScript(string name, NPC? subject = null, IReadOnlyList<string>? own = null, string? file = null,
        (string Item, int Count)? item = null, string? flag = null, Pokemon? pokemon = null, NPC? pair = null, (string Place, string Key)? sign = null)
    {
        file ??= subject?.ScriptFile ?? flow.Map.ScriptFileAt(subject?.GridX ?? flow.Player.GridX, subject?.GridY ?? flow.Player.GridY);
        if (flow.Scripts.Find(name, file) is not { } script)
        {
            Log.Error($"No script '{name}' for {file}.");
            return false;
        }
        flow.Runner.Start(script, subject, own, item, flag, pokemon, pair, sign);
        return true;
    }

    /// <summary>Starts a script that is in no file: one a tool wrote for the occasion (the screenshot harness's scene).</summary>
    public void StartScript(Script script, NPC? subject = null) => flow.Runner.Start(script, subject);

    /// <summary>The script of the place the player has come to (<c>OnEnter</c> in its file), if it has one.</summary>
    private bool StartEnterScript()
    {
        string file = flow.Map.ScriptFileAt(flow.Player.GridX, flow.Player.GridY);
        if (flow.Scripts.In(file, ScriptLibrary.OnEnter) is not { } script) return false;
        flow.Runner.Start(script);
        return true;
    }

    /// <summary>A step ended on tiles that start a script while the story is where they wait for it.</summary>
    private bool TryStepTrigger()
    {
        if (FieldScripts.TriggerAt(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Story) is not { } trigger) return false;
        return StartScript(trigger.Script, file: trigger.ScriptFile ?? flow.Map.Name);
    }

    /// <summary>Puts everyone of every map where the story has them, when the story has changed since they were last put.</summary>
    private void RefreshPresence(bool startOver = false)
    {
        if (!startOver && flow.PresenceRevision == flow.Story.Revision) return;
        flow.PresenceRevision = flow.Story.Revision;
        // Each badge won adds to the Trainer Card's score; a loaded save has counted its own already
        if (startOver) flow.ScoredBadges = flow.Story.TotalBadges;
        for (; flow.ScoredBadges < flow.Story.TotalBadges; flow.ScoredBadges++) flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, TrainerScore.Badge);
        // A place the story reveals is in the map from then on, and drawn again (the Spring Path, plan 01 · M8)
        foreach (var (map, place) in MapDatabase.ApplyHiddenPlaces(flow.Story.Var))
            world?.Forget(map, place.X, place.Y, place.Width, place.Height);
        // And so are chunks the story swaps for others (Route 224's, which open the way onto the Seabreak Path, plan 01 · M10)
        foreach (var (map, place) in MapDatabase.ApplySwappedPlaces(flow.Story.Var, flow.Story.Has))
            foreach (var (cx, cy) in place.Cells)
                world?.Forget(map, cx * Map.ChunkTiles, cy * Map.ChunkTiles, Map.ChunkTiles, Map.ChunkTiles);
        MapDatabase.ApplyPresence(flow.Story.Has, forget: startOver);
        KeepTheRegionsPicture();
    }

    /// <summary>The player has come to a map by a door, a warp or a save: whoever a script moved off it or onto it is back as the flags say.</summary>
    private void ArriveOnMap()
    {
        // The first arrival in a place is timed for the log, from here to its first frame (plan 16 · T3)
        TimeTheArrival();
        // The Pokégear's radio is off once a door has been gone through (plan 20 · J3)
        SilenceTheRadio();
        // A warp takes the player off the Cycling Road (FieldSystem_InitFlagsWarp)
        flow.Story.Unset(BicycleRules.OnCyclingRoadFlag);
        flow.Story.Unset(BicycleRules.ForcedInGateFlag);
        ChangePlace();
        NoteTheTrailOnArrival();
        // {rival} names the rival of the region the player has come to (plan 18 · W4, part b)
        PlayerIdentity.InRegion(Here);
        // A Pokémon out of its ball stays in Amity Square (plan 06 · R16c)
        LeaveStrollBehind();
        // Another map ends the Poké Radar's chain (RadarChain_Clear), and the roamers note where the player is (plan 06 · R13)
        EndRadarChain();
        Roamers.NotePlace(flow.Encounters, PlaceKey());
        flow.Map.ForgetForced();
        // Whoever a scene's camera kept on stays on the map left behind: the camera is the player's again (plan 09 · L4)
        if (world.Camera.Following is { } followed && !flow.Map.Everyone.Contains(followed)) world.Camera.ComeHome();
        // A face held and a gesture under way are let go on coming to a place, as the people's are (plan 11 · C11)
        flow.Player.StopActing();
        // and so is a stance a script set them in (plan 11 · C7)
        flow.Player.Posed = null;
        actor = default;
        flow.Map.ApplyPresence(flow.Story.Has);
        // Whoever was still walking on the map left behind stands where they were going, not between two tiles
        foreach (var walk in flow.NpcWalks) walk.Finish();
        flow.NpcWalks.Clear();
        flow.PlayerWalk = null;
        // Those who move about of their own accord start again from where they first stood, as the original lays
        // its objects out afresh (plan 02 · S6)
        flow.Wandering.Clear();
        Wandering.SendHome(flow.Map);
        KeepPartnerAlong();
        // A Gym's puzzle is laid out afresh, as the original's room scripts do as the player comes in (plan 01 · M9)
        ArrivePuzzle();
        // The tunnels are laid out as the player comes to them (plan 06 · R16a)
        if (IsUnderground) ArriveUnderground();
        // A flag carried and a trap sprung stay below, however the player leaves
        else if (flow.Underground.HeldFlag != null || flow.Trap != null)
        {
            flow.Underground.HeldFlag = null;
            flow.Trap = null;
        }
        // The Villa's furniture stands where it is owned (plan 06 · R16b)
        if (flow.Map.Name == VillaMap) FurnishVilla();
        // The people of the place come to are made ready behind the fade, and the rigs of the place left are let go
        KeepCharactersNear(trim: true);
        flow.Arrived = true;
    }

    /// <summary>
    /// The Journal's lines for leaving a place (plan 06 · R12; the original's map transitions): out of a cave into
    /// the open, out of home or the professor's lab, and into a Gym without its Badge.
    /// </summary>
    private void NoteLeaving(bool cave)
    {
        bool outdoors = !cave && flow.Map.IsStreamed && flow.Map.Name == "Sinnoh";
        if (flow.LeftCave && outdoors) flow.Journal.Tell(new JournalEvent(JournalEventKind.LeftCave, flow.LeftPlace));
        if (flow.LeftMap == "PlayerHouse" && outdoors) flow.Journal.Tell(new JournalEvent(JournalEventKind.RestedAtHome));
        if (flow.LeftMap == "RowanLab" && outdoors) flow.Journal.Tell(new JournalEvent(JournalEventKind.LeftResearchLab));
        if (Gyms.TryGetValue(flow.Map.Name, out var gym) && flow.LeftMap == "Sinnoh" && !flow.Story.HasBadge(RegionDatabase.Sinnoh, gym.Badge))
            flow.Journal.Tell(new JournalEvent(JournalEventKind.GymWasTooTough, gym.Town));
        flow.Journal.ChangedPlace();
        flow.LeftMap = flow.Map.Name;
        flow.LeftPlace = PlaceName();
        flow.LeftCave = cave;
    }

    /// <summary>
    /// Sinnoh's Gyms the Journal names, by their room: the town and the Badge, by its place in Sinnoh's list (the
    /// original's <c>sGymsInfo</c>): Coal, Forest, Relic, Cobble and Beacon.
    /// </summary>
    private static readonly Dictionary<string, (string Town, int Badge)> Gyms = new()
    {
        ["OreburghGym"] = ("Oreburgh", 0),
        ["EternaGym"] = ("Eterna", 1),
        ["HearthomeGym"] = ("Hearthome", 4),
        ["VeilstoneGym"] = ("Veilstone", 2),
        ["SunyshoreGym"] = ("Sunyshore", 7)
    };

    /// <summary>
    /// The player has come to another place, by a warp or a step into another area: what lasted only while they
    /// stayed is forgotten (plan 02 · S2), as the original does when the map's header changes: the place's local
    /// flags (a tree that was cut grows back), Strength, Flash and Defog outside the caves, and every boulder that
    /// was pushed goes back where it stood.
    /// </summary>
    private void ChangePlace()
    {
        bool cave = flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave;
        NoteLeaving(cave);
        // Going anywhere else ends the rematches the Vs. Seeker found (FieldMapChange_UpdateGameData)
        VsSeeker.Reset(flow.Story);
        foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) foreach (var npc in map.Everyone) npc.ReadyForRematch = false;
        // ...but not those a call over the Pokégear's phone asked to wait (plan 20 · J3)
        KeepPhoneRematchesWaiting();
        FieldMoveRules.LeavePlace(flow.Story, cave);
        // The ash on the grass of the place left settles again (plan 21 · H4)
        flow.AshBrushed.Clear();
        // Where the Bicycle isn't allowed the player gets off it (field_map_change_flags.c)
        if (flow.Player.Mode == TravelMode.Cycling && !flow.Map.BikeAllowedAt(flow.Player.GridX, flow.Player.GridY)) flow.Player.SetCycling(false);
        flow.Fishing = null;
        // A flute's tune lasts only while the player stays where it was played (field_map_change_flags.c)
        flow.EncounterAids.ChangePlace();
        foreach (var (boulder, _, _, _, _) in slides) boulder.StepOffsetX = boulder.StepOffsetY = 0f;
        slides.Clear();
        flow.Map.ResetObstacles();
        // The Battle Points a script was speaking of stay behind with the room (plan 06 · R18a)
        showBattlePoints = false;
        // And so do a scene's effects, held or not (plan 09 · L11)
        world?.Life.ClearEffects();
        // ... and its set pieces: a machine's balls, a PC left on, whoever was being shaken (plan 09 · L12)
        world?.Pieces.Clear();
    }

    /// <summary>
    /// A field move chosen in the party menu (plan 02 · S2): checked by the original's rules where the player
    /// stands, and either the menu says why not, or it closes and the move's script runs (Fly's map opens first).
    /// </summary>
    private void UseFieldMoveFromMenu(FieldMove move, int index)
    {
        var user = index >= 0 ? flow.Party.Members[index] : FieldHelp.HelperFor(move); // index -1: a wild helper (plan 12 · Q18)
        var walker = new Walker(flow.Player.Mode, flow.Player.HeightOn(flow.Map), Moves: flow.Player.Moves);
        var spot = FieldMoveRules.SpotOf(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing, walker) with { Partner = partner != null };
        var error = FieldMoveRules.Check(move, spot, flow.Story);
        // Dig needs a way out to lead to, and Fly a place on the region's map that is open to fly to
        // (in any region Fly can reach from here, plan 18 · W9)
        var flyChoice = move == FieldMove.Fly ? FlyChoiceNow() : default;
        if (error == FieldMoveError.None && ((move == FieldMove.Dig && flow.ExitSpot == null)
            || (move == FieldMove.Fly && !flyChoice.Tabs.Any(t => t.Spots.Any(s => s.Open)))))
            error = FieldMoveError.Location;
        if (error != FieldMoveError.None)
        {
            flow.PartyScreen.Message = FieldMoveRules.Why(error);
            AudioManager.PlaySound("error");
            return;
        }

        flow.PartyScreen.Close();
        if (move == FieldMove.Fly)
        {
            // Fly's choice is the map of the region in its own mode (TOWN_MAP_MODE_FLY; plan 01 · M12)
            townMapFromBag = false;
            flow.TownMapScreen.Open(flyChoice.Tabs, flyChoice.Home, TownMapMode.Fly, user);
            flow.State = GameState.FlyMap;
            return;
        }
        flow.State = GameState.Overworld;
        // Cut, Rock Smash and Strength act on the obstacle in front, which is whose script it is
        var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
        int ax = flow.Player.GridX + dx, ay = flow.Player.GridY + dy;
        NPC? subject = flow.Map.InBounds(ax, ay) ? flow.Map.NpcIn(ax, ay, flow.Map.FootingAt(ax, ay, flow.Player.HeightOn(flow.Map))) : null;
        StartScript(FieldScripts.FromMenu(move)!, subject is { IsObstacle: true } ? subject : null, pokemon: user);
    }

    /// <summary>
    /// A warp is taken: going from the map of Sinnoh into anywhere else marks the way back out for Dig and an
    /// Escape Rope (<c>Field_SetMapConnection</c>: the door's tile, or the one south of it for someone who walked
    /// in northward), and going into a Pokémon Center makes its town the one Teleport goes back to
    /// (<c>GetMapBlackOutWarpId</c>).
    /// </summary>
    private void NoteTheWayIn(Warp warp)
    {
        if (flow.Map.Name == "Sinnoh" && warp.TargetMap != "Sinnoh")
            flow.ExitSpot = new MapSpot("Sinnoh", flow.Player.GridX, flow.Player.GridY + (flow.Player.Facing == Direction.Up ? 1 : 0), flow.Player.Facing);
        // (in its own region: a Pokémon Center of Kanto is Kanto's place to come back to, plan 18 · W9)
        if (SpawnLocations.OfRoomAnywhere(warp.TargetMap) is { } center)
            flow.Story.SetVar(SpawnLocations.VariableOf(RegionDatabase.RegionOfMap(warp.TargetMap)?.Id), center.Id);
    }

    /// <summary>Arriving in a town for the first time is what lets one fly there (<c>TryUnlockFlyLocationByMap</c>).</summary>
    private void NoteArrival()
    {
        // A place walked into, or the one a game begins in, is timed as one come to by a door is (plan 16 · T3)
        TimeTheArrival();
        // (in the region the player is in: Johto's towns open to Fly as Sinnoh's do, plan 20 · J9)
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key is { } key && SpawnLocations.ArrivedIn(Here.Id, key) is { } town)
        {
            // A town come to for the first time is a line of the Journal (field_map_change_flags.c)
            if (!flow.Story.Has(town.ArrivalFlag)) flow.Journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, PlaceName()));
            flow.Story.Set(town.ArrivalFlag);
        }
        // And a landmark come to for the first time is told of on the region's map from then on (plan 01 · M12)
        NoteArrivalOnTheMap();
    }

    /// <summary>
    /// The Vs. Seeker used (plan 06 · R12): on the open map, with its battery full, every trainer in range not yet
    /// beaten shows a "!", and those beaten who want a rematch spin with a "!!".
    /// </summary>
    private void UseVsSeeker()
    {
        bool outdoors = flow.Map.Name == "Sinnoh" && !(flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? false);
        switch (VsSeeker.Use(flow.Map, flow.Player.GridX, flow.Player.GridY, outdoors, flow.Story, Dice.Shared, out var ready, out var notYet))
        {
            case VsSeekerResult.NotCharged:
                ShowNotification(Text.Of("engine.vsSeeker.charging", ("steps", VsSeeker.FullBattery - flow.Story.Var(VsSeeker.Battery))));
                AudioManager.PlaySound("error");
                return;
            case VsSeekerResult.NoTrainers:
                ShowNotification(Text.Of("engine.vsSeeker.noTrainers"));
                AudioManager.PlaySound("error");
                return;
        }
        AudioManager.PlaySound("vs_seeker");
        foreach (var npc in notYet) npc.ShowBubble(EmoteBubble.Exclaim, 1.2f);
        foreach (var npc in ready)
        {
            npc.Facing = Direction.Down;
            npc.ShowBubble(EmoteBubble.Exclaim, 1.6f);
        }
        if (ready.Count == 0 && notYet.Count == 0) ShowNotification(Text.Of("engine.vsSeeker.noRematch"));
    }

    /// <summary>
    /// The Poké Radar used (plan 06 · R13, <see cref="RadarChain"/>): in tall grass, on foot and alone, with its battery
    /// charged, it sets four patches shaking round the player; otherwise a notice says why not.
    /// </summary>
    private void UsePokeRadar()
    {
        switch (flow.Radar.Use(flow.Encounters, flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.HeightOn(flow.Map), flow.Player.Mode, partner != null, fieldRandom))
        {
            case RadarUse.CantUse:
                ShowNotification(partner != null ? FieldMoveRules.Why(FieldMoveError.Partner) : Text.Of("engine.radar.notInGrass"));
                AudioManager.PlaySound("error");
                return;
            case RadarUse.NotCharged:
                ShowNotification(Text.Of("engine.radar.dry", ("steps", RadarChain.BatterySteps - flow.Encounters.RadarCharge)));
                AudioManager.PlaySound("error");
                return;
            case RadarUse.Quiet:
                ShowNotification(Text.Of("engine.radar.quiet"));
                return;
        }
        AudioManager.PlaySound("grass");
    }

    /// <summary>
    /// An item used in the field itself (plan 02 · S2), from the bag or the item button: the Bicycle got on or off,
    /// a rod cast, an Escape Rope. Where it can't be, a notice says why.
    /// </summary>
    /// <summary>The bicycle an item is (plan 21 · H3): Emerald's Mach or Acro Bike, or Platinum's.</summary>
    internal static BikeKind BikeOf(string item) => item switch
    {
        "Mach Bike" => BikeKind.Mach,
        "Acro Bike" => BikeKind.Acro,
        _ => BikeKind.Bicycle
    };

    /// <summary>The bicycle the bag holds, Emerald's two before Platinum's: what a save made cycling is ridden on.</summary>
    private BikeKind BikeInTheBag()
    {
        foreach (var name in new[] { "Mach Bike", "Acro Bike" })
            if (ItemDatabase.Get(name) is { } bike && flow.Bag.GetQuantity(bike) > 0) return BikeOf(name);
        return BikeKind.Bicycle;
    }

    /// <summary>The Itemfinder used where the player stands (<see cref="Itemfinder"/>).</summary>
    private void UseItemfinder()
    {
        var (answer, toward) = Itemfinder.Search(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Story);
        switch (answer)
        {
            case ItemfinderAnswer.Nothing:
                ShowNotification(Text.Of("engine.itemfinder.nothing"));
                return;
            case ItemfinderAnswer.Underfoot:
                AudioManager.PlaySound("dowsing_ping");
                ShowNotification(Text.Of("engine.itemfinder.underfoot"));
                return;
            default:
                AudioManager.PlaySound("dowsing_ping");
                if (!flow.Player.IsMoving && toward is { } way) flow.Player.Facing = way;
                ShowNotification(Text.Of("engine.itemfinder.nearby"));
                return;
        }
    }

    private void UseFieldItem(ItemData item)
    {
        int x = flow.Player.GridX, y = flow.Player.GridY;
        if (item.FieldUse == "Journal")
        {
            flow.State = GameState.Journal;
            flow.JournalScreen.Open();
            return;
        }
        // The Vs. Recorder's battles off the item button, and back to the field (plan 08 · P11)
        if (item.FieldUse == "VsRecorder")
        {
            OpenReplays(fromBag: false);
            return;
        }
        // The Town Map off the item button: the map of the region, and back to the field (plan 01 · M12)
        if (item.FieldUse == "TownMap")
        {
            if (!OpenTownMap(TownMapMode.Item, fromBag: false)) ShowNotification(Text.Of("engine.noMapHere"));
            return;
        }
        // The Seal Case and the Fashion Case from the item button (plan 06 · R17b)
        if (item.Name == BagScreen.SealCaseItem)
        {
            OpenBallCapsules(fromBag: false);
            return;
        }
        if (item.Name == BagScreen.FashionCaseItem)
        {
            OpenFashionCase(fromBag: false);
            return;
        }
        // The Poffin Case opened from the item button goes back to the field when it closes (UsePoffinCaseInField)
        if (item.FieldUse == "PoffinCase")
        {
            OpenPoffinCase(fromBag: false);
            return;
        }
        // Emerald's Itemfinder (plan 21 · H3): a beep and a turn toward the nearest hidden thing, or a notice there's none
        if (item.FieldUse == "Itemfinder")
        {
            UseItemfinder();
            return;
        }
        if (item.FieldUse == "VsSeeker")
        {
            UseVsSeeker();
            return;
        }
        // The Explorer Kit: down into the Underground, and below ground its own bag (plan 06 · R16a)
        if (item.FieldUse == "ExplorerKit")
        {
            UseExplorerKit();
            return;
        }
        if (item.FieldUse == "PokeRadar")
        {
            UsePokeRadar();
            return;
        }
        // Honey from the bag draws out a wild Pokémon as Sweet Scent does, and is used up (UseHoneyFromMenu)
        if (item.FieldUse == "Honey")
        {
            StartScript(FieldScripts.UseHoney);
            return;
        }
        // Travelling with someone, the Bicycle, the rods and an Escape Rope stay in the bag (CanUseBicycle,
        // CanUseFishingRod, CanUseEscapeRope: ITEM_USE_CANNOT_USE_WITH_PARTNER)
        if (partner != null && (item.FieldUse is "Bicycle" or "EscapeRope" || FishingAttempt.RodOf(item) != null))
        {
            ShowNotification(FieldMoveRules.Why(FieldMoveError.Partner));
            AudioManager.PlaySound("error");
            return;
        }
        if (item.FieldUse == "Bicycle")
        {
            var check = BicycleRules.Check(flow.Map, x, y, flow.Player.Mode, flow.Story.Has(BicycleRules.OnCyclingRoadFlag) || flow.Story.Has(BicycleRules.ForcedInGateFlag));
            if (check != BicycleCheck.Ok)
            {
                ShowNotification(BicycleRules.Why(check));
                AudioManager.PlaySound("error");
                return;
            }
            bool getOn = flow.Player.Mode != TravelMode.Cycling;
            if (!flow.Player.SetCycling(getOn)) return;
            // Emerald's two bicycles are ridden as themselves (plan 21 · H3); Platinum's keeps its gears
            if (getOn) flow.Player.Bike = BikeOf(item.Name);
            // Getting on ends the Poké Radar's chain (MountOrUnmountBicycle)
            if (getOn) EndRadarChain();
            if (getOn) AudioManager.PlaySound("bike_bell");
            PlayFieldMusic();
            return;
        }
        if (FishingAttempt.RodOf(item) is { } rod)
        {
            if (!FishingAttempt.CanCast(flow.Map, x, y, flow.Player.Facing, flow.Player.HeightOn(flow.Map)))
            {
                ShowNotification(Text.Of("engine.noWater"));
                AudioManager.PlaySound("error");
                return;
            }
            var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
            flow.Fishing = new FishingAttempt(rod, flow.Map.Fish(x + dx, y + dy, rod, flow.Player.Lead, EncounterMomentNow()), fieldRandom);
            AudioManager.PlaySound("fish_cast");
            return;
        }
        if (item.FieldUse == "EscapeRope")
        {
            var spot = FieldMoveRules.SpotOf(flow.Map, x, y, flow.Player.Facing, new Walker(flow.Player.Mode, flow.Player.HeightOn(flow.Map))) with { Partner = partner != null };
            if (!spot.CaveWithAWayOut || flow.ExitSpot == null)
            {
                ShowNotification(Text.Of("engine.noWayOut"));
                AudioManager.PlaySound("error");
                return;
            }
            StartScript(FieldScripts.EscapeRope);
        }
    }

    /// <summary>
    /// A rod's cast plays out: the line lands, something bites (the "!" over the player) or nothing does, the
    /// button is pressed in time or not, and what happened is said; a Pokémon reeled in is battled.
    /// </summary>
    private void UpdateFishing(float dt)
    {
        var cast = flow.Fishing!;
        var before = cast.Stage;
        cast.Update(dt, InputManager.IsActionPressed(GameAction.Confirm) || FishingPress);
        FishingPress = false;
        flow.Player.StandStill(dt);
        // The rod out over the water from the cast, pulled on from the bite until the catch is told (plan 11 · C8)
        flow.Player.Posed = cast.Stage switch
        {
            FishingStage.Done => null,
            FishingStage.Hooked or FishingStage.Landed => Overworld.Stance.Tug,
            _ => Overworld.Stance.Fish
        };
        var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
        int wx = flow.Player.GridX + dx, wy = flow.Player.GridY + dy;
        if (cast.Stage != before)
        {
            if (cast.Stage == FishingStage.Waiting) world.Life.Splash(flow.Map, wx, wy, 0.8f, 2);
            if (cast.Stage == FishingStage.Hooked)
            {
                flow.Player.ShowBubble(EmoteBubble.Exclaim, FishingAttempt.HookSeconds(cast.Rod));
                world.Life.Splash(flow.Map, wx, wy, 1.2f, 4);
                AudioManager.PlaySound("fish_bite");
            }
            if (cast.Says)
            {
                if (cast.Stage == FishingStage.Landed) AudioManager.PlaySound("fish_reel");
                flow.Dialogue.ShowDialogue("", new[] { cast.Message });
                flow.State = GameState.Dialogue;
                cast.Read();
                return;
            }
        }
        if (cast.Stage != FishingStage.Done) return;
        flow.Fishing = null;
        if (cast.Caught is { } fish)
        {
            flow.Player.Encounters.Reset();
            flow.Hooked = true;
            StartWildBattle(fish);
        }
    }

    /// <summary>A press of the confirm button for a rod's cast, given by a tool in place of the keys (the screenshot harness).</summary>
    public bool FishingPress { get; set; }

    /// <summary>Where a rod's cast has got to, for tools; null when no rod is out.</summary>
    public FishingStage? CastStage => flow.Fishing?.Stage;

    /// <summary>What the field moves left in force, as the map and the player see it: boulders that can be pushed, a cave lit, fog lifted.</summary>
    private void KeepFieldMovesInForce()
    {
        // On the water after a save is loaded, nobody has said whose Surf it was: the first of the team who knows it
        if (flow.Player.Mount != null && flow.Player.Carrier == null)
            flow.Player.Carrier = flow.Party.Members.FirstOrDefault(p => p.Moves.Any(m => m.Name == "Surf"))?.LookName;
        flow.Player.PushesBoulders = flow.Story.Has(FieldMoveRules.StrengthFlag);
        flow.Player.HasRunningShoes = flow.Story.Has(StoryState.RunningShoesFlag);
        flow.Map.Lit = flow.Story.Has(FieldMoveRules.FlashFlag);
        flow.Map.FogLifted = flow.Story.Has(FieldMoveRules.DefogFlag);
    }

    /// <summary>A field move is used: its cut-in plays (with the Pokémon's cry, as the original's), and the script waits for it.</summary>
    private void StartCutIn(FieldMove move, Pokemon user, NPC? subject)
    {
        flow.CutIn = new FieldCutIn(user.LookName, move);
        flow.CutInSubject = subject;
        PokemonSprites.GetBaked(user.LookName, SpriteView.Front);
        AudioManager.PlayCry(user);
    }

    /// <summary>The cut-in runs on; as its band closes, what the move does to the obstacle in front happens.</summary>
    private void UpdateCutIn(float dt)
    {
        if (flow.CutIn == null) return;
        flow.CutIn.Advance(dt);
        if (!flow.CutIn.Done) return;
        if (flow.CutInSubject is { Obstacle: { } kind } subject && flow.CutIn.FieldMove is FieldMove.Cut or FieldMove.RockSmash)
        {
            world.Life.GiveWay(flow.Map, subject.GridX, subject.GridY, kind);
            AudioManager.PlaySound(kind == PropType.CutTree ? "cut" : "rock_smash");
        }
        flow.CutIn = null;
        flow.CutInSubject = null;
    }

    private void DrawCutIn()
    {
        if (flow.CutIn == null) return;
        var sprite = PixelArtGenerator.GetPokemonSprite(flow.CutIn.Model, isBack: false);
        ModernUi.DrawCutIn(VirtualWidth, VirtualHeight, sprite, flow.CutIn.Move, flow.CutIn.Band, flow.CutIn.Slide, flow.CutIn.Label);
    }

    /// <summary>
    /// A boulder the player pushed starts sliding on to its new tile (it is there already), with its sound; a block of
    /// ice slides over every tile the ice carried it (plan 20 · J11), at the ice's own pace.
    /// </summary>
    private void SlideBoulders(float dt)
    {
        if (flow.Player.TakePush() is var (pushed, way))
        {
            var (dx, dy) = FieldMovement.Delta(way);
            int tiles = Math.Max(1, flow.Player.PushedTiles);
            dx *= tiles;
            dy *= tiles;
            float length = pushed.IsIceBlock ? tiles * FieldMovement.IceBlockSecondsATile : FieldMovement.BoulderPushSeconds;
            slides.RemoveAll(s => s.Boulder == pushed);
            slides.Add((pushed, length, length, dx, dy));
            pushed.StepOffsetX = -dx;
            pushed.StepOffsetY = -dy;
            AudioManager.PlaySound("boulder");
        }
        for (int i = slides.Count - 1; i >= 0; i--)
        {
            var (boulder, left, length, dx, dy) = slides[i];
            left -= dt;
            float rest = Math.Max(0f, left / length);
            boulder.StepOffsetX = -dx * rest;
            boulder.StepOffsetY = -dy * rest;
            if (left > 0f)
            {
                slides[i] = (boulder, left, length, dx, dy);
                continue;
            }
            slides.RemoveAt(i);
            // Come to rest on a pit, it falls in: the pit's own script says where to (plan 02 · S12)
            if (FieldScripts.PitUnder(flow.Map, boulder) is { } pit) StartScript(pit, boulder, file: flow.Map.ScriptFileAt(boulder.GridX, boulder.GridY));
        }
    }

    // ------------------------------------------------------------------ a frame

    /// <summary>One frame of the field while a script has it.</summary>
    private void UpdateScript(float dt)
    {
        // The menu button held while a scene runs skips it (plan 12 · Q20): the picture closes, and nothing more of
        // this frame is the scene's
        if (ReadTheSkip(dt)) return;
        AdvanceScriptedWalks(FieldTime(dt));
        UpdateCutIn(dt);
        RunScript(dt);
    }

    /// <summary>
    /// A frame's time as the field has it (plan 09 · L4; style guide, "The camera kit"): the clock's, unless a scene's
    /// slow motion has lowered the field's pace. What the field does is handed this (people's steps and gestures, the
    /// player's, life, weather, a puzzle's pieces); the camera, the bars, the text, a bubble, a fade and a script's own
    /// waits are handed the clock's, so a scene's lines mean the same seconds at any pace.
    /// </summary>
    private float FieldTime(float dt)
    {
        float pace = world.Camera.Pace;
        return pace == 1f ? dt : dt * pace;
    }

    private void RunScript(float dt)
    {
        try
        {
            flow.Runner.Update(dt);
        }
        catch (ScriptException e)
        {
            // A script that goes wrong ends there; the game goes on
            Log.Error("A script went wrong: " + e.Message);
            ShowNotification(Text.Of("engine.scriptWentWrong", ("problem", e.Message)));
            flow.Runner.Abort();
        }
        RefreshPresence();
        if (!flow.Runner.IsRunning)
        {
            EndScript();
            // The tiles a step into a new place ended on are tried once that place's own script is done (OnStep)
            if (flow.TriggerAfterEnter)
            {
                flow.TriggerAfterEnter = false;
                if (flow.State == GameState.Overworld) TryStepTrigger();
            }
        }
    }

    /// <summary>People a script set walking take their steps, and the player theirs or stands still. Also under text.</summary>
    private void AdvanceScriptedWalks(float dt)
    {
        for (int i = flow.NpcWalks.Count - 1; i >= 0; i--)
        {
            flow.NpcWalks[i].Update(dt);
            if (flow.NpcWalks[i].IsDone) flow.NpcWalks.RemoveAt(i);
        }

        if (flow.PlayerWalk == null)
        {
            // A step a field move began (out onto the water, up a waterfall or a rock face) is taken to its end
            if (flow.Player.IsMoving)
            {
                flow.Player.Advance(dt, flow.Map, null, false, null, null, () =>
                {
                    StepLeavesItsMark();
                    return false;
                });
                if (!flow.Player.IsMoving && flow.Player.Mode != musicTravel) PlayFieldMusic();
            }
            else flow.Player.StandStill(dt);
            if (!flow.Player.IsMoving) flow.FieldMoveStep = false;
            return;
        }
        flow.PlayerWalk.Update(dt, flow.Player, flow.Map, StepLeavesItsMark);
        // A Pokémon out of its ball keeps behind the player on a walk a script sends them on too (plan 06 · R16c); a
        // person who travels along is moved by the scene itself
        if (flow.Along?.Pokemon != null) TrailBehind();
        if (flow.PlayerWalk.IsDone) flow.PlayerWalk = null;
    }

    /// <summary>
    /// Whatever a script left going is brought to its end: nobody is left mid-step, the screen comes back, the camera
    /// returns, and with it the lens, the bars and the field's pace (plan 09 · L4), however the script ended.
    /// </summary>
    private void EndScript()
    {
        foreach (var walk in flow.NpcWalks) walk.Finish();
        flow.NpcWalks.Clear();
        flow.PlayerWalk = null;
        if (flow.ScriptFade.Level > 0f) flow.ScriptFade.To(false, ScriptParser.FadeSeconds);
        if (flow.CameraSent)
        {
            world.ReleaseCamera(ScriptParser.ReleaseSeconds);
            flow.CameraSent = false;
        }
        // (Answers already given slide away by themselves; only a question left open is taken off the screen)
        if (flow.Choice.IsOpen) flow.Choice.Dismiss();
        flow.PendingAnswers = null;
        // A hold begun in a scene that is over counts for nothing (plan 12 · Q20)
        skipHold.Reset();
        // A trainer's eye theme that no battle cut in on (their script had no battle) gives way to the place's own
        if (eyeThemePlaying && flow.State is GameState.Overworld or GameState.Dialogue) PlayFieldMusic();
    }

    /// <summary>
    /// One frame of text on the screen: the line is written and waits, a question waits for its answer, and the
    /// script that showed it goes on in the same frame the box closes, so the box doesn't blink between two talks.
    /// </summary>
    private void UpdateDialogue(float dt)
    {
        // The menu button held while a scene's lines are on the screen skips it too: they are put away unread
        if (ScriptRunning && ReadTheSkip(dt)) return;
        if (ScriptRunning) AdvanceScriptedWalks(FieldTime(dt));

        flow.Dialogue.AutoAdvanceAfter = GameSettings.AutoAdvanceAfter(Settings.AutoAdvance);
        if (flow.Choice.IsOpen) flow.Choice.ReadKeys();
        else flow.Dialogue.Update(dt);
        AnswerQuestion();

        // Back to the field, unless the last line led somewhere else (a ship leaving for another region)
        if (!flow.Dialogue.IsActive && flow.State == GameState.Dialogue)
        {
            flow.State = GameState.Overworld;
            if (ScriptRunning) RunScript(0f);
        }
    }

    /// <summary>A question written out shows its answers; the answer given closes the box.</summary>
    private void AnswerQuestion()
    {
        if (!flow.Dialogue.AwaitingAnswer || flow.Choice.IsOpen) return;
        if (flow.Choice.Take() is { } picked)
        {
            // The Vs. Recorder's question has its own answer, which leaves the script's alone (plan 08 · P10)
            if (flow.RecordAsked)
            {
                flow.Dialogue.Close();
                AnswerTheRecord(picked == 0);
                return;
            }
            flow.ScriptAnswer = picked;
            flow.Dialogue.Close();
        }
        else if (flow.PendingAnswers != null)
        {
            flow.Choice.Open(flow.PendingAnswers, flow.PendingCancel);
            flow.PendingAnswers = null;
        }
    }

    /// <summary>The script's own fade and its question's answers, drawn over the field: the text box is between them.</summary>
    private void DrawScriptFade()
    {
        if (flow.ScriptFade.Level <= 0f) return;
        Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)MathF.Round(flow.ScriptFade.Level * 255f)));
    }

    /// <summary>
    /// A scene's cinema bars (plan 09 · L4; style guide, "The camera kit"): the title's own, plain black at the top and
    /// the bottom, over the field and a script's black screen and under the text box and every panel.
    /// </summary>
    private void DrawLetterbox()
    {
        int bar = (int)world.Camera.Bars;
        if (bar <= 0) return;
        Raylib.DrawRectangle(0, 0, VirtualWidth, bar, Color.Black);
        Raylib.DrawRectangle(0, VirtualHeight - bar, VirtualWidth, bar, Color.Black);
    }

    private void DrawChoice()
    {
        if (flow.Choice.Visible) ModernUi.DrawChoices(VirtualWidth, VirtualHeight, flow.Choice.Options, flow.Choice.Cursor, flow.Choice.Shown);
    }

    private bool steppedFromPlank;

    /// <summary>What a step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out onto it.</summary>
    private void StepLeavesItsMark()
    {
        // A step onto, along or off a plank bridge of Hoenn knocks on the wood (Emerald's SE_BRIDGE_WALK: the plank
        // under the player or the one they left; plan 21 · H6, part c). The dip itself is the renderer's.
        bool onPlank = (flow.Player.Mode is TravelMode.OnFoot or TravelMode.Cycling) && FieldLife.IsPlank(flow.Map, flow.Player.GridX, flow.Player.GridY);
        if (onPlank || steppedFromPlank) AudioManager.PlaySound("bridge_walk");
        steppedFromPlank = onPlank;
        if (flow.Player.JustRodeOut) world.Life.Splash(flow.Map, flow.Player.GridX, flow.Player.GridY);
        else world.Life.Footstep(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing, flow.Player.IsRunning, flow.Player.Mode);
        if (flow.Player.JustLanded && flow.Player.Mode != TravelMode.Surfing) world.Life.Landing(flow.Map, flow.Player.GridX, flow.Player.GridY);
    }

    /// <summary>Goes to a tile of a map through a fade, as a door does: a script's warp.</summary>
    private void WarpTo(string mapName, int x, int y, Direction? facing)
    {
        var target = MapDatabase.Get(mapName);
        PlayAreaMusic(target, x, y);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = target;
            flow.Player.SetPosition(x, y, facing ?? flow.Player.Facing);
            world.Life.Clear();
            ArriveOnMap();
            AnnounceLocation();
        });
    }

    // ------------------------------------------------------------------ the host

    /// <summary>The game as a script sees it.</summary>
    private sealed partial class FieldHost : IScriptHost
    {
        private readonly GameEngine game;

        public FieldHost(GameEngine game) => this.game = game;

        public StoryState Story => game.flow.Story;
        public string Region => game.Here.Id;
        public IReadOnlyCollection<FieldMove> Granted => game.GrantedMoves;
        public string BeganIn => game.flow.BeganIn;
        public bool ShowsObjectives => game.NextObjectiveOn;
        public Party Party => game.flow.Party;
        public Inventory Bag => game.flow.Bag;
        public Poketch Poketch => game.flow.Poketch;
        public SafariGame Safari => game.flow.Safari;
        public BugContest BugContest => game.flow.BugContest;
        public void PutInBox(Pokemon pokemon) => game.flow.Boxes.Store(pokemon);
        public int LevelHere(int level) => game.LevelHere(level);
        public int SeenInSinnoh => game.flow.Pokedex.SeenIn(Pokedex.Sinnoh);
        public int Caught => game.flow.Pokedex.CaughtCount;

        public int Money
        {
            get => game.flow.Money;
            set => game.flow.Money = Math.Max(0, value);
        }

        public string PlayerName => PlayerIdentity.Name;
        public PlayerLook PlayerLook => PlayerIdentity.Look;

        // ---- the field

        public NPC? FindNpc(string name, string? place) => game.flow.Map.FindPerson(name, place);

        public (int X, int Y) TileOf(NPC? who) => who == null ? (game.flow.Player.GridX, game.flow.Player.GridY) : (who.GridX, who.GridY);

        public Direction FacingOf(NPC? who) => who?.Facing ?? game.flow.Player.Facing;

        public void Face(NPC? who, Direction direction)
        {
            if (who == null) game.flow.Player.Facing = direction;
            else who.Facing = direction;
        }

        public void Place(NPC? who, int x, int y, Direction? facing)
        {
            if (who == null)
            {
                game.flow.Player.SetPosition(x, y, facing ?? game.flow.Player.Facing);
                // A Pokémon out of its ball is nobody's to place: it is behind the player wherever a script puts them
                // (plan 06 · R16c: the huts of Amity Square). A person who travels along is the scene's own to move
                if (game.flow.Along is { Pokemon: not null } stroller && game.flow.Map.Follower == stroller.Who)
                {
                    game.PutBehind(stroller, x, y, game.flow.Player.Facing);
                    game.partnerHeading = game.flow.Player.Heading;
                }
                return;
            }
            StopWalk(who);
            (who.GridX, who.GridY) = (x, y);
            if (facing is { } direction) who.Facing = direction;
        }

        public void SetVisible(NPC who, bool visible)
        {
            who.Forced = visible;
            game.flow.Map.ApplyPresence(game.flow.Story.Has);
        }

        public void Walk(NPC? who, IReadOnlyList<ScriptStep> steps, bool fast)
        {
            if (who == null)
            {
                game.flow.PlayerWalk = new PlayerWalk(steps, fast);
                return;
            }
            StopWalk(who);
            game.flow.NpcWalks.Add(new NpcWalk(who, steps, fast));
        }

        /// <summary>Whoever is told to go somewhere else first arrives where they were going.</summary>
        private void StopWalk(NPC who)
        {
            game.flow.Wandering.Settle(who);
            if (game.flow.Along?.Who == who) game.flow.Along.Settle();
            foreach (var walk in game.flow.NpcWalks.Where(w => w.Who == who)) walk.Finish();
            game.flow.NpcWalks.RemoveAll(w => w.Who == who);
        }

        public bool Walking => game.flow.PlayerWalk != null || game.flow.NpcWalks.Count > 0;

        public void Emote(NPC? who, EmoteBubble bubble, float seconds)
        {
            // (A bubble passes: a hurry drops it)
            if (Skipping) return;
            if (who == null) game.flow.Player.ShowBubble(bubble, seconds);
            else who.ShowBubble(bubble, seconds);
            if (bubble == EmoteBubble.Exclaim) AudioManager.PlaySound("exclaim", who == null ? 0f : game.PanAt(who.GridX));
        }

        public void Act(NPC? who, Gesture gesture, int times)
        {
            // (A gesture passes too; its time passes for whoever is walking)
            if (Skipping)
            {
                game.WalkOnFor(Acting.Total(gesture, times));
                return;
            }
            if (who == null) game.flow.Player.Act(gesture, times);
            else who.Act(gesture, times);
            game.actor = (true, who);
        }

        public void Mood(NPC? who, Mood mood)
        {
            if (who == null) game.flow.Player.Mood = mood;
            else who.Mood = mood;
        }

        public void Pose(NPC? who, Overworld.Stance stance)
        {
            if (who == null) game.flow.Player.Posed = stance == Overworld.Stance.Stand ? null : stance;
            else who.Posed = stance;
        }

        public float Camera(CameraMove move, int x, int y, float seconds, float factor = 1f, NPC? who = null, IReadOnlyList<(int X, int Y)>? path = null)
        {
            // In a hurry the camera is told the same and is there at once: where it was sent, at its lens, or home
            // again (a shake passes, and is dropped)
            if (!Skipping) return Tell(move, x, y, seconds, factor, who, path);
            // (The time the move would have taken is still told: it passes for whoever is walking)
            float takes = move == CameraMove.Shake ? 0f : Tell(move, x, y, seconds, factor, who, path);
            game.world.Camera.Settle();
            return takes;
        }

        private float Tell(CameraMove move, int x, int y, float seconds, float factor, NPC? who, IReadOnlyList<(int X, int Y)>? path)
        {
            var rig = game.world.Camera;
            switch (move)
            {
                case CameraMove.Pan:
                    rig.Pan(x + 0.5f, y + 0.5f, seconds);
                    game.flow.CameraSent = true;
                    return seconds;
                case CameraMove.Release:
                    rig.Release(seconds);
                    game.flow.CameraSent = false;
                    return seconds;
                case CameraMove.Zoom:
                    rig.ZoomTo(factor, seconds);
                    game.flow.CameraSent = true;
                    return seconds;
                case CameraMove.Follow:
                    // The player followed is the camera back where it belongs, with the lens and the bars left alone
                    if (who == null) rig.FollowPlayer(seconds);
                    else rig.Follow(who, seconds);
                    game.flow.CameraSent = true;
                    return seconds;
                case CameraMove.Path:
                    game.flow.CameraSent = true;
                    return rig.Path((path ?? Array.Empty<(int X, int Y)>()).Select(t => new System.Numerics.Vector2(t.X + 0.5f, t.Y + 0.5f)).ToList(), factor);
                default:
                    game.world.ShakeCamera(seconds);
                    return 0f;
            }
        }

        public void Letterbox(bool on, float seconds)
        {
            game.world.Camera.Letterbox(on, Skipping ? 0f : seconds);
            game.flow.CameraSent = true;
        }

        public void SlowMotion(float share, float seconds)
        {
            // (Slow motion passes by itself: a hurry never begins it)
            if (Skipping) return;
            game.world.Camera.SlowMotion(share, seconds);
            game.flow.CameraSent = true;
        }

        public void Effect(SceneEffect kind, int x, int y, float seconds, Direction? way, bool hold)
        {
            // In a hurry an effect that passes is dropped; one that is held is lit, since the scene leaves it lit
            if (Skipping && !hold) return;
            game.world.Life.Effect(game.flow.Map, kind, x, y, seconds, way ?? Direction.Right, hold);
            AudioManager.PlaySound(SceneEffects.SoundOf(kind), game.PanAt(x));
        }

        public void StopEffect(int x, int y) => game.world.Life.StopEffect(x, y);

        // ---- what the script waits for

        public bool Busy => game.flow.State != GameState.Overworld || game.flow.Dialogue.IsActive || game.flow.ScriptFade.IsMoving || game.flow.CutIn != null
            || game.flow.FieldMoveStep || game.PuzzleMoving || game.AwaitingStroll || game.ActorBusy
            // A skip's dark keeps the script waiting while it closes and opens, but not the hurry in its middle (plan 12 · Q20)
            || (game.skipPhase != SkipPhase.None && !game.skipRunning);

        public void Say(string? speaker, IReadOnlyList<string> lines)
        {
            // (In a hurry the lines are dropped unread)
            if (lines.Count == 0 || Skipping) return;
            game.flow.Dialogue.ShowDialogue(speaker ?? "", lines);
            game.flow.State = GameState.Dialogue;
        }

        public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel)
        {
            game.flow.Choice.Dismiss();
            game.flow.PendingAnswers = answers;
            game.flow.PendingCancel = cancel;
            game.flow.ScriptAnswer = Math.Max(0, cancel);
            game.flow.Dialogue.ShowQuestion(speaker ?? "", question);
            game.flow.State = GameState.Dialogue;
        }

        public int Answer => game.flow.ScriptAnswer;

        public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first, BattleTerms? terms = null)
        {
            game.flow.ScriptOutcome = BattleOutcome.None;
            game.flow.BattleMayBeLost = mayLose;
            game.StartTrainerBattle(trainer, second, partner, first, terms);
        }

        public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee, LessonCatcher? catcher = null)
        {
            game.flow.ScriptOutcome = BattleOutcome.None;
            game.flow.BattleMayBeLost = false;
            // A script writes its game's own level; a region crossed into meets it at the mapped one (plan 18 · W4)
            if (kind != BattleKind.CatchingLesson) wild.Relevel(game.LevelHere(wild.Level));
            game.MeetWildPokemon(wild, kind, cannotFlee, catcher: catcher);
        }

        public BattleOutcome Outcome => game.flow.ScriptOutcome;

        public void Open(ScriptScreen screen, NPC? subject, string? counter = null)
        {
            game.flow.ScriptAnswer = 0;
            switch (screen)
            {
                case ScriptScreen.Starter:
                    game.flow.State = GameState.StarterSelect;
                    game.flow.StarterSelectScreen.Open(game.Here);
                    break;
                case ScriptScreen.Shop:
                    game.flow.State = GameState.Shop;
                    // The counter's own stock: the clerk's specialties, or the common list by the badges (plan 06 · R11)
                    game.flow.ShopScreen.Open(game.flow.Map.ShownNameAt(game.flow.Player.GridX, game.flow.Player.GridY),
                        MartDatabase.Stock(counter ?? subject?.Mart, game.flow.Story.BadgeCount(game.Here.Id)));
                    break;
                case ScriptScreen.Wardrobe:
                    // At home the clothes owned; at a boutique its stock as well, headed with the place's name
                    game.flow.State = GameState.Wardrobe;
                    game.flow.WardrobeScreen.Open(game.flow.Wardrobe, counter != null ? ClothingDatabase.Stock(counter) : null,
                        counter != null ? game.flow.Map.ShownNameAt(game.flow.Player.GridX, game.flow.Player.GridY) : null);
                    break;
                case ScriptScreen.HallOfFame:
                    game.flow.State = GameState.HallOfFame;
                    game.flow.HallOfFameScreen.Open();
                    break;
                case ScriptScreen.ChoosePokemon:
                    game.flow.State = GameState.PartyMenu;
                    // A species named on the line lets only that species be chosen (plan 14 · B14): the rest are greyed
                    if (counter is { Length: > 0 } only) game.flow.PartyScreen.OpenToChoose(p => ScriptRunner.Fits(p, only), Text.Of("engine.onlyCanBeChosen", ("only", only)));
                    else game.flow.PartyScreen.OpenToChoose();
                    break;
                case ScriptScreen.ChooseItem:
                    // A berry to plant or a mulch to lay (plan 06 · R14a): the bag, on that pocket
                    game.flow.ScriptItem = null;
                    game.flow.State = GameState.BagMenu;
                    game.flow.BagScreen.Registered = game.flow.RegisteredItem;
                    game.flow.BagScreen.OpenToPick(counter ?? "berries");
                    break;
                case ScriptScreen.PoffinCooking:
                    // The Poffin House's pot (plan 06 · R14c): the bag, to choose the first berry
                    game.StartCooking();
                    break;
                case ScriptScreen.BallCapsules:
                    game.OpenBallCapsules(fromBag: false);
                    break;
                case ScriptScreen.SealShop:
                    game.OpenSealShop();
                    break;
                case ScriptScreen.ContestPhoto:
                    if (System.Enum.TryParse<ContestStat>(counter, out var framed)) game.ShowContestPhoto(framed);
                    break;
                case ScriptScreen.Pc:
                    game.flow.State = GameState.PCStorage;
                    game.flow.PCScreen.Open(game.flow.Boxes);
                    game.flow.PCScreen.ShowCondition = game.flow.Story.Has(ContestHallVisitedFlag);
                    break;
                // The Hall of Fame's end (plan 02 · S14): the scene of the team's entry, and the credits
                case ScriptScreen.Ceremony:
                    game.OpenCeremony();
                    break;
                case ScriptScreen.Credits:
                    game.OpenCredits();
                    break;
                // A stone panel of the Ruins of Alph (plan 20 · J5): the script waits, and hears whether it was solved
                case ScriptScreen.AlphPuzzle:
                    game.flow.AlphPuzzleScreen.Open(counter ?? "kabuto");
                    // The picture is the species' menu sprite, asked for now so that it is baked by the time it is seen
                    PokemonSprites.Request(game.flow.AlphPuzzleScreen.Puzzle!.Species);
                    game.flow.State = GameState.AlphPuzzle;
                    break;
                // A phrase of two words (plan 08 · P9): the picker, on the phrase given last time under the same name
                case ScriptScreen.Phrase:
                    game.OpenPhrases(counter ?? PokemonPlatinumEngine.Story.StoryState.InterviewPhrase);
                    break;
                // A map on a wall (plan 08 · P8): the Town Map's screen in its wall mode, back to the field when put
                // away; where the region has no map nothing opens and the script hears 0
                case ScriptScreen.TownMap:
                    game.flow.ScriptAnswer = game.OpenTownMap(TownMapMode.Wall, fromBag: false) ? 1 : 0;
                    break;
                // A region's rival named (plan 18 · W4, part b): the introduction's keyboard over the field
                case ScriptScreen.NameRival:
                    game.OpenRivalNaming();
                    break;
                // The Bug-Catching Contest's board of results (plan 15 · E5)
                case ScriptScreen.BugContestResults:
                    game.OpenBugContestResults();
                    break;
                case ScriptScreen.Travel:
                {
                    // The way to the next region, where this map has one: the attendant says how things stand, and
                    // the script hears 2 when the story of this region is done and the vessel may leave (plan 18 · W5:
                    // common.Crossing asks, and crosses with "travel go"), 1 when it may not
                    if (RegionDatabase.RegionOfMap(game.flow.Map.Name) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link) break;
                    var check = RegionDatabase.CheckTravel(link, game.flow.Story);
                    game.flow.ScriptAnswer = check == TravelCheck.Ready ? 2 : 1;
                    game.flow.Dialogue.ShowDialogue(subject == null ? "" : Text.PersonName(subject.LinesPlace, subject.LinesKey, subject.Name), RegionDatabase.AttendantLines(link, check));
                    game.flow.State = GameState.Dialogue;
                    break;
                }
                case ScriptScreen.Crossing:
                {
                    // The journey on screen (plan 18 · W5), over the script's dark: the field's music stops, the vessel
                    // is heard and its one line is said, and as the line is put away the crossing is made; the script
                    // waits through the arrival's own fade. Where no way leads on now nothing happens
                    if (RegionDatabase.RegionOfMap(game.flow.Map.Name) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link
                        || RegionDatabase.CheckTravel(link, game.flow.Story) != TravelCheck.Ready) break;
                    var to = RegionDatabase.Get(link.To)!;
                    AudioManager.StopMusic();
                    AudioManager.PlaySound(RegionDatabase.CrossingSound(link));
                    game.flow.Dialogue.ShowDialogue("", new[] { RegionDatabase.VoyageLine(link) }, () => game.TravelTo(to));
                    game.flow.State = GameState.Dialogue;
                    break;
                }
            }
        }

        public void EnterHallOfFame()
        {
            game.flow.HallOfFame.Enter(game.flow.Party, p => p.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (playerName, game.flow.TrainerId), GameClock.Moment);
            game.flow.TrainerScore = TrainerScore.Add(game.flow.TrainerScore, TrainerScore.HallOfFame);
        }

        public bool Trade(string trade, int slot)
        {
            if (NpcTrades.Get(trade) is not { } t || NpcTrades.Trade(t, game.flow.Party, slot, GameClock.Today) is not { } received) return false;
            game.flow.Pokedex.RegisterSeen(received);
            game.flow.Pokedex.RegisterCaught(received);
            game.flow.Poketch.Remember(received);
            AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
            return true;
        }

        public bool GivePokemon(Pokemon pokemon)
        {
            // A gift of a region crossed into comes at the region's mapped level (plan 18 · W4); an Egg hatches at its own
            if (!pokemon.IsEgg) pokemon.Relevel(game.LevelHere(pokemon.Level));
            game.flow.Pokedex.RegisterSeen(pokemon);
            game.flow.Pokedex.RegisterCaught(pokemon);
            // A gift is met where it is given (Pokemon_GiveMonFromScript)
            pokemon.Met(game.PlaceName(), GameClock.Today);
            game.flow.Poketch.Remember(pokemon);
            if (game.flow.Party.Add(pokemon)) return true;
            game.flow.Boxes.Store(pokemon);
            return false;
        }

        public void Warp(string map, int x, int y, Direction? facing)
        {
            // In a hurry the player arrives at once, behind the skip's dark, as at the middle of the warp's own fade
            if (Skipping) game.ArriveAtOnce(map, x, y, facing);
            else game.WarpTo(map, x, y, facing);
        }

        public void Fade(bool toBlack, float seconds)
        {
            if (!Skipping)
            {
                game.flow.ScriptFade.To(toBlack, seconds);
                return;
            }
            // In a hurry the screen is black, or the field again, at once; a fade's time passes for whoever is walking
            bool moves = game.flow.ScriptFade.Level != (toBlack ? 1f : 0f);
            game.flow.ScriptFade.To(toBlack, 0f);
            if (moves) game.WalkOnFor(seconds);
        }

        // ---- field moves

        public void UseMove(FieldMove move, Pokemon user, NPC? subject)
        {
            game.flow.Journal.Tell(new JournalEvent(JournalEventKind.UsedFieldMove, FieldMoveRules.MoveName(move)));
            // Whose Surf it is carries the player (plan 10 · F1)
            if (move is FieldMove.Surf or FieldMove.Dive) game.flow.Player.Carrier = user.LookName;
            // (A hurry begun while the move's own line was being read uses the move without its cut-in)
            if (Skipping) return;
            game.StartCutIn(move, user, subject);
        }

        public void Note(JournalEvent line) => game.flow.Journal.Tell(line);

        public bool Surf()
        {
            if (!game.flow.Player.StartSurf(game.flow.Map)) return false;
            AudioManager.PlaySound("surf");
            game.flow.FieldMoveStep = true;
            return true;
        }

        public bool Climb() => game.flow.FieldMoveStep = game.flow.Player.Climb(game.flow.Map);

        public bool Dive() => game.Dive();

        public bool Fly()
        {
            if (game.flow.FlyTarget is not { } town) return false;
            game.flow.FlyTarget = null;
            game.flow.Player.SetMode(TravelMode.OnFoot);
            // Flying sends every roamer anywhere (FieldSystem_SetFlyFlags; plan 06 · R13)
            Roamers.Scatter(game.flow.Encounters, game.fieldRandom);
            // ...to the overworld of the town's own region (plan 20 · J9); a town of another region is a crossing
            // there, under Fly's own fade (plan 18 · W9)
            if (RegionDatabase.RegionOfMap(town.Map) is { } to && !string.Equals(to.Id, game.Here.Id, StringComparison.OrdinalIgnoreCase))
                game.CrossTo(to, new MapSpot(town.Map, town.X, town.Y, Direction.Down));
            else game.WarpTo(town.Map, town.X, town.Y, Direction.Down);
            return true;
        }

        public bool Teleport()
        {
            // (the region's own place to come back to, on its own overworld: plan 18 · W9)
            if (SpawnLocations.Respawn(game.flow.Story, game.Here.Id) is not { } town) return false;
            game.flow.Player.SetMode(TravelMode.OnFoot);
            // So does Teleport (FieldSystem_SetTeleportFlags)
            Roamers.Scatter(game.flow.Encounters, game.fieldRandom);
            game.WarpTo(town.Map, town.X, town.Y, Direction.Down);
            return true;
        }

        public void Turnback() => TurnbackCave.Reaim(game.flow.Map, game.flow.Player.GridX, game.flow.Player.GridY, game.flow.Story, Dice.Shared);

        public void TravelWith(NPC? who, string? trainerId) => game.SetPartner(who, trainerId);

        public string? Partner => game.partner?.TrainerId;

        public bool Escape()
        {
            if (game.flow.ExitSpot is not { } exit) return false;
            game.flow.Player.SetMode(TravelMode.OnFoot);
            game.WarpTo(exit.Map, exit.X, exit.Y, exit.Facing);
            return true;
        }

        public bool SweetScent()
        {
            var map = game.flow.Map;
            int x = game.flow.Player.GridX, y = game.flow.Player.GridY;
            var underfoot = map.BehaviourAt(x, y);
            // (on the sea floor its seaweed draws out the floor's water Pokémon, as a step there meets them: plan 21 · H5)
            bool diving = game.flow.Player.Mode == TravelMode.Diving;
            bool water = game.flow.Player.Mode == TravelMode.Surfing || diving;
            bool where = diving ? underfoot is TileBehavior.Seaweed or TileBehavior.SeaweedNoSurfacing : water == TileBehaviors.IsSurfable(underfoot);
            if (!TileBehaviors.HasEncounters(underfoot) || !where) return false;
            if (map.DrawOutWild(x, y, water, game.flow.Player.Lead, game.EncounterMomentNow()) is not { } wild) return false;
            game.flow.ScriptOutcome = BattleOutcome.None;
            game.flow.BattleMayBeLost = false;
            game.flow.Player.Encounters.Reset();
            game.StartWildBattle(wild);
            return true;
        }

        public bool SmashEncounter()
        {
            var map = game.flow.Map;
            var player = game.flow.Player;
            // HeartGold's table is in hundredths and its rock stands on the tile ahead (plan 20 · J9); the GBA games' is in sixteenths and counted where the player stands (plan 21 · H4)
            WildEncounterEntry? found;
            if (game.Here.Id == "johto")
            {
                var (dx, dy) = player.Facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
                found = map.SmashRock(player.GridX + dx, player.GridY + dy, player.Lead);
            }
            else found = map.SmashOut(player.GridX, player.GridY, player.Lead);
            if (found is not { } wild) return false;
            game.flow.ScriptOutcome = BattleOutcome.None;
            game.flow.BattleMayBeLost = false;
            game.flow.Player.Encounters.Reset();
            game.StartWildBattle(wild);
            return true;
        }

        // ---- sound

        public void Music(string? song)
        {
            if (song == null) game.PlayFieldMusic();
            else if (song.Length == 0) AudioManager.StopMusic();
            else AudioManager.PlayMusic(song);
        }

        public void Fanfare(MusicRole role) => AudioManager.PlayFanfare(role);

        public void Sound(string name) => AudioManager.PlaySound(name);

        // ---- wild Pokémon (plan 06 · R13)

        public SpecialEncounters Encounters => game.flow.Encounters;

        public int? HoneyTreeFaced => HoneyTrees.Faced(game.flow.Map, game.flow.Player.GridX, game.flow.Player.GridY, game.flow.Player.Facing);

        public uint TrainerNumber => game.TrainerNumber;

        public Random Chance => game.fieldRandom;

        // ---- the clock (plan 06 · R14a)

        public TimeOfDay TimeOfDay => GameClock.Now;

        public DayOfWeek Weekday => GameClock.Today.DayOfWeek;

        public IEnumerable<Pokemon> Boxed => game.flow.Boxes.All;

        // ---- the television (plan 08 · P8): the clock as the game reads it, the time played and the whole Pokédex
        public BroadcastFacts Broadcast()
        {
            float hour = GameClock.Hour;
            int whole = (int)hour;
            return Broadcasts.FactsOf(this) with
            {
                Hour = ((whole % 24) + 24) % 24,
                Minute = Math.Clamp((int)((hour - whole) * 60f), 0, 59),
                PlayTimeSeconds = game.flow.PlayTime,
                Seen = game.flow.Pokedex.SeenCount
            };
        }

        public BerryPatches Berries => game.flow.Berries;
        public string? ChosenItem => game.flow.ScriptItem;

        // ---- Poffins (plan 06 · R14c)

        public PoffinCase Poffins => game.flow.PoffinCase;

        public DayCare DayCare => game.flow.DayCare;

        // ---- the Super Contests (plan 06 · R17a)

        public Models.SuperContests.FashionCase FashionCase => game.flow.FashionCase;
        public Models.SuperContests.ContestOutcome? ContestOutcome => game.flow.ContestOutcome;

        public void HoldContest(int slot, ContestStat category, Models.SuperContests.ContestRank rank, Models.SuperContests.ContestMode mode) =>
            game.HoldContest(slot, category, rank, mode);

        public bool Hatch() => game.HatchFirstEgg();

        public void Seen(PokemonSpecies species) => game.flow.Pokedex.RegisterSeen(species, null, Gender.Genderless);

        // A number registered: the jingle and the word over the field (plan 20 · J3; dropped while a scene is skipped)
        public void ContactAdded(PhoneContact contact)
        {
            if (Skipping) return;
            AudioManager.PlaySound("phone_registered");
            game.ShowNotification(contact.Region == "hoenn"
                ? Text.Of("pokenav.matchCall.registered", ("name", contact.Name))
                : Text.Of("pokegear.phone.registered", ("name", contact.Name)));
        }

        // ---- Ball Capsules, Seals and ribbons (plan 06 · R17b)

        public SealCase SealCase => game.flow.SealCase;
        public Models.SuperContests.ContestRecords ContestRecords => game.flow.ContestRecords;

        // ---- The Underground (plan 06 · R16a)

        public Models.Underground.UndergroundState Underground => game.flow.Underground;
        public int UndergroundAct(string what, NPC? subject) => game.UndergroundAct(what, subject);
        public string DiggerName => game.DiggerName;

        // A script's die is rolled on the game's chance, so the harness's runs repeat (plan 01 · M11)
        public int Random(int sides) => Dice.Shared.Next(Math.Max(1, sides));

        // A species the Pokédex has seen, drawn on the same chance (plan 19 · K9, part c: Selphy's wish)
        public int SpeciesSeenAtRandom()
        {
            var seen = game.flow.Pokedex.SeenSpecies.Order().ToList();
            return seen.Count == 0 ? 0 : seen[Dice.Shared.Next(seen.Count)];
        }

        public int BattleTurns => game.flow.ScriptTurns;

        // The original's field cries (a legendary in its lair, a Pokémon a script brings out) have an echo beside them
        public void Cry(string species)
        {
            if (PokemonDatabase.Get(species) is { } own) AudioManager.PlayCry(own, null, CryMode.FieldEvent);
            else if (PokemonDatabase.SpeciesOfForm(species) is { } of) AudioManager.PlayCry(of, species, CryMode.FieldEvent);
        }

        // ---- the Hall of Fame's end (plan 02 · S14)

        public bool NationalDex => game.flow.Pokedex.NationalUnlocked;

        // The save is written where the player stands, into the game's own slot as the start menu's is, with no question and no panel
        public bool Save() => game.WriteSave();

        // ---- after the Hall of Fame (plan 02 · S15)

        public void GiveNationalDex()
        {
            game.flow.Pokedex.UnlockNational();
            // The PC's boxes grow with it: eighteen held one of each of Platinum's species, and the National
            // Pokédex's get a hundred (plan 03 · D13)
            game.flow.Boxes.Grow();
        }

        public int LeagueVictories => game.flow.HallOfFame.Total;

        // ---- the Cycling Road's gates (plan 01 · M11, part d)

        public bool Cycling => game.flow.Player.Mode == TravelMode.Cycling;

        // FireRed's ForcePlayerOntoBike (plan 19 · K6): the Cycling Road's riders, with the riding song
        public void Ride(bool on)
        {
            if (Cycling == on) return;
            if (game.flow.Player.SetCycling(on)) game.PlayFieldMusic();
        }
    }
}
