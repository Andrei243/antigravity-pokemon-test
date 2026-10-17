using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Items in the battle's own course (plan 06 · R8): what the core does with a held item by its hold effect, where
// the original has it (the turn's order with a Quick Claw, a Custap Berry or a Lagging Tail; a Metronome's
// count; a berry against a type as a hit lands; the items that answer a hit; a Destiny Knot; an Amulet Coin's
// prize), and what the bag's items do when the player uses one. The held items' hooks are in
// Battle/Effects/HeldItemEffects.cs, and the formula's lines in DamageCalculator.
public sealed partial class BattleCore
{
    // ---------------------------------------------------------------- the turn's order

    /// <summary>
    /// <c>BattleSystem_CompareBattlerSpeed</c>: an item that makes its holder move first among moves of its
    /// priority this turn (a Quick Claw, when the number its place drew leaves nothing over 100 / its parameter; a
    /// Custap Berry, at a quarter of its HP or less, half with Gluttony), and one that makes it move last (a
    /// Lagging Tail, a Full Incense). What went off is remembered on the holder until its move begins
    /// (<see cref="Volatiles.ItemWentOff"/>), which keeps the item safe from an Embargo, Knock Off and Trick meanwhile.
    /// </summary>
    private void ItemsInTheOrder(Act act)
    {
        var user = act.User;
        act.MovesFirst = act.Lags = false;
        if (user.Pokemon == null) return;
        string? hold = BattleEffects.HoldEffectOf(user);
        int param = BattleEffects.HoldParamOf(user);
        if (hold == "SometimesPriority" && param > 0 && speedRolls[Number(user)] % (100 / param) == 0) act.MovesFirst = true;
        if (hold == "PinchPriority" && user.Pokemon.CurrentHP <= user.Pokemon.MaxHP / HeldItemEffects.PinchParam(user, param)) act.MovesFirst = true;
        if (act.MovesFirst) user.Volatile.ItemWentOff = true;
        if (hold == "PriorityDown") act.Lags = true;
    }

    /// <summary>
    /// <c>BattleSystem_CompareBattlerSpeed</c> as the trainer AI asks it, before anyone has chosen (plan 06 · R9): no
    /// priority, and a Quick Claw or Custap Berry that would go off is counted but nothing is remembered. Whether the
    /// first goes before the second, after it, or, between two of the same Speed, after it as a tie on a coin's
    /// flip (the original's <c>COMPARE_SPEED_TIE</c>).
    /// </summary>
    internal Ai.SpeedOrder CompareSpeed(Battler first, Battler second)
    {
        var a = first.Pokemon;
        var b = second.Pokemon;
        if (a == null || b == null) return Ai.SpeedOrder.Faster;
        if (a.CurrentHP == 0 && b.CurrentHP > 0) return Ai.SpeedOrder.Slower;
        if (a.CurrentHP > 0 && b.CurrentHP == 0) return Ai.SpeedOrder.Faster;

        int speedA = EffectiveSpeed(first), speedB = EffectiveSpeed(second);
        bool quickA = WouldMoveFirst(first), quickB = WouldMoveFirst(second);
        bool lagA = BattleEffects.HoldEffectOf(first) == "PriorityDown", lagB = BattleEffects.HoldEffectOf(second) == "PriorityDown";
        bool stallA = Has(first, "Stall"), stallB = Has(second, "Stall");

        Ai.SpeedOrder Tie() => rng.Roll(RollKind.SpeedTie, 2) == 1 ? Ai.SpeedOrder.Tie : Ai.SpeedOrder.Faster;
        if (quickA && quickB) return speedA < speedB ? Ai.SpeedOrder.Slower : speedA == speedB ? Tie() : Ai.SpeedOrder.Faster;
        if (quickA != quickB) return quickB ? Ai.SpeedOrder.Slower : Ai.SpeedOrder.Faster;
        if (lagA && lagB) return speedA > speedB ? Ai.SpeedOrder.Slower : speedA == speedB ? Tie() : Ai.SpeedOrder.Faster;
        if (lagA != lagB) return lagA ? Ai.SpeedOrder.Slower : Ai.SpeedOrder.Faster;
        if (stallA && stallB) return speedA > speedB ? Ai.SpeedOrder.Slower : speedA == speedB ? Tie() : Ai.SpeedOrder.Faster;
        if (stallA != stallB) return stallA ? Ai.SpeedOrder.Slower : Ai.SpeedOrder.Faster;
        if (speedA == speedB) return Tie();
        return (Field.TrickRoom ? speedA > speedB : speedA < speedB) ? Ai.SpeedOrder.Slower : Ai.SpeedOrder.Faster;
    }

