using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// A turn: the AI picks, everyone acts in order, the end-of-turn effects run, fainted Pokémon are replaced.
public partial class BattleEngine
{
    private void ExecuteTurn(List<BattleAction> playerActions)
    {
        HUD.MenuState = BattleMenuState.Message;

        var actions = new List<BattleAction>(playerActions);
        foreach (var foe in EnemySlots.Where(b => b.IsActive)) actions.Add(ChooseEnemyAction(foe));

        foreach (var b in AllBattlers)
        {
            b.MovedThisTurn = false;
            b.Flinched = false;
            b.TookCriticalHit = false;
        }

        // Order: running, items and switches first, then moves by priority; Quick Claw, then Speed, then chance
        foreach (var a in actions)
        {
            a.Speed = EffectiveSpeed(a.User!);
            a.QuickClaw = a.Type == ActionType.Fight && BattleEffects.Of(a.User!).Any(e => e.MovesFirstInBracket(rng));
            a.Tiebreak = rng.Next();
        }
        actions = actions
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.QuickClaw)
            .ThenByDescending(a => a.Speed)
            .ThenBy(a => a.Tiebreak)
            .ToList();

        RunActions(actions, 0);
        AdvanceEventQueue();
    }

    /// <summary>Speed after stages, paralysis and abilities or items.</summary>
    public int EffectiveSpeed(Battler b)
    {
        var p = b.Pokemon!;
        var effects = BattleEffects.Of(b).ToList();
        float speed = p.Speed * DamageCalculator.StageMultiplier(p.StatStages.GetValueOrDefault(StatType.Speed));
        if (p.Status == StatusCondition.Paralyze && !effects.Any(e => e.IgnoresParalysisSlowdown)) speed *= 0.25f;
        speed *= effects.Aggregate(1f, (m, e) => m * e.SpeedMultiplier(b));
        return Math.Max(1, (int)speed);
    }

    private void RunActions(List<BattleAction> actions, int index)
    {
        if (Result != BattleResult.None) return;
        if (index == actions.Count)
        {
            EndOfTurn();
            return;
        }

        var action = actions[index];
        var user = action.User!;
        // A Pokémon that fainted (or was switched out by its own trainer) loses its action
        if (user.Pokemon != action.Actor || !user.IsActive)
        {
            RunActions(actions, index + 1);
            return;
        }

        void Next() => ResolveFaints(() => RunActions(actions, index + 1));

        switch (action.Type)
        {
            case ActionType.Fight:
                ExecuteMove(user, action.Move!, action.Target, Next);
                break;
            case ActionType.Switch:
                ExecuteSwitch(user, action.SwitchToIndex, Next);
                break;
            case ActionType.UseItem:
                ExecuteItemUse(user, action.Item!, Next);
                break;
            default:
                Next();
                break;
        }
    }

    // ---------------------------------------------------------------- entering and leaving the field

    /// <summary>Puts a Pokémon in a place on the field (the send-out animation is the caller's).</summary>
    private void SendIn(Battler place, Pokemon pokemon)
    {
        place.Pokemon = pokemon;
        OnEntered(place);
    }

    private void OnEntered(Battler place)
    {
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        place.FoughtAgainst.Clear();
        if (!place.IsPlayerSide) Pokedex.RegisterSeen(place.Pokemon.Species.DexNumber);
        foreach (var foe in EnemySlots.Where(b => b.IsActive))
            foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);
    }

    private void Withdraw(Battler place)
    {
        foreach (var e in BattleEffects.Of(place)) e.OnWithdraw(place);
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
    }

    /// <summary>Abilities that act on entry (Intimidate), fastest first, then <paramref name="next"/>.</summary>
    private void RunEntryEffects(List<Battler> entered, Action next)
    {
        foreach (var b in entered.Where(b => b.IsActive).OrderByDescending(EffectiveSpeed))
        {
            foreach (var e in BattleEffects.Of(b)) e.OnEntry(this, b);
            CheckConditionHooks(b, null);
        }
        RunAnnouncements(next);
    }

    private void ExecuteSwitch(Battler place, int newPartyIndex, Action onComplete)
    {
        var outgoing = place.Pokemon!;
        var incoming = PlayerParty.Members[newPartyIndex];
        QueueMessage($"Come back, {outgoing.DisplayName}!", () =>
        {
            Withdraw(place);
            AudioManager.PlaySound("select");
            QueueMessage($"Go! {incoming.DisplayName}!", () => RunEntryEffects(new List<Battler> { place }, onComplete), onShow: () =>
            {
                SendIn(place, incoming);
                Anim.SendOut(BattleSide.Player, incoming, place.Slot);
            });
        }, onShow: () => Anim.Recall(BattleSide.Player, place.Slot));
    }

    // ---------------------------------------------------------------- items

    private void ExecuteItemUse(Battler user, ItemData item, Action onComplete)
    {
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            ThrowBall(item, onComplete);
        }
        else if (item.EffectType == ItemEffectType.HealHP)
        {
            var p = user.Pokemon!;
            p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + item.EffectValue);
            AudioManager.PlaySound("heal");
            QueueMessage($"Used a {item.Name}! {p.DisplayName}'s HP was restored!", onComplete);
        }
        else
        {
            onComplete();
        }
    }

    private void ThrowBall(ItemData item, Action onComplete)
    {
        var target = EnemySlots.First(b => b.IsActive);
        var foe = target.Pokemon!;
        QueueMessage($"Lucas used one {item.Name}!", () =>
        {
            AudioManager.PlaySound("ball_throw");
            var catchRes = CatchCalculator.AttemptCatch(foe, item);

            // The ball opens over the foe, which vanishes into it; the result shows once the ball settles.
            // A foe that breaks free bursts out with the ball and is back on its platform before the message.
            VFX.TriggerPokeballThrow(item.Name, new Vector2(200, 720), BattleHUD.Center(BattleSide.Enemy, target.Slot),
                BattleHUD.Feet(BattleSide.Enemy, target.Slot), catchRes.Shakes);
            Anim.Capture(BattleVFX.BallFlightTime, target.Slot);

            if (catchRes.IsCaught)
            {
                messageWaitTimer = BattleVFX.BallThrowTime(catchRes.Shakes) + 0.1f;
            }
            else
            {
                After(BattleVFX.BallSettleTime(catchRes.Shakes), () => Anim.BreakFree(target.Slot));
                messageWaitTimer = BattleVFX.BallSettleTime(catchRes.Shakes) + BattleAnimator.SendOutTime + 0.1f;
            }
            turnEventQueue.Enqueue(() =>
            {
                if (catchRes.IsCaught)
                {
                    AudioManager.PlayBGM("Victory");
                    Pokedex.RegisterCaught(foe.Species.DexNumber);
                    foe.ResetStatStages();

                    if (!PlayerParty.IsFull)
                    {
                        PlayerParty.Add(foe);
                        QueueMessage($"Gotcha! {foe.DisplayName} was caught!", () => Result = BattleResult.EnemyCaught);
                    }
                    else
                    {
                        pcBoxStorage?.Add(foe);
                        QueueMessage($"Gotcha! {foe.DisplayName} was caught!", () =>
                        {
                            QueueMessage($"{foe.DisplayName} was transferred to Box 1!", () => Result = BattleResult.EnemyCaught);
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

    // ---------------------------------------------------------------- end of the turn

    /// <summary>Leftovers, poison and burns, Speed Boost and the like, fastest Pokémon first; then replacements.</summary>
    private void EndOfTurn()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive).OrderByDescending(EffectiveSpeed).ToList())
        {
            var p = b.Pokemon!;
            var item = HeldItemEffects.For(p.HeldItem);
            item?.AtEndOfTurn(this, b);

            switch (p.Status)
            {
                case StatusCondition.Burn:
                    LoseHp(b, Math.Max(1, p.MaxHP / 8), $"{b.Name} is hurt by its burn!");
                    break;
                case StatusCondition.Poison:
                    LoseHp(b, Math.Max(1, p.MaxHP / 8), $"{b.Name} is hurt by poison!");
                    break;
                case StatusCondition.Toxic:
                    p.ToxicCounter = Math.Min(15, p.ToxicCounter + 1);
                    LoseHp(b, Math.Max(1, p.MaxHP * p.ToxicCounter / 16), $"{b.Name} is hurt by poison!");
                    break;
            }

            p.Ability?.Effect?.AtEndOfTurn(this, b);
        }

        RunAnnouncements(() => ResolveFaints(() => ReplaceFainted(StartTurn)));
    }

    /// <summary>Fills the places of fainted Pokémon from the bench: trainers send theirs, the player picks.</summary>
    private void ReplaceFainted(Action next)
    {
        if (Result != BattleResult.None) return;
        var entered = new List<Battler>();

        void Continue()
        {
            // The foes first
            foreach (var place in EnemySlots.Where(b => !b.IsActive && b.Roster != null))
            {
                var nextFoe = NextFromRoster(place, EnemySlots.Select(b => b.Pokemon));
                if (nextFoe == null) continue;
                entered.Add(place);
                QueueMessage($"{place.Trainer!.FullTitle} sent out {nextFoe.DisplayName}!", Continue, onShow: () =>
                {
                    SendIn(place, nextFoe);
                    Anim.SendOut(BattleSide.Enemy, nextFoe, place.Slot);
                });
                return;
            }

            // Then the player chooses for each empty place, while there is anyone left to send
            var empty = PlayerSlots.FirstOrDefault(b => !b.IsActive);
            bool reserves = PlayerParty.Members.Any(p => !p.IsFainted && PlayerSlots.All(b => b.Pokemon != p));
            if (empty != null && reserves && !entered.Contains(empty))
            {
                entered.Add(empty);
                menuSlot = empty.Slot;
                afterReplacement = Continue;
                HUD.SwitchMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                return;
            }

            RunEntryEffects(entered, next);
        }

        Continue();
    }

    /// <summary>The next Pokémon of a place's roster that can fight and isn't already out.</summary>
    private static Pokemon? NextFromRoster(Battler place, IEnumerable<Pokemon?> exclude)
    {
        var used = exclude.Where(p => p != null).ToHashSet();
        return place.Roster?.Members.FirstOrDefault(p => !p.IsFainted && !used.Contains(p));
    }

    // ---------------------------------------------------------------- fainting, EXP and the end of the battle

    /// <summary>Every Pokémon down to 0 HP faints (with EXP for the player's), then the battle ends or <paramref name="next"/> runs.</summary>
    private void ResolveFaints(Action next)
    {
        if (Result != BattleResult.None) return;

        var down = AllBattlers.FirstOrDefault(b => b.Pokemon != null && b.Pokemon.CurrentHP <= 0 && b.Pokemon.Status != StatusCondition.Faint);
        if (down == null)
        {
            CheckBattleEnd(next);
            return;
        }

        down.Pokemon!.Status = StatusCondition.Faint;
        down.Pokemon.CurrentHP = 0;
        down.ClearVolatile();
        QueueMessage($"{down.Name} fainted!", () =>
        {
            if (down.IsPlayerSide) ResolveFaints(next);
            else AwardExp(down, () => ResolveFaints(next));
        }, onShow: () => PlayFaint(down));
    }

    /// <summary>The fainting cry and slide off the platform, started as the "fainted!" message appears.</summary>
    private void PlayFaint(Battler b)
    {
        AudioManager.PlaySound("faint");
        Anim.Faint(b.Side, 0.15f, b.Slot);
    }

    private bool SideDefeated(IReadOnlyList<Battler> side) =>
        !side.Any(b => b.IsActive) && !side.Any(b => b.Roster != null && b.Roster.Members.Any(p => !p.IsFainted));

    private void CheckBattleEnd(Action next)
    {
        if (SideDefeated(PlayerSlots))
        {
            QueueMessage("Lucas is out of usable Pokémon!", () =>
            {
                QueueMessage("Lucas whited out...", () => Result = BattleResult.PlayerDefeat);
            });
            return;
        }

        if (SideDefeated(EnemySlots))
        {
            AudioManager.PlayBGM("Victory");
            if (IsTrainerBattle)
            {
                string beaten = string.Join(" and ", Trainers.Select(t => t.FullTitle));
                int prize = Trainers.Sum(t => t.PrizeMoney);
                QueueMessage($"Player defeated {beaten}!", () =>
                {
                    QueueMessage($"Lucas received ${prize} for winning!", () => Result = BattleResult.PlayerVictory);
                });
            }
            else
            {
                string wild = JoinNames(EnemySlots.Where(b => b.Pokemon != null).Select(b => b.Pokemon!.DisplayName));
                QueueMessage($"Player defeated the wild {wild}!", () => Result = BattleResult.PlayerVictory);
            }
            return;
        }

        next();
    }

    /// <summary>Shares the EXP for a fainted foe between the player's Pokémon that fought it and are still standing.</summary>
    private void AwardExp(Battler defeated, Action onComplete)
    {
        var foe = defeated.Pokemon!;
        var earners = PlayerParty.Members.Where(p => !p.IsFainted && defeated.FoughtAgainst.Contains(p)).ToList();
        if (earners.Count == 0)
        {
            onComplete();
            return;
        }

        // Generation 4: base EXP × level / 7, half as much again from a trainer's Pokémon, split between those who fought
        int total = foe.Species.BaseExpYield * foe.Level / 7;
        if (IsTrainerBattle) total = total * 3 / 2;
        int each = Math.Max(1, total / earners.Count);

        void Give(int i)
        {
            if (i == earners.Count)
            {
                onComplete();
                return;
            }

            var p = earners[i];
            if (p.Level >= 100)
            {
                Give(i + 1);
                return;
            }
            QueueMessage($"{p.DisplayName} gained {each} EXP. Points!", () =>
            {
                bool leveledUp = p.GainExp(each, out var moves, out bool evolved, out string oldName);
                if (!leveledUp)
                {
                    Give(i + 1);
                    return;
                }

                AudioManager.PlaySound("levelup");
                QueueMessage($"{p.DisplayName} grew to Lv. {p.Level}!", () =>
                {
                    var learnMsgs = moves.Select(m => $"{p.DisplayName} learned {m}!").ToList();
                    if (evolved)
                    {
                        learnMsgs.Add($"What? {oldName} is evolving!");
                        learnMsgs.Add($"Congratulations! Your {oldName} evolved into {p.Species.Name}!");
                    }
                    QueueMessageSequence(learnMsgs, () => Give(i + 1));
                });
            });
        }

        Give(0);
    }

    // ---------------------------------------------------------------- the opponents' choices

    /// <summary>
    /// Picks the move that looks best against the player's Pokémon: damage from power, type and STAB, with some
    /// value on status and stat moves while they would still do something, and a little chance thrown in.
    /// </summary>
    private BattleAction ChooseEnemyAction(Battler foe)
    {
        var moves = UsableMoves(foe);
        if (moves.Count == 0) return new BattleAction { Type = ActionType.Fight, Move = new Move(StruggleData), User = foe, Actor = foe.Pokemon };

        var targets = PlayerSlots.Where(b => b.IsActive).ToList();
        var ally = EnemySlots.FirstOrDefault(b => b != foe && b.IsActive);

        BattleAction? best = null;
        float bestScore = float.MinValue;
        foreach (var move in moves)
        {
            var aims = move.Target == MoveTarget.Selected ? targets.Select(t => (Battler?)t).ToList() : new List<Battler?> { null };
            foreach (var aim in aims)
            {
                float score = ScoreMove(foe, move, aim, targets, ally) * (0.85f + 0.3f * (float)rng.NextDouble());
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new BattleAction { Type = ActionType.Fight, Move = move, Target = aim, User = foe, Actor = foe.Pokemon };
                }
            }
        }
        return best!;
    }

    private float ScoreMove(Battler user, Move move, Battler? aim, List<Battler> foes, Battler? ally)
    {
        var data = move.Data;
        if (move.Category == MoveCategory.Status)
        {
            var target = aim ?? foes.FirstOrDefault();
            if (data.HealPercent > 0) return user.Pokemon!.CurrentHP * 2 < user.Pokemon.MaxHP ? 90f : 0f;
            if (data.InflictStatus != StatusCondition.None)
                return target != null && target.Pokemon!.Status == StatusCondition.None ? 55f : 0f;
            if (data.ConfuseChancePercent > 0) return target != null && !target.IsConfused ? 40f : 0f;
            if (data.TargetStatChange is { } stat)
            {
                var who = data.StatChangeTargetSelf ? user : target;
                if (who == null) return 0f;
                int stage = who.Pokemon!.StatStages.GetValueOrDefault(stat);
                return data.StatChangeTargetSelf ? (stage < 2 ? 35f : 0f) : (stage > -2 ? 25f : 0f);
            }
            return 5f;
        }

        float Hit(Battler t) => move.Power * DamageCalculator.Effectiveness(user, t, move) * (user.HasType(move.Type) ? 1.5f : 1f);

        float score;
        switch (move.Target)
        {
            case MoveTarget.Selected:
            case MoveTarget.RandomFoe:
                score = aim != null ? Hit(aim) : foes.Select(Hit).DefaultIfEmpty(0f).Max();
                break;
            default:
                score = foes.Sum(Hit) * (foes.Count > 1 ? 0.75f : 1f);
                if (move.Target == MoveTarget.AllOthers && ally != null) score -= Hit(ally);
                break;
        }
        return score * (data.Accuracy > 0 ? data.Accuracy / 100f : 1f);
    }
}
