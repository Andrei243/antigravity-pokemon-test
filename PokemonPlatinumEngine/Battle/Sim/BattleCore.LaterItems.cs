using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later games' held items in the battle's own course (plan 06 · R28a), by hold effect, each where Pokémon
// Showdown's data/items.ts at the pinned commit has it: what a move's user holds as the move is made (a Punching
// Glove, Protective Pads, a Gem), what answers a hit (Weakness Policy, a Rocky Helmet, an Air Balloon popping, the
// items and berries that raise a stat by the hit's type or kind), what goes off once the move is done (Throat Spray,
// Blunder Policy, a Mirror Herb, an Eject Button, a Red Card, an Eject Pack), and Clear Amulet, Heavy-Duty Boots, Room
// Service and the Binding Band in their rules. The hooks are in Battle/Effects/HeldItemEffects.Later.cs.
public sealed partial class BattleCore
{
    /// <summary>A move is under way: what an Eject Pack and a Mirror Herb note waits for its end.</summary>
    private bool itemsArmed;

    private readonly List<Battler> ejectButtons = new(), ejectPacks = new();
    private (Battler Holder, Battler Attacker)? redCard;
    private readonly List<(Battler Holder, StatType Stat, int Stages)> mirrored = new();

    private static bool Holds(Battler b, string hold) => LaterItems.Holds(b, hold);

    // ---------------------------------------------------------------- the move as its user makes it

    /// <summary>
    /// A Punching Glove takes the touch out of its holder's punches and Protective Pads out of every move it makes
    /// (Showdown's <c>onModifyMove</c> and <c>checkMoveMakesContact</c>), so nothing that answers a touch answers them.
    /// </summary>
    private void ShapedByItem(MoveUse use)
    {
        var user = use.User;
        user.Turn.GemBoosted = false;
        itemsArmed = true;
        var data = use.Move.Data;
        if (!data.MakesContact) return;
        string? hold = BattleEffects.HoldEffectOf(user);
        if (hold == "ProtectivePads" || (hold == "PunchingGlove" && (data.Flags & MoveFlags.Punch) != 0))
            use.Move = new Move(data.CopyWith(d => d.Flags &= ~MoveFlags.Contact), use.Move.CurrentPP);
    }

    /// <summary>
    /// A Gem of the move's type goes off as the move's first hit is worked out (<c>onSourceTryPrimaryHit</c>): it is
    /// used up and the move is 5325/4096 as strong against every target; not for a status move, Struggle, a Z-Move or
    /// a Max Move, nor a move of a fixed amount.
    /// </summary>
    private void GemGoesOff(MoveUse use)
    {
        var user = use.User;
        if (user.Turn.GemBoosted || use.Data == StruggleData || use.Data.Kind != MoveKind.Standard || use.Move.Category == MoveCategory.Status) return;
        if (LaterItems.TypeOf(BattleEffects.HoldEffectOf(user), LaterItems.Gem) is not { } type || type != use.Move.Type) return;
        string item = ItemAtWork(user);
        ConsumeItem(user);
        user.Turn.GemBoosted = true;
        Say(Text.Of("battle.gemStrengthened", ("item", item), ("move", use.Move.DisplayName)));
    }

    // ---------------------------------------------------------------- a hit answered