    /// <summary>A Quick Claw on the number its place last drew, or a Custap Berry at its HP: what <see cref="ItemsInTheOrder"/> counts, with nothing remembered.</summary>
    private bool WouldMoveFirst(Battler user)
    {
        string? hold = BattleEffects.HoldEffectOf(user);
        int param = BattleEffects.HoldParamOf(user);
        if (hold == "SometimesPriority" && param > 0 && speedRolls[Number(user)] % (100 / param) == 0) return true;
        return hold == "PinchPriority" && user.Pokemon!.CurrentHP <= user.Pokemon.MaxHP / HeldItemEffects.PinchParam(user, param);
    }

    /// <summary>
    /// <c>subscript_check_quick_claw</c>, as the holder's move begins: a Custap Berry that went off says so (unless
    /// its holder is the last to act anyway) and is eaten; a Quick Claw says nothing, as in the original, which
    /// only plays its animation. Either way the item is no longer kept safe.
    /// </summary>
    private void ItemLetItMoveFirst(Act act)
    {
        var user = act.User;
        if (!user.Volatile.ItemWentOff) return;
        user.Volatile.ItemWentOff = false;
        // The claw's animation with no line (plan 25 · Y3): a moment between lines, unless its holder is the last to act anyway
        if (act.MovesFirst && waiting.Count > 0 && BattleEffects.HoldEffectOf(user) == "SometimesPriority") Emit(Moment(user, BattleMoment.HeldItem));
        if (!act.MovesFirst || BattleEffects.ItemInHand(user) is not { HoldEffect: "PinchPriority" } berry) return;
        if (waiting.Count > 0) Say(Text.Of("battle.letItMoveFirst", ("user", user.Name), ("item", ItemAtWork(user))));
        ConsumeItem(user);
    }

    /// <summary>
    /// <c>AFTER_MOVE_EFFECT_HELD_ITEM_STATUS</c>: once an action is done, everyone's held item looks at its holder's
    /// condition (<c>BattleSystem_TriggerHeldItemOnStatus</c>), fastest first: a White Herb after a stat was
    /// lowered, a Mental Herb after Attract, a berry after what the action left its holder with.
    /// </summary>
    private void ItemsAfterTheAction()
    {
        foreach (var b in BySpeed())
        {
            if (!b.IsActive) continue;
            BattleEffects.ItemOf(b)?.OnConditionChanged(this, b, null);
            if (!Settle()) return;
        }
    }

    // ---------------------------------------------------------------- a Metronome

    /// <summary>
    /// <c>BattleSystem_UpdateMetronomeCount</c>, as a move begins: a Metronome counts the same move used in a row,
    /// up to ten, and starts again at another; not in the middle of a rampage, an uproar or a move the holder is
    /// held to. Without the item the count is nothing.
    /// </summary>
    private void CountForMetronome(Battler user, Move move)
    {
        var v = user.Volatile;
        if (BattleEffects.HoldEffectOf(user) != "BoostRepeated")
        {
            v.MetronomeCount = 0;
            return;
        }
        if (v.RampageTurns > 0 || v.UproarTurns > 0 || user.IsHeldToItsMove) return;
        if (v.MetronomeMove == move.Data)
        {
            if (v.MetronomeCount < 10) v.MetronomeCount++;
        }
        else
        {
            v.MetronomeCount = 0;
            v.MetronomeMove = move.Data;
        }
    }

