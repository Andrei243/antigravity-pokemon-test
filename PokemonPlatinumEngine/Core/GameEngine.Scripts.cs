using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
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
    private ScriptLibrary scripts = null!;
    private ScriptRunner runner = null!;

    // A question's answers, shown beside the text box once the question is written out
    private readonly ChoiceBox choice = new();
    private IReadOnlyList<string>? pendingAnswers;
    private int pendingCancel;
    private int scriptAnswer;

    // What a script set going: people walking, the player walking, the screen gone black, the camera sent away
    private readonly List<NpcWalk> npcWalks = new();
    private PlayerWalk? playerWalk;
    private readonly ScreenFade scriptFade = new();
    private bool cameraSent;

    // The battle a script started: how it came out, and whether losing it is part of the story
    private BattleOutcome scriptOutcome;
    private bool battleMayBeLost;

    // The story as the maps last saw it, and whether the place the player came to has yet to run its own script
    private int presenceRevision = -1;
    private bool arrived;

    // A field move being used (plan 02 · S2): its cut-in, and the obstacle it acts on as the band closes
    private FieldCutIn? cutIn;
    private NPC? cutInSubject;

    // A step a field move began (out onto the water, a climb), which the script waits for
    private bool fieldMoveStep;

    // The town chosen on Fly's map, for the script that flies there
    private SpawnLocation? flyTarget;

    // Boulders sliding on after a push: which, how long they have left, and which way they go
    private readonly List<(NPC Boulder, float Left, int Dx, int Dy)> slides = new();

    /// <summary>True while a script has the field: the player's keys do nothing.</summary>
    public bool ScriptRunning => runner != null && runner.IsRunning;

    /// <summary>The answers on screen for a question a script asked: tests and the harness pick one with its own buttons.</summary>
    public ChoiceBox Choice => choice;

    private void LoadScripts()
    {
        scripts = ScriptLibrary.Default;
        runner = new ScriptRunner(scripts, new FieldHost(this));
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
        (string Item, int Count)? item = null, string? flag = null, Pokemon? pokemon = null, NPC? pair = null)
    {
        file ??= subject?.ScriptFile ?? currentMap.ScriptFileAt(subject?.GridX ?? player.GridX, subject?.GridY ?? player.GridY);
        if (scripts.Find(name, file) is not { } script)
        {
            Console.Error.WriteLine($"No script '{name}' for {file}.");
            return false;
        }
        runner.Start(script, subject, own, item, flag, pokemon, pair);
        return true;
    }

    /// <summary>Starts a script that is in no file: one a tool wrote for the occasion (the screenshot harness's scene).</summary>
    public void StartScript(Script script, NPC? subject = null) => runner.Start(script, subject);

    /// <summary>The script of the place the player has come to (<c>OnEnter</c> in its file), if it has one.</summary>
    private bool StartEnterScript()
    {
        string file = currentMap.ScriptFileAt(player.GridX, player.GridY);
        if (scripts.In(file, ScriptLibrary.OnEnter) is not { } script) return false;
        runner.Start(script);
        return true;
    }

    /// <summary>A step ended on tiles that start a script while the story is where they wait for it.</summary>
    private bool TryStepTrigger()
    {
        if (FieldScripts.TriggerAt(currentMap, player.GridX, player.GridY, story) is not { } trigger) return false;
        return StartScript(trigger.Script, file: trigger.ScriptFile ?? currentMap.Name);
    }

    /// <summary>Puts everyone of every map where the story has them, when the story has changed since they were last put.</summary>
    private void RefreshPresence(bool startOver = false)
    {
        if (!startOver && presenceRevision == story.Revision) return;
        presenceRevision = story.Revision;
        // Each badge won adds to the Trainer Card's score; a loaded save has counted its own already
        if (startOver) scoredBadges = story.BadgeCount;
        for (; scoredBadges < story.BadgeCount; scoredBadges++) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.Badge);
        // A place the story reveals is in the map from then on, and drawn again (the Spring Path, plan 01 · M8)
        foreach (var (map, place) in MapDatabase.ApplyHiddenPlaces(story.Var))
            world?.Forget(map, place.X, place.Y, place.Width, place.Height);
        MapDatabase.ApplyPresence(story.Has, forget: startOver);
    }

    /// <summary>The player has come to a map by a door, a warp or a save: whoever a script moved off it or onto it is back as the flags say.</summary>
    private void ArriveOnMap()
    {
        // A warp takes the player off the Cycling Road (FieldSystem_InitFlagsWarp)
        story.Unset(BicycleRules.OnCyclingRoadFlag);
        ChangePlace();
        // Another map ends the Poké Radar's chain (RadarChain_Clear), and the roamers note where the player is (plan 06 · R13)
        EndRadarChain();
        Roamers.NotePlace(encounters, PlaceKey());
        currentMap.ForgetForced();
        currentMap.ApplyPresence(story.Has);
        // Whoever was still walking on the map left behind stands where they were going, not between two tiles
        foreach (var walk in npcWalks) walk.Finish();
        npcWalks.Clear();
        playerWalk = null;
        // Those who move about of their own accord start again from where they first stood, as the original lays
        // its objects out afresh (plan 02 · S6)
        wandering.Clear();
        Wandering.SendHome(currentMap);
        KeepPartnerAlong();
        // A Gym's puzzle is laid out afresh, as the original's room scripts do as the player comes in (plan 01 · M9)
        ArrivePuzzle();
        // The people of the place come to are made ready behind the fade, and the rigs of the place left are let go
        KeepCharactersNear(trim: true);
        arrived = true;
    }

    // Where the player was before the place they have come to: what the Journal says of leaving it
    private string leftMap = "", leftPlace = "";
    private bool leftCave;

    /// <summary>
    /// The Journal's lines for leaving a place (plan 06 · R12; the original's map transitions): out of a cave into
    /// the open, out of home or the professor's lab, and into a Gym without its Badge.
    /// </summary>
    private void NoteLeaving(bool cave)
    {
        bool outdoors = !cave && currentMap.IsStreamed && currentMap.Name == "Sinnoh";
        if (leftCave && outdoors) journal.Tell(new JournalEvent(JournalEventKind.LeftCave, leftPlace));
        if (leftMap == "PlayerHouse" && outdoors) journal.Tell(new JournalEvent(JournalEventKind.RestedAtHome));
        if (leftMap == "RowanLab" && outdoors) journal.Tell(new JournalEvent(JournalEventKind.LeftResearchLab));
        if (Gyms.TryGetValue(currentMap.Name, out var gym) && leftMap == "Sinnoh" && !story.HasBadge(gym.Badge))
            journal.Tell(new JournalEvent(JournalEventKind.GymWasTooTough, gym.Town));
        journal.ChangedPlace();
        leftMap = currentMap.Name;
        leftPlace = PlaceName();
        leftCave = cave;
    }

    /// <summary>The Gyms built so far, by their room: the town and the Badge (the original's <c>sGymsInfo</c>).</summary>
    private static readonly Dictionary<string, (string Town, Badge Badge)> Gyms = new()
    {
        ["OreburghGym"] = ("Oreburgh", Badge.Coal),
        ["EternaGym"] = ("Eterna", Badge.Forest),
        ["HearthomeGym"] = ("Hearthome", Badge.Relic),
        ["VeilstoneGym"] = ("Veilstone", Badge.Cobble),
        ["PastoriaGym"] = ("Pastoria", Badge.Fen)
    };

    /// <summary>
    /// The player has come to another place, by a warp or a step into another area: what lasted only while they
    /// stayed is forgotten (plan 02 · S2), as the original does when the map's header changes: the place's local
    /// flags (a tree that was cut grows back), Strength, Flash and Defog outside the caves, and every boulder that
    /// was pushed goes back where it stood.
    /// </summary>
    private void ChangePlace()
    {
        bool cave = currentMap.AreaAt(player.GridX, player.GridY)?.IsCave ?? currentMap.IsCave;
        NoteLeaving(cave);
        // Going anywhere else ends the rematches the Vs. Seeker found (FieldMapChange_UpdateGameData)
        VsSeeker.Reset(story);
        foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) foreach (var npc in map.Everyone) npc.ReadyForRematch = false;
        FieldMoveRules.LeavePlace(story, cave);
        // Where the Bicycle isn't allowed the player gets off it (field_map_change_flags.c)
        if (player.Mode == TravelMode.Cycling && !currentMap.BikeAllowedAt(player.GridX, player.GridY)) player.SetCycling(false);
        fishing = null;
        // A flute's tune lasts only while the player stays where it was played (field_map_change_flags.c)
        encounterAids.ChangePlace();
        foreach (var (boulder, _, _, _) in slides) boulder.StepOffsetX = boulder.StepOffsetY = 0f;
        slides.Clear();
        currentMap.ResetObstacles();
    }

    /// <summary>
    /// A field move chosen in the party menu (plan 02 · S2): checked by the original's rules where the player
    /// stands, and either the menu says why not, or it closes and the move's script runs (Fly's map opens first).
    /// </summary>
    private void UseFieldMoveFromMenu(FieldMove move, int index)
    {
        var user = playerParty.Members[index];
        var walker = new Walker(player.Mode, player.HeightOn(currentMap), Moves: player.Moves);
        var spot = FieldMoveRules.SpotOf(currentMap, player.GridX, player.GridY, player.Facing, walker) with { Partner = partner != null };
        var error = FieldMoveRules.Check(move, spot, story);
        // Dig needs a way out to lead to, and Fly a town to fly to
        var towns = SpawnLocations.FlyDestinations(story, key => MapDatabase.Get("Sinnoh").Areas.Any(a => a.Key == key && a.Open)).ToList();
        if (error == FieldMoveError.None && ((move == FieldMove.Dig && exitSpot == null) || (move == FieldMove.Fly && towns.Count == 0)))
            error = FieldMoveError.Location;
        if (error != FieldMoveError.None)
        {
            partyScreen.Message = FieldMoveRules.Why(error);
            AudioManager.PlaySound("error");
            return;
        }

        partyScreen.Close();
        if (move == FieldMove.Fly)
        {
            var here = currentMap.Name == "Sinnoh" ? (player.GridX, player.GridY) : exitSpot is { } exit ? (exit.X, exit.Y) : ((int, int)?)null;
            flyScreen.Open(towns, here, index);
            currentState = GameState.FlyMap;
            return;
        }
        currentState = GameState.Overworld;
        // Cut, Rock Smash and Strength act on the obstacle in front, which is whose script it is
        var (dx, dy) = FieldMovement.Delta(player.Facing);
        int ax = player.GridX + dx, ay = player.GridY + dy;
        NPC? subject = currentMap.InBounds(ax, ay) ? currentMap.NpcIn(ax, ay, currentMap.SurfaceAt(ax, ay, player.HeightOn(currentMap)).Height) : null;
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
        if (currentMap.Name == "Sinnoh" && warp.TargetMap != "Sinnoh")
            exitSpot = new MapSpot("Sinnoh", player.GridX, player.GridY + (player.Facing == Direction.Up ? 1 : 0), player.Facing);
        if (SpawnLocations.OfRoom(warp.TargetMap) is { } center) story.SetVar(SpawnLocations.Variable, center.Id);
    }

    /// <summary>Arriving in a town for the first time is what lets one fly there (<c>TryUnlockFlyLocationByMap</c>).</summary>
    private void NoteArrival()
    {
        if (currentMap.AreaAt(player.GridX, player.GridY)?.Key is { } key && SpawnLocations.ArrivedIn(key) is { } town)
        {
            // A town come to for the first time is a line of the Journal (field_map_change_flags.c)
            if (!story.Has(town.ArrivalFlag)) journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, PlaceName()));
            story.Set(town.ArrivalFlag);
        }
    }

    /// <summary>
    /// The Vs. Seeker used (plan 06 · R12): on the open map, with its battery full, every trainer in range not yet
    /// beaten shows a "!", and those beaten who want a rematch spin with a "!!".
    /// </summary>
    private void UseVsSeeker()
    {
        bool outdoors = currentMap.Name == "Sinnoh" && !(currentMap.AreaAt(player.GridX, player.GridY)?.IsCave ?? false);
        switch (VsSeeker.Use(currentMap, player.GridX, player.GridY, outdoors, story, Dice.Shared, out var ready, out var notYet))
        {
            case VsSeekerResult.NotCharged:
                ShowNotification($"The battery isn't charged enough. {VsSeeker.FullBattery - story.Var(VsSeeker.Battery)} steps to go!");
                AudioManager.PlaySound("error");
                return;
            case VsSeekerResult.NoTrainers:
                ShowNotification("There are no Trainers in range who want to battle.");
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
        if (ready.Count == 0 && notYet.Count == 0) ShowNotification("No Trainers are ready for a rematch yet.");
    }

    /// <summary>
    /// The Poké Radar used (plan 06 · R13, <see cref="RadarChain"/>): in tall grass, on foot and alone, with its battery
    /// charged, it sets four patches shaking round the player; otherwise a notice says why not.
    /// </summary>
    private void UsePokeRadar()
    {
        switch (radar.Use(encounters, currentMap, player.GridX, player.GridY, player.HeightOn(currentMap), player.Mode, partner != null, fieldRandom))
        {
            case RadarUse.CantUse:
                ShowNotification(partner != null ? FieldMoveRules.Why(FieldMoveError.Partner) : "The Poké Radar can only be used standing in tall grass.");
                AudioManager.PlaySound("error");
                return;
            case RadarUse.NotCharged:
                ShowNotification($"The battery has run dry! {RadarChain.BatterySteps - encounters.RadarCharge} more steps to charge it.");
                AudioManager.PlaySound("error");
                return;
            case RadarUse.Quiet:
                ShowNotification("The grass around stayed quiet...");
                return;
        }
        AudioManager.PlaySound("grass");
    }

    /// <summary>
    /// An item used in the field itself (plan 02 · S2), from the bag or the item button: the Bicycle got on or off,
    /// a rod cast, an Escape Rope. Where it can't be, a notice says why.
    /// </summary>
    private void UseFieldItem(ItemData item)
    {
        int x = player.GridX, y = player.GridY;
        if (item.FieldUse == "Journal")
        {
            currentState = GameState.Journal;
            journalScreen.Open();
            return;
        }
        if (item.FieldUse == "VsSeeker")
        {
            UseVsSeeker();
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
            var check = BicycleRules.Check(currentMap, x, y, player.Mode, story.Has(BicycleRules.OnCyclingRoadFlag));
            if (check != BicycleCheck.Ok)
            {
                ShowNotification(BicycleRules.Why(check));
                AudioManager.PlaySound("error");
                return;
            }
            bool getOn = player.Mode != TravelMode.Cycling;
            if (!player.SetCycling(getOn)) return;
            // Getting on ends the Poké Radar's chain (MountOrUnmountBicycle)
            if (getOn) EndRadarChain();
            if (getOn) AudioManager.PlaySound("bike_bell");
            PlayFieldMusic();
            return;
        }
        if (FishingAttempt.RodOf(item) is { } rod)
        {
            if (!FishingAttempt.CanCast(currentMap, x, y, player.Facing, player.HeightOn(currentMap)))
            {
                ShowNotification("There's no water here to fish in.");
                AudioManager.PlaySound("error");
                return;
            }
            var (dx, dy) = FieldMovement.Delta(player.Facing);
            fishing = new FishingAttempt(rod, currentMap.Fish(x + dx, y + dy, rod, player.Lead, EncounterMomentNow()), fieldRandom);
            AudioManager.PlaySound("fish_cast");
            return;
        }
        if (item.FieldUse == "EscapeRope")
        {
            var spot = FieldMoveRules.SpotOf(currentMap, x, y, player.Facing, new Walker(player.Mode, player.HeightOn(currentMap))) with { Partner = partner != null };
            if (!spot.CaveWithAWayOut || exitSpot == null)
            {
                ShowNotification("There's no way out to be found with it here.");
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
        var cast = fishing!;
        var before = cast.Stage;
        cast.Update(dt, InputManager.IsActionPressed(GameAction.Confirm) || FishingPress);
        FishingPress = false;
        player.StandStill(dt);
        var (dx, dy) = FieldMovement.Delta(player.Facing);
        int wx = player.GridX + dx, wy = player.GridY + dy;
        if (cast.Stage != before)
        {
            if (cast.Stage == FishingStage.Waiting) world.Life.Splash(currentMap, wx, wy, 0.8f, 2);
            if (cast.Stage == FishingStage.Hooked)
            {
                player.ShowBubble(EmoteBubble.Exclaim, FishingAttempt.HookSeconds(cast.Rod));
                world.Life.Splash(currentMap, wx, wy, 1.2f, 4);
                AudioManager.PlaySound("fish_bite");
            }
            if (cast.Says)
            {
                if (cast.Stage == FishingStage.Landed) AudioManager.PlaySound("fish_reel");
                dialogue.ShowDialogue("", new[] { cast.Message });
                currentState = GameState.Dialogue;
                cast.Read();
                return;
            }
        }
        if (cast.Stage != FishingStage.Done) return;
        fishing = null;
        if (cast.Caught is { } fish)
        {
            player.Encounters.Reset();
            hooked = true;
            StartWildBattle(fish);
        }
    }

    /// <summary>A press of the confirm button for a rod's cast, given by a tool in place of the keys (the screenshot harness).</summary>
    public bool FishingPress { get; set; }

    /// <summary>Where a rod's cast has got to, for tools; null when no rod is out.</summary>
    public FishingStage? CastStage => fishing?.Stage;

    /// <summary>What the field moves left in force, as the map and the player see it: boulders that can be pushed, a cave lit, fog lifted.</summary>
    private void KeepFieldMovesInForce()
    {
        // On the water after a save is loaded, nobody has said whose Surf it was: the first of the team who knows it
        if (player.Mount != null && player.Carrier == null)
            player.Carrier = playerParty.Members.FirstOrDefault(p => p.Moves.Any(m => m.Name == "Surf"))?.ModelName;
        player.PushesBoulders = story.Has(FieldMoveRules.StrengthFlag);
        player.HasRunningShoes = story.Has(StoryState.RunningShoesFlag);
        currentMap.Lit = story.Has(FieldMoveRules.FlashFlag);
        currentMap.FogLifted = story.Has(FieldMoveRules.DefogFlag);
    }

    /// <summary>A field move is used: its cut-in plays (with the Pokémon's cry, as the original's), and the script waits for it.</summary>
    private void StartCutIn(FieldMove move, Pokemon user, NPC? subject)
    {
        cutIn = new FieldCutIn(user.ModelName, move);
        cutInSubject = subject;
        PokemonSprites.GetBaked(user.ModelName, SpriteView.Front);
        AudioManager.PlayCry(user);
    }

    /// <summary>The cut-in runs on; as its band closes, what the move does to the obstacle in front happens.</summary>
    private void UpdateCutIn(float dt)
    {
        if (cutIn == null) return;
        cutIn.Advance(dt);
        if (!cutIn.Done) return;
        if (cutInSubject is { Obstacle: { } kind } subject && cutIn.FieldMove is FieldMove.Cut or FieldMove.RockSmash)
        {
            world.Life.GiveWay(currentMap, subject.GridX, subject.GridY, kind);
            AudioManager.PlaySound(kind == PropType.CutTree ? "cut" : "rock_smash");
        }
        cutIn = null;
        cutInSubject = null;
    }

    private void DrawCutIn()
    {
        if (cutIn == null) return;
        var sprite = PixelArtGenerator.GetPokemonSprite(cutIn.Model, isBack: false);
        ModernUi.DrawCutIn(VirtualWidth, VirtualHeight, sprite, cutIn.Move, cutIn.Band, cutIn.Slide, cutIn.Label);
    }

    /// <summary>A boulder the player pushed starts sliding on to its new tile (it is there already), with its sound.</summary>
    private void SlideBoulders(float dt)
    {
        if (player.TakePush() is var (pushed, way))
        {
            var (dx, dy) = FieldMovement.Delta(way);
            slides.RemoveAll(s => s.Boulder == pushed);
            slides.Add((pushed, FieldMovement.BoulderPushSeconds, dx, dy));
            pushed.StepOffsetX = -dx;
            pushed.StepOffsetY = -dy;
            AudioManager.PlaySound("boulder");
        }
        for (int i = slides.Count - 1; i >= 0; i--)
        {
            var (boulder, left, dx, dy) = slides[i];
            left -= dt;
            float rest = Math.Max(0f, left / FieldMovement.BoulderPushSeconds);
            boulder.StepOffsetX = -dx * rest;
            boulder.StepOffsetY = -dy * rest;
            if (left <= 0f) slides.RemoveAt(i);
            else slides[i] = (boulder, left, dx, dy);
        }
    }

    // ------------------------------------------------------------------ a frame

    /// <summary>One frame of the field while a script has it.</summary>
    private void UpdateScript(float dt)
    {
        AdvanceScriptedWalks(dt);
        UpdateCutIn(dt);
        RunScript(dt);
    }

    private void RunScript(float dt)
    {
        try
        {
            runner.Update(dt);
        }
        catch (ScriptException e)
        {
            // A script that goes wrong ends there; the game goes on
            Console.Error.WriteLine(e.Message);
            ShowNotification("A script went wrong: " + e.Message);
            runner.Abort();
        }
        RefreshPresence();
        if (!runner.IsRunning)
        {
            EndScript();
            // The tiles a step into a new place ended on are tried once that place's own script is done (OnStep)
            if (triggerAfterEnter)
            {
                triggerAfterEnter = false;
                if (currentState == GameState.Overworld) TryStepTrigger();
            }
        }
    }

    /// <summary>A step came into a new place, whose script runs first: the trigger under the player waits for it.</summary>
    private bool triggerAfterEnter;

    /// <summary>People a script set walking take their steps, and the player theirs or stands still. Also under text.</summary>
    private void AdvanceScriptedWalks(float dt)
    {
        for (int i = npcWalks.Count - 1; i >= 0; i--)
        {
            npcWalks[i].Update(dt);
            if (npcWalks[i].IsDone) npcWalks.RemoveAt(i);
        }

        if (playerWalk == null)
        {
            // A step a field move began (out onto the water, up a waterfall or a rock face) is taken to its end
            if (player.IsMoving)
            {
                player.Advance(dt, currentMap, null, false, null, null, () =>
                {
                    StepLeavesItsMark();
                    return false;
                });
                if (!player.IsMoving && player.Mode != musicTravel) PlayFieldMusic();
            }
            else player.StandStill(dt);
            if (!player.IsMoving) fieldMoveStep = false;
            return;
        }
        playerWalk.Update(dt, player, currentMap, StepLeavesItsMark);
        if (playerWalk.IsDone) playerWalk = null;
    }

    /// <summary>Whatever a script left going is brought to its end: nobody is left mid-step, the screen comes back, the camera returns.</summary>
    private void EndScript()
    {
        foreach (var walk in npcWalks) walk.Finish();
        npcWalks.Clear();
        playerWalk = null;
        if (scriptFade.Level > 0f) scriptFade.To(false, ScriptParser.FadeSeconds);
        if (cameraSent)
        {
            world.ReleaseCamera(ScriptParser.ReleaseSeconds);
            cameraSent = false;
        }
        // (Answers already given slide away by themselves; only a question left open is taken off the screen)
        if (choice.IsOpen) choice.Dismiss();
        pendingAnswers = null;
        // A trainer's eye theme that no battle cut in on (their script had no battle) gives way to the place's own
        if (eyeThemePlaying && currentState is GameState.Overworld or GameState.Dialogue) PlayFieldMusic();
    }

    /// <summary>
    /// One frame of text on the screen: the line is written and waits, a question waits for its answer, and the
    /// script that showed it goes on in the same frame the box closes, so the box doesn't blink between two talks.
    /// </summary>
    private void UpdateDialogue(float dt)
    {
        if (ScriptRunning) AdvanceScriptedWalks(dt);

        if (choice.IsOpen) choice.ReadKeys();
        else dialogue.Update(dt);
        AnswerQuestion();

        // Back to the field, unless the last line led somewhere else (a ship leaving for another region)
        if (!dialogue.IsActive && currentState == GameState.Dialogue)
        {
            currentState = GameState.Overworld;
            if (ScriptRunning) RunScript(0f);
        }
    }

    /// <summary>A question written out shows its answers; the answer given closes the box.</summary>
    private void AnswerQuestion()
    {
        if (!dialogue.AwaitingAnswer || choice.IsOpen) return;
        if (choice.Take() is { } picked)
        {
            scriptAnswer = picked;
            dialogue.Close();
        }
        else if (pendingAnswers != null)
        {
            choice.Open(pendingAnswers, pendingCancel);
            pendingAnswers = null;
        }
    }

    /// <summary>The script's own fade and its question's answers, drawn over the field: the text box is between them.</summary>
    private void DrawScriptFade()
    {
        if (scriptFade.Level <= 0f) return;
        Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)MathF.Round(scriptFade.Level * 255f)));
    }

    private void DrawChoice()
    {
        if (choice.Visible) ModernUi.DrawChoices(VirtualWidth, VirtualHeight, choice.Options, choice.Cursor, choice.Shown);
    }

    /// <summary>What a step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out onto it.</summary>
    private void StepLeavesItsMark()
    {
        if (player.JustRodeOut) world.Life.Splash(currentMap, player.GridX, player.GridY);
        else world.Life.Footstep(currentMap, player.GridX, player.GridY, player.Facing, player.IsRunning, player.Mode);
        if (player.JustLanded && player.Mode != TravelMode.Surfing) world.Life.Landing(currentMap, player.GridX, player.GridY);
    }

    /// <summary>Goes to a tile of a map through a fade, as a door does: a script's warp.</summary>
    private void WarpTo(string mapName, int x, int y, Direction? facing)
    {
        var target = MapDatabase.Get(mapName);
        PlayAreaMusic(target, x, y);
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = target;
            player.SetPosition(x, y, facing ?? player.Facing);
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

        public StoryState Story => game.story;
        public Party Party => game.playerParty;
        public Inventory Bag => game.playerInventory;
        public Poketch Poketch => game.poketch;
        public SafariGame Safari => game.safari;

        public int Money
        {
            get => game.playerMoney;
            set => game.playerMoney = Math.Max(0, value);
        }

        public string PlayerName => PlayerIdentity.Name;
        public PlayerLook PlayerLook => PlayerIdentity.Look;

        // ---- the field

        public NPC? FindNpc(string name, string? place) => game.currentMap.FindPerson(name, place);

        public (int X, int Y) TileOf(NPC? who) => who == null ? (game.player.GridX, game.player.GridY) : (who.GridX, who.GridY);

        public Direction FacingOf(NPC? who) => who?.Facing ?? game.player.Facing;

        public void Face(NPC? who, Direction direction)
        {
            if (who == null) game.player.Facing = direction;
            else who.Facing = direction;
        }

        public void Place(NPC? who, int x, int y, Direction? facing)
        {
            if (who == null)
            {
                game.player.SetPosition(x, y, facing ?? game.player.Facing);
                return;
            }
            StopWalk(who);
            (who.GridX, who.GridY) = (x, y);
            if (facing is { } direction) who.Facing = direction;
        }

        public void SetVisible(NPC who, bool visible)
        {
            who.Forced = visible;
            game.currentMap.ApplyPresence(game.story.Has);
        }

        public void Walk(NPC? who, IReadOnlyList<Direction> steps, bool fast)
        {
            if (who == null)
            {
                game.playerWalk = new PlayerWalk(steps, fast);
                return;
            }
            StopWalk(who);
            game.npcWalks.Add(new NpcWalk(who, steps, fast));
        }

        /// <summary>Whoever is told to go somewhere else first arrives where they were going.</summary>
        private void StopWalk(NPC who)
        {
            game.wandering.Settle(who);
            if (game.partner?.Who == who) game.partner.Settle();
            foreach (var walk in game.npcWalks.Where(w => w.Who == who)) walk.Finish();
            game.npcWalks.RemoveAll(w => w.Who == who);
        }

        public bool Walking => game.playerWalk != null || game.npcWalks.Count > 0;

        public void Emote(NPC? who, EmoteBubble bubble, float seconds)
        {
            if (who == null) game.player.ShowBubble(bubble, seconds);
            else who.ShowBubble(bubble, seconds);
            if (bubble == EmoteBubble.Exclaim) AudioManager.PlaySound("exclaim", who == null ? 0f : game.PanAt(who.GridX));
        }

        public void Camera(CameraMove move, int x, int y, float seconds)
        {
            switch (move)
            {
                case CameraMove.Pan:
                    game.world.PanCamera(x + 0.5f, y + 0.5f, seconds);
                    game.cameraSent = true;
                    break;
                case CameraMove.Release:
                    game.world.ReleaseCamera(seconds);
                    game.cameraSent = false;
                    break;
                default:
                    game.world.ShakeCamera(seconds);
                    break;
            }
        }

        // ---- what the script waits for

        public bool Busy => game.currentState != GameState.Overworld || game.dialogue.IsActive || game.scriptFade.IsMoving || game.cutIn != null
            || game.fieldMoveStep || game.PuzzleMoving;

        public void Say(string? speaker, IReadOnlyList<string> lines)
        {
            if (lines.Count == 0) return;
            game.dialogue.ShowDialogue(speaker ?? "", lines);
            game.currentState = GameState.Dialogue;
        }

        public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel)
        {
            game.choice.Dismiss();
            game.pendingAnswers = answers;
            game.pendingCancel = cancel;
            game.scriptAnswer = Math.Max(0, cancel);
            game.dialogue.ShowQuestion(speaker ?? "", question);
            game.currentState = GameState.Dialogue;
        }

        public int Answer => game.scriptAnswer;

        public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first)
        {
            game.scriptOutcome = BattleOutcome.None;
            game.battleMayBeLost = mayLose;
            game.StartTrainerBattle(trainer, second, partner, first);
        }

        public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee)
        {
            game.scriptOutcome = BattleOutcome.None;
            game.battleMayBeLost = false;
            game.MeetWildPokemon(wild, kind, cannotFlee);
        }

        public BattleOutcome Outcome => game.scriptOutcome;

        public void Open(ScriptScreen screen, NPC? subject, string? counter = null)
        {
            game.scriptAnswer = 0;
            switch (screen)
            {
                case ScriptScreen.Starter:
                    game.currentState = GameState.StarterSelect;
                    game.starterSelectScreen.Open();
                    break;
                case ScriptScreen.Shop:
                    game.currentState = GameState.Shop;
                    // The counter's own stock: the clerk's specialties, or the common list by the badges (plan 06 · R11)
                    game.shopScreen.Open(game.currentMap.DisplayNameAt(game.player.GridX, game.player.GridY),
                        MartDatabase.Stock(counter ?? subject?.Mart, game.story.BadgeCount));
                    break;
                case ScriptScreen.Wardrobe:
                    // At home the clothes owned; at a boutique its stock as well, headed with the place's name
                    game.currentState = GameState.Wardrobe;
                    game.wardrobeScreen.Open(game.wardrobe, counter != null ? ClothingDatabase.Stock(counter) : null,
                        counter != null ? game.currentMap.DisplayNameAt(game.player.GridX, game.player.GridY) : null);
                    break;
                case ScriptScreen.HallOfFame:
                    game.currentState = GameState.HallOfFame;
                    game.hallOfFameScreen.Open();
                    break;
                case ScriptScreen.ChoosePokemon:
                    game.currentState = GameState.PartyMenu;
                    game.partyScreen.OpenToChoose();
                    break;
                case ScriptScreen.Pc:
                    game.currentState = GameState.PCStorage;
                    game.pcScreen.Open(game.pcBoxStorage);
                    break;
                case ScriptScreen.Travel:
                    // The way to the next region, where this map has one: the attendant says how things stand,
                    // and the ship leaves when the story of this region is done
                    if (RegionDatabase.RegionOfMap(game.currentMap.Name) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link) break;
                    var check = RegionDatabase.CheckTravel(link, game.story);
                    game.scriptAnswer = 1;
                    game.dialogue.ShowDialogue(subject?.Name ?? "", RegionDatabase.AttendantLines(link, check),
                        check == TravelCheck.Ready ? () => game.TravelTo(RegionDatabase.Get(link.To)!) : null);
                    game.currentState = GameState.Dialogue;
                    break;
            }
        }

        public void EnterHallOfFame()
        {
            game.hallOfFame.Enter(game.playerParty, p => p.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (playerName, game.trainerId), DateTime.Now);
            game.trainerScore = TrainerScore.Add(game.trainerScore, TrainerScore.HallOfFame);
        }

        public bool Trade(string trade, int slot)
        {
            if (NpcTrades.Get(trade) is not { } t || NpcTrades.Trade(t, game.playerParty, slot, GameClock.Today) is not { } received) return false;
            game.playerPokedex.RegisterSeen(received.Species.DexNumber);
            game.playerPokedex.RegisterCaught(received.Species.DexNumber);
            AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
            return true;
        }

        public bool GivePokemon(Pokemon pokemon)
        {
            game.playerPokedex.RegisterSeen(pokemon.Species.DexNumber);
            game.playerPokedex.RegisterCaught(pokemon.Species.DexNumber);
            // A gift is met where it is given (Pokemon_GiveMonFromScript)
            pokemon.Met(game.PlaceName(), GameClock.Today);
            if (game.playerParty.Add(pokemon)) return true;
            game.pcBoxStorage.Store(pokemon);
            return false;
        }

        public void Warp(string map, int x, int y, Direction? facing) => game.WarpTo(map, x, y, facing);

        public void Fade(bool toBlack, float seconds) => game.scriptFade.To(toBlack, seconds);

        // ---- field moves

        public void UseMove(FieldMove move, Pokemon user, NPC? subject)
        {
            game.journal.Tell(new JournalEvent(JournalEventKind.UsedFieldMove, FieldMoveRules.MoveName(move)));
            // Whose Surf it is carries the player (plan 10 · F1)
            if (move == FieldMove.Surf) game.player.Carrier = user.ModelName;
            game.StartCutIn(move, user, subject);
        }

        public void Note(JournalEvent line) => game.journal.Tell(line);

        public bool Surf()
        {
            if (!game.player.StartSurf(game.currentMap)) return false;
            AudioManager.PlaySound("surf");
            game.fieldMoveStep = true;
            return true;
        }

        public bool Climb() => game.fieldMoveStep = game.player.Climb(game.currentMap);

        public bool Fly()
        {
            if (game.flyTarget is not { } town) return false;
            game.flyTarget = null;
            game.player.SetMode(TravelMode.OnFoot);
            // Flying sends every roamer anywhere (FieldSystem_SetFlyFlags; plan 06 · R13)
            Roamers.Scatter(game.encounters, game.fieldRandom);
            game.WarpTo("Sinnoh", town.X, town.Y, Direction.Down);
            return true;
        }

        public bool Teleport()
        {
            var town = SpawnLocations.Respawn(game.story);
            game.player.SetMode(TravelMode.OnFoot);
            // So does Teleport (FieldSystem_SetTeleportFlags)
            Roamers.Scatter(game.encounters, game.fieldRandom);
            game.WarpTo("Sinnoh", town.X, town.Y, Direction.Down);
            return true;
        }

        public void Turnback() => TurnbackCave.Reaim(game.currentMap, game.player.GridX, game.player.GridY, game.story, Dice.Shared);

        public void TravelWith(NPC? who, string? trainerId) => game.SetPartner(who, trainerId);

        public string? Partner => game.partner?.TrainerId;

        public bool Escape()
        {
            if (game.exitSpot is not { } exit) return false;
            game.player.SetMode(TravelMode.OnFoot);
            game.WarpTo(exit.Map, exit.X, exit.Y, exit.Facing);
            return true;
        }

        public bool SweetScent()
        {
            var map = game.currentMap;
            int x = game.player.GridX, y = game.player.GridY;
            var underfoot = map.BehaviourAt(x, y);
            bool water = game.player.Mode == TravelMode.Surfing;
            if (!TileBehaviors.HasEncounters(underfoot) || water != TileBehaviors.IsSurfable(underfoot)) return false;
            if (map.DrawOutWild(x, y, water, game.player.Lead, game.EncounterMomentNow()) is not { } wild) return false;
            game.scriptOutcome = BattleOutcome.None;
            game.battleMayBeLost = false;
            game.player.Encounters.Reset();
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

        public SpecialEncounters Encounters => game.encounters;

        public int? HoneyTreeFaced => HoneyTrees.Faced(game.currentMap, game.player.GridX, game.player.GridY, game.player.Facing);

        public uint TrainerNumber => game.TrainerNumber;

        public Random Chance => game.fieldRandom;

        // The original's field cries (a legendary in its lair, a Pokémon a script brings out) have an echo beside them
        public void Cry(string species)
        {
            if (PokemonDatabase.Get(species) is { } own) AudioManager.PlayCry(own, null, CryMode.FieldEvent);
            else if (PokemonDatabase.SpeciesOfForm(species) is { } of) AudioManager.PlayCry(of, species, CryMode.FieldEvent);
        }
    }
}
