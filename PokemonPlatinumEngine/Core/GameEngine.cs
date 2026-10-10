using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

public enum GameState
{
    Overworld,
    Battle,
    Dialogue,
    PartyMenu,
    BagMenu,
    PokedexMenu,
    TrainerCard,
    StarterSelect,
    Shop,
    PCStorage,
    Options,
    Title,
    Evolution,
    SaveMenu,
    Intro,
    /// <summary>Fly's map of the towns to fly to (plan 02 · S2).</summary>
    FlyMap,
    /// <summary>The PC's Hall of Fame (plan 06 · R12).</summary>
    HallOfFame,
    /// <summary>The Journal's pages (plan 06 · R12).</summary>
    Journal,
    /// <summary>The wardrobe at home, or a boutique's (plan 11 · C10).</summary>
    Wardrobe,
    Transition
}

public partial class GameEngine
{
    // Interface code lays out in these units; the screen itself is rendered RenderScale times larger (3840x2160)
    public const int VirtualWidth = 1920;
    public const int VirtualHeight = 1080;
    public const int RenderScale = 2;

    private RenderTexture2D virtualScreen;
    private GameState currentState = GameState.Title;
    private GameState stateBeforeTransition = GameState.Overworld;
    private GameState stateAfterTransition = GameState.Overworld;

    private Map currentMap = null!;

    /// <summary>The area of the overworld the player was last in; null on a small map, which is one place.</summary>
    private MapArea? currentArea;
    private Player player = null!;
    private readonly RenderContext renderContext = new(VirtualWidth, VirtualHeight);
    private readonly WorldRenderer world;
    private readonly BattleRenderer battleRenderer;
    private DialogueManager dialogue = new();
    private BattleEngine? battle;

    // A trainer who spotted the player and is walking up, and the one being battled
    private TrainerApproach? trainerApproach;

    // The second of two trainers who saw the player at once (plan 02 · S6)
    private TrainerApproach? pairApproach;

    // The player came through a warp, onto the tile in front of it: a trigger there starts as a step onto it would
    private bool steppedOutOfWarp;
    private readonly List<NPC> battleTrainers = new();

    // How the player was travelling when the field's music was last chosen, and whether a trainer's eye theme is on
    private TravelMode musicTravel = TravelMode.OnFoot;
    private bool eyeThemePlaying;

    // Set on arriving somewhere without walking (through a door, from a save, after a battle): trainers look once
    private bool trainersLookOnArrival = true;

    // UI Sub-screens
    private readonly StartMenu startMenu = new();
    private readonly PartyScreen partyScreen = new();
    private readonly FlyScreen flyScreen = new();

    // Where Dig and an Escape Rope lead out of the caves: outside the way the player went into them (the
    // original's exit location), saved with the game
    private MapSpot? exitSpot;

    // The key item kept on the item button (saved), a rod's cast under way, and whether the run button was down
    // last frame (on the Bicycle a press changes gear)
    private string? registeredItem;
    private FishingAttempt? fishing;
    private bool runWasDown;
    private readonly BagScreen bagScreen = new();
    private readonly PokedexScreen pokedexScreen = new();
    private readonly TrainerCardScreen trainerCardScreen = new();
    private readonly StarterSelectScreen starterSelectScreen = new();
    private readonly ShopScreen shopScreen = new();
    private readonly WardrobeScreen wardrobeScreen = new();

    /// <summary>The clothes the player owns and wears (plan 11 · C9).</summary>
    private readonly Wardrobe wardrobe = new();
    private readonly PCScreen pcScreen = new();
    private readonly OptionsScreen optionsScreen = new();
    private readonly SaveScreen saveScreen = new();
    private readonly IntroScreen introScreen = new();
    private readonly EvolutionScreen evolutionScreen = new();
    private readonly LocationSign locationSign = new();
    private readonly Toast toast = new();

    // Evolutions waiting to be played one after the other, and the screen the game goes back to after the last
    private readonly Queue<EvolutionRequest> pendingEvolutions = new();
    private GameState evolutionReturnState = GameState.Overworld;

    // Steps since friendship last grew from walking, and the player's turns on the spot (one Pokémon evolves on a spin)
    private int friendshipSteps;
    private readonly SpinTracker spin = new();
    private readonly Random fieldRandom = Dice.New();

    // The opening and title menu, the save it offers to continue, and where the options screen returns to
    private TitleScreen titleScreen = new(null);
    private readonly TitleScene titleScene = new();
    private SaveData? titleSave;
    private GameState optionsReturnState = GameState.Overworld;
    private bool gameStarted;

    /// <summary>Player options (graphics, window, time of day, sound), kept in settings.json.</summary>
    public GameSettings Settings { get; }

    // Player Save State
    private readonly Party playerParty = new();
    private readonly Inventory playerInventory = new();
    private readonly Pokedex playerPokedex = new();
    private PcBoxes pcBoxStorage = new();
    private int playerMoney = 3000;
    private float playTime = 0f;
    private int trainerId;

    /// <summary>The Trainer Card's score (plan 06 · R12, <see cref="TrainerScore"/>), and the badges it has counted.</summary>
    private int trainerScore, scoredBadges;

    /// <summary>The teams entered into the Hall of Fame (plan 06 · R12).</summary>
    private HallOfFame hallOfFame = new();
    private readonly HallOfFameScreen hallOfFameScreen = new();

    /// <summary>The Journal's pages (plan 06 · R12) and the screen that shows them.</summary>
    private Journal journal = new();
    private readonly JournalScreen journalScreen = new();
    private float rematchSpin;

    // The people who move about of their own accord (plan 02 · S6), and their chance: drawn from Dice the first time
    // anyone moves, so a run in which nobody does draws nothing
    private readonly Wandering wandering = new();
    private Random? peopleRandom;

    /// <summary>Keeps everyone standing where they are: the harness, whose shots count on people being where they first stood.</summary>
    public bool PeopleStayPut { get; set; }

    // Whoever travels with the player (plan 02 · S6): Cheryl through Eterna Forest. The tile the player was last
    // seen heading for tells when they set off from another
    private Follower? partner;
    private (int X, int Y) partnerHeading;
    private DateTime? adventureStarted;

    /// <summary>What the story remembers: flags, variables, trainers beaten, items taken, the badges, the starters (plan 02 · S1).</summary>
    private readonly StoryState story = new();

    // The Pokétch (plan 02 · S2), and whether it is out on the screen
    private readonly Poketch poketch = new();

    /// <summary>The Great Marsh's Safari Game, while one is on (plan 01 · M7; saved).</summary>
    private readonly SafariGame safari = new();

    /// <summary>The Repel's steps and the flute played here (plan 06 · R11); the Repel's steps are saved.</summary>
    private readonly EncounterAids encounterAids = new();
    private readonly PoketchView poketchView = new();

    private static string playerName => PlayerIdentity.Name;

    /// <summary>The rules picked on the title screen, held while the introduction plays.</summary>
    private RulesPreset newGameRules;

    /// <summary>
    /// The region a new game starts in; null for the first region in the chain (Kanto). Set from the command line
    /// (--region Sinnoh) to test a later region without playing through the ones before it.
    /// </summary>
    public string? NewGameRegion { get; set; }

    // Screen Transitions
    private float transitionTimer = 0f;
    private TransitionKind transitionKind = TransitionKind.Fade;
    private bool isFadingOut = true;
    private Action? midTransitionCallback;

    /// <summary>Set when the player chose to leave the game from a menu; the main loop closes the window.</summary>
    public bool QuitRequested { get; private set; }


    public GameEngine(GameSettings? settings = null)
    {
        Settings = settings ?? GameSettings.Load();
        world = new WorldRenderer(renderContext);
        battleRenderer = new BattleRenderer(renderContext);
    }

