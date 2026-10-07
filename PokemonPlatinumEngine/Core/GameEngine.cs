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
    private readonly DialogueManager dialogue = new();
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

        // Every character the maps use (those a flag hides for now too), sculpted and meshed in the background while the title screen plays
        CharacterModels.Preload(MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).Everyone).Where(n => !n.IsThing).Select(n => PlayerIdentity.CharacterFor(n.NpcType, PlayerLook.Boy)).Append("PLAYER").Append("DAWN").Append("ROWAN"));
        // The Pokémon models of the story's opening too, meshed side by side; the menu sprites below wait for each one
        var modelled = PokemonModels.Preloaded.ToList();
        PokemonModels.Preload(modelled.Append(PokemonSprites.Fallback));
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
        StoryMigration.Upgrade(story, save.StoryVersion, playerParty.Members.Concat(pcBoxStorage.All), scripts);
        MapDatabase.RestoreDefeatedTrainers(story.DefeatedTrainers);
        RefreshPresence(startOver: true);

        playerMoney = save.Money;
        playTime = save.PlayTimeSeconds;
        PlayerIdentity.Set(save.PlayerName, save.Look);
        PlayerIdentity.SetRival(save.RivalName);
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
        KeepTheClock();
        SlideBoulders(dt);
        poketchView.Update(dt);

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
    private bool OnStep()
    {
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
        // Another place: what lasted only while the player stayed in the last one is forgotten
        if (another) ChangePlace();
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
        var trainer = TrainerApproach.FindSpotter(currentMap, player.GridX, player.GridY);
        if (trainer == null) return false;

        wandering.Settle(trainer);
        trainerApproach = new TrainerApproach(trainer, player);
        // Two trainers who see the player at once come together (APPROACH_TYPE_VS2): two against one when the
        // player has two Pokémon able to fight, or against the player and whoever travels with them
        pairApproach = null;
        if (TrainerApproach.FindSpotter(currentMap, player.GridX, player.GridY, except: trainer) is { } second
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
        var npc = currentMap.NpcIn(targetX, targetY, currentMap.SurfaceAt(targetX, targetY, standing).Height);
        if (npc == null && currentMap.IsCounter(targetX, targetY))
        {
            npc = currentMap.GetNpcAt(targetX + dx, targetY + dy);
        }
        if (npc != null)
        {
            // Face player (a trainer goes back to looking the old way if they win); a thing stays as it is
            if (npc.IsTrainer) npc.LeavePost();
            if (!npc.IsThing) npc.FaceTowards(player.GridX, player.GridY);

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

    private void StartWildBattle(WildEncounterEntry entry)
    {
        var wildSpecies = PokemonDatabase.Get(entry.SpeciesName)!;
        Random rng = fieldRandom;
        int lvl = rng.Next(entry.MinLevel, entry.MaxLevel + 1);
        // The gender and the nature are the lead's ability's choice when it made one (Cute Charm, Synchronize)
        var wild = new Pokemon(wildSpecies, lvl, gender: entry.Gender, nature: entry.Nature);
        // Shellos and Gastrodon east of Mt. Coronet, Unown in their room's letters (AddWildMonToParty; plan 06 · R10)
        var area = currentMap.AreaAt(player.GridX, player.GridY);
        if (FormRules.WildForm(wildSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } form) wild.ChangeForm(form);
        // Beside a partner every Pokémon of the grass comes with a second (TryGenerateGrassEncounter_DoubleBattle): both
        // are drawn as the first was, and the lead's ability scaring either off leaves the grass quiet
        Pokemon? second = null;
        if (partner != null && player.Mode != TravelMode.Surfing && !safari.Active)
        {
            if (currentMap.MeetAnother(player.GridX, player.GridY, player.Lead) is not { } other) return;
            var otherSpecies = PokemonDatabase.Get(other.SpeciesName)!;
            second = new Pokemon(otherSpecies, other.MinLevel, gender: other.Gender, nature: other.Nature);
            if (FormRules.WildForm(otherSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } otherForm) second.ChangeForm(otherForm);
        }
        // In the Great Marsh's game every Pokémon is met in a Safari battle (plan 01 · M7)
        MeetWildPokemon(wild, safari.Active ? BattleKind.Safari : BattleKind.Normal, second: second);
    }

    /// <summary>
    /// A battle with one wild Pokémon: one met in the grass, or one a script puts in the player's way, perhaps one
    /// that can't be run from (a legendary the story brings), or the assistant's catching lesson, which the player
    /// watches (plan 06 · R9). (Not a second <c>StartWildBattle</c>: the screenshot harness finds that one by its
    /// name alone.)
    /// </summary>
    private void MeetWildPokemon(Pokemon wildPkmn, BattleKind kind = BattleKind.Normal, bool cannotFlee = false, Pokemon? second = null)
    {
        if (second != null) playerPokedex.RegisterSeen(second.Species.DexNumber);
        // The lesson is fought with a Pokédex of its own (FieldBattleDTO_NewCatchingTutorial): the player's isn't told
        if (kind != BattleKind.CatchingLesson) playerPokedex.RegisterSeen(wildPkmn.Species.DexNumber);
        // The lesson is the assistant's: their own Pokémon, their name, a bag of twenty Poké Balls (FieldBattleDTO_NewCatchingTutorial)
        var party = playerParty;
        var bag = playerInventory;
        string? name = null;
        if (kind == BattleKind.CatchingLesson)
        {
            party = new Party();
            party.Add(new Pokemon(PokemonDatabase.Get(story.AssistantStarter ?? "Piplup")!, 5));
            bag = new Inventory();
            bag.AddItem(ItemDatabase.Get("Poké Ball")!, 20);
            name = PlayerIdentity.Fill("{assistant}");
        }

        // The battle theme cuts in as the screen starts to flash, before the battle itself appears: a legendary's or
        // a mythical's own, or the wild battle theme
        eyeThemePlaying = false;
        AudioManager.PlayWildBattleMusic(wildPkmn.Species);
        // Whoever travels with the player battles beside them, with a fresh team of their own
        var beside = kind == BattleKind.Normal ? PartnerTrainer() : null;
        var wilds = second != null ? new List<Pokemon> { wildPkmn, second } : new List<Pokemon> { wildPkmn };
        var shown = PrepareModels(wilds.Concat(beside?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = party,
                Inventory = bag,
                Pokedex = playerPokedex,
                PcStorage = pcBoxStorage,
                Place = PlaceName(),
                WildPokemon = wilds,
                Format = second != null ? BattleFormat.Double : BattleFormat.Single,
                Partner = beside,
                Conditions = BattleConditionsHere(),
                Kind = kind,
                CannotFlee = cannotFlee,
                PlayerName = name,
                SpecialBalls = kind == BattleKind.Safari ? safari.Balls : 0
            });
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
        }, SceneTransition.ForBattle(trainer: false, leader: false, wildPkmn.Level, LeadLevel()));
    }

    /// <summary>
    /// What a battle's rules ask of where and when it is fought: the Dive, Dusk and Repeat Balls do, and the
    /// weather of the place comes into the battle with it. And of the player (plan 06 · R10): the badges a traded
    /// Pokémon obeys by, the money a loss takes a share of, and who the player is.
    /// </summary>
    private Battle.Sim.BattleConditions BattleConditionsHere() => new()
    {
        Terrain = TerrainAt(currentMap, player.GridX, player.GridY),
        Night = GameClock.IsNight,
        HasCaught = species => playerPokedex.IsCaught(species.DexNumber),
        Weather = Weathers.InBattle(currentMap.WeatherAt(player.GridX, player.GridY)),
        Badges = story.BadgeCount,
        Money = playerMoney,
        Player = PlayerMark
    };

    /// <summary>
    /// What the clock does to the team (plan 06 · R10): each new day takes a day off Pokérus
    /// (<c>Party_UpdatePokerusStatus</c>), and from eight in the evening a Shaymin in its Sky Forme is back in its
    /// Land Forme (<c>Party_SetShayminForm</c>, which reverts it as the clock passes eight).
    /// </summary>
    private void KeepTheClock()
    {
        var today = GameClock.Today;
        if (lastDay is { } before && today > before) PokerusRules.DaysPass(playerParty, (today - before).Days);
        lastDay = today;
        if (FormRules.ShayminNight((int)GameClock.Hour))
            foreach (var p in playerParty.Members) FormRules.BackToLand(p);
    }

    // The day the clock was last looked at, for Pokérus's days (saved)
    private DateTime? lastDay;

    /// <summary>Who the player is, as a Pokémon's original trainer is marked (plan 06 · R10).</summary>
    private TrainerMark PlayerMark => new(PlayerIdentity.Name, trainerId, PlayerIdentity.Look);

    /// <summary>
    /// What follows any battle in the field (plan 06 · R10): Pokérus may come to the team and spread through it
    /// (<c>BattleControllerPlayer_EndFight</c>, on the field's own chance), and an item the battle left in a
    /// Pokémon's hands (Thief, Pickup) puts Giratina and Arceus in its form.
    /// </summary>
    private void AfterBattle(BattleEngine fought)
    {
        if (fought.Kind != BattleKind.CatchingLesson)
        {
            PokerusRules.TryInfect(playerParty, fieldRandom);
            PokerusRules.Spread(playerParty, fieldRandom);
        }
        foreach (var p in playerParty.Members) FormRules.ByHeldItem(p);
    }

    /// <summary>
    /// The ground a battle here is fought on, as the original picks it (<c>CalcTerrain</c>, <c>sTerrainForBackground</c>):
    /// the tile under the player first (ice, tall grass, sand, snow, the marsh's mud, a cave floor, water), then the
    /// area's battle background, and the map's stage where it has none. The Dive and Dusk Balls, Camouflage,
    /// Nature Power and Secret Power go by it.
    /// </summary>
    internal static Battle.Sim.BattleTerrain TerrainAt(Map map, int x, int y)
    {
        var b = map.BehaviourAt(x, y);
        switch (b)
        {
            case TileBehavior.Ice: return Battle.Sim.BattleTerrain.Ice;
            case TileBehavior.TallGrass or TileBehavior.VeryTallGrass: return Battle.Sim.BattleTerrain.Grass;
            case TileBehavior.Sand: return Battle.Sim.BattleTerrain.Sand;
            case TileBehavior.ShallowSnow or TileBehavior.ShadedSnow or TileBehavior.DeepSnow or TileBehavior.DeeperSnow or TileBehavior.DeepestSnow:
                return Battle.Sim.BattleTerrain.Snow;
            case TileBehavior.Mud or TileBehavior.DeepMud or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass:
                return Battle.Sim.BattleTerrain.GreatMarsh;
            case TileBehavior.CaveFloor: return Battle.Sim.BattleTerrain.Cave;
        }
        if (TileBehaviors.IsSurfable(b)) return Battle.Sim.BattleTerrain.Water;

        switch (map.AreaAt(x, y)?.BattleBackground)
        {
            case "Plain": return Battle.Sim.BattleTerrain.Plain;
            case "Water": return Battle.Sim.BattleTerrain.Water;
            case "City": return Battle.Sim.BattleTerrain.Building;
            case "Forest": return Battle.Sim.BattleTerrain.Grass;
            case "Mountain": return Battle.Sim.BattleTerrain.Mountain;
            case "Snow": return Battle.Sim.BattleTerrain.Snow;
            case "Indoors1" or "Indoors2" or "Indoors3": return Battle.Sim.BattleTerrain.Building;
            case "Cave1" or "Cave2" or "Cave3": return Battle.Sim.BattleTerrain.Cave;
            case null or "": break;
            // The League's rooms, the Distortion World and the Battle Frontier
            default: return Battle.Sim.BattleTerrain.Special;
        }

        return map.ArenaAt(x, y) switch
        {
            BattleArena.Forest => Battle.Sim.BattleTerrain.Grass,
            BattleArena.Cave => Battle.Sim.BattleTerrain.Cave,
            BattleArena.Water => Battle.Sim.BattleTerrain.Water,
            BattleArena.Snow => Battle.Sim.BattleTerrain.Snow,
            BattleArena.Sand => Battle.Sim.BattleTerrain.Sand,
            BattleArena.Indoors or BattleArena.Gym => Battle.Sim.BattleTerrain.Building,
            BattleArena.League => Battle.Sim.BattleTerrain.Special,
            _ => Battle.Sim.BattleTerrain.Plain
        };
    }

    /// <summary>
    /// Someone of the map starts travelling with the player (plan 02 · S6): they walk behind and battle beside the
    /// player as a trainer of Platinum's data; null for both and they stop, where they stand.
    /// </summary>
    private void SetPartner(NPC? who, string? trainerId)
    {
        if (partner != null)
        {
            partner.Settle();
            foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) if (map.Follower == partner.Who) map.Follower = null;
        }
        partner = who != null && trainerId != null ? new Follower(who, trainerId) : null;
        if (partner == null) return;
        // Off the Bicycle: nobody rides with someone walking beside them (SetPlayerBike FALSE as Cheryl joins)
        if (player.Mode == TravelMode.Cycling && player.SetCycling(false)) PlayFieldMusic();
        wandering.Settle(partner.Who);
        currentMap.Follower = partner.Who;
        partnerHeading = player.Heading;
    }

    /// <summary>The player has come to a map: whoever travels with them is behind them if they belong to it.</summary>
    private void KeepPartnerAlong()
    {
        if (partner == null) return;
        if (!currentMap.Everyone.Contains(partner.Who)) return;
        currentMap.Follower = partner.Who;
        partner.Behind(player.GridX, player.GridY, player.Facing);
        partnerHeading = player.Heading;
    }

    /// <summary>A fresh team of whoever travels with the player, as the original builds it for every battle; null when nobody does.</summary>
    private Trainer? PartnerTrainer()
    {
        if (partner == null || TrainerDatabase.Get(partner.TrainerId) is not { } record) return null;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    /// <summary>The level of the first Pokémon the player would send out: what Platinum measures a foe against to pick the way into the battle.</summary>
    private int LeadLevel() => playerParty.Members.FirstOrDefault(p => !p.IsFainted)?.Level ?? 0;

    /// <summary>A battle with a trainer of the map; with a <paramref name="partner"/>, a tag battle beside them (plan 06 · R9).</summary>
    private void StartTrainerBattle(NPC trainerNpc, NPC? secondNpc = null, Trainer? partner = null, bool firstBattle = false)
    {
        var trainer = trainerNpc.TrainerData!;
        if (trainer.Party.Count == 0)
        {
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        }
        // Two trainers at once (a tag battle's foes), each with a team of their own
        var trainers = new List<Trainer> { trainer };
        if (secondNpc?.TrainerData is { } second) trainers.Add(second);

        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(trainers.Select(t => t.TrainerClass)), immediate: true);
        var shown = PrepareModels(trainers.SelectMany(t => t.Party.Members).Concat(partner?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = playerParty,
                Inventory = playerInventory,
                Pokedex = playerPokedex,
                PcStorage = pcBoxStorage,
                Place = PlaceName(),
                Format = trainer.DoubleBattle || trainers.Count > 1 || partner != null ? BattleFormat.Double : BattleFormat.Single,
                Trainers = trainers,
                Partner = partner,
                FirstBattle = firstBattle,
                Conditions = BattleConditionsHere()
            });
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
            battleTrainers.Clear();
            battleTrainers.Add(trainerNpc);
            if (secondNpc?.TrainerData != null) battleTrainers.Add(secondNpc);
        }, SceneTransition.ForBattle(trainer: true, trainer.TrainerClass.Contains("Leader", StringComparison.OrdinalIgnoreCase),
            trainer.Party.Members[0].Level, LeadLevel()));
    }

    /// <summary>
    /// Starts building the models a battle will show (the foes and the whole team, who may be sent out), so they are
    /// ready by the time the screen has faded out; returns their species.
    /// </summary>
    private List<string> PrepareModels(IEnumerable<Pokemon> foes)
    {
        var all = foes.Concat(playerParty.Members).ToList();
        var species = all.Select(p => p.ModelName).Concat(all.SelectMany(BattleShapesOf)).Distinct().ToList();
        foreach (var name in species) PokemonModels.Request(name);
        return species;
    }

    /// <summary>The shapes a Pokémon may take in the battle itself (plan 06 · R7): Castform's weather, Cherrim's sun, Arceus's plate. Their models are asked for with its own.</summary>
    private static IEnumerable<string> BattleShapesOf(Pokemon p)
    {
        switch (p.Species.Name)
        {
            case "Castform" when p.AbilityName == "Forecast":
                return new[] { "Castform-Sunny", "Castform-Rainy", "Castform-Snowy" };
            case "Cherrim":
                return new[] { "Cherrim-Sunshine" };
            case "Arceus" when p.AbilityName == "Multitype" && p.HeldItem?.HoldEffect is { } hold && hold.StartsWith("Arceus") && p.Species.Form("Arceus-" + hold["Arceus".Length..]) != null:
                return new[] { "Arceus-" + hold["Arceus".Length..] };
            default:
                return Array.Empty<string>();
        }
    }

    /// <summary>Waits for models still being built, while the screen is black between scenes.</summary>
    private static void AwaitModels(IEnumerable<string> species)
    {
        foreach (var name in species) PokemonModels.Get(name);
    }

    private void EndBattle()
    {
        bool isDefeat = battle?.Result == BattleResult.PlayerDefeat;
        ScoreBattle();
        // Travelling with someone, the team is healed after every battle that isn't lost (encounter.c,
        // Party_HealAllMembers when the partner flag is set)
        if (!isDefeat && partner != null && battle != null) playerParty.HealAll();
        // A battle a script says may be lost is lost without waking up at home: the story goes on from the loss
        bool goesOn = isDefeat && runner.IsRunning && battleMayBeLost;
        battleMayBeLost = false;

        // Pokémon that gained a level evolve now that the battle is over, before the field comes back, and so do
        // those waiting for the battle's end (Sirfetch'd's critical hits)
        if (!isDefeat && battle != null)
        {
            var evolutions = FindEvolutions(battle.LeveledUp, EvolutionTrigger.LevelUp, cancellable: true);
            evolutions.AddRange(FindEvolutions(playerParty.Members.Where(p => evolutions.All(r => r.Pokemon != p)), EvolutionTrigger.BattleEnd, cancellable: true));
            if (PlayEvolutions(evolutions, GameState.Overworld)) return;
        }

        StartTransition(GameState.Overworld, () =>
        {
            // The foes' models are no longer needed; the team's stay for the next battle
            PokemonModels.Trim(playerParty.Members.Select(p => p.ModelName));
            if (goesOn)
            {
                playerParty.HealAll();
            }
            else if (isDefeat)
            {
                WhiteOut();
            }
            PlayFieldMusic();
            // The Safari Game keeps the balls the battle didn't throw, and ends with the last of them
            if (safari.Active && battle?.Kind == BattleKind.Safari)
            {
                safari.Balls = battle.SpecialBalls;
                if (safari.OutOfBalls) StartScript(FieldScripts.SafariOutOfBalls);
            }
        });
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
        else if (battle.Result == BattleResult.EnemyCaught && battle.Caught is { } caught) journal.Caught(caught.Species.Name, place);
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
                // A script's black screen covers the field and leaves the text over it
                DrawScriptFade();
                DrawCutIn();
                // The Pokétch shows over the field while nothing else is on the screen
                if (currentState == GameState.Overworld && !startMenu.IsActive && !ScriptRunning) poketchView.Draw(VirtualWidth, VirtualHeight, poketch, playerParty);
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
