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
    private readonly LocationSign locationSign = new();
    private readonly Toast toast = new();

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
    private readonly string playerName = "Lucas";
    private readonly StoryProgress story = new();

    /// <summary>
    /// The region a new game starts in; null for the first region in the chain (Kanto). Set from the command line
    /// (--region Sinnoh) to test a later region without playing through the ones before it.
    /// </summary>
    public string? NewGameRegion { get; set; }

    // Screen Transitions
    private float transitionTimer = 0f;
    private float transitionDuration = 0.4f;
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
        CharacterModels.Preload(MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).NPCs).Select(n => n.NpcType).Append("PLAYER"));
        // The Pokémon models too, meshed side by side; the menu sprites below wait for each one
        var modelled = PokemonDatabase.GetAll().Select(s => s.Name).Where(PokemonModels.HasModel).ToList();
        PokemonModels.Preload(modelled.Append(PokemonSprites.Fallback));

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
    public void StartNewGame()
    {
        playerParty.Clear();
        playerInventory.Clear();
        playerPokedex.Clear();
        pcBoxStorage.Clear();
        MapDatabase.RestoreDefeatedTrainers(Array.Empty<string>());
        story.Clear();
        playerMoney = 3000;
        badgesMask = 0;
        playTime = 0f;

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
        PlayAreaMusic(currentMap);
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
        currentMap = MapDatabase.Get(save.CurrentMapName);
        player = new Player(save.PlayerGridX, save.PlayerGridY);
        player.Facing = save.PlayerFacing;

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
    }

    private void SaveCurrentGame()
    {
        var save = new SaveData
        {
            PlayerName = playerName,
            CurrentMapName = currentMap.Name,
            PlayerGridX = player.GridX,
            PlayerGridY = player.GridY,
            PlayerFacing = player.Facing,
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
        SaveManager.SaveGame(save);
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
                        StartNewGame();
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
                bagScreen.Update(playerInventory, playerParty, ShowNotification);
                if (!bagScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.PokedexMenu:
                pokedexScreen.Update();
                if (!pokedexScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.TrainerCard:
                trainerCardScreen.Update();
                if (!trainerCardScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.StarterSelect:
                var chosen = starterSelectScreen.Update();
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
                shopScreen.Update(playerInventory, ref playerMoney, ShowNotification);
                if (!shopScreen.IsActive) currentState = GameState.Overworld;
                break;
            case GameState.PCStorage:
                pcScreen.Update(playerParty, pcBoxStorage, ShowNotification);
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
        player.Update(dt, currentMap, StartWildBattle, HandleWarp, CheckTrainerSight);
        if (trainerApproach != null) return;

        // Interaction (Z / Space)
        if (!player.IsMoving && InputManager.IsActionPressed(GameAction.Confirm))
        {
            TryInteract();
        }
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
                shopScreen.Open();
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
        }
    }

    private void HandleWarp(Warp warp)
    {
        // The music fades with the screen, so a building with its own theme starts as the door opens on it
        PlayAreaMusic(MapDatabase.Get(warp.TargetMap));
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(warp.TargetMap);
            player.SetPosition(warp.TargetX, warp.TargetY, warp.TargetFacing);
            AnnounceLocation();
        });
    }

    /// <summary>Crosses to another region, landing where arrivals from the previous region come in.</summary>
    private void TravelTo(Region region)
    {
        var spot = region.ArrivalSpot!;
        PlayAreaMusic(MapDatabase.Get(spot.Map));
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(spot.Map);
            player.SetPosition(spot.X, spot.Y, spot.Facing);
            trainersLookOnArrival = true;
            AnnounceLocation();
        });
    }

    /// <summary>
    /// Plays a map's theme (its night arrangement at night) with its region's versions of the shared themes.
    /// A map that names no theme keeps whatever is playing; the same theme carries on without a restart.
    /// </summary>
    private static void PlayAreaMusic(Map map)
    {
        AudioManager.Region = RegionDatabase.RegionOfMap(map.Name)?.Id;
        if (!string.IsNullOrEmpty(map.BgmTrack)) AudioManager.PlayMusic(map.BgmTrack);
    }

    /// <summary>
    /// Shows the place's name on arriving outdoors, as the games do on entering a town or route or stepping out
    /// of a building. Rooms have no sign.
    /// </summary>
    private void AnnounceLocation()
    {
        if (currentMap.IsIndoors) locationSign.Hide();
        else locationSign.Show(currentMap.DisplayName);
    }

    private void HandleStartMenuChoice(StartMenuChoice choice)
    {
        switch (choice)
        {
            case StartMenuChoice.None:
                return;
            case StartMenuChoice.Pokedex:
                currentState = GameState.PokedexMenu;
                pokedexScreen.Open();
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
                trainerCardScreen.Open();
                break;
            case StartMenuChoice.Options:
                optionsReturnState = GameState.Overworld;
                currentState = GameState.Options;
                optionsScreen.Open();
                break;
            case StartMenuChoice.Save:
                SaveCurrentGame();
                startMenu.Close();
                return;
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
            battleRenderer.SetArena(currentMap);
        });
    }

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
            battleRenderer.SetArena(currentMap);
            battleTrainer = trainerNpc;
        });
    }

    private void EndBattle()
    {
        bool isDefeat = battle?.Result == BattleResult.PlayerDefeat;
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
                    ? $"Lucas whited out and paid ¥{penalty}... Restored at home!" 
                    : "Lucas whited out... Restored at home!");
            }
            PlayAreaMusic(currentMap);
        });
    }

    private void StartTransition(GameState nextState, Action? onMidpoint = null)
    {
        stateBeforeTransition = currentState;
        stateAfterTransition = nextState;
        midTransitionCallback = onMidpoint;
        currentState = GameState.Transition;
        transitionTimer = 0f;
        isFadingOut = true;
    }

    private void UpdateTransition(float dt)
    {
        transitionTimer += dt;
        if (isFadingOut)
        {
            if (transitionTimer >= transitionDuration)
            {
                isFadingOut = false;
                transitionTimer = 0f;
                midTransitionCallback?.Invoke();
                midTransitionCallback = null;
            }
        }
        else
        {
            if (transitionTimer >= transitionDuration)
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
        renderContext.OutputIsNative = Raylib.GetScreenWidth() > VirtualWidth * RenderScale * 0.75f;
        // During a fade, show the screen being left while fading out and the new one while fading in
        GameState scene = currentState == GameState.Transition
            ? (isFadingOut ? stateBeforeTransition : stateAfterTransition)
            : currentState;
        bool showWorld = scene is GameState.Overworld or GameState.Dialogue;
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
                bagScreen.Draw(VirtualWidth, VirtualHeight, playerInventory);
                break;
            case GameState.PokedexMenu:
                pokedexScreen.Draw(VirtualWidth, VirtualHeight, playerPokedex);
                break;
            case GameState.TrainerCard:
                var currSave = new SaveData
                {
                    PlayerName = playerName,
                    Money = playerMoney,
                    Badges = badgesMask,
                    PlayTimeSeconds = playTime,
                    CaughtSpecies = playerPokedex.CaughtSpecies.ToList()
                };
                trainerCardScreen.Draw(VirtualWidth, VirtualHeight, currSave);
                break;
            case GameState.StarterSelect:
                starterSelectScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Shop:
                shopScreen.Draw(VirtualWidth, VirtualHeight, playerMoney);
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
                break;
        }

        toast.Draw(VirtualWidth);

        // Draw Fade overlay
        if (currentState == GameState.Transition)
        {
            float alpha = isFadingOut
                ? Math.Clamp(transitionTimer / transitionDuration, 0f, 1f)
                : Math.Clamp(1f - (transitionTimer / transitionDuration), 0f, 1f);

            Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)(alpha * 255)));
        }

        Raylib.EndMode2D();
        Raylib.EndTextureMode();

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

        Raylib.EndDrawing();
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
