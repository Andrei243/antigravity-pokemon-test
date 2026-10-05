using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The moves of their own (plan 06 · R5): those that call or copy another move, take, swap, throw or eat an item,
// change a type or an ability, swap stats or stat changes, and the few left over (Follow Me, Helping Hand, Spite,
// Chatter). Each was written from its command in the original's battle_script.c (BtlCmd_*), its effect script and
// the subscript it points to (res/battle/scripts), named beside it. What a move changes for the battle alone (a
// shape, a move, an ability, swapped stats) is kept in Volatiles.Original and put back when the Pokémon leaves
// (BattleCore.Switch.cs, Revert); an item knocked off comes back with the battle's end.
public sealed partial class BattleCore
{
    /// <summary>The moves that call another: none of them can be called, copied or talked in one's sleep (<c>Move_IsInvoker</c>).</summary>
    private static readonly HashSet<string> Invokers = new() { "Sleep Talk", "Copycat", "Assist", "Me First", "Mirror Move", "Metronome" };

    /// <summary>What Mimic can't copy: the first part of the original's <c>sCannotMetronomeMoves</c>.</summary>
    private static readonly HashSet<string> NotMimicked = new() { "Metronome", "Struggle", "Sketch", "Mimic", "Chatter" };

    /// <summary>What Metronome, Assist and Copycat can't call: the whole of <c>sCannotMetronomeMoves</c>.</summary>
    private static readonly HashSet<string> NotCalled = new(NotMimicked)
    {
        "Sleep Talk", "Assist", "Mirror Move", "Counter", "Mirror Coat", "Protect", "Detect", "Endure", "Destiny Bond", "Thief",
        "Follow Me", "Snatch", "Helping Hand", "Covet", "Trick", "Focus Punch", "Feint", "Copycat", "Me First", "Switcheroo"
    };

    /// <summary>The effects Me First can't copy (<c>sCannotMeFirstMoves</c>): Counter, Mirror Coat, Thief and Covet, Focus Punch, Chatter.</summary>
    private static readonly HashSet<string> NotMeFirst = new() { "Counter", "MirrorCoat", "StealHeldItem", "HitLastWhiffIfHit", "Chatter" };

    /// <summary>The effects Mirror Move leaves alone, which it shares with Encore (<c>sCannotEncoreMoves</c>).</summary>
    private static readonly HashSet<string> NotMirrored = new() { "Transform", "CopyMoveForBattle", "LearnMovePermanent", "CopyMove", "Encore", "Struggle" };

    /// <summary>The effects that take more than a turn (<c>Move_IsMultiTurn</c>): Sleep Talk can't call them, and Conversion 2 waits on one.</summary>
    private static readonly HashSet<string> MultiTurn = new()
    {
        "Bide", "ChargeTurnHighCrit", "ChargeTurnHighCritFlinch", "ChargeTurnDefUp", "SkipChargeTurnInSun", "Fly", "Dive", "Dig", "Bounce", "ShadowForce"
    };

    /// <summary>Platinum's 467 moves in their order, which Metronome draws from.</summary>
    private static readonly MoveData[] PlatinumMoves =
        MoveDatabase.GetAll().Where(m => m.Id is >= 1 and <= 467 && m.Kind == MoveKind.Standard).OrderBy(m => m.Id).ToArray();

    private static readonly Dictionary<string, MoveEffect> UniqueEffects = new()
    {
        // ---- Calling and copying moves
        ["CallRandomMove"] = new() { Does = (b, use) => b.Metronome(use) },
        ["CopyMove"] = new() { Does = (b, use) => b.MirrorMove(use) },
        ["UseRandomLearnedMoveSleep"] = new() { Does = (b, use) => b.SleepTalk(use) },
        ["UseRandomAllyMove"] = new() { Does = (b, use) => b.Assist(use) },
        ["UseLastUsedMove"] = new() { Does = (b, use) => b.Copycat(use) },
        ["UseMoveFirst"] = new() { Does = (b, use) => b.MeFirst(use) },
        ["CopyMoveForBattle"] = new() { Does = (b, use) => b.OnEach(use, t => b.Mimic(use, t)) },
        ["LearnMovePermanent"] = new() { Does = (b, use) => b.OnEach(use, t => b.Sketch(use, t)) },
        ["ApplyMagicCoat"] = new() { Does = (b, use) => b.Shroud(use, snatch: false) },
        ["StealStatusMove"] = new() { Does = (b, use) => b.Shroud(use, snatch: true) },

        // ---- Items
        ["StealHeldItem"] = new() { Hit = (b, use, hit) => b.Steal(use, hit) },
        ["RemoveHeldItem"] = new()
        {
            // Knock Off: by the modern rules, half as strong again against an item it can knock off
            Power = (b, use, t) => b.Rules.KnockOffStrongerOnItem && CanKnockOff(t) ? 15 : 10,
            Hit = (b, use, hit) => b.KnockOff(use, hit)
        },
        ["SwitchHeldItems"] = new() { Does = (b, use) => b.OnEach(use, t => b.Trick(use, t)) },
        ["Recycle"] = new() { Does = (b, use) => b.Recycle(use) },
        ["Fling"] = new()
        {
            Start = (b, use) => b.Fling(use),
            BasePower = (b, use, t) => ((ItemData)use.Scratch!).FlingPower,
            Hit = (b, use, hit) => b.Flung(use, hit),
            Miss = (b, use, hit) => b.FlingDone(use)
        },
        ["NaturalGift"] = new() { Start = (b, use) => b.NaturalGift(use), BasePower = (b, use, t) => (int)use.Scratch! },
        ["EatBerry"] = new() { Hit = (b, use, hit) => b.Pluck(use, hit) },

        // ---- Types and abilities
        ["Conversion"] = new() { Does = (b, use) => b.Conversion(use) },
        ["Conversion2"] = new() { Does = (b, use) => b.Conversion2(use) },
        ["Transform"] = new() { Does = (b, use) => b.OnEach(use, t => b.Transform(use, t)) },
        ["CopyAbility"] = new() { Does = (b, use) => b.OnEach(use, t => b.RolePlay(use, t)) },
        ["SwitchAbilities"] = new() { Does = (b, use) => b.OnEach(use, t => b.SkillSwap(use, t)) },
        ["SetAbilityToInsomnia"] = new() { Does = (b, use) => b.OnEach(use, t => b.WorrySeed(use, t)) },

        // ---- Stats and stat changes
        ["SwapAtkDef"] = new() { Does = (b, use) => b.PowerTrick(use) },
        ["SwapAtkSpAtkStatChanges"] = new() { Does = (b, use) => b.OnEach(use, t => b.SwapStages(use, t, "Attack and Sp. Atk", StatType.Attack, StatType.SpAttack)) },
        ["SwapDefSpDefStatChanges"] = new() { Does = (b, use) => b.OnEach(use, t => b.SwapStages(use, t, "Defense and Sp. Def", StatType.Defense, StatType.SpDefense)) },
        ["SwapStatChanges"] = new() { Does = (b, use) => b.OnEach(use, t => b.HeartSwap(use, t)) },

        // ---- The rest
        ["MakeGlobalTarget"] = new() { Does = (b, use) => b.FollowMe(use) },
        ["BoostAllyPowerBy50Percent"] = new() { Does = (b, use) => b.HelpingHand(use) },
        ["DecreaseLastMovePp"] = new() { Does = (b, use) => b.OnEach(use, t => b.Spite(use, t)) },
        ["Chatter"] = new() { Hit = (b, use, hit) => b.Chatter(use, hit) },

        // ---- The battlefield (plan 06 · R6)
        ["Camouflage"] = new() { Does = (b, use) => b.Camouflage(use) },
        ["NaturePower"] = new() { Does = (b, use) => b.NaturePower(use) },
        ["SecretPower"] = new() { Hit = (b, use, hit) => b.SecretPower(use, hit) }
    };

