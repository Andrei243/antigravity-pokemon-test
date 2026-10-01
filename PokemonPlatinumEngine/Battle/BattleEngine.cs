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

    /// <summary>What the battle looks like: send-outs, attacks, hits, faints and the HP bars as they drain.</summary>
    public BattleAnimator Anim { get; } = new();

    private readonly Queue<Action> turnEventQueue = new();
    private string currentMessage = "";
    private float messageWaitTimer = 0f;
    private bool waitingForMessageConfirm = false;

    // Effects that land a moment after the message that started them, like a hit after the attacker's lunge
    private const float HitDelay = 0.35f;
    private readonly List<(float Delay, Action Effect)> pendingEffects = new();

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

        // Initial Intro Message: trainers stand on their platforms until they send their Pokémon out
        if (IsTrainerBattle)
        {
            Anim.EnemyTrainer = OpponentTrainer!.TrainerClass;
            QueueMessage($"{OpponentTrainer.FullTitle} wants to battle!", () =>
            {
                QueueMessage($"{OpponentTrainer.FullTitle} sent out {EnemyPokemon.DisplayName}!", () =>
                {
                    QueueMessage($"Go! {PlayerPokemon.DisplayName}!", () =>
                    {
                        HUD.MenuState = BattleMenuState.Main;
                    }, onShow: () => Anim.SendOut(BattleSide.Player, PlayerPokemon));
                }, onShow: () => Anim.SendOut(BattleSide.Enemy, EnemyPokemon));
            });
        }
        else
        {
            Anim.Appear(BattleSide.Enemy, EnemyPokemon);
            QueueMessage($"A wild {EnemyPokemon.DisplayName} appeared!", () =>
            {
                QueueMessage($"Go! {PlayerPokemon.DisplayName}!", () =>
                {
                    HUD.MenuState = BattleMenuState.Main;
                }, onShow: () => Anim.SendOut(BattleSide.Player, PlayerPokemon));
            });
        }

        AdvanceEventQueue();
    }

    public void Update(float dt)
    {
        VFX.Update(dt);

        Anim.Update(dt, PlayerPokemon, EnemyPokemon);
        UpdatePendingEffects(dt);

        if (waitingForMessageConfirm)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                ConfirmMessage();
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
                    SelectMainMenuOption(HUD.MainMenuIndex);
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
                    SelectMove(HUD.MoveMenuIndex);
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
                // The team is laid out three to a row
                int switchDx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
                int switchDy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
                if (switchDx != 0 || switchDy != 0)
                {
                    int next = UI.Kit.UiNav.Grid(HUD.SwitchMenuIndex, PlayerParty.Count, 3, switchDx, switchDy);
                    if (next != HUD.SwitchMenuIndex) AudioManager.PlaySound("cursor");
                    HUD.SwitchMenuIndex = next;
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
                            }, onShow: () => Anim.SendOut(BattleSide.Player, PlayerPokemon));
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
                    SelectBagItem(HUD.BagMenuIndex);
                }
                break;
        }
    }

    /// <summary>The items offered by the battle's BAG menu, in slot order.</summary>
    public static IReadOnlyList<string> BagItems { get; } = new[] { "Poké Ball", "Great Ball", "Potion", "Super Potion" };

    /// <summary>Uses the item in the given slot, as chosen from the BAG menu.</summary>
    public void SelectBagItem(int index)
    {
        if (index < 0 || index >= BagItems.Count) return;

        string itemName = BagItems[index];
        var itemData = ItemDatabase.Get(itemName);
        if (itemData == null) return;

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

    /// <summary>The message currently on screen.</summary>
    public string CurrentMessage => currentMessage;

    /// <summary>Acts on a choice from the main battle menu: 0 FIGHT, 1 BAG, 2 POKÉMON, 3 RUN.</summary>
    public void SelectMainMenuOption(int index)
    {
        switch (index)
        {
            case 0:
                HUD.MenuState = BattleMenuState.Moves;
                HUD.MoveMenuIndex = 0;
                break;
            case 1:
                HUD.MenuState = BattleMenuState.SelectBagItem;
                HUD.BagMenuIndex = 0;
                break;
            case 2:
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                HUD.SwitchMenuIndex = 0;
                break;
            case 3:
                AttemptRun();
                break;
        }
    }

    /// <summary>Uses the move in the given slot, as chosen from the FIGHT menu.</summary>
    public void SelectMove(int index)
    {
        if (index < 0 || index >= PlayerPokemon.Moves.Count) return;

        var move = PlayerPokemon.Moves[index];
        if (move.CurrentPP > 0)
        {
            AudioManager.PlaySound("select");
            ExecuteTurn(new BattleAction { Type = ActionType.Fight, IsPlayer = true, Move = move });
        }
        else
        {
            AudioManager.PlaySound("cancel");
            QueueMessage("There's no PP left for this move!", () =>
            {
                HUD.MenuState = BattleMenuState.Moves;
            });
            AdvanceEventQueue();
        }
    }

    /// <summary>Dismisses the message on screen and carries on with whatever it was waiting to trigger.</summary>
    public void ConfirmMessage()
    {
        if (!waitingForMessageConfirm) return;

        // Anything the message started lands now, so the battle logic never runs ahead of it
        FlushPendingEffects();

        waitingForMessageConfirm = false;
        var cb = currentMessageCallback;
        currentMessageCallback = null;
        cb?.Invoke();

        // A timed animation (the Poké Ball throw) holds the queue; Update releases it when the animation ends
        if (messageWaitTimer > 0f) return;

        if (turnEventQueue.Count > 0)
        {
            AdvanceEventQueue();
        }
        else if (Result == BattleResult.None && HUD.MenuState == BattleMenuState.Message)
        {
            // Back to the main menu, unless the message led somewhere else (like choosing a Pokémon to send out)
            HUD.MenuState = BattleMenuState.Main;
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
        }
        else
        {
            QueueMessage("Got away safely!", () =>
            {
                Result = BattleResult.PlayerRan;
            });
        }

        // Show the message right away; queued messages otherwise wait for the next turn
        AdvanceEventQueue();
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

        var attackerSide = isPlayer ? BattleSide.Player : BattleSide.Enemy;
        var defenderSide = isPlayer ? BattleSide.Enemy : BattleSide.Player;

        // The move resolves as its message appears: the attacker lunges, the effect flies across and the hit lands
        // a moment later. The follow-up messages come once the player dismisses it.
        bool missed = false;
        var dmg = new DamageCalculator.DamageResult { TypeMultiplier = 1f };

        void PlayAttack()
        {
            Anim.Attack(attackerSide);

            // Accuracy Check
            missed = move.Accuracy > 0 && rng.Next(100) >= move.Accuracy;
            if (missed || move.Category == MoveCategory.Status) return;

            // Damage Calculation
            dmg = DamageCalculator.CalculateDamage(attacker, defender, move);

            Vector2 src = isPlayer ? BattleHUD.PlayerCenter : BattleHUD.EnemyCenter;
            Vector2 dst = isPlayer ? BattleHUD.EnemyCenter : BattleHUD.PlayerCenter;

            VfxType vfxType = move.Type switch
            {
                PokemonType.Fire => VfxType.Ember,
                PokemonType.Water => VfxType.WaterGun,
                PokemonType.Grass => VfxType.RazorLeaf,
                PokemonType.Electric => VfxType.Thunderbolt,
                _ => VfxType.TackleBump
            };

            VFX.TriggerVfx(vfxType, src, dst, 0.4f);
            if (dmg.IsImmune) return;

            // Apply Damage: the target flinches and its HP bar drains
            After(HitDelay, () =>
            {
                defender.CurrentHP = Math.Max(0, defender.CurrentHP - dmg.Damage);
                Anim.Hit(defenderSide);

                if (dmg.IsSuperEffective) AudioManager.PlaySound("hit_super");
                else AudioManager.PlaySound("hit_normal");
            });
        }

        QueueMessage($"{userStr} used {move.Name}!", () =>
        {
            if (missed)
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

            if (dmg.IsImmune)
            {
                QueueMessage($"It doesn't affect {defStr}...", onComplete);
                return;
            }

            List<string> msgs = new();
            if (dmg.IsCritical) msgs.Add("A critical hit!");
            if (dmg.IsSuperEffective) msgs.Add("It's super effective!");
            if (dmg.IsNotVeryEffective) msgs.Add("It's not very effective...");

            if (defender.CurrentHP <= 0)
            {
                defender.Status = StatusCondition.Faint;
                QueueMessageSequence(msgs, () =>
                {
                    QueueMessage($"{defStr} fainted!", () =>
                    {
                        if (isPlayer)
                        {
                            AwardExp(defender, onComplete);
                        }
                        else
                        {
                            HandlePlayerPokemonFaint(onComplete);
                        }
                    }, onShow: () => PlayFaint(defenderSide));
                });
            }
            else
            {
                QueueMessageSequence(msgs, onComplete);
            }
        }, onShow: PlayAttack);
    }

    /// <summary>The fainting cry and slide off the platform, started as the "fainted!" message appears.</summary>
    private void PlayFaint(BattleSide side)
    {
        AudioManager.PlaySound("faint");
        Anim.Faint(side, 0.15f);
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
            QueueMessage($"Go! {PlayerPokemon.DisplayName}!", onComplete, onShow: () => Anim.SendOut(BattleSide.Player, PlayerPokemon));
        }, onShow: () => Anim.Recall(BattleSide.Player));
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

                // The ball opens over the foe, which vanishes into it; the result shows once the ball settles.
                // A foe that breaks free bursts out with the ball and is back on its platform before the message.
                VFX.TriggerPokeballThrow(item.Name, new Vector2(200, 720), BattleHUD.EnemyCenter, BattleHUD.EnemyFeet, catchRes.Shakes);
                Anim.Capture(BattleVFX.BallFlightTime);

                if (catchRes.IsCaught)
                {
                    messageWaitTimer = BattleVFX.BallThrowTime(catchRes.Shakes) + 0.1f;
                }
                else
                {
                    After(BattleVFX.BallSettleTime(catchRes.Shakes), Anim.BreakFree);
                    messageWaitTimer = BattleVFX.BallSettleTime(catchRes.Shakes) + BattleAnimator.SendOutTime + 0.1f;
                }
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
            }, onShow: () => Anim.SendOut(BattleSide.Enemy, EnemyPokemon));
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
        // Burn and poison sting at the end of the turn: each Pokémon flinches and loses HP as its message appears
        var hurt = new List<(Pokemon Pokemon, BattleSide Side, string Message)>();
        if (!PlayerPokemon.IsFainted && (PlayerPokemon.Status == StatusCondition.Burn || PlayerPokemon.Status == StatusCondition.Poison))
        {
            hurt.Add((PlayerPokemon, BattleSide.Player, $"{PlayerPokemon.DisplayName} is hurt by its {PlayerPokemon.Status}!"));
        }

        if (!EnemyPokemon.IsFainted && (EnemyPokemon.Status == StatusCondition.Burn || EnemyPokemon.Status == StatusCondition.Poison))
        {
            hurt.Add((EnemyPokemon, BattleSide.Enemy, $"Foe {EnemyPokemon.DisplayName} is hurt by its {EnemyPokemon.Status}!"));
        }

        void QueueNext(int index)
        {
            if (index == hurt.Count)
            {
                FinishPostTurnStatusChecks(hurt.Exists(h => h.Side == BattleSide.Player), hurt.Exists(h => h.Side == BattleSide.Enemy));
                return;
            }

            var (pokemon, side, message) = hurt[index];
            QueueMessage(message, () => QueueNext(index + 1), onShow: () =>
            {
                pokemon.CurrentHP = Math.Max(0, pokemon.CurrentHP - Math.Max(1, pokemon.MaxHP / 16));
                Anim.Hit(side);
                AudioManager.PlaySound("hit_normal");
            });
        }

        QueueNext(0);
    }

    private void FinishPostTurnStatusChecks(bool playerHurt, bool enemyHurt)
    {
        if (playerHurt && PlayerPokemon.CurrentHP <= 0)
        {
            PlayerPokemon.Status = StatusCondition.Faint;
            QueueMessage($"{PlayerPokemon.DisplayName} fainted!", () =>
            {
                HandlePlayerPokemonFaint(() => { });
            }, onShow: () => PlayFaint(BattleSide.Player));
            return;
        }

        if (enemyHurt && EnemyPokemon.CurrentHP <= 0)
        {
            EnemyPokemon.Status = StatusCondition.Faint;
            QueueMessage($"Foe {EnemyPokemon.DisplayName} fainted!", () =>
            {
                AwardExp(EnemyPokemon, () =>
                {
                    if (!IsBattleOver && !PlayerPokemon.IsFainted && !EnemyPokemon.IsFainted)
                    {
                        HUD.MenuState = BattleMenuState.Main;
                    }
                });
            }, onShow: () => PlayFaint(BattleSide.Enemy));
            return;
        }

        if (!IsBattleOver && !PlayerPokemon.IsFainted && !EnemyPokemon.IsFainted)
        {
            HUD.MenuState = BattleMenuState.Main;
        }
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

    /// <param name="onComplete">Runs once the player dismisses the message.</param>
    /// <param name="onShow">Runs as the message appears (starts the animation the message describes).</param>
    private void QueueMessage(string msg, Action? onComplete = null, Action? onShow = null)
    {
        turnEventQueue.Enqueue(() =>
        {
            currentMessage = msg;
            waitingForMessageConfirm = true;
            HUD.MenuState = BattleMenuState.Message;
            currentMessageCallback = onComplete;
            onShow?.Invoke();
        });
    }

    private void After(float delay, Action effect) => pendingEffects.Add((delay, effect));

    private void UpdatePendingEffects(float dt)
    {
        for (int i = 0; i < pendingEffects.Count; i++)
        {
            var (delay, effect) = pendingEffects[i];
            if ((delay -= dt) > 0f)
            {
                pendingEffects[i] = (delay, effect);
                continue;
            }
            pendingEffects.RemoveAt(i--);
            effect();
        }
    }

    private void FlushPendingEffects()
    {
        while (pendingEffects.Count > 0)
        {
            var effect = pendingEffects[0].Effect;
            pendingEffects.RemoveAt(0);
            effect();
        }
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

    /// <summary>Draws the HUD, menus and move effects over the battle field (see <see cref="Graphics.BattleRenderer"/>).</summary>
    public void Draw(int screenWidth, int screenHeight)
    {
        HUD.Draw(
            screenWidth,
            screenHeight,
            PlayerPokemon,
            PlayerParty,
            EnemyParty,
            currentMessage,
            VFX,
            Anim,
            PlayerInventory);
    }
}
