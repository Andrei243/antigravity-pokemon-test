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
    Transition
}

public class GameEngine
{
    public const int VirtualWidth = 1920;  // Native 1080p Full HD Viewport
    public const int VirtualHeight = 1080;

    private RenderTexture2D virtualScreen;
    private GameState currentState = GameState.Overworld;
    private GameState stateAfterTransition = GameState.Overworld;

    private Map currentMap = null!;
    private Player player = null!;
    private readonly DialogueManager dialogue = new();
    private BattleEngine? battle;

    // UI Sub-screens
    private readonly StartMenu startMenu = new();
    private readonly PartyScreen partyScreen = new();
    private readonly BagScreen bagScreen = new();
    private readonly PokedexScreen pokedexScreen = new();
    private readonly TrainerCardScreen trainerCardScreen = new();
    private readonly StarterSelectScreen starterSelectScreen = new();
    private readonly ShopScreen shopScreen = new();
    private readonly PCScreen pcScreen = new();

    // Player Save State
    private readonly Party playerParty = new();
    private readonly Inventory playerInventory = new();
    private readonly Pokedex playerPokedex = new();
    private readonly List<Pokemon> pcBoxStorage = new();
    private int playerMoney = 3000;
    private int badgesMask = 0;
    private float playTime = 0f;
    private readonly string playerName = "Lucas";

    // Camera & Screen Transitions
    private int cameraX = 0;
    private int cameraY = 0;
    private float transitionTimer = 0f;
    private float transitionDuration = 0.4f;
    private bool isFadingOut = true;
    private Action? midTransitionCallback;

    // Toast Notifications
    private string notificationMessage = "";
    private float notificationTimer = 0f;

    public void Initialize()
    {
        virtualScreen = Raylib.LoadRenderTexture(VirtualWidth, VirtualHeight);
        Raylib.SetTextureFilter(virtualScreen.Texture, TextureFilter.Bilinear);

        AudioManager.Initialize();
        MoveDatabase.Initialize();
        PokemonDatabase.Initialize();
        ItemDatabase.Initialize();
        MapDatabase.Initialize();

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
            AudioManager.ToggleMute();
            ShowNotification("Sound toggled");
        }

        switch (currentState)
        {
            case GameState.Overworld:
                UpdateOverworld(dt);
                break;
            case GameState.Dialogue:
                dialogue.Update(dt);
                if (!dialogue.IsActive)
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
            case GameState.Transition:
                UpdateTransition(dt);
                break;
        }
    }

    private void UpdateOverworld(float dt)
    {
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
                        AudioManager.ToggleMute();
                        ShowNotification(AudioManager.IsMuted ? "Sound muted (press M to unmute)" : "Sound unmuted");
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
        player.Update(dt, currentMap, StartWildBattle, HandleWarp);

        // Interaction (Z / Space)
        if (!player.IsMoving && InputManager.IsActionPressed(GameAction.Confirm))
        {
            TryInteract();
        }

        // Update Camera
        cameraX = (int)(player.PixelX + Player.TileSize / 2 - VirtualWidth / 2);
        cameraY = (int)(player.PixelY + Player.TileSize / 2 - VirtualHeight / 2);

        cameraX = Math.Clamp(cameraX, 0, Math.Max(0, currentMap.Width * Player.TileSize - VirtualWidth));
        cameraY = Math.Clamp(cameraY, 0, Math.Max(0, currentMap.Height * Player.TileSize - VirtualHeight));
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

        // Check NPC interaction
        var npc = currentMap.GetNpcAt(targetX, targetY);
        if (npc != null)
        {
            // Face player
            npc.Facing = (Direction)(((int)player.Facing + 2) % 4);

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
                dialogue.ShowDialogue(npc.Name, npc.TrainerData!.DialogueBefore, () =>
                {
                    StartTrainerBattle(npc);
                });
                currentState = GameState.Dialogue;
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
            trainerNpc.HasBattled = true;
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
            }
        }
    }

    private void DrawOverworldScene()
    {
        // 2.0x Zoom 2D Camera for crisp Sinnoh overworld in 1080p Full HD
        Camera2D overworldCam = new()
        {
            Target = new Vector2(player.PixelX + Player.TileSize / 2f, player.PixelY + Player.TileSize / 2f),
            Offset = new Vector2(VirtualWidth / 2f, VirtualHeight / 2f),
            Rotation = 0f,
            Zoom = 2.0f
        };

        // Clamp camera target to map boundaries
        float halfVisibleW = (VirtualWidth / (2f * overworldCam.Zoom));
        float halfVisibleH = (VirtualHeight / (2f * overworldCam.Zoom));
        float mapPixelW = currentMap.Width * Player.TileSize;
        float mapPixelH = currentMap.Height * Player.TileSize;

        if (mapPixelW <= halfVisibleW * 2f)
        {
            overworldCam.Target.X = mapPixelW / 2f;
        }
        else
        {
            overworldCam.Target.X = Math.Clamp(overworldCam.Target.X, halfVisibleW, mapPixelW - halfVisibleW);
        }

        if (mapPixelH <= halfVisibleH * 2f)
        {
            overworldCam.Target.Y = mapPixelH / 2f;
        }
        else
        {
            overworldCam.Target.Y = Math.Clamp(overworldCam.Target.Y, halfVisibleH, mapPixelH - halfVisibleH);
        }

        Raylib.BeginMode2D(overworldCam);
        currentMap.DrawGroundAndEntities(0, 0, player);
        currentMap.DrawOverhead(0, 0);
        Raylib.EndMode2D();
    }

    public void ShowNotification(string message)
    {
        notificationMessage = message;
        notificationTimer = 3.0f;
    }

    public void Draw()
    {
        // Render scene to native 1920x1080 Full HD buffer
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.ClearBackground(Color.Black);

        switch (currentState)
        {
            case GameState.Overworld:
            case GameState.Dialogue:
                DrawOverworldScene();
                dialogue.Draw(VirtualWidth, VirtualHeight);
                startMenu.Draw(VirtualWidth);
                break;
            case GameState.Battle:
                battle?.Draw(VirtualWidth, VirtualHeight);
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
            case GameState.Transition:
                DrawOverworldScene();
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

    public void Close()
    {
        Raylib.UnloadRenderTexture(virtualScreen);
        AudioManager.Close();
    }
}