    /// <summary>
    /// The original's tables of the ground (<c>include/data/terrain</c>): what Camouflage turns its user into,
    /// what Nature Power becomes and what Secret Power does, on each. Anything past the list is "special".
    /// </summary>
    private static readonly Dictionary<BattleTerrain, (PokemonType Type, string Move, string Effect)> Terrains = new()
    {
        [BattleTerrain.Plain] = (PokemonType.Ground, "Earthquake", "AccuracyDown"),
        [BattleTerrain.Sand] = (PokemonType.Ground, "Earthquake", "AccuracyDown"),
        [BattleTerrain.Grass] = (PokemonType.Grass, "Seed Bomb", "Sleep"),
        [BattleTerrain.Puddle] = (PokemonType.Grass, "Seed Bomb", "Sleep"),
        [BattleTerrain.Mountain] = (PokemonType.Rock, "Rock Slide", "Flinch"),
        [BattleTerrain.Cave] = (PokemonType.Rock, "Rock Slide", "Flinch"),
        [BattleTerrain.Snow] = (PokemonType.Ice, "Blizzard", "Freeze"),
        [BattleTerrain.Water] = (PokemonType.Water, "Hydro Pump", "AttackDown"),
        [BattleTerrain.Ice] = (PokemonType.Ice, "Ice Beam", "Freeze"),
        [BattleTerrain.Building] = (PokemonType.Normal, "Tri Attack", "Paralyze"),
        [BattleTerrain.GreatMarsh] = (PokemonType.Ground, "Mud Bomb", "SpeedDown"),
        [BattleTerrain.Bridge] = (PokemonType.Flying, "Air Slash", "EvasionDown"),
        [BattleTerrain.Special] = (PokemonType.Normal, "Tri Attack", "Paralyze")
    };

    /// <summary>What the ground this battle is fought on comes to in the tables.</summary>
    private (PokemonType Type, string Move, string Effect) Ground =>
        Terrains.TryGetValue(Conditions.Terrain, out var ground) ? ground : Terrains[BattleTerrain.Special];

    /// <summary>Whether Metronome, Assist or Copycat may call a move (<c>Move_CanBeMetronomed</c>): not one of the list, nor one Gravity or Heal Block would stop.</summary>
    private bool CanBeCalled(Battler user, MoveData data) =>
        !NotCalled.Contains(data.Name) && !(Field.Gravity && FailsUnderGravity(data)) && !(user.Volatile.HealBlockTurns > 0 && IsHealingMove(data));

    // ---------------------------------------------------------------- calling and copying moves

    /// <summary><c>BtlCmd_Metronome</c>: any of Platinum's moves its user doesn't know and may call, drawn until one comes up.</summary>
    private void Metronome(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        MoveData? pick = null;
        for (int tries = 0; tries < 1000 && pick == null; tries++)
        {
            var drawn = PlatinumMoves[rng.Roll(RollKind.Pick, PlatinumMoves.Length)];
            if (user.Pokemon!.Moves.Any(m => m.Data == drawn) || !CanBeCalled(user, drawn)) continue;
            pick = drawn;
        }
        if (pick == null)
        {
            Fails();
            return;
        }
        CallMove(use, pick, null);
    }

    /// <summary><c>BtlCmd_SetMirrorMove</c>: the last move aimed at its user, unless it is one Encore couldn't hold either.</summary>
    private void MirrorMove(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var copied = user.Volatile.MirrorMove;
        if (copied == null || NotMirrored.Contains(copied.Effect ?? ""))
        {
            Fails();
            return;
        }
        CallMove(use, copied, null);
    }

