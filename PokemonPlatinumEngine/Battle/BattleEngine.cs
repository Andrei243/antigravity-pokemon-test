using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleResult
{
    None,
    PlayerVictory,
    PlayerDefeat,
    EnemyCaught,
    PlayerRan
}

public class BattleEngine
{
    public Pokemon PlayerPokemon { get; private set; }
    public Pokemon EnemyPokemon { get; private set; }
    public Party PlayerParty { get; }
    public Party? EnemyParty { get; }
    public Trainer? OpponentTrainer { get; }
    public Inventory PlayerInventory { get; }
    public Pokedex Pokedex { get; }

    public bool IsTrainerBattle => OpponentTrainer != null;
    public BattleHUD HUD { get; } = new();
    public BattleVFX VFX { get; } = new();

    private readonly Queue<Action> turnEventQueue = new();
    private string currentMessage = "";
    private float messageWaitTimer = 0f;
    private bool waitingForMessageConfirm = false;

    // Sprite animation offsets
    private float playerSpriteOffset = 0f;
    private float enemySpriteOffset = 0f;
    private float playerDamageFlashTimer = 0f;
    private float enemyDamageFlashTimer = 0f;

    private readonly List<Pokemon>? pcBoxStorage;
    private Action? currentMessageCallback = null;

    public BattleResult Result { get; private set; } = BattleResult.None;
    public bool IsBattleOver => Result != BattleResult.None && !waitingForMessageConfirm && turnEventQueue.Count == 0;

    private readonly Random rng = new();

    public BattleEngine(
        Party playerParty,
        Pokemon wildOrEnemyPokemon,
        Inventory playerInventory,
        Pokedex pokedex,
        Trainer? trainer = null,
        List<Pokemon>? pcStorage = null)
    {
        PlayerParty = playerParty;
        PlayerPokemon = playerParty.FirstUsable ?? playerParty.Members.First();
        EnemyPokemon = wildOrEnemyPokemon;
        EnemyParty = trainer?.Party;
        OpponentTrainer = trainer;
        PlayerInventory = playerInventory;
        Pokedex = pokedex;
        pcBoxStorage = pcStorage;

        // Register enemy seen in Pokédex
        Pokedex.RegisterSeen(EnemyPokemon.Species.DexNumber);

        AudioManager.PlayBGM("Battle");

        // Initial Intro Message
        if (IsTrainerBattle)
        {
            QueueMessage($"{OpponentTrainer!.FullTitle} wants to battle!", () =>
            {
                QueueMessage($"{OpponentTrainer.FullTitle} sent out {EnemyPokemon.DisplayName}!", () =>
                {
                    QueueMessage($"Go! {PlayerPokemon.DisplayName}!", () =>
                    {
                        HUD.MenuState = BattleMenuState.Main;
                    });
                });
            });
        }
        else
        {
            QueueMessage($"A wild {EnemyPokemon.DisplayName} appeared!", () =>
            {
                QueueMessage($"Go! {PlayerPokemon.DisplayName}!", () =>
                {
                    HUD.MenuState = BattleMenuState.Main;
                });
            });
        }

        AdvanceEventQueue();
    }