    /// <summary>The later items that answer a hit their holder took itself (<c>onDamagingHit</c>), from <see cref="ItemsOnHit"/>.</summary>
    private void LaterItemOnHit(MoveUse use, MoveHit hit, ItemData item)
    {
        var t = hit.Target;
        var user = use.User;
        var move = use.Move;
        string hold = item.HoldEffect!;
        if (t == user) return;
        switch (hold)
        {
            case "AirBalloon":
                if (!t.IsActive) break;
                Say(Text.Of("battle.balloonPopped", ("pokemon", t.Name), ("item", ItemOf(t, item))));
                ConsumeItem(t);
                break;
            case "RockyHelmet":
                // A sixth of the attacker's HP for a touch, past nothing but Magic Guard
                if (user.IsActive && use.Data.MakesContact && !BattleEffects.Of(user).Any(e => e.PreventsIndirectDamage))
                    LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, Math.Max(1, item.HoldParam)), Text.Of("battle.wasHurtBy", ("user", user.Name), ("target", t.Name), ("item", ItemAtWork(t))));
                break;
            case "WeaknessPolicy":
                // Not for a hit of a fixed amount (move.damage, damageCallback)
                if (t.IsActive && hit.Damage.IsSuperEffective && use.Effect.Deals == null)
                {
                    Say(Text.Of("battle.usedItsItem", ("pokemon", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                    ChangeStat(t, StatType.Attack, item.HoldParam, t, false, By.Other);
                    ChangeStat(t, StatType.SpAttack, item.HoldParam, t, false, By.Other);
                }
                break;
            case "KeeBerry" or "MarangaBerry":
                if (t.IsActive && move.Category == (hold == "KeeBerry" ? MoveCategory.Physical : MoveCategory.Special))
                {
                    Say(Text.Of("battle.ateIts", ("pokemon", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                    ChangeStat(t, hold == "KeeBerry" ? StatType.Defense : StatType.SpDefense, Math.Max(1, item.HoldParam), t, false, By.Other);
                }
                break;
            case "RedCard":
                if (t.IsActive && user.IsActive && redCard == null) redCard = (t, user);
                break;
            case "EjectButton":
                if (t.IsActive && !ejectButtons.Contains(t)) ejectButtons.Add(t);
                break;
            default:
                if (LaterItems.RaisedByAHit(hold) is { } by && t.IsActive && move.Type == by.Type)
                {
                    Say(Text.Of("battle.usedItsItem", ("pokemon", t.Name), ("item", ItemAtWork(t))));
                    ConsumeItem(t);
                    ChangeStat(t, by.Stat, Math.Max(1, item.HoldParam), t, false, By.Other);
                }
                break;
        }
    }

    // ---------------------------------------------------------------- a stage changed

    /// <summary>Clear Amulet (<c>onTryBoost</c>): no other Pokémon lowers its holder's stats; said when the move was for that alone.</summary>
    private bool ClearAmuletKeeps(Battler target, bool announce)
    {
        if (!Holds(target, "ClearAmulet")) return false;
        if (announce) Say(Text.Of("battle.clearAmulet", ("pokemon", target.Name), ("item", ItemOf(target))));
        return true;
    }

    /// <summary>A stage changed while a move is under way: an Eject Pack notes a drop of its holder's, a foe's Mirror Herb a rise (each for the move's end).</summary>
    private void StagesNoted(Battler target, StatType stat, int change)
    {
        if (!itemsArmed || change == 0) return;
        if (change < 0 && Holds(target, "EjectPack") && !ejectPacks.Contains(target)) ejectPacks.Add(target);
        if (change > 0)
            foreach (var foe in SlotsOf(Other(target.Side)).Where(b => b.IsActive && Holds(b, "MirrorHerb")))
                mirrored.Add((foe, stat, change));
    }

    // ---------------------------------------------------------------- the move done

    /// <summary>
    /// Once the move is done (<c>onAfterMoveSecondary</c> and its kin): Throat Spray after a sound move, Blunder Policy
    /// after a miss, a Mirror Herb with the stages it saw rise, then whoever leaves: an Eject Button's holder (and its
    /// attacker's U-turn is called off), else a Red Card's attacker, dragged out, else an Eject Pack's holder.
    /// </summary>
    private IEnumerable<BattleRequest> ItemsAfterTheMove(MoveUse use)
    {
        itemsArmed = false;
        var user = use.User;
        var data = use.Data;
        if (Result != BattleResult.None)
        {
            ForgetItemsAfterTheMove();
            yield break;
        }

        // Throat Spray: a sound move that did something
        bool failed = user.Pokemon != null && failedOnTurn.TryGetValue(user.Pokemon, out int when) && when == Turn;
        if (user.IsActive && (data.Flags & MoveFlags.Sound) != 0 && Holds(user, "ThroatSpray") && !failed)
            LaterItems.GoesOff(this, user, StatType.SpAttack, Math.Max(1, BattleEffects.HoldParamOf(user)));
        // Blunder Policy: a move that missed by its accuracy (not a one-hit knockout, which decides for itself)
        if (user.IsActive && Holds(user, "BlunderPolicy") && use.Effect.Decides == null && use.Hits.Any(h => h.Missed && h.FailLine == null))
            LaterItems.GoesOff(this, user, StatType.Speed, Math.Max(1, BattleEffects.HoldParamOf(user)));
        // A Mirror Herb takes on what its foe gained
        foreach (var holder in mirrored.Select(m => m.Holder).Distinct().ToList())
        {
            if (!holder.IsActive || !Holds(holder, "MirrorHerb")) continue;
            Say(Text.Of("battle.mirroredStats", ("pokemon", holder.Name), ("item", ItemAtWork(holder))));
            ConsumeItem(holder);
            foreach (var (_, stat, stages) in mirrored.Where(m => m.Holder == holder)) ChangeStat(holder, stat, stages, holder, false, By.Other);
        }

        var buttons = ejectButtons.ToList();
        var card = redCard;
        var packs = ejectPacks.ToList();
        ForgetItemsAfterTheMove();

        foreach (var holder in buttons)
        {
            if (!holder.IsActive || !Holds(holder, "EjectButton") || !HasReplacement(holder)) continue;
            Say(Text.Of("battle.ejectButton", ("pokemon", holder.Name), ("item", ItemAtWork(holder))));
            ConsumeItem(holder);
            use.Leaves = Leaving.No;
            foreach (var request in SwitchOut(holder)) yield return request;
            yield break;
        }

        if (card is { } c && c.Holder is var cardHolder && c.Attacker is var attacker && cardHolder.IsActive && attacker.IsActive && Holds(cardHolder, "RedCard") && HasReplacement(attacker))
        {
            // Used up even against a Pokémon that can't be made to leave
            Say(Text.Of("battle.heldUpRedCard", ("target", cardHolder.Name), ("item", ItemAtWork(cardHolder)), ("user", attacker.Name)));
            ConsumeItem(cardHolder);
            if (!attacker.Volatile.Ingrained && !BattleEffects.Of(attacker).Any(e => e.HoldsItsGround))
            {
                if (attacker == user) use.Leaves = Leaving.No;
                DragOut(attacker);
            }
            yield break;
        }

        foreach (var holder in packs)
        {
            if (!holder.IsActive || !Holds(holder, "EjectPack") || !HasReplacement(holder)) continue;
            // A U-turn's user leaving by its own move goes as it would have (Showdown: no pack while another is leaving)
            if (holder != user && use.Leaves != Leaving.No) continue;
            Say(Text.Of("battle.ejectPack", ("pokemon", holder.Name), ("item", ItemAtWork(holder))));
            ConsumeItem(holder);
            if (holder == user) use.Leaves = Leaving.No;
            foreach (var request in SwitchOut(holder)) yield return request;
            yield break;
        }
    }

    /// <summary>Nothing the move noted goes on to another.</summary>
    private void ForgetItemsAfterTheMove()
    {
        itemsArmed = false;
        ejectButtons.Clear();
        ejectPacks.Clear();
        redCard = null;
        mirrored.Clear();
    }

    /// <summary>The Pokémon leaves its place by its item, and whoever chooses for its side picks who comes in.</summary>
    private IEnumerable<BattleRequest> SwitchOut(Battler place)
    {
        foreach (var request in PickReplacement(place)) yield return request;
        if (picked == null) yield break;
        var incoming = picked;
        Withdraw(place);
        Emit(new Recalled(place.Place));
        Emit(new Left(place.Place));
        Arrive(place, incoming, SentOutLine(place, incoming));
    }

    // ---------------------------------------------------------------- in their rules

    /// <summary>
    /// Heavy-Duty Boots (Showdown's hazards' <c>onEntryHazard</c>): its holder walks over Spikes, Toxic Spikes, Stealth
    /// Rock, Sticky Web and G-Max Steelsurge; a Poison type on the ground still takes the Toxic Spikes up.
    /// </summary>
    private bool WalksInBoots(Battler b)
    {
        if (!b.IsActive || !Holds(b, "HeavyDutyBoots")) return false;
        var side = Field.Side(b.Side);
        if (side.ToxicSpikes > 0 && IsOnTheGround(b) && b.HasType(PokemonType.Poison))
        {
            side.ToxicSpikes = 0;
            Say(Text.Of("battle.thePoisonSpikesDisappeared", ("team", TeamOf(b.Side)))).With(Now(b.Side, SideCondition.ToxicSpikes));
        }
        return true;
    }

    /// <summary>Room Service (<c>onAnyPseudoWeatherChange</c>): everyone holding one is a step slower once Trick Room is twisted up.</summary>
    private void RoomServiceAnswers()
    {
        foreach (var b in BySpeed().Where(b => b.IsActive && Holds(b, "RoomService")).ToList()) LaterItems.GoesOff(this, b, StatType.Speed, -1);
    }

    /// <summary>A Binding Band in the binder's hands makes a binding move take a sixth each turn (Showdown's <c>boundDivisor</c>); nought leaves the rules' own.</summary>
    private static int BindDivisorOf(Battler binder) => Holds(binder, "BindingBand") ? Math.Max(1, BattleEffects.HoldParamOf(binder)) : 0;
}
