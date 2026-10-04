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
    Transition
}

public class GameEngine
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
    private NPC? battleTrainer;

    // Set on arriving somewhere without walking (through a door, from a save, after a battle): trainers look once
    private bool trainersLookOnArrival = true;

    // UI Sub-screens
    private readonly StartMenu startMenu = new();
    private readonly PartyScreen partyScreen = new();
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
    private readonly Random fieldRandom = new();

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
    private readonly List<Pokemon> pcBoxStorage = new();
    private int playerMoney = 3000;
    private int badgesMask = 0;
    private float playTime = 0f;
    private int trainerId;
    private DateTime? adventureStarted;
    private readonly StoryProgress story = new();

    private static string playerName => PlayerIdentity.Name;

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

        // Every character the maps use, sculpted and meshed in the background while the title screen plays
        CharacterModels.Preload(MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).NPCs).Select(n => PlayerIdentity.CharacterFor(n.NpcType, PlayerLook.Boy)).Append("PLAYER").Append("DAWN").Append("ROWAN"));
        // The Pokémon models too, meshed side by side; the menu sprites below wait for each one
        var modelled = PokemonDatabase.GetAll().Select(s => s.Name).Where(PokemonModels.HasModel).ToList();
        PokemonModels.Preload(modelled.Append(PokemonSprites.Fallback));
        // And the balls the battles throw
        BattleBall.Preload("Poké Ball", "Great Ball", "Ultra Ball", "Master Ball");

        ApplySettings(window: false);

        // Menu sprites are rendered from the 3D Pokémon models once, up front. Species without a model of their own
        // all share the generic stand-in, baked once as PokemonSprites.Fallback.
        renderContext.EnsureLoaded();
        PokemonSprites.BakeAll(renderContext, modelled);

        // The game opens on the title screen, which offers the saved game if there is one
        titleSave = SaveManager.LoadGame();
        titleScreen = new TitleScreen(titleSave);
        AudioManager.PlayMusic(MusicRole.Title);
    }

    /// <summary>Leaves the title screen into a fresh game.</summary>
    /// <summary>Starts a new game at once as the boy with his own name: for tests and the screenshot harness.</summary>
    public void StartNewGame() => StartNewGame(null, PlayerLook.Boy);

    /// <summary>Starts a new game as the character and under the name chosen in the introduction.</summary>
    public void StartNewGame(string? name, PlayerLook look)
    {
        playerParty.Clear();
        playerInventory.Clear();
        playerPokedex.Clear();
        pcBoxStorage.Clear();
        MapDatabase.RestoreDefeatedTrainers(Array.Empty<string>());
        story.Clear();
        pendingEvolutions.Clear();
        friendshipSteps = 0;
        playerMoney = 3000;
        badgesMask = 0;
        playTime = 0f;
        // As in the games, the Trainer Card's number is drawn when the adventure begins
        PlayerIdentity.Set(name, look);
        trainerId = fieldRandom.Next(0, 65536);
        adventureStarted = DateTime.Now;

        InitializeNewGame();
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
        EnterGame();
    }

    /// <summary>Fades in on the field from the black the title screen left behind.</summary>
    private void EnterGame()
    {
        gameStarted = true;
        trainersLookOnArrival = true;
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

        // Give starter items
        playerInventory.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        playerInventory.AddItem(ItemDatabase.Get("Potion")!, 5);
        playerInventory.AddItem(ItemDatabase.Get("Revive")!, 2);

        // Initial party
        var starterTurtwig = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
        playerParty.Add(starterTurtwig);
        playerPokedex.RegisterSeen(starterTurtwig.Species.DexNumber);
        playerPokedex.RegisterCaught(starterTurtwig.Species.DexNumber);
    }

    private void ApplySaveData(SaveData save)
    {
        var place = save.Place();
        currentMap = MapDatabase.Get(place.Map);
        player = new Player(place.X, place.Y);
        player.Facing = place.Facing;
        // A save made out on the water wakes up there, still on its Pokémon's back; one made on a bridge, on its deck
        player.SetMode(save.Travel);
        if (save.PlayerHeight is { } standing) player.SetHeight(standing);

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

        pcBoxStorage.Clear();
        foreach (var pData in save.BoxStorage)
        {
            pcBoxStorage.Add(pData.ToPokemon());
        }

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

        MapDatabase.RestoreDefeatedTrainers(save.DefeatedTrainers);
        story.Restore(save.StoryFlags);

        playerMoney = save.Money;
        badgesMask = save.Badges;
        playTime = save.PlayTimeSeconds;
        PlayerIdentity.Set(save.PlayerName, save.Look);
        // A save from before the card had a number gets one now, and keeps it
        trainerId = save.TrainerId != 0 ? save.TrainerId : fieldRandom.Next(1, 65536);
        adventureStarted = save.Started;
    }

    /// <summary>The game as it stands, as a save.</summary>
    private SaveData BuildSave() =>
        new()
        {
            PlayerName = playerName,
            Look = PlayerIdentity.Look,
            TrainerId = trainerId,
            Started = adventureStarted,
            CurrentMapName = currentMap.Name,
            PlayerGridX = player.GridX,
            PlayerGridY = player.GridY,
            PlayerFacing = player.Facing,
            Travel = player.Mode,
            PlayerHeight = player.HeightOn(currentMap),
            WorldVersion = SaveData.ImportedWorld,
            Party = playerParty.Members.Select(SavedPokemonData.FromPokemon).ToList(),
            BoxStorage = pcBoxStorage.Select(SavedPokemonData.FromPokemon).ToList(),
            Inventory = playerInventory.AllItems.Select(i => new SavedItemData { ItemName = i.Name, Quantity = i.Quantity }).ToList(),
            SeenSpecies = playerPokedex.SeenSpecies.ToList(),
            CaughtSpecies = playerPokedex.CaughtSpecies.ToList(),
            DefeatedTrainers = MapDatabase.DefeatedTrainerIds(),
            StoryFlags = story.Flags.ToList(),
            Money = playerMoney,
            Badges = badgesMask,
            PlayTimeSeconds = playTime
        };

    /// <param name="quiet">True when a panel says the game was saved, so no notice is needed.</param>
    private void SaveCurrentGame(bool quiet = false)
    {
        SaveManager.SaveGame(BuildSave());
        if (quiet) return;
        ShowNotification("Game saved.");
        AudioManager.PlaySound("select");
    }

    public void Update(float dt)
    {
        if (gameStarted) playTime += dt;

        toast.Update(dt);
        startMenu.Animate(dt);

        // The location sign waits while a fade or another screen covers the field
        if (currentState is GameState.Overworld or GameState.Dialogue) locationSign.Update(dt);

        // The field's small life runs on while a dialogue or a fade covers it: dust settles, a door finishes opening
        if (gameStarted && currentState is GameState.Overworld or GameState.Dialogue or GameState.Transition)
        {
            world.Life.Advance(dt);
            // Rain and hail land round the player while they stand in it
            world.Life.Rainfall(currentMap, player.PixelX / Player.TileSize + 0.5f, player.PixelY / Player.TileSize + 0.5f,
                Weathers.LandsABeat(currentMap.WeatherAt(player.GridX, player.GridY)));
            player.TickBubble(dt);
            foreach (var npc in currentMap.NPCs) npc.TickBubble(dt);
        }

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
                        // The professor's welcome comes first; the game begins when it ends
                        currentState = GameState.Intro;
                        introScreen.Open(GameSettings.CharactersPerSecond(Settings.TextSpeed));
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
                    StartNewGame(introScreen.Name, introScreen.Look);
                }
                break;
            case GameState.Overworld:
                UpdateOverworld(dt);
                break;
            case GameState.Dialogue:
                dialogue.Update(dt);

                // Back to the field, unless the last line led somewhere else (a trainer's challenge fades into battle)
                if (!dialogue.IsActive && currentState == GameState.Dialogue)
                {
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Battle:
                if (battle != null)
                {
                    battle.Update(dt);
                    if (battle.IsBattleOver)
                    {
                        if (battle.Result == BattleResult.PlayerVictory && battle.IsTrainerBattle && battle.OpponentTrainer != null)
                        {
                            playerMoney += battle.OpponentTrainer.PrizeMoney;
                        }

                        // Only a beaten trainer is done; after a loss they wait for a rematch
                        battleTrainer?.FinishBattle(battle.Result == BattleResult.PlayerVictory);
                        battleTrainer = null;
                        EndBattle();
                    }
                }
                break;
            case GameState.PartyMenu:
                partyScreen.Update(playerParty, dt);
                if (!partyScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.BagMenu:
                bagScreen.Update(playerInventory, playerParty, ShowNotification, EvolutionContextNow(), dt);
                if (bagScreen.TakeEvolution() is { } fromBag) PlayEvolutions(new[] { fromBag }, GameState.BagMenu);
                else if (!bagScreen.IsActive) currentState = GameState.Overworld;
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
                    playerParty.Clear();
                    playerParty.Add(chosen);
                    playerPokedex.RegisterSeen(chosen.Species.DexNumber);
                    playerPokedex.RegisterCaught(chosen.Species.DexNumber);
                    ShowNotification($"Received {chosen.Species.Name} from Professor Rowan!");
                    currentState = GameState.Overworld;
                }
                break;
            case GameState.Shop:
                shopScreen.Update(playerInventory, ref playerMoney, ShowNotification, dt);
                if (!shopScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.PCStorage:
                pcScreen.Update(playerParty, pcBoxStorage, ShowNotification, dt);
                if (!pcScreen.IsActive) currentState = GameState.Overworld;
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

    private void UpdateOverworld(float dt)
    {
        // A trainer has spotted the player: nothing else happens until they have walked up
        if (trainerApproach != null)
        {
            trainerApproach.Update(dt, player);
            if (trainerApproach.IsDone)
            {
                var trainer = trainerApproach.Trainer;
                trainerApproach = null;
                ChallengeTrainer(trainer);
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
            startMenu.Open();
            return;
        }

        // Overworld Player Movement
        player.Moves = FieldMovement.MovesOf(playerParty);
        if (Steering is { } steer) player.Advance(dt, currentMap, steer.Want, steer.Run, StartWildBattle, HandleWarp, OnStep);
        else player.Update(dt, currentMap, StartWildBattle, HandleWarp, OnStep);

        // A door that leads somewhere opens as the player steps up to it: the door's own tile, or the open way
        // into a porch with the door in the wall behind
        var (headX, headY) = player.Heading;
        if (player.IsMoving && currentMap.GetWarpAt(headX, headY) != null && WorldRenderer.DoorPlace(currentMap, headX, headY) != null)
            world.Life.OpenDoor(currentMap, headX, headY);

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
        EnterArea();

        // What the step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out
        // onto it, dust where they came down from a hop
        if (player.JustRodeOut) world.Life.Splash(currentMap, player.GridX, player.GridY);
        else world.Life.Footstep(currentMap, player.GridX, player.GridY, player.Facing, player.IsRunning, player.Mode);
        if (player.JustLanded && player.Mode != TravelMode.Surfing) world.Life.Landing(currentMap, player.GridX, player.GridY);
        return CheckTrainerSight();
    }

    /// <summary>
    /// On the overworld a town runs into its routes with no door between them: stepping over the border brings
    /// up the new place's name and its music, as arriving through a door does.
    /// </summary>
    private void EnterArea()
    {
        var area = currentMap.AreaAt(player.GridX, player.GridY);
        if (area == currentArea) return;
        currentArea = area;
        if (area == null) return;
        PlayAreaMusic(currentMap, player.GridX, player.GridY);
        if (!string.IsNullOrEmpty(area.DisplayName)) locationSign.Show(area.DisplayName);
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
        IsRaining = Weathers.IsRain(currentMap.WeatherAt(player.GridX, player.GridY)),
        Sites = currentMap.EvolutionSitesAt(player.GridX, player.GridY)
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
        foreach (var request in requests) pendingEvolutions.Enqueue(request);
        evolutionReturnState = returnTo;
        StartTransition(GameState.Evolution, () => BeginNextEvolution());
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
                if (Evolution.Find(request.Pokemon, EvolutionTrigger.LevelUp, context) is not { } still) continue;
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

        if (evolutionReturnState == GameState.Overworld) StartTransition(GameState.Overworld, () => PlayAreaMusic(currentMap, player.GridX, player.GridY));
        else StartTransition(evolutionReturnState);
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
            if (!playerParty.Add(received)) pcBoxStorage.Add(received);
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

        trainerApproach = new TrainerApproach(trainer, player);
        AudioManager.PlaySound("exclaim");
        return true;
    }

    private void ChallengeTrainer(NPC npc)
    {
        dialogue.ShowDialogue(npc.Name, npc.TrainerData!.DialogueBefore, () =>
        {
            StartTrainerBattle(npc);
        });
        currentState = GameState.Dialogue;
    }

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
        var npc = currentMap.GetNpcAt(targetX, targetY);
        if (npc == null && currentMap.IsCounter(targetX, targetY))
        {
            npc = currentMap.GetNpcAt(targetX + dx, targetY + dy);
        }
        if (npc != null)
        {
            // Face player (a trainer goes back to looking the old way if they win)
            if (npc.IsTrainer) npc.LeavePost();
            npc.FaceTowards(player.GridX, player.GridY);

            if (npc.IsStarterBriefcase)
            {
                currentState = GameState.StarterSelect;
                starterSelectScreen.Open();
                return;
            }

            if (npc.IsHealingNurse)
            {
                dialogue.ShowDialogue(npc.Name, npc.DialogLines, () =>
                {
                    playerParty.HealAll();
                    AudioManager.PlayFanfare(MusicRole.FanfareHeal);
                    ShowNotification("All Pokémon were fully healed!");
                });
                currentState = GameState.Dialogue;
                return;
            }

            if (npc.IsPokeMartClerk)
            {
                currentState = GameState.Shop;
                shopScreen.Open(currentMap.DisplayNameAt(player.GridX, player.GridY));
                return;
            }

            if (npc.IsPCTerminal)
            {
                currentState = GameState.PCStorage;
                pcScreen.Open();
                return;
            }

            if (npc.IsTransportAttendant && RegionDatabase.RegionOfMap(currentMap.Name) is { } here
                && RegionDatabase.LinkFrom(here.Id) is { } link)
            {
                var check = RegionDatabase.CheckTravel(link, story);
                dialogue.ShowDialogue(npc.Name, RegionDatabase.AttendantLines(link, check),
                    check == TravelCheck.Ready ? () => TravelTo(RegionDatabase.Get(link.To)!) : null);
                currentState = GameState.Dialogue;
                return;
            }

            if (npc.IsTrainer && !npc.HasBattled)
            {
                ChallengeTrainer(npc);
                return;
            }

            if (npc.IsTrainer && npc.HasBattled && npc.TrainerData != null && !string.IsNullOrEmpty(npc.TrainerData.DialogueAfter))
            {
                dialogue.ShowDialogue(npc.Name, new List<string> { npc.TrainerData.DialogueAfter });
                currentState = GameState.Dialogue;
                return;
            }

            if (npc.DialogLines.Count > 0)
            {
                dialogue.ShowDialogue(npc.Name, npc.DialogLines);
                currentState = GameState.Dialogue;
                return;
            }
        }

        // Check Signboard
        var sign = currentMap.GetSignboardAt(targetX, targetY);
        if (sign != null)
        {
            dialogue.ShowDialogue("Sign", sign);
            currentState = GameState.Dialogue;
            return;
        }

        TryStartSurf();
    }

    /// <summary>
    /// At the edge of deep water with a Pokémon that knows Surf, the confirm button sends it out and the player
    /// rides off on it. (Platinum also asks for the Fen Badge; badges come with the story, plan 02.)
    /// </summary>
    private void TryStartSurf()
    {
        if (!player.Moves.HasFlag(FieldMoves.Surf) || !player.StartSurf(currentMap)) return;
        var carrier = playerParty.Members.First(p => p.Moves.Any(m => m.Name == "Surf"));
        ShowNotification($"{carrier.Nickname} used Surf!");
    }

    private void HandleWarp(Warp warp)
    {
        // The music fades with the screen, so a building with its own theme starts as the door opens on it
        PlayAreaMusic(MapDatabase.Get(warp.TargetMap), warp.TargetX, warp.TargetY);
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(warp.TargetMap);
            player.SetPosition(warp.TargetX, warp.TargetY, warp.TargetFacing);
            AnnounceLocation();

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
            AnnounceLocation();
        });
    }

    /// <summary>
    /// Plays the theme of a place on a map (its night arrangement at night) with its region's versions of the
    /// shared themes. A place that names no theme keeps whatever is playing; the same theme carries on without a
    /// restart.
    /// </summary>
    private static void PlayAreaMusic(Map map, int x, int y)
    {
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
                pokedexScreen.Open(playerPokedex);
                break;
            case StartMenuChoice.Pokemon:
                currentState = GameState.PartyMenu;
                partyScreen.Open();
                break;
            case StartMenuChoice.Bag:
                currentState = GameState.BagMenu;
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
        Random rng = new();
        int lvl = rng.Next(entry.MinLevel, entry.MaxLevel + 1);
        var wildPkmn = new Pokemon(wildSpecies, lvl);

        playerPokedex.RegisterSeen(wildSpecies.DexNumber);

        // The battle theme cuts in as the screen starts to flash, before the battle itself appears
        AudioManager.PlayMusic(MusicRole.BattleWild, immediate: true);
        StartTransition(GameState.Battle, () =>
        {
            battle = new BattleEngine(playerParty, wildPkmn, playerInventory, playerPokedex, null, pcBoxStorage);
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
        }, SceneTransition.ForBattle(trainer: false, leader: false, wildPkmn.Level, LeadLevel()));
    }

    /// <summary>The level of the first Pokémon the player would send out: what Platinum measures a foe against to pick the way into the battle.</summary>
    private int LeadLevel() => playerParty.Members.FirstOrDefault(p => !p.IsFainted)?.Level ?? 0;

    private void StartTrainerBattle(NPC trainerNpc)
    {
        var trainer = trainerNpc.TrainerData!;
        if (trainer.Party.Count == 0)
        {
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        }

        AudioManager.PlayMusic(MusicDirector.BattleRole(new[] { trainer.TrainerClass }), immediate: true);
        StartTransition(GameState.Battle, () =>
        {
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = playerParty,
                Inventory = playerInventory,
                Pokedex = playerPokedex,
                PcStorage = pcBoxStorage,
                Format = trainer.DoubleBattle ? BattleFormat.Double : BattleFormat.Single,
                Trainers = new List<Trainer> { trainer }
            });
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
            battleTrainer = trainerNpc;
        }, SceneTransition.ForBattle(trainer: true, trainer.TrainerClass.Contains("Leader", StringComparison.OrdinalIgnoreCase),
            trainer.Party.Members[0].Level, LeadLevel()));
    }

    private void EndBattle()
    {
        bool isDefeat = battle?.Result == BattleResult.PlayerDefeat;

        // Pokémon that gained a level evolve now that the battle is over, before the field comes back
        if (!isDefeat && battle != null &&
            PlayEvolutions(FindEvolutions(battle.LeveledUp, EvolutionTrigger.LevelUp, cancellable: true), GameState.Overworld))
            return;

        StartTransition(GameState.Overworld, () =>
        {
            if (isDefeat)
            {
                playerParty.HealAll();
                int penalty = Math.Min(playerMoney, 120);
                playerMoney -= penalty;
                currentMap = MapDatabase.Get("PlayerHouse");
                player.SetPosition(4, 5, Direction.Down);
                ShowNotification(penalty > 0 
                    ? $"{playerName} whited out and lost {penalty} in money. Restored at home!"
                    : $"{playerName} whited out. Restored at home!");
            }
            PlayAreaMusic(currentMap, player.GridX, player.GridY);
        });
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
        renderContext.OutputIsNative = Raylib.GetScreenWidth() > VirtualWidth * RenderScale * 0.75f;
        // During a fade, show the screen being left while fading out and the new one while fading in
        GameState scene = currentState == GameState.Transition
            ? (isFadingOut ? stateBeforeTransition : stateAfterTransition)
            : currentState;
        bool showWorld = scene is GameState.Overworld or GameState.Dialogue or GameState.SaveMenu;
        bool showBattle = scene == GameState.Battle && battle != null;
        bool showTitle = scene == GameState.Title;

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
        Raylib.BeginMode2D(new Camera2D { Zoom = RenderScale });

        switch (currentState)
        {
            case GameState.Title:
                titleScreen.Draw(VirtualWidth, VirtualHeight, renderContext, world);
                break;
            case GameState.Overworld:
            case GameState.Dialogue:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                dialogue.Draw(VirtualWidth, VirtualHeight);
                locationSign.Draw();
                startMenu.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Battle:
                DrawBattle();
                break;
            case GameState.PartyMenu:
                partyScreen.Draw(VirtualWidth, VirtualHeight, playerParty);
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
                trainerCardScreen.Draw(VirtualWidth, VirtualHeight, new TrainerCardInfo(playerName, trainerId, playerMoney,
                    playerPokedex.SeenCount, playerPokedex.CaughtCount, playTime, badgesMask, adventureStarted));
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
            case GameState.PCStorage:
                pcScreen.Draw(VirtualWidth, VirtualHeight, playerParty, pcBoxStorage);
                break;
            case GameState.Options:
                optionsScreen.Draw(VirtualWidth, VirtualHeight, Settings);
                break;
            case GameState.Transition:
                if (showBattle) DrawBattle();
                else if (showWorld) world.DrawToScreen(VirtualWidth, VirtualHeight);
                else if (scene == GameState.Evolution) evolutionScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.BagMenu) bagScreen.Draw(VirtualWidth, VirtualHeight, playerInventory, playerParty, EvolutionContextNow());
                break;
        }

        toast.Draw(VirtualWidth);

        // The scene closing, or the next one opening
        if (currentState == GameState.Transition)
            SceneTransition.Draw(transitionKind, isFadingOut, transitionTimer, VirtualWidth, VirtualHeight);

        Raylib.EndMode2D();
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