    /// <summary><c>BattleSystem_VerifyMetronomeCount</c>: a move that came to nothing doesn't count.</summary>
    private void MetronomeMissed(Battler user)
    {
        var v = user.Volatile;
        if (BattleEffects.HoldEffectOf(user) == "BoostRepeated" && v.MetronomeCount > 0 && v.RampageTurns == 0 && v.UproarTurns == 0 && !user.IsHeldToItsMove) v.MetronomeCount--;
    }

    // ---------------------------------------------------------------- a berry against the type

    /// <summary>
    /// <c>subscript_type_resist_berry</c>, as a hit lands on the Pokémon itself: a berry of the move's type halves
    /// a super-effective hit (the Chilan Berry any Normal hit) and is eaten. The move's type is the one it has
    /// now (Normal under Normalize); nothing weakens a one-hit knockout or a hit of a fixed amount.
    /// </summary>
    private bool WeakenedByBerry(Battler t, Move move, DamageCalculator.DamageResult result, out ItemData berry)
    {
        berry = null!;
        var item = BattleEffects.ItemInHand(t);
        if (item?.HoldEffect == null || HeldItemEffects.TypeWeakenedBy(item.HoldEffect) is not { } type || type != move.Type) return false;
        if (item.HoldEffect != HeldItemEffects.WeakensAnyNormalHit && !result.IsSuperEffective) return false;
        berry = item;
        return true;
    }

    // ---------------------------------------------------------------- the items that answer a hit