    public void Update(float dt)
    {
        VFX.Update(dt);

        if (playerDamageFlashTimer > 0f) playerDamageFlashTimer -= dt;
        if (enemyDamageFlashTimer > 0f) enemyDamageFlashTimer -= dt;
        playerSpriteOffset = MathF.Sin((float)Raylib.GetTime() * 4f) * 2f;
        enemySpriteOffset = MathF.Cos((float)Raylib.GetTime() * 4f) * 2f;

        if (waitingForMessageConfirm)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                waitingForMessageConfirm = false;
                var cb = currentMessageCallback;
                currentMessageCallback = null;
                cb?.Invoke();

                if (turnEventQueue.Count > 0)
                {
                    AdvanceEventQueue();
                }
                else if (Result == BattleResult.None)
                {
                    HUD.MenuState = BattleMenuState.Main;
                }
            }
            return;
        }

        if (messageWaitTimer > 0f)
        {
            messageWaitTimer -= dt;
            if (messageWaitTimer <= 0f)
            {
                AdvanceEventQueue();
            }
            return;
        }

        if (HUD.MenuState == BattleMenuState.Message)
        {
            if (turnEventQueue.Count > 0)
            {
                AdvanceEventQueue();
            }
            else if (Result == BattleResult.None && !waitingForMessageConfirm)
            {
                HUD.MenuState = BattleMenuState.Main;
            }
            return;
        }

        // Handle Menu Navigation
        HandleMenuInput();
    }

    private void HandleMenuInput()
    {
        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
                {
                    HUD.MainMenuIndex = (HUD.MainMenuIndex + 2) % 4;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right))
                {
                    HUD.MainMenuIndex = HUD.MainMenuIndex % 2 == 0 ? HUD.MainMenuIndex + 1 : HUD.MainMenuIndex - 1;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Confirm))
                {
                    AudioManager.PlaySound("select");
                    switch (HUD.MainMenuIndex)
                    {
                        case 0: // FIGHT
                            HUD.MenuState = BattleMenuState.Moves;
                            HUD.MoveMenuIndex = 0;
                            break;
                        case 1: // BAG
                            HUD.MenuState = BattleMenuState.SelectBagItem;
                            HUD.BagMenuIndex = 0;
                            break;
                        case 2: // POKEMON
                            HUD.MenuState = BattleMenuState.SwitchPokemon;
                            HUD.SwitchMenuIndex = 0;
                            break;
                        case 3: // RUN
                            AttemptRun();
                            break;
                    }
                }
                break;

            case BattleMenuState.Moves:
                if (InputManager.IsActionPressed(GameAction.Cancel))
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
                {
                    HUD.MoveMenuIndex = (HUD.MoveMenuIndex + 2) % 4;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right))
                {
                    HUD.MoveMenuIndex = HUD.MoveMenuIndex % 2 == 0 ? HUD.MoveMenuIndex + 1 : HUD.MoveMenuIndex - 1;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Confirm))
                {
                    if (HUD.MoveMenuIndex < PlayerPokemon.Moves.Count)
                    {
                        var move = PlayerPokemon.Moves[HUD.MoveMenuIndex];
                        if (move.CurrentPP > 0)
                        {
                            AudioManager.PlaySound("select");
                            ExecuteTurn(new BattleAction { Type = ActionType.Fight, IsPlayer = true, Move = move });
                        }
                        else
                        {
                            AudioManager.PlaySound("cancel");
                            QueueMessage("There's no PP left for this move!");
                        }
                    }
                }
                break;

            case BattleMenuState.SwitchPokemon:
                if (InputManager.IsActionPressed(GameAction.Cancel))
                {
                    if (!PlayerPokemon.IsFainted)
                    {
                        AudioManager.PlaySound("cancel");
                        HUD.MenuState = BattleMenuState.Main;
                    }
                    return;
                }
                if (InputManager.IsActionPressed(GameAction.Left)) HUD.SwitchMenuIndex = Math.Max(0, HUD.SwitchMenuIndex - 1);
                if (InputManager.IsActionPressed(GameAction.Right)) HUD.SwitchMenuIndex = Math.Min(PlayerParty.Count - 1, HUD.SwitchMenuIndex + 1);
                if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
                {
                    HUD.SwitchMenuIndex = (HUD.SwitchMenuIndex + 3) % PlayerParty.Count;
                }
                if (InputManager.IsActionPressed(GameAction.Confirm))
                {
                    var chosen = PlayerParty.Members[HUD.SwitchMenuIndex];
                    if (chosen == PlayerPokemon && !PlayerPokemon.IsFainted)
                    {
                        QueueMessage($"{chosen.DisplayName} is already in battle!", () =>
                        {
                            HUD.MenuState = BattleMenuState.SwitchPokemon;
                        });
                        AdvanceEventQueue();
                    }
                    else if (chosen.IsFainted)
                    {
                        QueueMessage($"{chosen.DisplayName} has no energy left to battle!", () =>
                        {
                            HUD.MenuState = BattleMenuState.SwitchPokemon;
                        });
                        AdvanceEventQueue();
                    }
                    else
                    {
                        AudioManager.PlaySound("select");
                        if (PlayerPokemon.IsFainted)
                        {
                            PlayerPokemon = chosen;
                            QueueMessage($"Go! {PlayerPokemon.DisplayName}!", () =>
                            {
                                HUD.MenuState = BattleMenuState.Main;
                            });
                            AdvanceEventQueue();
                        }
                        else
                        {
                            ExecuteTurn(new BattleAction { Type = ActionType.Switch, IsPlayer = true, SwitchToIndex = HUD.SwitchMenuIndex });
                        }
                    }
                }
                break;

            case BattleMenuState.SelectBagItem:
                if (InputManager.IsActionPressed(GameAction.Cancel))
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                string[] bagItems = { "Poké Ball", "Great Ball", "Potion", "Super Potion" };
                if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
                {
                    HUD.BagMenuIndex = (HUD.BagMenuIndex + 2) % 4;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right))
                {
                    HUD.BagMenuIndex = HUD.BagMenuIndex % 2 == 0 ? HUD.BagMenuIndex + 1 : HUD.BagMenuIndex - 1;
                    AudioManager.PlaySound("cursor");
                }
                if (InputManager.IsActionPressed(GameAction.Confirm))
                {
                    string itemName = bagItems[HUD.BagMenuIndex];
                    var itemData = ItemDatabase.Get(itemName);
                    if (itemData != null)
                    {
                        int qty = PlayerInventory.GetQuantity(itemData);
                        if (qty <= 0)
                        {
                            AudioManager.PlaySound("cancel");
                            QueueMessage($"You don't have any {itemName}s left!", () =>
                            {
                                HUD.MenuState = BattleMenuState.SelectBagItem;
                            });
                            AdvanceEventQueue();
                            return;
                        }

                        if (itemData.Pocket == ItemPocket.PokeBalls && IsTrainerBattle)
                        {
                            AudioManager.PlaySound("cancel");
                            QueueMessage("The Trainer blocked the Ball! Don't be a thief!", () =>
                            {
                                HUD.MenuState = BattleMenuState.SelectBagItem;
                            });
                            AdvanceEventQueue();
                            return;
                        }

                        if (itemData.EffectType == ItemEffectType.HealHP && PlayerPokemon.CurrentHP >= PlayerPokemon.MaxHP)
                        {
                            AudioManager.PlaySound("cancel");
                            QueueMessage("It won't have any effect!", () =>
                            {
                                HUD.MenuState = BattleMenuState.SelectBagItem;
                            });
                            AdvanceEventQueue();
                            return;
                        }

                        PlayerInventory.RemoveItem(itemData, 1);
                        AudioManager.PlaySound("select");
                        ExecuteTurn(new BattleAction { Type = ActionType.UseItem, IsPlayer = true, Item = itemData });
                    }
                }
                break;
        }
    }

    private void AttemptRun()
    {
        if (IsTrainerBattle)
        {
            QueueMessage("No! There's no running from a Trainer battle!", () =>
            {
                HUD.MenuState = BattleMenuState.Main;
            });
            return;
        }

        AudioManager.PlaySound("select");
        QueueMessage("Got away safely!", () =>
        {
            Result = BattleResult.PlayerRan;
        });
    }

    private void ExecuteTurn(BattleAction playerAction)
    {
        HUD.MenuState = BattleMenuState.Message;

        // Choose Enemy Action via Smart AI
        BattleAction enemyAction = ChooseEnemyAction();

        // Determine Action Order
        List<BattleAction> turnOrder = new();
        if (playerAction.Priority > enemyAction.Priority)
        {
            turnOrder.Add(playerAction);
            turnOrder.Add(enemyAction);
        }
        else if (playerAction.Priority < enemyAction.Priority)
        {
            turnOrder.Add(enemyAction);
            turnOrder.Add(playerAction);
        }
        else
        {
            // Speed Tie Check
            int playerSpd = PlayerPokemon.GetEffectiveStat(StatType.Speed);
            int enemySpd = EnemyPokemon.GetEffectiveStat(StatType.Speed);
            if (playerSpd >= enemySpd)
            {
                turnOrder.Add(playerAction);
                turnOrder.Add(enemyAction);
            }
            else
            {
                turnOrder.Add(enemyAction);
                turnOrder.Add(playerAction);
            }
        }

        ExecuteAction(turnOrder[0], () =>
        {
            if (IsBattleOver) return;

            Pokemon secondUser = turnOrder[1].IsPlayer ? PlayerPokemon : EnemyPokemon;
            if (!secondUser.IsFainted)
            {
                ExecuteAction(turnOrder[1], () =>
                {
                    if (IsBattleOver) return;
                    ExecutePostTurnStatusChecks();
                });
            }
            else
            {
                ExecutePostTurnStatusChecks();
            }
        });

        AdvanceEventQueue();
    }

    private BattleAction ChooseEnemyAction()
    {
        // Simple Smart AI: picks super effective or strongest move
        var usableMoves = EnemyPokemon.Moves.Where(m => m.CurrentPP > 0).ToList();
        if (usableMoves.Count == 0)
        {
            usableMoves.Add(MoveDatabase.Create("Tackle"));
        }

        Move bestMove = usableMoves.First();
        float bestScore = -1f;

        foreach (var m in usableMoves)
        {
            float score = m.Power;
            float eff = TypeChart.GetEffectiveness(m.Type, PlayerPokemon.Species.PrimaryType, PlayerPokemon.Species.SecondaryType);
            score *= eff;
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = m;
            }
        }

        return new BattleAction { Type = ActionType.Fight, IsPlayer = false, Move = bestMove };
    }

    private void ExecuteAction(BattleAction action, Action onComplete)
    {
        if (IsBattleOver) return;

        Pokemon user = action.IsPlayer ? PlayerPokemon : EnemyPokemon;
        Pokemon target = action.IsPlayer ? EnemyPokemon : PlayerPokemon;

        if (user.IsFainted)
        {
            onComplete();
            return;
        }

        switch (action.Type)
        {
            case ActionType.Fight:
                ExecuteAttack(action.IsPlayer, user, target, action.Move!, onComplete);
                break;
            case ActionType.Switch:
                ExecuteSwitch(action.SwitchToIndex, onComplete);
                break;
            case ActionType.UseItem:
                ExecuteItemUse(action.Item!, onComplete);
                break;
            default:
                onComplete();
                break;
        }
    }

    private void ExecuteAttack(bool isPlayer, Pokemon attacker, Pokemon defender, Move move, Action onComplete)
    {
        move.CurrentPP = Math.Max(0, move.CurrentPP - 1);
        string userStr = isPlayer ? attacker.DisplayName : $"Foe {attacker.DisplayName}";
        string defStr = isPlayer ? $"Foe {defender.DisplayName}" : defender.DisplayName;

        QueueMessage($"{userStr} used {move.Name}!", () =>
        {
            // Trigger Visual FX
            Vector2 src = isPlayer ? new Vector2(450, 500) : new Vector2(1450, 240);
            Vector2 dst = isPlayer ? new Vector2(1450, 240) : new Vector2(450, 500);

            VfxType vfxType = move.Type switch
            {
                PokemonType.Fire => VfxType.Ember,
                PokemonType.Water => VfxType.WaterGun,
                PokemonType.Grass => VfxType.RazorLeaf,
                PokemonType.Electric => VfxType.Thunderbolt,
                _ => VfxType.TackleBump
            };

            VFX.TriggerVfx(vfxType, src, dst, 0.4f);

            // Accuracy Check
            if (move.Accuracy > 0 && rng.Next(100) >= move.Accuracy)
            {
                QueueMessage($"{userStr}'s attack missed!", onComplete);
                return;
            }

            // Status moves
            if (move.Category == MoveCategory.Status)
            {
                ApplyStatusMove(isPlayer, attacker, defender, move, onComplete);
                return;
            }

            // Damage Calculation
            var dmg = DamageCalculator.CalculateDamage(attacker, defender, move);

            if (dmg.IsImmune)
            {
                QueueMessage($"It doesn't affect {defStr}...", onComplete);
                return;
            }

            // Apply Damage
            defender.CurrentHP = Math.Max(0, defender.CurrentHP - dmg.Damage);
            if (isPlayer) enemyDamageFlashTimer = 0.25f;
            else playerDamageFlashTimer = 0.25f;

            if (dmg.IsSuperEffective) AudioManager.PlaySound("hit_super");
            else AudioManager.PlaySound("hit_normal");

            List<string> msgs = new();
            if (dmg.IsCritical) msgs.Add("A critical hit!");
            if (dmg.IsSuperEffective) msgs.Add("It's super effective!");
            if (dmg.IsNotVeryEffective) msgs.Add("It's not very effective...");

            if (defender.CurrentHP <= 0)
            {
                defender.Status = StatusCondition.Faint;
                AudioManager.PlaySound("faint");
                msgs.Add($"{defStr} fainted!");

                QueueMessageSequence(msgs, () =>
                {
                    if (isPlayer)
                    {
                        AwardExp(defender, onComplete);
                    }
                    else
                    {
                        HandlePlayerPokemonFaint(onComplete);
                    }
                });
            }
            else
            {
                QueueMessageSequence(msgs, onComplete);
            }
        });
    }

    private void ApplyStatusMove(bool isPlayer, Pokemon attacker, Pokemon defender, Move move, Action onComplete)
    {
        string defStr = isPlayer ? $"Foe {defender.DisplayName}" : defender.DisplayName;
        string userStr = isPlayer ? attacker.DisplayName : $"Foe {attacker.DisplayName}";

        if (move.Data.InflictStatus != StatusCondition.None && defender.Status == StatusCondition.None)
        {
            defender.Status = move.Data.InflictStatus;
            string statusName = move.Data.InflictStatus.ToString().ToUpperInvariant();
            QueueMessage($"{defStr} was {statusName}ED!", onComplete);
        }
        else if (move.Data.TargetStatChange.HasValue)
        {
            var stat = move.Data.TargetStatChange.Value;
            Pokemon target = move.Data.StatChangeTargetSelf ? attacker : defender;
            string targetName = target == attacker ? userStr : defStr;

            int cur = target.StatStages.GetValueOrDefault(stat, 0);
            target.StatStages[stat] = Math.Clamp(cur + move.Data.StatStageAmount, -6, 6);

            string changeStr = move.Data.StatStageAmount > 1 ? "sharply rose!" : move.Data.StatStageAmount > 0 ? "rose!" : "fell!";
            QueueMessage($"{targetName}'s {stat} {changeStr}", onComplete);
        }
        else
        {
            onComplete();
        }
    }

    private void ExecuteSwitch(int newPartyIndex, Action onComplete)
    {
        var nextPkmn = PlayerParty.Members[newPartyIndex];
        QueueMessage($"Come back, {PlayerPokemon.DisplayName}!", () =>
        {
            PlayerPokemon = nextPkmn;
            AudioManager.PlaySound("select");
            QueueMessage($"Go! {PlayerPokemon.DisplayName}!", onComplete);
        });
    }

    private void ExecuteItemUse(ItemData item, Action onComplete)
    {
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            if (IsTrainerBattle)
            {
                QueueMessage("The Trainer blocked the Ball! Don't be a thief!", onComplete);
                return;
            }

            QueueMessage($"Lucas used one {item.Name}!", () =>
            {
                AudioManager.PlaySound("ball_throw");
                var catchRes = CatchCalculator.AttemptCatch(EnemyPokemon, item);
                VFX.TriggerPokeballThrow(new Vector2(350, 520), new Vector2(1450, 240), catchRes.Shakes, 2.2f);

                messageWaitTimer = 2.4f;
                turnEventQueue.Enqueue(() =>
                {
                    if (catchRes.IsCaught)
                    {
                        AudioManager.PlayBGM("Victory");
                        Pokedex.RegisterCaught(EnemyPokemon.Species.DexNumber);

                        if (!PlayerParty.IsFull)
                        {
                            PlayerParty.Add(EnemyPokemon);
                            QueueMessage($"Gotcha! {EnemyPokemon.DisplayName} was caught!", () =>
                            {
                                Result = BattleResult.EnemyCaught;
                            });
                        }
                        else
                        {
                            pcBoxStorage?.Add(EnemyPokemon);
                            QueueMessage($"Gotcha! {EnemyPokemon.DisplayName} was caught!", () =>
                            {
                                QueueMessage($"{EnemyPokemon.DisplayName} was transferred to Box 1!", () =>
                                {
                                    Result = BattleResult.EnemyCaught;
                                });
                            });
                        }
                    }
                    else
                    {
                        string msg = catchRes.Shakes switch
                        {
                            0 => "Oh no! The Pokémon broke free!",
                            1 => "Aww! It appeared to be caught!",
                            2 => "Aargh! Almost had it!",
                            3 => "Gah! It was so close, too!",
                            _ => "The Pokémon broke free!"
                        };
                        QueueMessage(msg, onComplete);
                    }
                });
            });
        }
        else if (item.EffectType == ItemEffectType.HealHP)
        {
            PlayerPokemon.CurrentHP = Math.Min(PlayerPokemon.MaxHP, PlayerPokemon.CurrentHP + item.EffectValue);
            AudioManager.PlaySound("heal");
            QueueMessage($"Used a {item.Name}! {PlayerPokemon.DisplayName}'s HP was restored!", onComplete);
        }
        else
        {
            onComplete();
        }
    }

    private void AwardExp(Pokemon defeated, Action onComplete)
    {
        // Gen 4 Exp formula: (BaseExp * DefeatedLevel) / 7
        int exp = (defeated.Species.BaseExpYield * defeated.Level) / 7;
        exp = Math.Max(10, exp);

        QueueMessage($"{PlayerPokemon.DisplayName} gained {exp} EXP. Points!", () =>
        {
            bool leveledUp = PlayerPokemon.GainExp(exp, out var moves, out bool evolved, out string oldName);
            if (leveledUp)
            {
                AudioManager.PlaySound("levelup");
                QueueMessage($"{PlayerPokemon.DisplayName} grew to Lv. {PlayerPokemon.Level}!", () =>
                {
                    List<string> learnMsgs = new();
                    foreach (var m in moves)
                    {
                        learnMsgs.Add($"{PlayerPokemon.DisplayName} learned {m}!");
                    }

                    if (evolved)
                    {
                        learnMsgs.Add($"What? {oldName} is evolving!");
                        learnMsgs.Add($"Congratulations! Your {oldName} evolved into {PlayerPokemon.Species.Name}!");
                    }

                    QueueMessageSequence(learnMsgs, () =>
                    {
                        CheckBattleVictory(onComplete);
                    });
                });
            }
            else
            {
                CheckBattleVictory(onComplete);
            }
        });
    }

    private void CheckBattleVictory(Action onComplete)
    {
        if (IsTrainerBattle && EnemyParty != null && EnemyParty.HasUsablePokemon)
        {
            var nextEnemy = EnemyParty.FirstUsable!;
            EnemyPokemon = nextEnemy;
            Pokedex.RegisterSeen(EnemyPokemon.Species.DexNumber);
            QueueMessage($"{OpponentTrainer!.FullTitle} sent out {EnemyPokemon.DisplayName}!", () =>
            {
                onComplete();
            });
        }
        else
        {
            AudioManager.PlayBGM("Victory");
            if (IsTrainerBattle)
            {
                QueueMessage($"Player defeated {OpponentTrainer!.FullTitle}!", () =>
                {
                    QueueMessage($"Lucas received ${OpponentTrainer.PrizeMoney} for winning!", () =>
                    {
                        Result = BattleResult.PlayerVictory;
                    });
                });
            }
            else
            {
                QueueMessage($"Player defeated the wild {EnemyPokemon.DisplayName}!", () =>
                {
                    Result = BattleResult.PlayerVictory;
                });
            }
        }
    }

    private void HandlePlayerPokemonFaint(Action onComplete)
    {
        if (PlayerParty.HasUsablePokemon)
        {
            QueueMessage("Choose next Pokémon to send out!", () =>
            {
                HUD.MenuState = BattleMenuState.SwitchPokemon;
            });
        }
        else
        {
            QueueMessage("Lucas is out of usable Pokémon!", () =>
            {
                QueueMessage("Lucas whited out...", () =>
                {
                    Result = BattleResult.PlayerDefeat;
                });
            });
        }
    }

    private void ExecutePostTurnStatusChecks()
    {
        List<string> msgs = new();
        bool playerHurt = false;
        bool enemyHurt = false;

        if (!PlayerPokemon.IsFainted && (PlayerPokemon.Status == StatusCondition.Burn || PlayerPokemon.Status == StatusCondition.Poison))
        {
            int dmg = Math.Max(1, PlayerPokemon.MaxHP / 16);
            PlayerPokemon.CurrentHP = Math.Max(0, PlayerPokemon.CurrentHP - dmg);
            msgs.Add($"{PlayerPokemon.DisplayName} is hurt by its {PlayerPokemon.Status}!");
            playerHurt = true;
        }

        if (!EnemyPokemon.IsFainted && (EnemyPokemon.Status == StatusCondition.Burn || EnemyPokemon.Status == StatusCondition.Poison))
        {
            int dmg = Math.Max(1, EnemyPokemon.MaxHP / 16);
            EnemyPokemon.CurrentHP = Math.Max(0, EnemyPokemon.CurrentHP - dmg);
            msgs.Add($"Foe {EnemyPokemon.DisplayName} is hurt by its {EnemyPokemon.Status}!");
            enemyHurt = true;
        }

        QueueMessageSequence(msgs, () =>
        {
            if (playerHurt && PlayerPokemon.CurrentHP <= 0)
            {
                PlayerPokemon.Status = StatusCondition.Faint;
                AudioManager.PlaySound("faint");
                QueueMessage($"{PlayerPokemon.DisplayName} fainted!", () =>
                {
                    HandlePlayerPokemonFaint(() => { });
                });
                return;
            }

            if (enemyHurt && EnemyPokemon.CurrentHP <= 0)
            {
                EnemyPokemon.Status = StatusCondition.Faint;
                AudioManager.PlaySound("faint");
                QueueMessage($"Foe {EnemyPokemon.DisplayName} fainted!", () =>
                {
                    AwardExp(EnemyPokemon, () =>
                    {
                        if (!IsBattleOver && !PlayerPokemon.IsFainted && !EnemyPokemon.IsFainted)
                        {
                            HUD.MenuState = BattleMenuState.Main;
                        }
                    });
                });
                return;
            }

            if (!IsBattleOver && !PlayerPokemon.IsFainted && !EnemyPokemon.IsFainted)
            {
                HUD.MenuState = BattleMenuState.Main;
            }
        });
    }

    private void QueueMessageSequence(List<string> msgs, Action? onComplete)
    {
        if (msgs == null || msgs.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        void QueueNext(int index)
        {
            if (index < msgs.Count - 1)
            {
                QueueMessage(msgs[index], () => QueueNext(index + 1));
            }
            else
            {
                QueueMessage(msgs[index], onComplete);
            }
        }

        QueueNext(0);
    }

    private void QueueMessage(string msg, Action? onComplete = null)
    {
        turnEventQueue.Enqueue(() =>
        {
            currentMessage = msg;
            waitingForMessageConfirm = true;
            HUD.MenuState = BattleMenuState.Message;
            currentMessageCallback = onComplete;
        });
    }

    private void AdvanceEventQueue()
    {
        if (turnEventQueue.Count > 0)
        {
            var next = turnEventQueue.Dequeue();
            next.Invoke();
        }
        else if (Result == BattleResult.None && !waitingForMessageConfirm)
        {
            HUD.MenuState = BattleMenuState.Main;
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        HUD.Draw(
            screenWidth,
            screenHeight,
            PlayerPokemon,
            EnemyPokemon,
            PlayerParty,
            EnemyParty,
            IsTrainerBattle,
            currentMessage,
            VFX,
            playerSpriteOffset,
            enemySpriteOffset,
            playerDamageFlashTimer > 0f,
            enemyDamageFlashTimer > 0f,
            PlayerInventory);
    }
}