    public void Initialize()
    {
        virtualScreen = Raylib.LoadRenderTexture(VirtualWidth * RenderScale, VirtualHeight * RenderScale);
        UiShapes.PixelScale = RenderScale;
        Raylib.SetTextureFilter(virtualScreen.Texture, TextureFilter.Bilinear);

        AudioManager.Initialize();
        MoveDatabase.Initialize();
        PokemonDatabase.Initialize();
        ItemDatabase.Initialize();
        MapDatabase.Initialize();
        LoadScripts();

        // The characters of the introduction, sculpted and meshed in the background while the title screen plays; the
        // people of the field are made ready where the player is (KeepCharactersNear, plan 11 · C1)
        CharacterModels.Preload(new[] { "PLAYER", "DAWN", "ROWAN" });
        // The Pokémon models of the story's opening too, meshed side by side; the menu sprites below wait for each one
        var modelled = PokemonModels.Preloaded.ToList();
        PokemonModels.Preload(modelled.Append(PokemonSprites.Fallback));
        // The Pokémon that stand in the field (plan 10 · F1), whose sprites are baked from their models when first in view
        foreach (var species in MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).Everyone).Where(n => n.IsPokemon).Select(n => n.Species!).Distinct())
            PokemonModels.Request(species);
        // And the balls the battles throw
        BattleBall.Preload("Poké Ball", "Great Ball", "Ultra Ball", "Master Ball");

        ApplySettings(window: false);

        // The menu sprites of the species the story shows first are made up front (read from the sprite cache once
        // they have been baked); every other species' are made the first time a menu asks for them, showing the
        // stand-in (PokemonSprites.Fallback) for a frame or two meanwhile.
        renderContext.EnsureLoaded();
        PokemonSprites.BakeAll(renderContext, modelled);

        // The game opens on the title screen, which offers the saved game if there is one
        titleSave = SaveManager.LoadGame();
        titleScreen = new TitleScreen(titleSave);
        AudioManager.PlayMusic(MusicRole.Title);
    }

    /// <summary>Starts a new game at once as the boy with his own name: for tests and the screenshot harness.</summary>
    public void StartNewGame() => StartNewGame(null, PlayerLook.Boy);

    /// <summary>
    /// Starts a new game as the character and under the name chosen in the introduction, played by the rules chosen
    /// on the title screen. The rules stay with the adventure: nothing in the game changes them afterwards.
    /// </summary>
    public void StartNewGame(string? name, PlayerLook look, RulesPreset rules = RulesPreset.Platinum, string? rival = null)
    {
        // Before any Pokémon is made: the moves they learn take their values from the rules
        Ruleset.Use(rules);
        playerParty.Clear();
        playerInventory.Clear();
        playerPokedex.Clear();
        pcBoxStorage = new PcBoxes();
        poketch.Clear();
        poketchView.Hide();
        safari.End();
        encounterAids.RepelSteps = 0;
        encounterAids.Flute = Flute.None;
        // What the game remembers of its wild Pokémon starts over: the day's numbers drawn, no roamer loose (plan 06 · R13)
        encounters = SpecialEncounters.NewGame(fieldRandom);
        berries = BerryPatches.NewGame();
        radar.Clear();
        registeredItem = null;
        exitSpot = null;
        lastDay = null;
        partner = null;
        MapDatabase.RestoreDefeatedTrainers(Array.Empty<string>());
        // The story starts over: the flags and variables every game begins with, and everyone where those put them
        runner.Abort();
        story.Clear();
        StoryMigration.BeginNewGame(story, scripts);
        RefreshPresence(startOver: true);
        pendingEvolutions.Clear();
        friendshipSteps = 0;
        playerMoney = 3000;
        playTime = 0f;
        // As in the games, the Trainer Card's number is drawn when the adventure begins
        PlayerIdentity.Set(name, look);
        PlayerIdentity.SetRival(rival);
        wardrobe.Clear();
        PlayerIdentity.SetOutfit(wardrobe.Worn);
        trainerId = fieldRandom.Next(0, 65536);
        adventureStarted = DateTime.Now;
        trainerScore = scoredBadges = 0;
        hallOfFame = new HallOfFame();
        journal = new Journal();

        InitializeNewGame();
        journal.TakenUp(GameClock.Today, PlaceName());
        EnterGame();
    }

    /// <summary>Leaves the title screen into the saved game, or into a fresh one when nothing is saved.</summary>
    public void ContinueGame()
    {
        var save = titleSave ?? SaveManager.LoadGame();
        if (save == null)
        {
            StartNewGame();
            return;
        }

        ApplySaveData(save);
        // The Journal opens by itself on a game left two days or more (Journal_CheckOpenOnContinue), and today's page is begun
        bool showJournal = playerInventory.GetQuantity(ItemDatabase.Get("Journal")!) > 0 && journal.OpensOnContinue(GameClock.Today);
        journal.TakenUp(GameClock.Today, PlaceName());
        EnterGame();
        if (showJournal)
        {
            stateAfterTransition = GameState.Journal;
            journalScreen.Open();
        }
    }

    /// <summary>Fades in on the field from the black the title screen left behind.</summary>
    private void EnterGame()
    {
        gameStarted = true;
        trainersLookOnArrival = true;
        arrived = true;
        scriptFade.Clear();
        startMenu.PlayerName = playerName;
        KeepCharactersNear(trim: false);
        PlayAreaMusic(currentMap, player.GridX, player.GridY);
        AnnounceLocation();

        stateBeforeTransition = GameState.Overworld;
        stateAfterTransition = GameState.Overworld;
        midTransitionCallback = null;
        currentState = GameState.Transition;
        isFadingOut = false;
        transitionTimer = 0f;
    }

    /// <summary>Applies the options to the renderer, the clock and the sound, and optionally to the window.</summary>
    public void ApplySettings(bool window)
    {
        renderContext.SetQuality(Settings.Quality);
        GameClock.Fixed = Settings.TimeOfDay;
        if (AudioManager.IsMuted != Settings.Muted) AudioManager.ToggleMute();
        AudioManager.SetVolumes(Settings.MusicVolume / 100f, Settings.SoundVolume / 100f, Settings.CryVolume / 100f, Settings.AmbienceVolume / 100f);
        AudioManager.Handheld = Settings.Speakers == SpeakerMode.Handheld;
        dialogue.CharactersPerSecond = GameSettings.CharactersPerSecond(Settings.TextSpeed);
        if (window) WindowSettings.Apply(Settings);
    }

    public void ToggleFullscreen()
    {
        Settings.Fullscreen = !Settings.Fullscreen;
        ApplySettings(window: true);
        Settings.Save();
    }

    private void InitializeNewGame()
    {
        var region = (NewGameRegion != null ? RegionDatabase.Get(NewGameRegion) : null) ?? RegionDatabase.First;
        var start = region.Start ?? RegionDatabase.First.Start!;
        currentMap = MapDatabase.Get(start.Map);
        player = new Player(start.X, start.Y);
        player.Facing = start.Facing;
        // As in Platinum, the adventure begins with an empty bag and no Pokémon: the first comes from the
        // professor's briefcase on Route 201 (plan 02 · S4). A region whose story isn't written yet (Kanto) still
        // starts as every game did before: a Pokémon and a few items.
        if (region.Id != RegionDatabase.Sinnoh)
        {
            playerInventory.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            playerInventory.AddItem(ItemDatabase.Get("Potion")!, 5);
            playerInventory.AddItem(ItemDatabase.Get("Revive")!, 2);
            var first = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
            playerParty.Add(first);
            playerPokedex.RegisterSeen(first.Species.DexNumber);
            playerPokedex.RegisterCaught(first.Species.DexNumber);
        }
    }

    private void ApplySaveData(SaveData save)
    {
        // The rules the adventure began under, before any of its Pokémon are made
        Ruleset.Use(save.Rules);

        var place = save.Place();
        currentMap = MapDatabase.Get(place.Map);
        player = new Player(place.X, place.Y);
        player.Facing = place.Facing;
        // A save made out on the water wakes up there, still on its Pokémon's back; one made on a bridge, on its deck
        player.SetMode(save.Travel);
        if (save.PlayerHeight is { } standing) player.SetHeight(standing);
        exitSpot = save.Exit;
        lastDay = save.LastDay;
        registeredItem = save.RegisteredItem;
        poketch.Load(save.Poketch);
        poketchView.Hide();
        if (save.Safari is { } game) safari.Resume(game.Balls, game.Steps);
        else safari.End();
        encounterAids.RepelSteps = Math.Max(0, save.RepelSteps);
        encounterAids.Flute = Flute.None;
        // What the game remembered of its wild Pokémon (a save from before plan 06 · R13 is given a new start); a
        // game come back to sends every roamer anywhere (the original's continue task)
        encounters = save.Encounters ?? SpecialEncounters.NewGame(fieldRandom);
        berries = save.Berries ?? BerryPatches.NewGame();
        radar.Clear();
        Roamers.Scatter(encounters, fieldRandom);

        playerParty.Clear();
        foreach (var pData in save.Party)
        {
            var p = pData.ToPokemon();
            playerParty.Add(p);
            playerPokedex.RegisterSeen(p.Species.DexNumber);
            playerPokedex.RegisterCaught(p.Species.DexNumber);
        }

        if (playerParty.Count == 0)
        {
            var fallback = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
            playerParty.Add(fallback);
            playerPokedex.RegisterSeen(fallback.Species.DexNumber);
            playerPokedex.RegisterCaught(fallback.Species.DexNumber);
        }

        // Each stored Pokémon in its own place; a save from before the boxes had places lays its list out in them
        pcBoxStorage = save.Boxes?.ToBoxes() ?? PcBoxes.FromList(save.BoxStorage.Select(p => p.ToPokemon()));

        playerInventory.Clear();
        foreach (var itData in save.Inventory)
        {
            var itemDef = ItemDatabase.Get(itData.ItemName);
            if (itemDef != null)
            {
                playerInventory.AddItem(itemDef, itData.Quantity);
            }
        }

        playerPokedex.Clear();
        foreach (var seen in save.SeenSpecies) playerPokedex.RegisterSeen(seen);
        foreach (var caught in save.CaughtSpecies) playerPokedex.RegisterCaught(caught);
        playerPokedex.Restore(save.NationalPokedex,
            save.Diplomas.Select(d => Enum.TryParse<PokedexMode>(d, out var mode) ? mode : (PokedexMode?)null).OfType<PokedexMode>());

        // The story as the save has it, brought up to date if an older game wrote it; then everyone where it puts them
        runner.Abort();
        story.Restore(save.ToStory());
        StoryMigration.Upgrade(story, save.StoryVersion, playerParty.Members.Concat(pcBoxStorage.All), scripts, playerInventory);
        story.RestoreGreetings(save.GreetedPeople);
        MapDatabase.RestoreDefeatedTrainers(story.DefeatedTrainers);
        RefreshPresence(startOver: true);

        playerMoney = save.Money;
        playTime = save.PlayTimeSeconds;
        PlayerIdentity.Set(save.PlayerName, save.Look);
        PlayerIdentity.SetRival(save.RivalName);
        // What the player wears: a save from before the wardrobe is the look's own clothes
        wardrobe.Restore(save.Outfit, save.Wardrobe);
        PlayerIdentity.SetOutfit(wardrobe.Worn);
        CharacterModels.Preload(new[] { PlayerIdentity.Character });
        // A save from before the card had a number gets one now, and keeps it
        trainerId = save.TrainerId != 0 ? save.TrainerId : fieldRandom.Next(1, 65536);
        adventureStarted = save.Started;
        trainerScore = save.TrainerScore;
        hallOfFame = new HallOfFame();
        hallOfFame.Restore(save.HallOfFameTotal, save.HallOfFameDebut, save.HallOfFame.Select(e => e.ToEntry()));
        journal = new Journal();
        journal.Restore(save.Journal);

        // Whoever was travelling with the player goes on doing so, behind them
        partner = null;
        if (save.Partner is { } partnerId && save.PartnerPerson is { } person && currentMap.FindPerson(person) is { } along)
        {
            partner = new Follower(along, partnerId);
            KeepPartnerAlong();
        }
    }

    /// <summary>The game as it stands, as a save.</summary>
    private SaveData BuildSave()
    {
        // A trainer marked as beaten on the map itself (a test, a tool) is beaten in the story too
        foreach (string id in MapDatabase.DefeatedTrainerIds()) story.Defeat(id);
        var told = story.Snapshot();
        return new()
        {
            PlayerName = playerName,
            Look = PlayerIdentity.Look,
            RivalName = PlayerIdentity.RivalName,
            Outfit = wardrobe.Worn.IsOwn ? null : wardrobe.Worn,
            Wardrobe = wardrobe.Owned.Count > 0 ? wardrobe.Owned.ToList() : null,
            TrainerId = trainerId,
            Started = adventureStarted,
            TrainerScore = trainerScore,
            HallOfFameTotal = hallOfFame.Total,
            HallOfFameDebut = hallOfFame.Debut,
            HallOfFame = hallOfFame.Entries.Select(SavedHallOfFameEntry.From).ToList(),
            Journal = journal.All.ToList(),
            Rules = Ruleset.Current.Preset,
            CurrentMapName = currentMap.Name,
            PlayerGridX = player.GridX,
            PlayerGridY = player.GridY,
            PlayerFacing = player.Facing,
            Travel = player.Mode,
            PlayerHeight = player.HeightOn(currentMap),
            Exit = exitSpot,
            LastDay = lastDay,
            RegisteredItem = registeredItem,
            Partner = partner?.TrainerId,
            PartnerPerson = partner?.Who.Key,
            RepelSteps = encounterAids.RepelSteps,
            Poketch = poketch.Save(),
            Safari = safari.Active ? new SafariSave(safari.Balls, safari.Steps) : null,
            Encounters = encounters,
            Berries = berries,
            WorldVersion = SaveData.CurrentWorld,
            Party = playerParty.Members.Select(SavedPokemonData.FromPokemon).ToList(),
            Boxes = SavedBoxes.From(pcBoxStorage),
            Inventory = playerInventory.AllItems.Select(i => new SavedItemData { ItemName = i.Name, Quantity = i.Quantity }).ToList(),
            SeenSpecies = playerPokedex.SeenSpecies.ToList(),
            CaughtSpecies = playerPokedex.CaughtSpecies.ToList(),
            NationalPokedex = playerPokedex.NationalUnlocked,
            Diplomas = playerPokedex.Diplomas.Order().Select(d => d.ToString()).ToList(),
            DefeatedTrainers = told.DefeatedTrainers,
            StoryFlags = told.Flags,
            StoryVersion = StoryState.CurrentVersion,
            StoryVariables = told.Variables,
            TakenItems = told.TakenItems,
            GreetedPeople = story.Greeted.Order(StringComparer.Ordinal).ToList(),
            PlayerStarter = told.PlayerStarter,
            RivalStarter = told.RivalStarter,
            Money = playerMoney,
            Badges = told.Badges,
            PlayTimeSeconds = playTime
        };
    }

    /// <param name="quiet">True when a panel says the game was saved, so no notice is needed.</param>
    private void SaveCurrentGame(bool quiet = false)
    {
        SaveManager.SaveGame(BuildSave());
        if (quiet) return;
        ShowNotification("Game saved.");
        AudioManager.PlaySound("save");
    }

    public void Update(float dt)
    {
        if (gameStarted) playTime += dt;

        toast.Update(dt);
        startMenu.Animate(dt);
        scriptFade.Update(dt);
        choice.Update(dt);

        // The location sign waits while a fade or another screen covers the field
        if (currentState is GameState.Overworld or GameState.Dialogue) locationSign.Update(dt);

        // The field's small life runs on while a dialogue or a fade covers it: dust settles, a door finishes opening
        if (gameStarted && currentState is GameState.Overworld or GameState.Dialogue or GameState.Transition)
        {
            double before = world.Life.Now;
            world.Life.Advance(dt);
            // The poison's flash fades, and the Poké Radar's patches and the honey trees stir (plan 06 · R13)
            TickEncounters(dt);
            // Thunder follows a storm's lightning, a moment after the flash
            foreach (var thunder in WeatherFx.Thunder(currentMap.WeatherAt(player.GridX, player.GridY), before, world.Life.Now))
                AudioManager.PlaySound(thunder);
            // Rain and hail land round the player while they stand in it
            world.Life.Rainfall(currentMap, player.PixelX / Player.TileSize + 0.5f, player.PixelY / Player.TileSize + 0.5f,
                Weathers.LandsABeat(currentMap.WeatherAt(player.GridX, player.GridY)));
            player.TickBubble(dt);
            foreach (var npc in currentMap.NPCs) npc.TickBubble(dt);
            // A trainer waiting for a rematch spins where they stand (MOVEMENT_TYPE_VS_SEEKER_SPIN)
            rematchSpin += dt;
            if (rematchSpin >= 0.25f)
            {
                rematchSpin = 0f;
                foreach (var npc in currentMap.NPCs.Where(n => n.ReadyForRematch && !runner.IsRunning))
                    npc.Facing = npc.Facing switch { Direction.Down => Direction.Left, Direction.Left => Direction.Up, Direction.Up => Direction.Right, _ => Direction.Down };
            }
        }

        UpdateAmbience();

        // Global Mute Toggle (M)
        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            Settings.Muted = !Settings.Muted;
            ApplySettings(window: false);
            Settings.Save();
            ShowNotification(Settings.Muted ? "Sound off" : "Sound on");
        }

        switch (currentState)
        {
            case GameState.Title:
                titleScreen.Update(dt);
                switch (titleScreen.TakeChoice())
                {
                    case TitleChoice.Continue:
                        ContinueGame();
                        break;
                    case TitleChoice.NewGame:
                        // The professor's welcome comes first; the game begins when it ends, by the rules just chosen
                        newGameRules = titleScreen.Rules;
                        currentState = GameState.Intro;
                        introScreen.Open(GameSettings.CharactersPerSecond(Settings.TextSpeed));
                        AudioManager.PlayMusic(MusicRole.Introduction);
                        break;
                    case TitleChoice.Options:
                        optionsReturnState = GameState.Title;
                        currentState = GameState.Options;
                        optionsScreen.Open();
                        break;
                    case TitleChoice.Quit:
                        QuitRequested = true;
                        break;
                }
                break;
            case GameState.Intro:
                introScreen.Update(dt);
                if (introScreen.Phase == IntroPhase.Done)
                {
                    introScreen.Close();
                    StartNewGame(introScreen.Name, introScreen.Look, newGameRules, introScreen.RivalName);
                }
                break;
            case GameState.Overworld:
                UpdateOverworld(dt);
                break;
            case GameState.Dialogue:
                UpdateDialogue(dt);
                break;
            case GameState.Battle:
                if (battle != null)
                {
                    battle.Update(dt);
                    // The battle theme turns agitated while a Pokémon of the player's is in the red, and calms for the end
                    AudioManager.LowHp = battle.Result == BattleResult.None && battle.PlayerInDanger;
                    if (battle.IsBattleOver)
                    {
                        if (battle.Result == BattleResult.PlayerVictory && battle.IsTrainerBattle) playerMoney += battle.PrizeMoney;
                        if (battle.Result == BattleResult.PlayerVictory) playerMoney += battle.PayDayMoney;
                        playerMoney = Math.Max(0, playerMoney - battle.MoneyLost);
                        AfterBattle(battle);

                        // Only a beaten trainer is done; after a loss they wait for a rematch
                        bool beaten = battle.Result == BattleResult.PlayerVictory;
                        foreach (var battleTrainer in battleTrainers)
                        {
                            battleTrainer.FinishBattle(beaten);
                            // Two people who battle as one trainer (a pair of twins) are beaten together, and the story remembers it
                            if (beaten && battleTrainer.TrainerData is { Id.Length: > 0 } pair)
                            {
                                story.Defeat(pair.Id);
                                foreach (var other in currentMap.Everyone.Where(n => n != battleTrainer && n.TrainerData?.Id == pair.Id)) other.FinishBattle(true);
                            }
                        }
                        battleTrainers.Clear();
                        // What a script that started the battle is told of it
                        scriptOutcome = battle.Result switch
                        {
                            BattleResult.PlayerVictory => BattleOutcome.Won,
                            BattleResult.PlayerDefeat => BattleOutcome.Lost,
                            BattleResult.EnemyCaught => BattleOutcome.Caught,
                            BattleResult.PlayerRan or BattleResult.EnemyFled => BattleOutcome.Fled,
                            _ => BattleOutcome.None
                        };
                        EndBattle();
                    }
                }
                break;
            case GameState.PartyMenu:
                partyScreen.Update(playerParty, dt);
                if (partyScreen.TakeFieldMove() is var (move, index)) UseFieldMoveFromMenu(move, index);
                else if (!partyScreen.IsActive)
                {
                    // A script that asked for a Pokémon to be chosen hears which
                    if (partyScreen.Choosing) scriptAnswer = partyScreen.Chosen;
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.FlyMap:
                flyScreen.Update(dt);
                if (flyScreen.TakeChoice() is { } town)
                {
                    flyTarget = town;
                    currentState = GameState.Overworld;
                    StartScript("common.UseFly", pokemon: playerParty.Members[flyScreen.Flier]);
                }
                else if (!flyScreen.IsActive)
                {
                    currentState = GameState.PartyMenu;
                    partyScreen.IsActive = true;
                }
                break;
            case GameState.BagMenu:
                bagScreen.Update(playerInventory, playerParty, ShowNotification, EvolutionContextNow(), dt);
                registeredItem = bagScreen.Registered;
                if (bagScreen.TakeEvolution() is { } fromBag) PlayEvolutions(new[] { fromBag }, GameState.BagMenu);
                else if (!bagScreen.IsActive && bagScreen.TakePick(out var picked))
                {
                    // A script that asked for an item hears which (plan 06 · R14a)
                    scriptItem = picked?.Name;
                    scriptAnswer = picked != null ? 1 : 0;
                    currentState = GameState.Overworld;
                }
                else if (!bagScreen.IsActive)
                {
                    currentState = GameState.Overworld;
                    if (bagScreen.TakeFieldUse() is { } usedHere) UseFieldItem(usedHere);
                }
                break;
            case GameState.Evolution:
                evolutionScreen.Update(dt);
                if (!evolutionScreen.IsActive) FinishEvolution();
                break;
            case GameState.PokedexMenu:
                pokedexScreen.Update(dt);
                if (!pokedexScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.TrainerCard:
                trainerCardScreen.Update(dt);
                if (!trainerCardScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.SaveMenu:
                saveScreen.Update(dt);
                if (saveScreen.TakeRequest())
                {
                    SaveCurrentGame(quiet: true);
                    saveScreen.MarkSaved();
                }
                // Back to the field once the panel has slid away
                if (!saveScreen.Visible) currentState = GameState.Overworld;
                break;
            case GameState.StarterSelect:
                var chosen = starterSelectScreen.Update(dt);
                if (chosen != null)
                {
                    chosen.Met(PlaceName(), GameClock.Today);
                    playerParty.Add(chosen);
                    playerPokedex.RegisterSeen(chosen.Species.DexNumber);
                    playerPokedex.RegisterCaught(chosen.Species.DexNumber);
                    // The story remembers which was taken, and with it which the rival takes
                    story.ChooseStarter(chosen.Species.Name);
                    scriptAnswer = Math.Max(0, Array.IndexOf(StoryState.Starters, chosen.Species.Name));
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Shop:
                shopScreen.Update(playerInventory, ref playerMoney, ShowNotification, dt);
                if (!shopScreen.IsActive)
                {
                    // What the visit came to is a line of the Journal (shop_menu.c)
                    if (ShopLine(shopScreen.Purchases, shopScreen.UnitsSold) is { } line) journal.Tell(new JournalEvent(line));
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Wardrobe:
                wardrobeScreen.Update(ref playerMoney, ShowNotification, dt);
                if (!wardrobeScreen.IsActive)
                {
                    // The field and the battle draw the player as the outfit names them; what was only tried on is let go
                    PlayerIdentity.SetOutfit(wardrobe.Worn);
                    foreach (string tried in wardrobeScreen.TakeTried()) CharacterModels.Forget(tried);
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Journal:
                journalScreen.Update(journal, dt);
                if (!journalScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.HallOfFame:
                hallOfFameScreen.Update(hallOfFame, dt);
                if (!hallOfFameScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.PCStorage:
                pcScreen.Update(playerParty, pcBoxStorage, ShowNotification, dt);
                if (!pcScreen.IsActive)
                {
                    if (pcScreen.Changed) journal.Tell(new JournalEvent(JournalEventKind.UsedPcBox));
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Options:
                if (optionsScreen.Update(Settings))
                {
                    ApplySettings(window: true);
                    Settings.Save();
                }
                if (!optionsScreen.IsActive) currentState = optionsReturnState;
                break;
            case GameState.Transition:
                UpdateTransition(dt);
                break;
        }
    }

    private List<AmbienceLayer> fieldAmbience = new();
    private (Map? Map, int X, int Y, FieldWeather Weather) ambienceAt;

    /// <summary>
    /// The field's ambience (plan 05 · A7): the beds heard where the player stands, worked out again when they step
    /// onto another tile or the weather changes, and silence while a battle, an evolution, the title or the
    /// introduction has the screen.
    /// </summary>
    private void UpdateAmbience()
    {
        bool heard = gameStarted && currentMap != null && player != null
            && currentState is not (GameState.Title or GameState.Intro or GameState.Battle or GameState.Evolution);
        if (!heard)
        {
            if (fieldAmbience.Count > 0) fieldAmbience = new List<AmbienceLayer>();
            ambienceAt = default;
        }
        else
        {
            var at = (currentMap, player!.GridX, player.GridY, currentMap!.WeatherAt(player.GridX, player.GridY));
            if (at != ambienceAt)
            {
                fieldAmbience = FieldAmbience.Around(currentMap, player.GridX, player.GridY);
                ambienceAt = at;
            }
        }
        AudioManager.SetAmbience(fieldAmbience);
    }

    /// <summary>The pan of a sound made at a tile column of the field: from its side of the screen.</summary>
    internal float PanAt(int tileX) => FieldAmbience.PanOf(tileX - player.GridX);

    private void UpdateOverworld(float dt)
    {
        // What field moves left in force (Strength, Flash, Defog), and boulders sliding on from a push
        KeepFieldMovesInForce();
        KeepPuzzleInForce(dt);
        KeepTheClock();
        KeepBerriesInView();
        SlideBoulders(dt);
        poketchView.Update(dt);
        // The app on the Pokétch runs on whether it is out or not, as the original's lower screen does
        if (poketch.Enabled) poketch.State?.Update(dt, PoketchNow());
        // Who is on the map follows the story's flags as soon as they change, whatever changed them (a script, a
        // first arrival, a field move, a tool): only a script's end and an arrival looked before, so a flag set
        // anywhere else left people where they were until the next of those
        RefreshPresence();

        // Whoever travels with the player keeps up, whatever else has the field
        partner?.Update(dt, player.Stride);

        // A script has the field: the keys do nothing until it ends, and people moving of their own accord only
        // finish the step they are on
        if (runner.IsRunning)
        {
            wandering.Continue(dt);
            UpdateScript(dt);
            return;
        }

        // A Gym's puzzle has the field for a moment: a punching bag on its run (plan 01 · M9)
        if (PuzzleHoldsField) return;

        // A rod is out: the keys are the rod's until it is put away
        if (fishing != null)
        {
            UpdateFishing(dt);
            return;
        }

        // The place just come to runs its own script first, if it has one
        if (arrived)
        {
            arrived = false;
            RefreshPresence();
            // Out of a warp the original walks the player a step onto the tile in front, and that step can start a
            // trigger, once the place's own script is done (Cheryl at Eterna Forest's edge, plan 02 · S6)
            bool stepped = steppedOutOfWarp;
            steppedOutOfWarp = false;
            if (StartEnterScript())
            {
                triggerAfterEnter |= stepped;
                return;
            }
            if (stepped && TryStepTrigger()) return;
        }

        // A trainer has spotted the player: nothing else happens until they have walked up (both of them, when two
        // came together)
        if (trainerApproach != null)
        {
            trainerApproach.Update(dt, player);
            pairApproach?.Update(dt, player);
            if (trainerApproach.IsDone && pairApproach?.IsDone != false)
            {
                var trainer = trainerApproach.Trainer;
                var pair = pairApproach?.Trainer;
                trainerApproach = null;
                pairApproach = null;
                if (pair != null) StartScript(FieldScripts.TrainerPair, trainer, pair: pair);
                else ChallengeTrainer(trainer);
            }
            return;
        }

        if (trainersLookOnArrival)
        {
            trainersLookOnArrival = false;
            if (CheckTrainerSight()) return;
        }

        // Check Start Menu
        if (startMenu.IsActive)
        {
            HandleStartMenuChoice(startMenu.Update());
            return;
        }

        // The Pokétch in hand: the arrows and buttons are its, and the player stands still
        if (poketchView.InHand)
        {
            UpdatePoketchInHand(dt);
            return;
        }

        if (InputManager.IsActionPressed(GameAction.Menu))
        {
            startMenu.HasPokedex = story.Has(StoryState.PokedexFlag);
            startMenu.HasPokemon = playerParty.Count > 0;
            startMenu.Open();
            return;
        }

        // The Pokétch comes out or goes away; its side button changes the app
        if (InputManager.IsActionPressed(GameAction.Poketch)) poketchView.Toggle(poketch);
        if (InputManager.IsActionPressed(GameAction.PoketchApp)) poketchView.NextApp(poketch);
        if (InputManager.IsActionPressed(GameAction.PoketchTouch) && !player.IsMoving && poketchView.TakeInHand(poketch, PoketchNow())) return;

        // The key item kept on the item button (the original's Y button)
        if (InputManager.IsActionPressed(GameAction.Item) && !player.IsMoving)
        {
            if (registeredItem != null && ItemDatabase.Get(registeredItem) is { } kept && playerInventory.GetQuantity(kept) > 0) UseFieldItem(kept);
            else ShowNotification("A key item can be kept on this button: register one in the bag.");
            return;
        }

        // On the Bicycle the run button changes gear (the original's B button)
        bool runDown = InputManager.IsActionDown(GameAction.Run);
        if (player.Mode == TravelMode.Cycling && runDown && !runWasDown)
        {
            player.FastGear = !player.FastGear;
            AudioManager.PlaySound("gear");
            ShowNotification(player.FastGear ? "Top gear." : "Low gear.");
        }
        runWasDown = runDown;

        // People move about of their own accord, kept out of the player's way
        if (!PeopleStayPut) wandering.Update(dt, currentMap, (player.GridX, player.GridY), player.Heading, peopleRandom ??= Dice.New());

        // Overworld Player Movement
        player.Moves = FieldMovement.MovesOf(playerParty);
        player.Lead = WildLead.Of(playerParty, encounterAids);
        player.Moment = momentOf ??= EncounterMomentNow;
        if (Steering is { } steer) player.Advance(dt, currentMap, steer.Want, steer.Run, StartWildBattle, HandleWarp, OnStep);
        else player.Update(dt, currentMap, StartWildBattle, HandleWarp, OnStep);

        // The player has set off from a tile: whoever travels with them goes there next
        if (partner != null && currentMap.Follower == partner.Who)
        {
            var head = player.Heading;
            if (head != (player.GridX, player.GridY) && head != partnerHeading) partner.PlayerLeft(player.GridX, player.GridY);
            partnerHeading = head;
        }

        // A door that leads somewhere opens as the player steps up to it: the door's own tile, or the open way
        // into a porch with the door in the wall behind
        var (headX, headY) = player.Heading;
        if (player.IsMoving && currentMap.GetWarpAt(headX, headY) != null && WorldRenderer.DoorPlace(currentMap, headX, headY) is { } opening
            && world.Life.OpenDoor(currentMap, headX, headY))
            AudioManager.PlaySound(opening.Glass ? "door_slide" : "door_open");

        // The door the player came out of shuts behind them
        if (world.Life.TakeShutting() is { } shutting && WorldRenderer.DoorPlace(shutting.Map, shutting.X, shutting.Y) is { } shut)
            AudioManager.PlaySound(shut.Glass ? "door_slide" : "door_close");

        if (trainerApproach != null || currentState != GameState.Overworld) return;

        // Turning a full circle is how one Pokémon evolves
        if (spin.Update(player.Facing, dt) && PlayEvolutions(FindEvolutions(playerParty.Members, EvolutionTrigger.Spin, cancellable: false), GameState.Overworld))
            return;

        // Interaction (Z / Space)
        if (!player.IsMoving && InputManager.IsActionPressed(GameAction.Confirm))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// After each step: the party walks along (Platinum raises friendship every 128 steps, and the Pokémon at
    /// the head of the party counts its steps for the evolutions that ask for a long walk), then trainers look.
    /// </summary>
    // Where the people round the player were last made ready: the map and the chunk (plan 11 · C1)
    private (string Map, int X, int Y) charactersReadyAt;

    /// <summary>How far from the player people's looks are made ready, in tiles: two chunks, well beyond what is in view.</summary>
    private const int CharacterReach = 64;

    /// <summary>
    /// Starts building, in the background, the looks of everyone within reach of the player (those a flag hides for
    /// now too) and the player's own, so none holds up a frame as it comes into sight (plan 11 · C1). On a warp
    /// (<paramref name="trim"/>), every other look's rig is let go: the map left behind is behind a fade.
    /// </summary>
    private void KeepCharactersNear(bool trim)
    {
        charactersReadyAt = (currentMap.Name, player.GridX >> 5, player.GridY >> 5);
        var looks = currentMap.Everyone
            .Where(n => !n.IsThing && !n.IsPokemon && Math.Abs(n.GridX - player.GridX) <= CharacterReach && Math.Abs(n.GridY - player.GridY) <= CharacterReach)
            .Select(n => PlayerIdentity.CharacterFor(n.NpcType))
            .Append(PlayerIdentity.Character).Append("PLAYER").Append("DAWN")
            .ToList();
        if (trim) CharacterModels.Trim(looks);
        CharacterModels.Preload(looks);
    }

    private readonly HeldKey poketchAcross = new(), poketchDown = new();
    private PoketchContext? poketchContext;

    /// <summary>What the Pokétch's apps read of the game this frame (plan 06 · R14b).</summary>
    internal PoketchContext PoketchNow()
    {
        var c = poketchContext ??= new PoketchContext
        {
            Poketch = poketch,
            Sound = name => AudioManager.PlaySound(name),
            Cry = p => AudioManager.PlayCry(p)
        };
        c.Party = playerParty;
        c.Now = PoketchView.Clock();
        c.Map = currentMap;
        c.X = player?.GridX ?? 0;
        c.Y = player?.GridY ?? 0;
        c.Story = story;
        c.Pokedex = playerPokedex;
        c.Encounters = encounters;
        c.Radar = radar;
        c.Berries = berries;
        c.Rng = fieldRandom;
        return c;
    }

    /// <summary>The Pokétch in the player's hand: the arrows move its cursor (held, they go on), confirm touches, cancel lets go.</summary>
    private void UpdatePoketchInHand(float dt)
    {
        var context = PoketchNow();
        int across = poketchAcross.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        int down = poketchDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        for (int i = 0; i < Math.Abs(across); i++) poketchView.MoveCursor(poketch, context, Math.Sign(across), 0);
        for (int i = 0; i < Math.Abs(down); i++) poketchView.MoveCursor(poketch, context, 0, Math.Sign(down));
        if (InputManager.IsActionPressed(GameAction.Confirm)) poketchView.Touch(poketch, context);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.PoketchTouch)) poketchView.LetGo();
    }

    private bool OnStep()
    {
        if (charactersReadyAt != (currentMap.Name, player.GridX >> 5, player.GridY >> 5)) KeepCharactersNear(trim: false);
        if (++friendshipSteps >= FriendshipRules.WalkCycleSteps)
        {
            friendshipSteps = 0;
            foreach (var p in playerParty.Members) FriendshipRules.Apply(p, FriendshipEvent.WalkCycle, fieldRandom);
        }
        if (playerParty.Count > 0) Evolution.CountStep(playerParty.Members[0]);
        poketch.Step();
        // The Vs. Seeker charges in the bag, and the rematches it found end after a hundred steps (plan 06 · R12)
        if (VsSeeker.Step(story, playerInventory.GetQuantity(ItemDatabase.Get(VsSeeker.Item)!) > 0)) StopRematches(Direction.Down);
        // Riding out onto the water or back onto land, or getting on or off the bicycle, changes the music
        if (player.Mode != musicTravel) PlayFieldMusic();
        bool entered = EnterArea();

        // What the step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out
        // onto it, dust where they came down from a hop
        StepLeavesItsMark();

        // A step onto one of the Canalave Gym's platforms is a ride, and nothing more (plan 01 · M9)
        if (TryRideLift()) return true;
        // Poison bites every fourth step, the Poké Radar charges and its patches out of sight go still (plan 06 · R13)
        if (EncountersStep()) return true;
        // The Safari Game counts its steps, and its last one ends it (plan 01 · M7)
        if (safari.Step() && StartScript(FieldScripts.SafariTimeUp)) return true;
        // A Repel's last step says it has worn off (Repel_UpdateSteps, plan 06 · R11)
        if (encounterAids.Step() && StartScript(FieldScripts.RepelWoreOff)) return true;

        // The story comes first: the script of the place walked into, then of the tiles stepped on; only then do trainers
        // look. A step into a place onto a trigger does both, the place's first (as the original runs a map's own
        // script as it loads, before its coordinate events): the trigger is tried once that script has ended
        if (entered)
        {
            triggerAfterEnter = true;
            return true;
        }
        if (TryStepTrigger()) return true;
        return CheckTrainerSight();
    }

    /// <summary>
    /// On the overworld a town runs into its routes with no door between them: stepping over the border brings
    /// up the new place's name and its music, as arriving through a door does.
    /// </summary>
    /// <returns>True when the place walked into has a script of its own, which has been started.</returns>
    /// <summary>The name of the place the player stands in, with whoever it names filled in: where a Pokémon met here was met.</summary>
    private string PlaceName() => PlayerIdentity.Fill(currentMap.DisplayNameAt(player.GridX, player.GridY));

    private bool EnterArea()
    {
        var area = currentMap.AreaAt(player.GridX, player.GridY);
        if (area == currentArea) return false;
        bool another = currentArea != null && area != null;
        currentArea = area;
        if (area == null) return false;
        // Another place: what lasted only while the player stayed in the last one is forgotten, and the roamers move on
        // (FieldSystem_InitFlagsOnMapChange, not during a Safari Game; plan 06 · R13)
        if (another) ChangePlace();
        if (another && !safari.Active) Roamers.PlayerWalkedInto(encounters, area.Key, fieldRandom);
        NoteArrival();
        PlayFieldMusic();
        if (!string.IsNullOrEmpty(area.DisplayName)) locationSign.Show(area.DisplayName);
        return StartEnterScript();
    }

    // ---------------------------------------------------------------- evolution

    /// <summary>
    /// What the evolution rules need to know about this moment: the team, the bag, the hour and the place.
    /// There is no weather in the field yet, so it never rains.
    /// </summary>
    private EvolutionContext EvolutionContextNow() => new()
    {
        Party = playerParty,
        Bag = playerInventory,
        IsNight = GameClock.IsNight,
        IsDusk = GameClock.Now == TimeOfDay.Twilight,
        IsRaining = Weathers.IsRain(currentMap.WeatherAt(player.GridX, player.GridY)),
        Sites = currentMap.EvolutionSitesAt(player.GridX, player.GridY),
        Region = RegionDatabase.RegionOfMap(currentMap.Name)?.Name
    };

    /// <summary>The evolutions a trigger sets off among these Pokémon right now.</summary>
    private List<EvolutionRequest> FindEvolutions(IEnumerable<Pokemon> candidates, EvolutionTrigger trigger, bool cancellable, PokemonSpecies? tradedFor = null)
    {
        var context = EvolutionContextNow();
        context.TradedFor = tradedFor;
        var found = new List<EvolutionRequest>();
        foreach (var p in candidates)
        {
            if (playerParty.Members.Contains(p) && Evolution.Find(p, trigger, context) is { } evolution)
                found.Add(new EvolutionRequest(p, evolution, cancellable));
        }
        return found;
    }

    /// <summary>
    /// Fades into the evolution scene and plays these one after the other, then fades to <paramref name="returnTo"/>.
    /// False (and nothing happens) when there are none.
    /// </summary>
    private bool PlayEvolutions(IReadOnlyCollection<EvolutionRequest> requests, GameState returnTo)
    {
        if (requests.Count == 0) return false;
        // The scene shows each Pokémon before and after: their models are built while the screen fades out
        var shown = new List<string>();
        foreach (var request in requests)
        {
            pendingEvolutions.Enqueue(request);
            shown.Add(request.Pokemon.ModelName);
            shown.Add(Evolution.ModelAfter(request.Pokemon, request.Evolution));
        }
        foreach (var name in shown) PokemonModels.Request(name);
        evolutionReturnState = returnTo;
        StartTransition(GameState.Evolution, () =>
        {
            AwaitModels(shown);
            BeginNextEvolution();
        });
        return true;
    }

    private bool BeginNextEvolution()
    {
        while (pendingEvolutions.Count > 0)
        {
            var request = pendingEvolutions.Dequeue();
            var context = EvolutionContextNow();
            var evolution = request.Evolution;

            // An earlier evolution in the queue may have changed things (the party, the bag): look again
            if (request.Cancellable)
            {
                var trigger = Evolution.TriggerOf(evolution.Method) ?? EvolutionTrigger.LevelUp;
                if (Evolution.Find(request.Pokemon, trigger, context) is not { } still) continue;
                evolution = still;
            }
            evolutionScreen.Begin(request.Pokemon, evolution, context, request.Cancellable);
            return true;
        }
        return false;
    }

    /// <summary>The scene has ended: the new species goes into the Pokédex, then the next evolution or the way back.</summary>
    private void FinishEvolution()
    {
        if (evolutionScreen.Outcome is { } outcome)
        {
            playerPokedex.RegisterCaught(outcome.Into.DexNumber);
            if (outcome.Shed != null) playerPokedex.RegisterCaught(outcome.Shed.Species.DexNumber);
        }
        if (BeginNextEvolution()) return;

        // The evolution's theme gives way to the field's again, wherever the scene goes back to
        StartTransition(evolutionReturnState, PlayFieldMusic);
    }

    /// <summary>
    /// A Pokémon arrives by trade: the one call a trade needs to make here, whether it is with a friend over the
    /// internet (plan 07) or with a character in the game. It takes the place of the Pokémon given for it
    /// (<paramref name="given"/>; null when nothing left the team), starts over at its species' base friendship,
    /// goes into the Pokédex, and evolves there and then if trading is what it was waiting for, which can't be
    /// stopped. Saving and what the other side receives are the caller's.
    /// </summary>
    public void ReceiveTradedPokemon(Pokemon received, Pokemon? given = null)
    {
        int place = given != null ? playerParty.Members.IndexOf(given) : -1;
        if (place >= 0) playerParty.Members[place] = received;
        else
        {
            if (given != null) pcBoxStorage.Remove(given);
            if (!playerParty.Add(received)) pcBoxStorage.Store(received);
        }

        received.Friendship = received.Species.BaseFriendship;
        playerPokedex.RegisterCaught(received.Species.DexNumber);

        var returnTo = currentState == GameState.Transition ? stateAfterTransition : currentState;
        PlayEvolutions(FindEvolutions(new[] { received }, EvolutionTrigger.Trade, cancellable: false, given?.Species), returnTo);
    }

    /// <summary>After each step: a trainer looking this way notices the player and comes over to battle.</summary>
    private bool CheckTrainerSight()
    {
        // Nobody challenges a player with no Pokémon able to fight (StartWildBattle says why)
        if (!playerParty.HasUsablePokemon) return false;
        var trainer = TrainerApproach.FindSpotter(currentMap, player.GridX, player.GridY, height: player.HeightOn(currentMap));
        if (trainer == null) return false;

        wandering.Settle(trainer);
        trainerApproach = new TrainerApproach(trainer, player);
        // Two trainers who see the player at once come together (APPROACH_TYPE_VS2): two against one when the
        // player has two Pokémon able to fight, or against the player and whoever travels with them
        pairApproach = null;
        if (TrainerApproach.FindSpotter(currentMap, player.GridX, player.GridY, except: trainer, height: player.HeightOn(currentMap)) is { } second
            && (partner != null || playerParty.Members.Count(p => !p.IsFainted) >= 2))
        {
            wandering.Settle(second);
            pairApproach = new TrainerApproach(second, player, turnPlayer: false);
        }
        AudioManager.PlaySound("exclaim", PanAt(trainer.GridX));

        // Their eye theme, by their class, plays while they walk up and talk, until the battle theme cuts in
        AudioManager.Region = RegionDatabase.RegionOfMap(currentMap.Name)?.Id;
        AudioManager.PlayMusic(MusicDirector.EyeRole(trainer.TrainerData?.TrainerClass), immediate: true);
        eyeThemePlaying = true;
        return true;
    }

    /// <summary>A trainer who came up to the player says their piece and battles: their own script, or the common one for trainers.</summary>
    private void ChallengeTrainer(NPC npc) => StartScript(FieldScripts.For(npc) ?? FieldScripts.Trainer, npc);

    /// <summary>Who someone is for <see cref="StoryState.Greet"/>: their place and their key, the same in every game.</summary>
    internal static string GreetingKey(Map map, NPC npc) => $"{npc.ScriptFile ?? map.Name}.{npc.Key ?? npc.Name}";

    private void TryInteract()
    {
        int dx = 0, dy = 0;
        switch (player.Facing)
        {
            case Direction.Up: dy = -1; break;
            case Direction.Down: dy = 1; break;
            case Direction.Left: dx = -1; break;
            case Direction.Right: dx = 1; break;
        }

        int targetX = player.GridX + dx;
        int targetY = player.GridY + dy;

        // Check NPC interaction; reception and shop counters can be talked across
        // Someone on a bridge's deck is out of reach from the ground under it
        float standing = player.HeightOn(currentMap);
        var npc = currentMap.NpcIn(targetX, targetY, currentMap.FootingAt(targetX, targetY, standing));
        if (npc == null && currentMap.IsCounter(targetX, targetY))
        {
            npc = currentMap.GetNpcAt(targetX + dx, targetY + dy);
        }
        // A punching bag of the Veilstone Gym is kicked, not spoken to (plan 01 · M9)
        if (npc != null && TryKickBag(npc)) return;
        if (npc != null)
        {
            // Face player (a trainer goes back to looking the old way if they win); a thing stays as it is
            if (npc.IsTrainer) npc.LeavePost();
            if (!npc.IsThing) npc.FaceTowards(player.GridX, player.GridY);
            // Someone spoken to counts toward the Hallowed Tower's stirring, once each (plan 08 · P12)
            if (!npc.IsThing && !npc.IsPokemon) story.Greet(GreetingKey(currentMap, npc));

            // What happens next is theirs to say: their own script, or the common one for what they are
            // (a nurse, a clerk, a PC, the briefcase, a trainer, someone with lines)
            if (FieldScripts.For(npc) is { } theirs && StartScript(theirs, npc)) return;
        }

        // A signboard is read, or runs the script it has
        var sign = currentMap.GetSignboardAt(targetX, targetY);
        if (sign != null)
        {
            string read = currentMap.SignScripts.GetValueOrDefault((targetX, targetY)) ?? FieldScripts.Sign;
            if (StartScript(read, own: new[] { sign }, file: currentMap.ScriptFileAt(targetX, targetY))) return;
        }

        // A tile that is read when faced: an inscription, a pillar
        if (currentMap.TileScripts.GetValueOrDefault((targetX, targetY)) is { } faced
            && StartScript(faced, file: currentMap.ScriptFileAt(targetX, targetY))) return;

        // Something hidden where the player is looking: it is found, once
        if (FieldScripts.HiddenAt(currentMap, targetX, targetY, story) is { } hidden
            && StartScript(FieldScripts.HiddenItem, file: currentMap.ScriptFileAt(targetX, targetY), item: (hidden.Item, hidden.Count), flag: hidden.Flag)) return;

        // A honey tree faced from the south (HoneyTree_TryInteract, plan 06 · R13)
        if (HoneyTrees.Faced(currentMap, player.GridX, player.GridY, player.Facing) != null && StartScript(FieldScripts.HoneyTree)) return;

        // What the tile ahead is (Field_TileBehaviorToScript): a waterfall faced from the water, a rock face along
        // its grain, and deep water at the player's feet, offered only to someone who may surf (the move and the
        // Fen Badge), as the original offers it
        var walker = new Walker(player.Mode, standing, Moves: player.Moves);
        var spot = FieldMoveRules.SpotOf(currentMap, player.GridX, player.GridY, player.Facing, walker) with { Partner = partner != null };
        if (spot.Waterfall && spot.Surfing) StartScript(FieldScripts.Waterfall);
        else if (spot.RockFace && player.Mode == TravelMode.OnFoot) StartScript(FieldScripts.RockFace);
        else if (spot.Water && FieldMoveRules.Knower(playerParty, FieldMove.Surf) != null
                 && FieldMoveRules.Check(FieldMove.Surf, spot, story) == FieldMoveError.None) StartScript(FieldScripts.Water);
    }

    private void HandleWarp(Warp warp)
    {
        // A trigger waiting on the tile goes first, as the original's coordinate events come before the step off a
        // mat that takes its warp: Cheryl turns the player back at the forest's edge (plan 02 · S6)
        if (FieldScripts.TriggerAt(currentMap, warp.SourceX, warp.SourceY, story) != null && TryStepTrigger()) return;
        // Onto the Cycling Road only a rider is let through: the gate keeper turns anyone else back
        if (warp.CyclistsOnly && player.Mode != TravelMode.Cycling)
        {
            StartScript(FieldScripts.CyclistsOnly);
            return;
        }
        NoteTheWayIn(warp);

        // A door has made its own sound as it opened; any other way through is the original's footsteps on stairs,
        // and a warp panel its own
        if (WorldRenderer.DoorPlace(currentMap, warp.SourceX, warp.SourceY) == null)
            AudioManager.PlaySound(currentMap.BehaviourAt(warp.SourceX, warp.SourceY) == TileBehavior.WarpPanel ? "warp" : "stairs");
        // The music fades with the screen, so a building with its own theme starts as the door opens on it
        PlayAreaMusic(MapDatabase.Get(warp.TargetMap), warp.TargetX, warp.TargetY);
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(warp.TargetMap);
            player.SetPosition(warp.TargetX, warp.TargetY, warp.TargetFacing);
            ArriveOnMap();
            steppedOutOfWarp = true;
            AnnounceLocation();
            // Through a gate onto the Cycling Road, the Bicycle stays under the player until the next warp
            if (warp.CyclistsOnly) story.Set(BicycleRules.OnCyclingRoadFlag);

            // Coming out of a building, the door stands open behind the player for a moment, then shuts
            world.Life.Clear();
            var (dx, dy) = FieldMovement.Delta(warp.TargetFacing);
            int doorX = warp.TargetX - dx, doorY = warp.TargetY - dy;
            if (!currentMap.IsIndoors && WorldRenderer.DoorPlace(currentMap, doorX, doorY) != null)
                world.Life.LeaveDoor(currentMap, doorX, doorY);
        });
    }

    /// <summary>
    /// Walks the player in place of the keys while it is set: the way they are steered each frame (null stands
    /// still) and whether they run. For the story's scripted walks and for the screenshot harness.
    /// </summary>
    public (Direction? Want, bool Run)? Steering { get; set; }

    /// <summary>Puts a bubble over someone's head for a moment: a person of the map by name, or the player (null).</summary>
    public void ShowEmote(string? npcName, EmoteBubble bubble, float seconds = 0.9f)
    {
        if (npcName == null) player.ShowBubble(bubble, seconds);
        else currentMap.NPCs.FirstOrDefault(n => n.Name == npcName)?.ShowBubble(bubble, seconds);
    }

    /// <summary>Whether lightning is whitening the field at this moment (a thunderstorm where the player stands).</summary>
    public bool LightningNow =>
        gameStarted && WeatherFx.Lightning(currentMap.WeatherAt(player.GridX, player.GridY), world.Life.Now) > 0.6f;

    /// <summary>Sends the field's camera to look at a tile, easing there over the given time; it stays until released.</summary>
    public void PanCamera(int x, int y, float seconds) => world.PanCamera(x + 0.5f, y + 0.5f, seconds);

    /// <summary>Brings the field's camera back to the player.</summary>
    public void ReleaseCamera(float seconds) => world.ReleaseCamera(seconds);

    /// <summary>Crosses to another region, landing where arrivals from the previous region come in.</summary>
    private void TravelTo(Region region)
    {
        var spot = region.ArrivalSpot!;
        PlayAreaMusic(MapDatabase.Get(spot.Map), spot.X, spot.Y);
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(spot.Map);
            player.SetPosition(spot.X, spot.Y, spot.Facing);
            trainersLookOnArrival = true;
            ArriveOnMap();
            AnnounceLocation();
        });
    }

    /// <summary>
    /// The field's music now: the surf theme while the player rides a Pokémon over the water, the bicycle's while
    /// they cycle (Platinum keeps those across towns and routes), and otherwise the theme of the place they stand in.
    /// </summary>
    private void PlayFieldMusic()
    {
        eyeThemePlaying = false;
        musicTravel = player.Mode;
        AudioManager.Region = RegionDatabase.RegionOfMap(currentMap.Name)?.Id;
        var ride = player.Mode switch { TravelMode.Surfing => MusicRole.Surf, TravelMode.Cycling => MusicRole.Bicycle, _ => (MusicRole?)null };
        if (ride != null && MusicDirector.Resolve(ride.Value, AudioManager.Region, MusicLibrary.Exists) != null) AudioManager.PlayMusic(ride.Value);
        else PlayAreaMusic(currentMap, player.GridX, player.GridY);
    }

    /// <summary>
    /// Plays the theme of a place on a map (its night arrangement at night) with its region's versions of the
    /// shared themes. A place that names no theme keeps whatever is playing; the same theme carries on without a
    /// restart.
    /// </summary>
    private static void PlayAreaMusic(Map map, int x, int y)
    {
        AudioManager.LowHp = false;
        AudioManager.Region = RegionDatabase.RegionOfMap(map.Name)?.Id;
        string track = map.BgmTrackAt(x, y);
        if (!string.IsNullOrEmpty(track)) AudioManager.PlayMusic(track);
    }

    /// <summary>
    /// Shows the place's name on arriving outdoors, as the games do on entering a town or route or stepping out
    /// of a building. Rooms have no sign.
    /// </summary>
    private void AnnounceLocation()
    {
        currentArea = currentMap.AreaAt(player.GridX, player.GridY);
        NoteArrival();
        if (currentMap.IsIndoors) locationSign.Hide();
        else locationSign.Show(currentMap.DisplayNameAt(player.GridX, player.GridY));
    }

    private void HandleStartMenuChoice(StartMenuChoice choice)
    {
        switch (choice)
        {
            case StartMenuChoice.None:
                return;
            case StartMenuChoice.Pokedex:
                currentState = GameState.PokedexMenu;
                pokedexScreen.Open(playerPokedex, world.Portrait(PlayerIdentity.Character));
                break;
            case StartMenuChoice.Pokemon:
                currentState = GameState.PartyMenu;
                partyScreen.Open();
                break;
            case StartMenuChoice.Bag:
                currentState = GameState.BagMenu;
                bagScreen.Registered = registeredItem;
                bagScreen.Aids = encounterAids;
                bagScreen.PlayerName = PlayerIdentity.Name;
                bagScreen.Open();
                break;
            case StartMenuChoice.Trainer:
                currentState = GameState.TrainerCard;
                trainerCardScreen.Open(world.Portrait(PlayerIdentity.Character));
                break;
            case StartMenuChoice.Options:
                optionsReturnState = GameState.Overworld;
                currentState = GameState.Options;
                optionsScreen.Open();
                break;
            case StartMenuChoice.Save:
                // The save as it will be, with the question under it
                currentState = GameState.SaveMenu;
                saveScreen.Open(BuildSave());
                break;
            case StartMenuChoice.SaveAndQuit:
                SaveCurrentGame();
                QuitRequested = true;
                return;
            case StartMenuChoice.Quit:
                QuitRequested = true;
                return;
        }

        // A full screen takes over: the menu is gone when the player comes back
        startMenu.Hide();
    }

    /// <summary>The trainers who were waiting for a rematch give up (the original turns them to look about).</summary>
    private void StopRematches(Direction facing)
    {
        foreach (var npc in currentMap.Everyone.Where(n => n.ReadyForRematch))
        {
            npc.ReadyForRematch = false;
            npc.Facing = facing;
        }
    }

    /// <summary>The Journal's line for a visit to a Mart: bought and sold, bought plenty, sold plenty, bought, sold, or nothing.</summary>
    public static JournalEventKind? ShopLine(int purchases, int unitsSold) =>
        purchases > 0 && unitsSold > 0 ? JournalEventKind.BusinessAtMart
        : purchases > 1 ? JournalEventKind.LotsOfShopping
        : unitsSold > 1 ? JournalEventKind.SoldALot
        : purchases > 0 ? JournalEventKind.ShoppedAtMart
        : unitsSold > 0 ? JournalEventKind.SoldALittle
        : null;

    /// <summary>What the Trainer Card shows now: who the player is, what they have, and its colour by what they have done.</summary>
    private TrainerCardInfo TrainerCardNow()
    {
        bool famous = hallOfFame.Total > 0 || RegionDatabase.All.Any(r => story.Has(r.StoryCompleteFlag));
        int stars = TrainerCardRules.Level(famous, TrainerCardRules.NationalCaught(playerPokedex.CaughtSpecies), 0, false, 0);
        return new TrainerCardInfo(playerName, trainerId, playerMoney, playerPokedex.SeenCount, playerPokedex.CaughtCount, playTime,
            story.BadgeMask, adventureStarted)
        {
            Score = trainerScore,
            Stars = stars,
            Colour = TrainerCardRules.Colour(story.Has(StoryState.PokedexFlag), stars),
            HallOfFameDebut = hallOfFame.Debut
        };
    }

    /// <summary>
    /// What a battle writes in the Journal (encounter.c, UpdateJournal): a Gym Leader, one of the Elite Four or the
    /// Champion beaten has a line of their own; any other trainer is the day's trainer; a Pokémon caught is the day's
    /// Pokémon, and so is one knocked out once five have been in one place.
    /// </summary>
    private void NoteBattle()
    {
        if (battle == null) return;
        string place = PlaceName();
        if (battle.Result == BattleResult.PlayerVictory && battle.IsTrainerBattle)
        {
            var foe = battle.Trainers[0];
            if (foe.TrainerClass == "Leader") journal.Tell(new JournalEvent(JournalEventKind.BeatGymLeader, foe.Name));
            else if (foe.TrainerClass == "Elite Four") journal.Tell(new JournalEvent(JournalEventKind.BeatEliteFourMember, foe.Name));
            else if (foe.TrainerClass == "Champion") journal.Tell(new JournalEvent(JournalEventKind.BeatChampion, foe.Name));
            else journal.BeatTrainer(PlayerIdentity.Fill(foe.FullTitle), place);
        }
        else if (battle.Result == BattleResult.EnemyCaught && battle.Caught is { } caught)
        {
            journal.Caught(caught.Species.Name, place);
            poketch.Remember(caught);
        }
        else if (battle.Result == BattleResult.PlayerVictory && battle.EnemyPokemon is { } wild) journal.Defeated(wild.Species.Name, place);
    }

    /// <summary>
    /// What a battle adds to the Trainer Card's score (the original's <c>GameRecords_IncrementTrainerScore</c>): a won
    /// trainer battle, a wild Pokémon knocked out, or one caught (more for one of the National Pokédex than of
    /// Sinnoh's, and twenty for a species never caught before).
    /// </summary>
    private void ScoreBattle()
    {
        if (battle == null || battle.Kind == BattleKind.CatchingLesson) return;
        NoteBattle();
        int points = battle.Result switch
        {
            BattleResult.PlayerVictory => battle.IsTrainerBattle ? TrainerScore.WonTrainerBattle : TrainerScore.WonWildBattle,
            BattleResult.EnemyCaught when battle.Caught is { } caught =>
                (caught.Species.SinnohNumber != null ? TrainerScore.CaughtRegional : TrainerScore.CaughtNational)
                + (battle.CaughtNewSpecies ? TrainerScore.CaughtNewSpecies : 0),
            _ => 0
        };
        trainerScore = TrainerScore.Add(trainerScore, points);
    }

    /// <summary>
    /// Whiting out (plan 06 · R10; the original's <c>FieldTask_BlackOutFromBattle</c>): the player comes round in
    /// front of the nurse of the Pokémon Center they last went into, or beside Mom at home before any, and the one
    /// who looks after them heals the team (<c>common.BlackOutCenter</c>, <c>common.BlackOutHome</c>). Giratina takes
    /// the form its item gives it, the Bicycle and the water are left behind, and Dig and an Escape Rope lead out to
    /// the town. The money was taken by the battle (<see cref="BattleEngine.MoneyLost"/>).
    /// </summary>
    private void WhiteOut()
    {
        // Whoever travelled with the player stays behind (FieldSystem_ClearPartnerTrainer)
        SetPartner(null, null);
        foreach (var p in playerParty.Members) FormRules.ByHeldItem(p);
        var (room, home) = WhiteOutRoom();
        currentMap = MapDatabase.Get(room);
        var healer = currentMap.Everyone.FirstOrDefault(n => n.IsHealingNurse);
        // In front of the nurse, the counter between them; at home, beside Mom
        var (x, y, facing) = healer == null ? (4, 5, Direction.Down)
            : home ? (healer.GridX - 1, healer.GridY, Direction.Right) : (healer.GridX, healer.GridY + 2, Direction.Up);
        player.SetMode(TravelMode.OnFoot);
        player.SetPosition(x, y, facing);
        if (SpawnLocations.Get(story.Var(SpawnLocations.Variable)) is { } town) exitSpot = new MapSpot("Sinnoh", town.X, town.Y, Direction.Down);
        ArriveOnMap();
        AnnounceLocation();
        if (healer == null || !StartScript(home ? FieldScripts.BlackOutHome : FieldScripts.BlackOutCenter, healer)) playerParty.HealAll();
    }

    /// <summary>The room the player comes round in: in Sinnoh the last Pokémon Center's (<see cref="SpawnLocations.Respawn"/>), elsewhere the region's home.</summary>
    private (string Room, bool Home) WhiteOutRoom()
    {
        var region = RegionDatabase.RegionOfMap(currentMap.Name);
        if (region?.Id == RegionDatabase.Sinnoh)
        {
            var spawn = SpawnLocations.Respawn(story);
            if (MapDatabase.MapNames.Contains(spawn.Room)) return (spawn.Room, spawn.Room == "PlayerHouse");
            return ("PlayerHouse", true);
        }
        string house = region?.Maps.FirstOrDefault(m => m.EndsWith("PlayerHouse", StringComparison.Ordinal)) ?? "PlayerHouse";
        return (house, true);
    }

    private void StartTransition(GameState nextState, Action? onMidpoint = null, TransitionKind kind = TransitionKind.Fade)
    {
        stateBeforeTransition = currentState;
        stateAfterTransition = nextState;
        midTransitionCallback = onMidpoint;
        currentState = GameState.Transition;
        transitionKind = kind;
        transitionTimer = 0f;
        isFadingOut = true;
    }

    private void UpdateTransition(float dt)
    {
        transitionTimer += dt;
        if (isFadingOut)
        {
            if (transitionTimer >= SceneTransition.OutSeconds(transitionKind))
            {
                isFadingOut = false;
                transitionTimer = 0f;
                midTransitionCallback?.Invoke();
                midTransitionCallback = null;
            }
        }
        else
        {
            if (transitionTimer >= SceneTransition.InSeconds(transitionKind))
            {
                currentState = stateAfterTransition;
                trainersLookOnArrival = currentState == GameState.Overworld;
            }
        }
    }

    public void ShowNotification(string message)
    {
        toast.Show(message);
    }

    public void Draw()
    {
        FrameProfiler.LapCpu(FrameSection.Update);
        renderContext.OutputWidth = Raylib.GetScreenWidth();
        // During a fade, show the screen being left while fading out and the new one while fading in
        GameState scene = currentState == GameState.Transition
            ? (isFadingOut ? stateBeforeTransition : stateAfterTransition)
            : currentState;
        bool showWorld = scene is GameState.Overworld or GameState.Dialogue or GameState.SaveMenu;
        bool showBattle = scene == GameState.Battle && battle != null;
        bool showTitle = scene == GameState.Title;

        // Menu sprites asked for since the last frame are loaded or baked first, offscreen like the 3D scenes
        PokemonSprites.Service(renderContext);

        // The 3D scenes render into their own targets first (texture modes can't nest)
        if (showWorld)
        {
            world.Render(currentMap, player);
        }
        else if (showBattle)
        {
            battleRenderer.Render(battle!);
        }
        else if (showTitle)
        {
            titleScreen.Render(renderContext, world, titleScene);
        }
        else if (scene == GameState.Evolution)
        {
            evolutionScreen.Render(renderContext);
        }
        else if (scene == GameState.Intro)
        {
            introScreen.Render(renderContext);
        }
        else if (scene == GameState.Wardrobe)
        {
            wardrobeScreen.Render(renderContext);
        }
        // (The field and the battle have told the profiler of their own passes; this takes whatever else was rendered)
        FrameProfiler.Lap(FrameSection.Scene);

        // Render scene to native 1920x1080 Full HD buffer
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.ClearBackground(Color.Black);
        // The picture stays opaque whatever is drawn on it: raylib's usual blending blends alpha like a colour, so
        // anything translucent lowered the alpha of what it covered, and the window, which shows the picture over
        // black, showed every translucent panel darker than it is. The clear is the only thing that writes alpha.
        Rlgl.ColorMask(true, true, true, false);
        Raylib.BeginMode2D(new Camera2D { Zoom = RenderScale });

        switch (currentState)
        {
            case GameState.Title:
                titleScreen.Draw(VirtualWidth, VirtualHeight, renderContext, world);
                break;
            case GameState.Overworld:
            case GameState.Dialogue:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                DrawPoisonFlash();
                // A script's black screen covers the field and leaves the text over it
                DrawScriptFade();
                DrawCutIn();
                // The Pokétch shows over the field while nothing else is on the screen
                if (currentState == GameState.Overworld && !startMenu.IsActive && !ScriptRunning) poketchView.Draw(VirtualWidth, VirtualHeight, poketch, PoketchNow());
                if (safari.Active) ModernUi.SafariCount(VirtualWidth, safari.Balls, safari.Steps);
                dialogue.Draw(VirtualWidth, VirtualHeight);
                DrawChoice();
                locationSign.Draw();
                startMenu.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Battle:
                DrawBattle();
                break;
            case GameState.PartyMenu:
                partyScreen.Draw(VirtualWidth, VirtualHeight, playerParty);
                break;
            case GameState.FlyMap:
                flyScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.BagMenu:
                bagScreen.Draw(VirtualWidth, VirtualHeight, playerInventory, playerParty, EvolutionContextNow());
                break;
            case GameState.Evolution:
                evolutionScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PokedexMenu:
                pokedexScreen.Draw(VirtualWidth, VirtualHeight, playerPokedex);
                break;
            case GameState.TrainerCard:
                trainerCardScreen.Draw(VirtualWidth, VirtualHeight, TrainerCardNow());
                break;
            case GameState.SaveMenu:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                saveScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Intro:
                introScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.StarterSelect:
                starterSelectScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Shop:
                shopScreen.Draw(VirtualWidth, VirtualHeight, playerMoney, playerInventory);
                break;
            case GameState.Wardrobe:
                wardrobeScreen.Draw(VirtualWidth, VirtualHeight, playerMoney);
                break;
            case GameState.HallOfFame:
                hallOfFameScreen.Draw(VirtualWidth, VirtualHeight, hallOfFame);
                break;
            case GameState.Journal:
                journalScreen.Draw(VirtualWidth, VirtualHeight, journal);
                break;
            case GameState.PCStorage:
                pcScreen.Draw(VirtualWidth, VirtualHeight, playerParty, pcBoxStorage);
                break;
            case GameState.Options:
                optionsScreen.Draw(VirtualWidth, VirtualHeight, Settings);
                break;
            case GameState.Transition:
                if (showBattle) DrawBattle();
                else if (showWorld)
                {
                    world.DrawToScreen(VirtualWidth, VirtualHeight);
                    DrawScriptFade();
                }
                else if (scene == GameState.Evolution) evolutionScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.BagMenu) bagScreen.Draw(VirtualWidth, VirtualHeight, playerInventory, playerParty, EvolutionContextNow());
                break;
        }

        toast.Draw(VirtualWidth);

        // The scene closing, or the next one opening
        if (currentState == GameState.Transition)
            SceneTransition.Draw(transitionKind, isFadingOut, transitionTimer, VirtualWidth, VirtualHeight);

        Raylib.EndMode2D();
        // (Ending the 2D mode drew what was batched; every other target is written whole)
        Rlgl.ColorMask(true, true, true, true);
        Raylib.EndTextureMode();
        FrameProfiler.Lap(FrameSection.Interface);

        // Fit the 4K screen into the window (at 1080p this halves it, which also anti-aliases it)
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        float scale = MathF.Min((float)screenW / VirtualWidth, (float)screenH / VirtualHeight);
        int destW = (int)(VirtualWidth * scale);
        int destH = (int)(VirtualHeight * scale);
        int destX = (screenW - destW) / 2;
        int destY = (screenH - destH) / 2;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Rectangle src = new(0, 0, virtualScreen.Texture.Width, -virtualScreen.Texture.Height);
        Rectangle dst = new(destX, destY, destW, destH);
        Raylib.DrawTexturePro(virtualScreen.Texture, src, dst, Vector2.Zero, 0f, Color.White);
        FrameProfiler.Lap(FrameSection.Present);

        Raylib.EndDrawing();
        FrameProfiler.Lap(FrameSection.Swap);
        FrameProfiler.EndFrame();
    }

    /// <summary>The 3D battle field and Pokémon, then the HUD and menus on top.</summary>
    private void DrawBattle()
    {
        if (battle == null) return;
        battleRenderer.DrawField(battle, VirtualWidth, VirtualHeight);
        battle.Draw(VirtualWidth, VirtualHeight);
    }

    public void Close()
    {
        renderContext.Unload();
        Raylib.UnloadRenderTexture(virtualScreen);
        AudioManager.Close();
    }
}