    /// <summary>
    /// <c>BattleSystem_TriggerHeldItemOnHit</c>, after the target's ability has answered a hit it took itself (not
    /// through a Substitute): a Sticky Barb moves to an attacker that touched it holding nothing; a Jaboca Berry
    /// costs a physical attacker an eighth of its HP and a Rowap Berry a special one, unless Magic Guard, and is
    /// eaten; an Enigma Berry gives a quarter back after a super-effective hit. An attacker on its way out (U-turn)
    /// takes no Sticky Barb and no Jaboca Berry, as the original has it.
    /// </summary>
    private void ItemsOnHit(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        var user = use.User;
        if (hit.IntoSubstitute || hit.LastDealt <= 0) return;
        var item = BattleEffects.ItemInHand(t);
        if (item?.HoldEffect == null) return;
        int param = Math.Max(1, item.HoldParam);
        bool physical = use.Move.Category == MoveCategory.Physical;
        bool leaving = use.Data.Effect == "SwitchHit";
        switch (item.HoldEffect)
        {
            case "DmgUserContactXfr":
                if (user.IsActive && user.Pokemon!.HeldItem == null && !knockedOff.ContainsKey(user.Pokemon) && use.Data.Effect != "RemoveHeldItem"
                    && !leaving && (use.Data.Flags & MoveFlags.Contact) != 0)
                {
                    t.Pokemon!.HeldItem = null;
                    user.Pokemon.HeldItem = item;
                    // The line tells of the barb in both places: where it was and where it is now
                    _ = ItemOf(user);
                    Say(Text.Of("battle.wasTransferredTo", ("target", t.Name), ("item", ItemOf(t, item)), ("user", user.Name)));
                }
                break;
            case "RecoilPhysical":
                if (user.IsActive && !Has(user, "Magic Guard") && !leaving && physical)
                {
                    LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, param), Text.Of("battle.wasHurtBy", ("user", user.Name), ("target", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                }
                break;
            case "RecoilSpecial":
                if (user.IsActive && !Has(user, "Magic Guard") && !physical)
                {
                    LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, param), Text.Of("battle.wasHurtBy", ("user", user.Name), ("target", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                }
                break;
            case "HpRestoreSe":
                if (t.IsActive && hit.Damage.IsSuperEffective)
                {
                    RestoreHp(t, Formulas.Divide(t.Pokemon!.MaxHP, param), Text.Of("battle.restoredItsHealthUsing", ("target", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                }
                break;
        }
    }

    // ---------------------------------------------------------------- a Destiny Knot

    /// <summary>
    /// <c>HOLD_EFFECT_RECIPROCATE_INFAT</c> (in <c>BattleSystem_TriggerPrimaryEffect</c>): whoever made the knot's
    /// holder fall in love falls in love with it, by Attract's own rules (the other gender; Oblivious says so;
    /// never twice over).
    /// </summary>
    private void Reciprocate(Battler smitten, Battler by)
    {
        if (BattleEffects.ItemInHand(smitten) is not { HoldEffect: "ReciprocateInfat" } knot || !by.IsActive || by == smitten) return;
        if (by.Ability is { Effect.BlocksInfatuation: true })
        {
            Say(Text.Of("battle.preventsRomance", ("target", by.Name), ("ability", AbilityOf(by))));
            return;
        }
        var mine = smitten.Pokemon!.Gender;
        var theirs = by.Pokemon!.Gender;
        if (mine == theirs || mine == Gender.Genderless || theirs == Gender.Genderless || by.Volatile.InLoveWith != null) return;
        by.Volatile.InLoveWith = smitten.Place;
        Say(Text.Of("battle.madeFallInLove", ("pokemon", smitten.Name), ("item", ItemAtWork(smitten)), ("attacker", by.Name))).With(Passing(by));
    }

    // ---------------------------------------------------------------- the prize

    private int prizeMoneyMul = 1;

    /// <summary>What a trainer battle won pays: the trainers' prize money, doubled once anyone on the field has held an Amulet Coin or a Luck Incense.</summary>
    public int PrizeMoney => IsTrainerBattle ? Trainers.Sum(t => t.PrizeMoney) * prizeMoneyMul : 0;

    /// <summary><c>SWITCH_IN_CHECK_STATE_AMULET_COIN</c>: the item itself is read, on either side, and the prize is doubled for the battle.</summary>
    private void CheckAmuletCoin()
    {
        if (AllBattlers.Any(b => b.IsActive && b.Pokemon!.HeldItem?.HoldEffect == HeldItemEffects.MoneyUp)) prizeMoneyMul = 2;
    }

    // ---------------------------------------------------------------- the bag

    /// <summary>The use parameters of <c>items.json</c> the battle carries out (<c>BtlCmd_UseBagItem</c>); the friendship ones are allowed and left to plan 06 · R10.</summary>
    private static readonly HashSet<string> UseRuns = new()
    {
        "hpRestored", "healSleep", "healPoison", "healBurn", "healFreeze", "healParalysis", "healConfusion", "healAttract",
        "revive", "ppRestored", "restorePPAllMoves",
        "atkStages", "defStages", "speedStages", "spatkStages", "spdefStages", "accStages", "critStages", "guardSpec",
        "friendshipLow", "friendshipMed", "friendshipHigh"
    };

    private static readonly HashSet<string> FriendshipKeys = new() { "friendshipLow", "friendshipMed", "friendshipHigh" };

    /// <summary>
    /// Whether the battle knows what to do with an item from the bag: a Poké Ball, one of the two that get the
    /// player away (a Poké Doll, a Fluffy Tail), or medicine and a battle item whose every use parameter it runs.
    /// The coverage report asks this.
    /// </summary>
    public static bool CanUseInBattle(ItemData item)
    {
        if (item.Pocket == ItemPocket.PokeBalls) return true;
        if (item.BattleUse == "Escaping") return true;
        // The berries that heal show in the battle's pockets too (their battle pocket; plan 06 · R11)
        if (!(item.CanUseInBattle || item.BattlePocket != null) || item.Use == null) return false;
        return item.Use.Keys.All(UseRuns.Contains) && item.Use.Keys.Any(k => !FriendshipKeys.Contains(k));
    }

    /// <summary>The Pokémon an item from the bag is used on, and its place on the field if it stands there: the party member named, or the Pokémon whose menu was open.</summary>
    private (Pokemon? Target, Battler? Where) TargetOfItem(Battler user, BattleChoice choice)
    {
        var target = choice.SwitchTo >= 0 && choice.SwitchTo < PlayerParty.Members.Count ? PlayerParty.Members[choice.SwitchTo] : user.Pokemon;
        if (target == null) return (null, null);
        var where = AllBattlers.FirstOrDefault(b => b.Pokemon == target && b.IsActive);
        return (target, where);
    }

    /// <summary>What a medicine's HP code comes to (the original's: all, half or a quarter of the Pokémon's HP below zero).</summary>
    private static int HpAmount(int code, Pokemon p) => code switch
    {
        -1 => p.MaxHP,
        -2 => Math.Max(1, p.MaxHP / 2),
        -3 => Math.Max(1, p.MaxHP / 4),
        _ => Math.Max(0, code)
    };

    private static readonly (string Key, StatusCondition[] Cures)[] StatusKeys =
    {
        ("healSleep", new[] { StatusCondition.Sleep }), ("healPoison", new[] { StatusCondition.Poison, StatusCondition.Toxic }),
        ("healBurn", new[] { StatusCondition.Burn }), ("healFreeze", new[] { StatusCondition.Freeze }), ("healParalysis", new[] { StatusCondition.Paralyze })
    };

    private static readonly (string Key, StatType Stat)[] StageKeys =
    {
        ("atkStages", StatType.Attack), ("defStages", StatType.Defense), ("speedStages", StatType.Speed),
        ("spatkStages", StatType.SpAttack), ("spdefStages", StatType.SpDefense), ("accStages", StatType.Accuracy)
    };

    /// <summary>The move an Ether or Elixir is for: the one named, or the first with PP missing.</summary>
    private static Move? MoveForPp(Pokemon p, int index) =>
        index >= 0 && index < p.Moves.Count ? p.Moves[index] : p.Moves.FirstOrDefault(m => m.CurrentPP < m.MaxPP);

    /// <summary>What a battle of the Battle Frontier says of the Bag (plan 06 · R18a): nothing comes out of it there.</summary>
    public static string FrontierBagRefusal => Text.Of("battle.nothingFromTheBag");

    /// <summary>What a battle with no items says when one is reached for (plan 14 · B14): a line of our own.</summary>
    public static string NoItemsRefusal => Text.Of("battle.noItemsMayBe");

    /// <summary>
    /// Why an item from the bag can't be used now; null when it can (<c>BattleSystem_CheckItemUsability</c> and the
    /// bag's own ABLE and NOT ABLE): a Ball in a trainer battle or at two foes, an item the battle doesn't know,
    /// a Poké Doll against a trainer, a Pokémon under an Embargo, and an item that would do nothing to the Pokémon
    /// it is for.
    /// </summary>
    private string? WhyNotItem(Battler b, BattleChoice choice)
    {
        var item = ItemDatabase.Get(choice.Item ?? "");
        if (item == null) return Text.Of("battle.thereIsNoItem", ("item", choice.Item));
        // PLAYER_INPUT_ITEM in the Battle Frontier
        if (Frontier) return FrontierBagRefusal;
        // A Master Trainer's battle (plan 14 · B14): nothing from the bag, for either side
        if (NoBattleItems) return NoItemsRefusal;
        // The Great Marsh and Pal Park have their own balls and nothing else
        if (Kind is BattleKind.Safari or BattleKind.PalPark)
            return item.Name != "Safari Ball" ? Text.Of("battle.onlyTheBallsYou") : SpecialBalls <= 0 ? Text.Of("battle.youHaveNoBalls") : null;
        if (item.Name == "Safari Ball") return Text.Of("battle.safariBallsAreFor");
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            if (IsTrainerBattle) return Text.Of("battle.theTrainerBlockedThe");
            if (EnemySlots.Count(f => f.IsActive) > 1) return Text.Of("battle.thereAreTwoPokemon");
            return null;
        }
        if (!CanUseInBattle(item)) return Text.Of("battle.thereATimeAnd");
        if (item.BattleUse == "Escaping") return IsTrainerBattle ? Text.Of("battle.itCantBeUsed") : null;

        var (target, where) = TargetOfItem(b, choice);
        if (target == null) return Text.Of("battle.thereIsNoPokemonTo");
        if ((where ?? b).Volatile.EmbargoTurns > 0) return Text.Of("battle.itemsCantBeUsed", ("pokemon", (where ?? b).Name));
        return ItemWouldHelp(item, target, where, b, choice.Move) ? null : Text.Of("battle.itWontHaveAny");
    }

    /// <summary>Whether using the item on this Pokémon now would change anything.</summary>
    private bool ItemWouldHelp(ItemData item, Pokemon target, Battler? where, Battler user, int moveIndex)
    {
        var use = item.Use ?? new Dictionary<string, int>();
        if (target.IsEgg) return false;
        if (use.ContainsKey("revive")) return target.IsFainted;
        if (target.IsFainted) return false;
        if (use.ContainsKey("hpRestored") && target.CurrentHP < target.MaxHP) return true;
        if (StatusKeys.Any(s => use.ContainsKey(s.Key) && s.Cures.Contains(target.Status))) return true;
        if (use.ContainsKey("healConfusion") && where is { IsConfused: true }) return true;
        if (use.ContainsKey("healAttract") && where?.Volatile.InLoveWith != null) return true;
        if (use.ContainsKey("restorePPAllMoves") && target.Moves.Any(m => m.CurrentPP < m.MaxPP)) return true;
        if (use.ContainsKey("ppRestored") && !use.ContainsKey("restorePPAllMoves") && MoveForPp(target, moveIndex) is { } move && move.CurrentPP < move.MaxPP) return true;
        if (where == user)
        {
            if (StageKeys.Any(s => use.ContainsKey(s.Key) && target.StatStages.GetValueOrDefault(s.Stat) < 6)) return true;
            if (use.ContainsKey("critStages") && !user.Volatile.FocusEnergy) return true;
            if (use.ContainsKey("guardSpec") && !Field.Side(user.Side).Mist) return true;
        }
        return false;
    }

    /// <summary>
    /// The player uses an item from the bag (<c>BtlCmd_UseBagItem</c>): a Ball is thrown; a Poké Doll or a Fluffy
    /// Tail ends a wild battle; medicine is used on the Pokémon it is for, on the field or on the bench (its HP,
    /// its condition, its PP); a battle item raises a stat of the Pokémon whose menu was open, gives it the focus
    /// of a Dire Hit, or puts up a Guard Spec.'s mist over its side.
    /// </summary>
    private void ExecuteItemUse(Battler user, Act act)
    {
        var item = act.Item!;
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            ThrowBall(item);
            return;
        }

        var used = Say(Text.Of("battle.usedThe", ("trainer", user.Trainer?.FullTitle ?? playerName), ("item", item.DisplayName)));
        if (item.BattleUse == "Escaping")
        {
            if (IsTrainerBattle) return;
            // subscript_escape_item plays its animation on the player's Pokémon before the line that follows (plan 25 · Y3)
            used.With(Moment(user, BattleMoment.EscapeItem));
            Say(Text.Of("battle.gotAwaySafely")).With(new GotAway());
            End(BattleResult.PlayerRan);
            return;
        }

        var (target, where) = TargetOfItem(user, act.Choice);
        if (target == null) return;
        // An item used on a Pokémon that stands on the field shows on it; one used on the bench has nowhere to show
        if (where != null) used.With(Moment(where, BattleMoment.BagItem));
        var use = item.Use ?? new Dictionary<string, int>();
        string name = where?.Name ?? target.DisplayName;

        if (use.ContainsKey("revive"))
        {
            if (!target.IsFainted) return;
            target.Revive(HpAmount(use.GetValueOrDefault("hpRestored", -2), target));
            Say(Text.Of("battle.wasRevived", ("pokemon", name)));
            return;
        }
        if (target.IsFainted) return;

        if (use.TryGetValue("hpRestored", out int code) && target.CurrentHP < target.MaxHP)
        {
            int before = target.CurrentHP;
            int after = Math.Min(target.MaxHP, before + HpAmount(code, target));
            if (where != null) RestoreHp(where, after - before, Text.Of("battle.recoveredHp", ("pokemon", name), ("amount", after - before)));
            else
            {
                target.CurrentHP = after;
                Say(Text.Of("battle.recoveredHp", ("pokemon", name), ("amount", after - before)));
            }
        }

        foreach (var (key, cures) in StatusKeys)
        {
            if (!use.ContainsKey(key) || !cures.Contains(target.Status)) continue;
            string status = BattleText.StatusName(target.Status);
            if (where != null) CureStatus(where, Text.Of("battle.wasCuredOf", ("pokemon", name), ("status", status)));
            else
            {
                target.Status = StatusCondition.None;
                target.SleepTurns = 0;
                target.ToxicCounter = 0;
                Say(Text.Of("battle.wasCuredOf", ("pokemon", name), ("status", status)));
            }
            break;
        }
        if (use.ContainsKey("healConfusion") && where is { IsConfused: true })
        {
            where.ConfusionTurns = 0;
            Say(Text.Of("battle.snappedOutOfIts", ("pokemon", name))).With(Passing(where));
        }
        if (use.ContainsKey("healAttract") && where?.Volatile.InLoveWith != null)
        {
            where.Volatile.InLoveWith = null;
            Say(Text.Of("battle.gotOverItsInfatuation", ("pokemon", name))).With(Passing(where));
        }

        if (use.ContainsKey("restorePPAllMoves"))
        {
            int pp = use.GetValueOrDefault("ppRestored", -1);
            foreach (var move in target.Moves) move.CurrentPP = pp < 0 ? move.MaxPP : Math.Min(move.MaxPP, move.CurrentPP + pp);
            Say(Text.Of("battle.thePpOfMoves", ("pokemon", name)));
        }
        else if (use.TryGetValue("ppRestored", out int pp) && MoveForPp(target, act.Choice.Move) is { } move && move.CurrentPP < move.MaxPP)
        {
            move.CurrentPP = pp < 0 ? move.MaxPP : Math.Min(move.MaxPP, move.CurrentPP + pp);
            Say(Text.Of("battle.hadItsPpRestored", ("pokemon", name), ("move", move.DisplayName)));
        }

        if (where == user)
        {
            foreach (var (key, stat) in StageKeys)
                if (use.ContainsKey(key)) ChangeStat(user, stat, 1, user, announceFailure: true, By.Other);
            if (use.ContainsKey("critStages") && !user.Volatile.FocusEnergy)
            {
                user.Volatile.FocusEnergy = true;
                Say(Text.Of("battle.isGettingPumped", ("user", user.Name)));
            }
            if (use.ContainsKey("guardSpec") && !Field.Side(user.Side).Mist)
            {
                Field.Side(user.Side).MistTurns = 5;
                Say(Text.Of("battle.becameShroudedInMist", ("team", TeamOf(user.Side, capital: true)))).With(Now(user.Side, SideCondition.Mist));
            }
        }
    }
}