    /// <summary><c>BtlCmd_TrySleepTalk</c>: one of its user's own moves it could use now but for the PP, drawn evenly; only while asleep.</summary>
    private void SleepTalk(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Pokemon!.Status != StatusCondition.Sleep)
        {
            Fails();
            return;
        }
        var usable = user.Pokemon.Moves
            .Where(m => !Invokers.Contains(m.Name) && m.Name is not ("Focus Punch" or "Uproar" or "Chatter") && !MultiTurn.Contains(m.Data.Effect ?? "")
                        && WhyNotMove(user, m, ignorePp: true) == null)
            .ToList();
        if (usable.Count == 0)
        {
            Fails();
            return;
        }
        CallMove(use, usable[rng.Roll(RollKind.Pick, usable.Count)].Data, null);
    }

    /// <summary><c>BtlCmd_TryAssist</c>: a move of the rest of the party, drawn evenly from all of theirs that may be called.</summary>
    private void Assist(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var party = user.Roster?.Members ?? new List<Pokemon>();
        var pool = party.Where(p => p != user.Pokemon).SelectMany(p => p.Moves).Select(m => m.Data)
            .Where(d => !Invokers.Contains(d.Name) && CanBeCalled(user, d)).ToList();
        if (pool.Count == 0)
        {
            Fails();
            return;
        }
        CallMove(use, pool[rng.Roll(RollKind.Pick, pool.Count)], null);
    }

    /// <summary><c>BtlCmd_TryCopycat</c>: the last move anyone got to use, unless it called another or can't be called.</summary>
    private void Copycat(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (lastMoveShown is not { } last || Invokers.Contains(last.Name) || !CanBeCalled(user, last))
        {
            Fails();
            return;
        }
        CallMove(use, last, null);
    }

    /// <summary>
    /// <c>BtlCmd_TryMeFirst</c>: the damaging move its target is about to use, half as strong again, unless the
    /// target has moved, is struggling, or the move is one of the few Me First can't copy.
    /// </summary>
    private void MeFirst(MoveUse use)
    {
        var user = use.User;
        var t = use.Targets[0];
        use.Line.With(Shown(user, t, use.Move, null));
        var pending = waiting.FirstOrDefault(a => a.User == t && a.Actor == t.Pokemon);
        if (pending == null || pending.Choice.Kind != ChoiceKind.Fight || pending.Move == null)
        {
            Fails();
            return;
        }
        var chosen = pending.Move;
        if (t.Volatile.Encored is { } encored && t.Pokemon!.Moves.FirstOrDefault(m => m.Data == encored) is { } instead) chosen = instead;
        if (chosen.Data == StruggleData || chosen.Category == MoveCategory.Status || NotMeFirst.Contains(chosen.Data.Effect ?? ""))
        {
            Fails();
            return;
        }
        CallMove(use, chosen.Data, t, damageTenths: 15);
    }

    /// <summary>
    /// <c>BtlCmd_TryMimic</c>: the target's last move takes Mimic's place for the rest of the user's time on the
    /// field, with 5 PP (or fewer if the move has fewer): never while transformed, through a Substitute, a move
    /// it already knows, or one of the few that can't be copied.
    /// </summary>
    private bool Mimic(MoveUse use, Battler t)
    {
        var user = use.User;
        var v = user.Volatile;
        var last = t.Volatile.LastMove;
        if (last == null || NotMimicked.Contains(last.Name) || v.Transformed || t.HasSubstitute) return Fails();
        var moves = user.Pokemon!.Moves;
        if (moves.Any(m => m.Data == last)) return Fails();
        int slot = moves.FindIndex(m => m.Name == "Mimic");
        if (slot < 0) return Fails();

        var o = Keep(user);
        o.MoveSlots ??= new Dictionary<int, (MoveData, int)>();
        o.MoveSlots.TryAdd(slot, (moves[slot].Data, moves[slot].CurrentPP));
        moves[slot] = new Move(last, Math.Min(5, last.MaxPP));
        if (last.Name == "Last Resort") v.UsedMoveSlots = 0;
        Say($"{user.Name} learned {last.Name}!");
        return true;
    }

    /// <summary><c>BtlCmd_TrySketch</c>: the target's last move takes Sketch's place for good, with all its PP.</summary>
    private bool Sketch(MoveUse use, Battler t)
    {
        var user = use.User;
        var v = user.Volatile;
        var last = t.Volatile.LastMove;
        if (v.Transformed || last == null || last == StruggleData || last.Name is "Sketch" or "Chatter" || t.HasSubstitute) return Fails();
        var moves = user.Pokemon!.Moves;
        if (moves.Any(m => m.Data == last)) return Fails();
        int slot = moves.FindIndex(m => m.Name == "Sketch");
        if (slot < 0) return Fails();

        moves[slot] = new Move(last);
        if (last.Name == "Last Resort") v.UsedMoveSlots = 0;
        Say($"{user.Name} sketched {last.Name}!");
        return true;
    }

    /// <summary>
    /// Magic Coat and Snatch (<c>BtlCmd_TrySetMagicCoat</c>, <c>BtlCmd_TrySnatch</c>): the user waits for the
    /// rest of the turn, and nothing comes of either for the last Pokémon to act.
    /// </summary>
    private void Shroud(MoveUse use, bool snatch)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        bool othersToAct = waiting.Any(a => a.User.IsActive && a.User.Pokemon == a.Actor);
        if (!othersToAct)
        {
            Fails();
            return;
        }
        if (snatch)
        {
            user.Turn.Snatching = true;
            Say($"{user.Name} waits for a target to make a move!");
        }
        else
        {
            user.Turn.MagicCoat = true;
            Say($"{user.Name} shrouded itself with Magic Coat!");
        }
    }

    // ---------------------------------------------------------------- items

    /// <summary>An item another Pokémon can take: one that is held, wasn't knocked off, and isn't mail (<c>BattleSystem_CanStealItem</c>).</summary>
    private bool CanTake(Battler t) =>
        t.Pokemon?.HeldItem is { } item && !knockedOff.ContainsKey(t.Pokemon) && item.Pocket != ItemPocket.Mail;

    /// <summary>An item Knock Off can knock off: anything held but Arceus's plate and Giratina's orb.</summary>
    private static bool CanKnockOff(Battler t) =>
        t.Pokemon?.HeldItem is { } item && t.Pokemon.AbilityName != "Multitype" && item.Name != "Griseous Orb";

    /// <summary>Sticky Hold keeps the target's item against a move, unless the user's ability breaks through, and says so.</summary>
    private bool KeptByStickyHold(Battler user, Battler t, Move move)
    {
        if (t.Pokemon?.HeldItem == null) return false;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (breaks || !BattleEffects.Of(t).Any(e => e.KeepsItem)) return false;
        Say($"{t.Name}'s {t.Ability!.Name} made {move.Name} ineffective!");
        return true;
    }

    /// <summary>
    /// Thief and Covet (<c>BtlCmd_TryStealItem</c>, after the hit): the target's item becomes the user's, for
    /// good, when the user holds none. Not through a Substitute; not by the opponents' Pokémon under Platinum's
    /// rules; never Arceus's plate or Giratina's orb; Sticky Hold keeps it.
    /// </summary>
    private void Steal(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched) return;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (!user.IsPlayerSide && !Rules.FoesCanTakeItems) return;
        if (knockedOff.ContainsKey(p) || p.AbilityName == "Multitype" || q.AbilityName == "Multitype" || q.HeldItem?.Name == "Griseous Orb") return;
        if (KeptByStickyHold(user, t, use.Move)) return;
        if (p.HeldItem != null || !CanTake(t)) return;

        var item = q.HeldItem!;
        q.HeldItem = null;
        p.HeldItem = item;
        Say($"{user.Name} stole {t.Name}'s {item.Name}!");
    }

    /// <summary>
    /// <c>BtlCmd_TryKnockOff</c>: the target's item is gone for the rest of the battle (and back in its hands
    /// afterwards). Not an item that went off this turn and whose holder hasn't moved yet (<c>subscript_knock_off</c>).
    /// </summary>
    private void KnockOff(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched || !CanKnockOff(t) || t.Volatile.ItemWentOff) return;
        if (KeptByStickyHold(user, t, use.Move)) return;

        var q = t.Pokemon!;
        var item = q.HeldItem!;
        q.HeldItem = null;
        knockedOff[q] = item;
        Say($"{user.Name} knocked off {t.Name}'s {item.Name}!");
    }

    /// <summary>
    /// Trick and Switcheroo (<c>BtlCmd_TrySwapItems</c>): the two items change hands, for good. Not through a
    /// Substitute, not by the opponents' Pokémon under Platinum's rules, not with mail, a knocked-off item,
    /// Arceus's plate or Giratina's orb, not an item that went off this turn before its holder moved, and not
    /// when neither holds anything; Sticky Hold keeps its holder's.
    /// </summary>
    private bool Trick(MoveUse use, Battler t)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (t.HasSubstitute || t.Volatile.ItemWentOff) return Fails();
        if (p.AbilityName == "Multitype" || q.AbilityName == "Multitype" || p.HeldItem?.Name == "Griseous Orb" || q.HeldItem?.Name == "Griseous Orb") return Fails();
        if (!user.IsPlayerSide && !Rules.FoesCanTakeItems) return Fails();
        if (knockedOff.ContainsKey(p) || knockedOff.ContainsKey(q)) return Fails();
        if (p.HeldItem == null && q.HeldItem == null) return Fails();
        if (p.HeldItem?.Pocket == ItemPocket.Mail || q.HeldItem?.Pocket == ItemPocket.Mail) return Fails();
        if (KeptByStickyHold(user, t, use.Move)) return false;

        (p.HeldItem, q.HeldItem) = (q.HeldItem, p.HeldItem);
        Say($"{user.Name} switched items with its target!");
        if (p.HeldItem != null) Say($"{user.Name} obtained one {p.HeldItem.Name}.");
        if (q.HeldItem != null) Say($"{t.Name} obtained one {q.HeldItem.Name}.");
        return true;
    }

    /// <summary><c>BtlCmd_TryRecycle</c>: the item its user used up or threw is back, while it holds nothing else.</summary>
    private void Recycle(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var p = user.Pokemon!;
        if (p.HeldItem != null || user.Volatile.ConsumedItem is not { } item)
        {
            Fails();
            return;
        }
        p.HeldItem = item;
        user.Volatile.ConsumedItem = null;
        Say($"{user.Name} found one {item.Name}!");
    }

    /// <summary>
    /// <c>BtlCmd_TryFling</c>: its user's item is the hit's power, and is thrown whether the move lands or not.
    /// Nothing is thrown under an Embargo, by Arceus, or with Giratina's orb, and an item the table gives no
    /// power to can't be.
    /// </summary>
    private bool Fling(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        if (p.AbilityName == "Multitype" || p.HeldItem?.Name == "Griseous Orb") return Fails();
        var item = BattleEffects.ItemInHand(user);
        if (item == null || item.FlingPower == 0) return Fails();
        use.Scratch = item;
        Say($"{user.Name} flung its {item.Name}!");
        return true;
    }

    /// <summary>What a thrown item does to whoever it hits (<c>BattleSystem_FlingItem</c>), unless it hit a Substitute or the target is under an Embargo; then the item is gone.</summary>
    private void Flung(MoveUse use, MoveHit hit)
    {
        if (use.Scratch is not ItemData item) return;
        var t = hit.Target;
        if (hit.Touched && t.IsActive && t.Volatile.EmbargoTurns == 0 && item.FlingEffect is { } effect) Thrown(use.User, t, item, effect);
        FlingDone(use);
    }

    /// <summary>The thrown item leaves its user's hands (the original's <c>RemoveItem</c> at the script's end, so Recycle can bring it back).</summary>
    private void FlingDone(MoveUse use)
    {
        if (use.Scratch is not ItemData item) return;
        var p = use.User.Pokemon!;
        if (p.HeldItem == item)
        {
            use.User.Volatile.ConsumedItem = item;
            p.HeldItem = null;
        }
        use.Scratch = null;
    }

    /// <summary>What lands with a thrown item: a flinch, a condition, a herb's cure, or a berry the target eats.</summary>
    private void Thrown(Battler user, Battler t, ItemData item, string effect)
    {
        var q = t.Pokemon!;
        switch (effect)
        {
            case "Flinch":
                if (!t.MovedThisTurn && !BattleEffects.Of(t).Any(e => e.BlocksFlinch)) t.Flinched = true;
                break;
            case "Paralyze": TryInflictStatus(t, StatusCondition.Paralyze, user, false, By.SideEffect); break;
            case "Poison": TryInflictStatus(t, StatusCondition.Poison, user, false, By.SideEffect); break;
            case "BadlyPoison": TryInflictStatus(t, StatusCondition.Toxic, user, false, By.SideEffect); break;
            case "Burn": TryInflictStatus(t, StatusCondition.Burn, user, false, By.SideEffect); break;
            case "StatdownRestore":
            {
                bool any = false;
                foreach (var stat in q.StatStages.Keys.ToList())
                    if (q.StatStages[stat] < 0)
                    {
                        q.StatStages[stat] = 0;
                        any = true;
                    }
                if (any) Say($"{t.Name}'s lowered stats were put back to normal by the {item.Name}!");
                break;
            }
            case "HealInfatuation":
                if (t.Volatile.InLoveWith != null)
                {
                    t.Volatile.InLoveWith = null;
                    Say($"{t.Name} got over its infatuation thanks to the {item.Name}!");
                }
                break;
            default:
                EatBerry(t, item, effect);
                break;
        }
    }

    /// <summary>
    /// <c>BtlCmd_TryPluck</c> (Pluck, Bug Bite): the user eats the berry its target holds and gets what the berry
    /// gives, unless the hit went into a Substitute; Sticky Hold keeps the berry. The target can Recycle it.
    /// </summary>
    private void Pluck(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched) return;
        var q = t.Pokemon!;
        var berry = q.HeldItem;
        if (berry?.PluckEffect == null) return;
        if (KeptByStickyHold(user, t, use.Move)) return;

        q.HeldItem = null;
        t.Volatile.ConsumedItem = berry;
        Say($"{user.Name} stole and ate {t.Name}'s {berry.Name}!");
        // With Klutz or under an Embargo the berry is gone all the same and does nothing for whoever ate it
        if (user.IsActive && !Has(user, "Klutz") && user.Volatile.EmbargoTurns == 0) EatBerry(user, berry, berry.PluckEffect);
    }

    /// <summary>
    /// What a berry gives the Pokémon that eats it out of turn (Pluck, Bug Bite, a thrown berry), by the item
    /// table's effect for that (<c>BattleSystem_PluckBerry</c>): HP, a cure, PP, a stat, the focus, a Micle
    /// Berry's accuracy; a flavour its nature dislikes confuses it.
    /// </summary>
    private void EatBerry(Battler eater, ItemData berry, string effect)
    {
        var p = eater.Pokemon!;
        int param = berry.HoldParam;
        void Heal(int amount)
        {
            if (p.CurrentHP < p.MaxHP) RestoreHp(eater, Math.Max(1, amount), $"{eater.Name} restored its health using its {berry.Name}!");
        }
        void Cure(params StatusCondition[] cures)
        {
            if (cures.Contains(p.Status)) CureStatus(eater, $"{eater.Name}'s {berry.Name} cured its {BattleText.StatusName(p.Status)}!");
        }
        void Unconfuse()
        {
            if (!eater.IsConfused) return;
            eater.ConfusionTurns = 0;
            Say($"{eater.Name}'s {berry.Name} snapped it out of its confusion!");
        }
        void Raise(StatType stat, int by)
        {
            if (p.StatStages.GetValueOrDefault(stat) < 6) ChangeStat(eater, stat, by, eater);
        }

        switch (effect)
        {
            case "HpRestore": Heal(param); break;
            case "HpPctRestore": Heal(Formulas.Divide(p.MaxHP * param, 100)); break;
            case "HpRestoreSpicy" or "HpRestoreDry" or "HpRestoreSweet" or "HpRestoreBitter" or "HpRestoreSour":
                Heal(Formulas.Divide(p.MaxHP, Math.Max(1, param)));
                // A flavour its nature dislikes confuses it (subscript_held_item_dislike_flavor)
                if (HeldItemEffects.FlavorOf(effect) is { } flavor && HeldItemEffects.DislikedBy(p.Nature) == flavor)
                {
                    Say($"For {eater.Name}, the {berry.Name} was too {flavor.ToString().ToLowerInvariant()}!");
                    Confuse(eater, null, false, By.Other);
                }
                break;
            case "TempAccUp":
                eater.Volatile.MicleBerry = true;
                Say($"{eater.Name} boosted the accuracy of its next move using its {berry.Name}!");
                break;
            case "PrzRestore": Cure(StatusCondition.Paralyze); break;
            case "SlpRestore": Cure(StatusCondition.Sleep); break;
            case "PsnRestore": Cure(StatusCondition.Poison, StatusCondition.Toxic); break;
            case "BrnRestore": Cure(StatusCondition.Burn); break;
            case "FrzRestore": Cure(StatusCondition.Freeze); break;
            case "CnfRestore": Unconfuse(); break;
            case "AllRestore":
                Cure(StatusCondition.Paralyze, StatusCondition.Sleep, StatusCondition.Poison, StatusCondition.Toxic, StatusCondition.Burn, StatusCondition.Freeze);
                Unconfuse();
                break;
            case "PpRestore":
            {
                // The move with the most PP missing gets them back
                var move = p.Moves.OrderByDescending(m => m.MaxPP - m.CurrentPP).FirstOrDefault();
                if (move != null && move.CurrentPP < move.MaxPP)
                {
                    move.CurrentPP = Math.Min(move.MaxPP, move.CurrentPP + param);
                    Say($"{eater.Name}'s {berry.Name} restored {move.Name}'s PP!");
                }
                break;
            }
            case "AtkUp": Raise(StatType.Attack, 1); break;
            case "DefUp": Raise(StatType.Defense, 1); break;
            case "SpeedUp": Raise(StatType.Speed, 1); break;
            case "SpatkUp": Raise(StatType.SpAttack, 1); break;
            case "SpdefUp": Raise(StatType.SpDefense, 1); break;
            case "CritUp":
                if (!eater.Volatile.FocusEnergy)
                {
                    eater.Volatile.FocusEnergy = true;
                    Say($"{eater.Name} is getting pumped!");
                }
                break;
            case "RandomUp2":
            {
                var stats = new[] { StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense }
                    .Where(s => p.StatStages.GetValueOrDefault(s) < 6).ToList();
                if (stats.Count > 0) ChangeStat(eater, stats[rng.Roll(RollKind.Pick, stats.Count)], 2, eater);
                break;
            }
        }
    }

    /// <summary>
    /// <c>BtlCmd_CalcNaturalGiftParams</c>: the berry its user holds gives the move its type and power, and is
    /// gone whether the move lands or not; nothing happens without a berry, or under an Embargo.
    /// </summary>
    private bool NaturalGift(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var berry = BattleEffects.ItemInHand(user);
        if (berry == null || berry.NaturalGiftPower == 0 || berry.NaturalGiftType is not { } type) return Fails();
        use.Scratch = berry.NaturalGiftPower;
        Retype(use, type);
        user.Volatile.ConsumedItem = berry;
        p.HeldItem = null;
        return true;
    }

    // ---------------------------------------------------------------- types and abilities

    /// <summary>The type a move counts as for Conversion: Curse, of no type in the original, is Ghost for a Ghost and Normal for anyone else.</summary>
    private static PokemonType TypeForConversion(Battler user, MoveData data) =>
        data.Name == "Curse" ? (user.HasType(PokemonType.Ghost) ? PokemonType.Ghost : PokemonType.Normal) : data.Type;

    /// <summary><c>BtlCmd_TryConversion</c>: the type of one of its user's other moves that it isn't already, drawn evenly; never for Arceus.</summary>
    private void Conversion(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Pokemon!.AbilityName == "Multitype")
        {
            Fails();
            return;
        }
        var candidates = user.Pokemon.Moves.Where(m => m.Data.Effect != "Conversion").Select(m => TypeForConversion(user, m.Data)).Where(type => !user.HasType(type)).ToList();
        if (candidates.Count == 0)
        {
            Fails();
            return;
        }
        var pick = candidates[rng.Roll(RollKind.Pick, candidates.Count)];
        user.Volatile.Types = new List<PokemonType> { pick };
        Say($"{user.Name} transformed into the {pick} type!");
    }

    /// <summary>
    /// <c>BtlCmd_TryConversion2</c>: a type that resists or is immune to the last move that hit its user, drawn
    /// evenly from the chart's rows, and not one it has already; nothing has hit it, its attacker is still in the
    /// first turn of a move that takes two, or Arceus: it fails.
    /// </summary>
    private void Conversion2(MoveUse use)
    {
        var user = use.User;
        var v = user.Volatile;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Pokemon!.AbilityName == "Multitype" || v.Conversion2Move == null || v.Conversion2By is not { } byPlace)
        {
            Fails();
            return;
        }
        var by = At(byPlace);
        if (MultiTurn.Contains(v.Conversion2Move.Effect ?? "") && (by.Volatile.Charging || by.Volatile.BideTurns > 0))
        {
            Fails();
            return;
        }
        var candidates = Enum.GetValues<PokemonType>()
            .Where(t => (t != PokemonType.Fairy || Rules.ModernSpeciesValues) && !user.HasType(t) && TypeChart.GetEffectiveness(v.Conversion2Type, t, null, Rules) <= 0.5f)
            .ToList();
        if (candidates.Count == 0)
        {
            Fails();
            return;
        }
        var pick = candidates[rng.Roll(RollKind.Pick, candidates.Count)];
        v.Types = new List<PokemonType> { pick };
        Say($"{user.Name} transformed into the {pick} type!");
    }

    /// <summary>
    /// <c>BtlCmd_Transform</c>: the user takes the target's species and shape, types, stats (not its HP),
    /// ability, stat changes and moves with 5 PP each, until it leaves the field. Not a target out of sight or
    /// one that is itself transformed. It keeps its own name while it wears another's shape.
    /// </summary>
    private bool Transform(MoveUse use, Battler t)
    {
        var user = use.User;
        if (t.IsElsewhere || t.Volatile.Transformed) return Fails();
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        var v = user.Volatile;

        var o = Keep(user);
        if (o.Species == null)
        {
            o.Species = p.Species;
            o.Form = p.Form;
        }
        if (!o.NicknameGiven)
        {
            o.NicknameGiven = true;
            o.Nickname = p.Nickname;
        }
        KeepStats(o, p);
        KeepAbility(o, p);
        if (o.Moves == null)
        {
            // The moves it had before anything changed them: a slot Mimic filled goes back to Mimic first
            o.Moves = p.Moves.Select(m => (m.Data, m.CurrentPP)).ToList();
            if (o.MoveSlots != null)
                foreach (var (slot, kept) in o.MoveSlots)
                    if (slot < o.Moves.Count) o.Moves[slot] = kept;
            o.MoveSlots = null;
        }

        if (string.IsNullOrWhiteSpace(p.Nickname)) p.Nickname = p.Species.Name;
        p.Species = q.Species;
        p.Form = q.Form;
        p.Attack = q.Attack;
        p.Defense = q.Defense;
        p.SpAttack = q.SpAttack;
        p.SpDefense = q.SpDefense;
        p.Speed = q.Speed;
        p.AbilityName = q.AbilityName;
        p.Moves.Clear();
        foreach (var m in q.Moves) p.Moves.Add(new Move(m.Data, Math.Min(5, m.Data.MaxPP)));
        foreach (var (stat, stage) in q.StatStages) p.StatStages[stat] = stage;
        v.Types = t.Types.ToList();
        v.Transformed = true;
        v.Disabled = null;
        v.DisableTurns = 0;
        v.UsedMoveSlots = 0;
        user.ChoiceLock = null;
        // The ability it took on acts as if it had just come in (BtlCmd_Transform clears the announced flags),
        // Slow Start counts its five turns from the next, and Truant acts this turn
        v.Announced = EntryCheck.None;
        v.SlowStartTurn = Turn + 1;
        v.SlowStartEnded = false;
        v.TruantParity = Turn & 1;
        Say($"{user.Name} transformed into {t.Name}!").With(new Reshaped(user.Place));
        return true;
    }

    private static void KeepStats(Original o, Pokemon p)
    {
        if (o.StatsKept) return;
        o.StatsKept = true;
        o.Attack = p.Attack;
        o.Defense = p.Defense;
        o.SpAttack = p.SpAttack;
        o.SpDefense = p.SpDefense;
        o.Speed = p.Speed;
    }

    private static void KeepAbility(Original o, Pokemon p)
    {
        if (o.AbilityKept) return;
        o.AbilityKept = true;
        o.AbilityName = p.AbilityName;
    }

    /// <summary><c>subscript_copy_ability</c> (Role Play): the user takes the target's ability; not Wonder Guard, not Arceus's, not Giratina's.</summary>
    private bool RolePlay(MoveUse use, Battler t)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (q.AbilityName is null or "Wonder Guard" or "Multitype" || p.AbilityName == "Multitype" || p.HeldItem?.Name == "Griseous Orb") return Fails();
        KeepAbility(Keep(user), p);
        p.AbilityName = q.AbilityName;
        Say($"{user.Name} copied {t.Name}'s {q.AbilityName}!");
        return true;
    }

    /// <summary><c>subscript_exchange_abilities</c> (Skill Swap): the two abilities change hands; not Wonder Guard, Arceus or Giratina, and not two of nothing.</summary>
    private bool SkillSwap(MoveUse use, Battler t)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (p.AbilityName is "Wonder Guard" or "Multitype" || q.AbilityName is "Wonder Guard" or "Multitype") return Fails();
        if (p.HeldItem?.Name == "Griseous Orb" || q.HeldItem?.Name == "Griseous Orb") return Fails();
        if (p.AbilityName == null && q.AbilityName == null) return Fails();
        KeepAbility(Keep(user), p);
        KeepAbility(Keep(t), q);
        (p.AbilityName, q.AbilityName) = (q.AbilityName, p.AbilityName);
        Say($"{user.Name} swapped abilities with its target!");
        return true;
    }

    /// <summary><c>subscript_give_target_insomnia</c> (Worry Seed): the target's ability becomes Insomnia; not through a Substitute, not Truant, Arceus or Giratina.</summary>
    private bool WorrySeed(MoveUse use, Battler t)
    {
        var q = t.Pokemon!;
        if (t.HasSubstitute || q.AbilityName is "Truant" or "Multitype" || q.HeldItem?.Name == "Griseous Orb") return Fails();
        KeepAbility(Keep(t), q);
        q.AbilityName = "Insomnia";
        Say($"{t.Name} acquired Insomnia!");
        return true;
    }

    // ---------------------------------------------------------------- stats and stat changes

    /// <summary><c>subscript_user_swap_atk_and_def</c> (Power Trick): the user's Attack and Defense change places, and back again on a second use.</summary>
    private void PowerTrick(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        KeepStats(Keep(user), p);
        (p.Attack, p.Defense) = (p.Defense, p.Attack);
        user.Volatile.PowerTrick = !user.Volatile.PowerTrick;
        Say($"{user.Name} switched its Attack and Defense!");
    }

    /// <summary>Power Swap and Guard Swap: the changes to two stats trade places between the user and its target.</summary>
    private bool SwapStages(MoveUse use, Battler t, string what, params StatType[] stats)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        foreach (var stat in stats)
            (p.StatStages[stat], q.StatStages[stat]) = (q.StatStages.GetValueOrDefault(stat), p.StatStages.GetValueOrDefault(stat));
        Say($"{user.Name} switched all changes to its {what} with the target!");
        return true;
    }

    /// <summary><c>subscript_exchange_all_stat_stages</c> (Heart Swap): every stat change trades places, Focus Energy with them.</summary>
    private bool HeartSwap(MoveUse use, Battler t)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed, StatType.Accuracy, StatType.Evasion })
            (p.StatStages[stat], q.StatStages[stat]) = (q.StatStages.GetValueOrDefault(stat), p.StatStages.GetValueOrDefault(stat));
        (user.Volatile.FocusEnergy, t.Volatile.FocusEnergy) = (t.Volatile.FocusEnergy, user.Volatile.FocusEnergy);
        Say($"{user.Name} switched stat changes with the target!");
        return true;
    }

    // ---------------------------------------------------------------- the rest

    /// <summary><c>BtlCmd_FollowMe</c>: the foes' moves aimed at one Pokémon of the user's side come to the user this turn.</summary>
    private void FollowMe(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        Field.Side(user.Side).FollowMe = user.Place;
        Say($"{user.Name} became the center of attention!");
    }

    /// <summary>
    /// <c>BtlCmd_TryHelpingHand</c>: the user's ally hits half as hard again this turn, if there is one that is
    /// still to act and neither is helping already.
    /// </summary>
    private void HelpingHand(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var partner = IsDouble ? SlotsOf(user.Side).FirstOrDefault(b => b != user && b.IsActive) : null;
        bool stillToAct = partner != null && waiting.Any(a => a.User == partner && a.Actor == partner.Pokemon);
        if (partner == null || !stillToAct || user.Turn.HelpingHand || partner.Turn.HelpingHand)
        {
            Fails();
            return;
        }
        partner.Turn.HelpingHand = true;
        Say($"{user.Name} is ready to help {partner.Name}!");
    }

    /// <summary><c>BtlCmd_TrySpite</c>: the target's last move loses 4 PP, or what it has left; nothing when it has no last move or none left.</summary>
    private bool Spite(MoveUse use, Battler t)
    {
        var last = t.Volatile.LastMove;
        var move = last == null ? null : t.Pokemon!.Moves.FirstOrDefault(m => m.Data == last);
        if (move == null || move.CurrentPP == 0) return Fails();
        int taken = Math.Min(4, move.CurrentPP);
        move.CurrentPP -= taken;
        Say($"It reduced the PP of {t.Name}'s {move.Name} by {taken}!");
        return true;
    }

    /// <summary>
    /// <c>BtlCmd_CheckChatterActivation</c>: Chatot's own chatter confuses by the rules' chance (the original's by
    /// the recorded cry: a roll of 100 at or under it); a Chatot that has transformed, or anyone else, confuses no one.
    /// </summary>
    private void Chatter(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive) return;
        if (user.Pokemon!.Species.Name != "Chatot" || user.Volatile.Transformed) return;
        if (rng.Roll(RollKind.SideEffect, 100) > Rules.ChatterConfusionChance) return;
        Confuse(t, user, false, By.SideEffect);
    }

    // ---------------------------------------------------------------- the battlefield (plan 06 · R6)

    /// <summary><c>BtlCmd_TryCamouflage</c>: the user becomes the type of the ground; not Arceus, and not when it is that type already.</summary>
    private void Camouflage(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var type = Ground.Type;
        if (user.Pokemon!.AbilityName == "Multitype" || user.HasType(type))
        {
            Fails();
            return;
        }
        user.Volatile.Types = new List<PokemonType> { type };
        Say($"{user.Name} transformed into the {type} type!");
    }

    /// <summary><c>BtlCmd_GetTerrainMove</c>: Nature Power becomes the ground's move and uses it.</summary>
    private void NaturePower(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var move = MoveDatabase.Get(Ground.Move);
        Say($"{use.Move.Name} turned into {move.Name}!");
        CallMove(use, move, null);
    }

    /// <summary>
    /// <c>BtlCmd_GetTerrainSecondaryEffect</c>: Secret Power's side effect is the ground's (a stat lowered, a
    /// condition, a flinch), by the move's own chance, on a target it touched that isn't shielded from side effects.
    /// </summary>
    private void SecretPower(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || t == user) return;
        var userEffects = BattleEffects.Of(user).ToList();
        bool breaks = userEffects.Any(e => e.IgnoresTargetAbility);
        if (BattleEffects.Of(t, includeAbility: !breaks).Any(e => e.BlocksSideEffects)) return;
        int chance = (use.Data.EffectChance > 0 ? use.Data.EffectChance : 30) * userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();
        if (!Chance(chance)) return;

        switch (Ground.Effect)
        {
            case "AccuracyDown": ChangeStat(t, StatType.Accuracy, -1, user, false, By.SideEffect); break;
            case "AttackDown": ChangeStat(t, StatType.Attack, -1, user, false, By.SideEffect); break;
            case "SpeedDown": ChangeStat(t, StatType.Speed, -1, user, false, By.SideEffect); break;
            case "EvasionDown": ChangeStat(t, StatType.Evasion, -1, user, false, By.SideEffect); break;
            case "Sleep": TryInflictStatus(t, StatusCondition.Sleep, user, false, By.SideEffect); break;
            case "Freeze": TryInflictStatus(t, StatusCondition.Freeze, user, false, By.SideEffect); break;
            case "Paralyze": TryInflictStatus(t, StatusCondition.Paralyze, user, false, By.SideEffect); break;
            case "Flinch":
                if (!t.MovedThisTurn && !BattleEffects.Of(t).Any(e => e.BlocksFlinch)) t.Flinched = true;
                break;
        }
    }
}
