using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

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
    Transition
}

public class GameEngine
{
    public const int VirtualWidth = 1920;  // Native 1080p Full HD Viewport
    public const int VirtualHeight = 1080;

    private RenderTexture2D virtualScreen;
    private GameState currentState = GameState.Overworld;
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

    // Screen Transitions
    private float transitionTimer = 0f;
    private float transitionDuration = 0.4f;
    private bool isFadingOut = true;
    private Action? midTransitionCallback;

    // Toast Notifications
    private string notificationMessage = "";
    private float notificationTimer = 0f;

    public GameEngine(GameSettings? settings = null)
    {
        Settings = settings ?? GameSettings.Load();
        world = new WorldRenderer(renderContext);
        battleRenderer = new BattleRenderer(renderContext);
    }

    public void Initialize()
    {
        virtualScreen = Raylib.LoadRenderTexture(VirtualWidth, VirtualHeight);
        Raylib.SetTextureFilter(virtualScreen.Texture, TextureFilter.Bilinear);

        AudioManager.Initialize();
        MoveDatabase.Initialize();
        PokemonDatabase.Initialize();
        ItemDatabase.Initialize();
        MapDatabase.Initialize();

        ApplySettings(window: false);

        // Menu sprites are rendered from the 3D Pokémon models once, up front
        renderContext.EnsureLoaded();
        PokemonSprites.BakeAll(renderContext, PokemonDatabase.GetAll().Select(s => s.Name));

        // Check savegame or init fresh
        var save = SaveManager.LoadGame();
        if (save != null)
        {
            ApplySaveData(save);
        }
        else
        {
            InitializeNewGame();
        }

        AudioManager.PlayBGM(currentMap.BgmTrack);
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
        currentMap = MapDatabase.Get("TwinleafTown");
        player = new Player(11, 8);

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
            Money = playerMoney,
            Badges = badgesMask,
            PlayTimeSeconds = playTime
        };
        SaveManager.SaveGame(save);
        ShowNotification("Game saved successfully!");
        AudioManager.PlaySound("select");
    }

    public void Update(float dt)
    {
        playTime += dt;
        AudioManager.Update(dt);

        if (notificationTimer > 0f)
        {
            notificationTimer -= dt;
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
                partyScreen.Update(playerParty);
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
                if (!optionsScreen.IsActive) currentState = GameState.Overworld;
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
            string? choice = startMenu.Update();
            if (choice != null)
            {
                switch (choice)
                {
                    case "POKÉDEX":
                        currentState = GameState.PokedexMenu;
                        pokedexScreen.Open();
                        break;
                    case "POKÉMON":
                        currentState = GameState.PartyMenu;
                        partyScreen.Open();
                        break;
                    case "BAG":
                        currentState = GameState.BagMenu;
                        bagScreen.Open();
                        break;
                    case "TRAINER":
                        currentState = GameState.TrainerCard;
                        trainerCardScreen.Open();
                        break;
                    case "SAVE":
                        SaveCurrentGame();
                        break;
                    case "OPTIONS":
                        currentState = GameState.Options;
                        optionsScreen.Open();
                        break;
                }
                startMenu.Close();
            }
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
                    AudioManager.PlaySound("heal");
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
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = MapDatabase.Get(warp.TargetMap);
            player.SetPosition(warp.TargetX, warp.TargetY, warp.TargetFacing);
            AudioManager.PlayBGM(currentMap.BgmTrack);
        });
    }

    private void StartWildBattle(WildEncounterEntry entry)
    {
        var wildSpecies = PokemonDatabase.Get(entry.SpeciesName)!;
        Random rng = new();
        int lvl = rng.Next(entry.MinLevel, entry.MaxLevel + 1);
        var wildPkmn = new Pokemon(wildSpecies, lvl);

        playerPokedex.RegisterSeen(wildSpecies.DexNumber);

        StartTransition(GameState.Battle, () =>
        {
            battle = new BattleEngine(playerParty, wildPkmn, playerInventory, playerPokedex, null, pcBoxStorage);
            battleRenderer.Trees = currentMap.Trees;
            AudioManager.PlayBGM("Battle");
        });
    }

    private void StartTrainerBattle(NPC trainerNpc)
    {
        var trainer = trainerNpc.TrainerData!;
        if (trainer.Party.Count == 0)
        {
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        }

        StartTransition(GameState.Battle, () =>
        {
            battle = new BattleEngine(playerParty, trainer.Party.Members.First(), playerInventory, playerPokedex, trainer, pcBoxStorage);
            battleRenderer.Trees = currentMap.Trees;
            battleTrainer = trainerNpc;
            AudioManager.PlayBGM("Battle");
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
            AudioManager.PlayBGM(currentMap.BgmTrack);
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
        notificationMessage = message;
        notificationTimer = 3.0f;
    }

    public void Draw()
    {
        // During a fade, show the screen being left while fading out and the new one while fading in
        GameState scene = currentState == GameState.Transition
            ? (isFadingOut ? stateBeforeTransition : stateAfterTransition)
            : currentState;
        bool showWorld = scene is GameState.Overworld or GameState.Dialogue;
        bool showBattle = scene == GameState.Battle && battle != null;

        // The 3D scenes render into their own targets first (texture modes can't nest)
        if (showWorld)
        {
            world.Render(currentMap, player);
        }
        else if (showBattle)
        {
            battleRenderer.Render(battle!);
        }

        // Render scene to native 1920x1080 Full HD buffer
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.ClearBackground(Color.Black);

        switch (currentState)
        {
            case GameState.Overworld:
            case GameState.Dialogue:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                dialogue.Draw(VirtualWidth, VirtualHeight);
                startMenu.Draw(VirtualWidth);
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

        // Draw Notification Toast (Full HD scaled)
        if (notificationTimer > 0f)
        {
            int toastW = 800;
            int toastH = 64;
            int tx = (VirtualWidth - toastW) / 2;
            int ty = 30;

            RenderHelper.DrawPlatinumPanel(tx, ty, toastW, toastH, Palette.UiAccentSecondary);
            RenderHelper.DrawTextWithShadow(notificationMessage, tx + 24, ty + 18, 22, Color.White);
        }

        // Draw Fade overlay
        if (currentState == GameState.Transition)
        {
            float alpha = isFadingOut
                ? Math.Clamp(transitionTimer / transitionDuration, 0f, 1f)
                : Math.Clamp(1f - (transitionTimer / transitionDuration), 0f, 1f);

            Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)(alpha * 255)));
        }

        Raylib.EndTextureMode();

        // Scale virtual buffer to target window / Full HD display
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
