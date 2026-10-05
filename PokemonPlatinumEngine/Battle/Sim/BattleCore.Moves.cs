using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Using a move, in the original's order (battle_controller_player.c): what can stop it (sleep, a freeze, having to
// recharge, flinching, Disable, Taunt, Imprison, Gravity, Heal Block, confusion, paralysis, love), its PP, its
// targets, a turn spent getting ready, then for each target whether it gets there (Protect, accuracy, a target
// out of reach), the damage (into a Substitute if there is one), the side effects, and what the attacker gets out
// of it. What a move does of its own is its MoveEffect (BattleCore.Effects.cs).
public sealed partial class BattleCore
{
    /// <summary>What a Pokémon does when it has no PP left: hurts the target and a quarter of its own HP.</summary>
    public static readonly MoveData StruggleData = new()
    {
        Name = "Struggle",
        Type = PokemonType.Normal,
        Category = MoveCategory.Physical,
        Power = 50,
        Accuracy = 0,
        MaxPP = 1,
        Flags = MoveFlags.Contact,
        Description = "Used only when no other move has PP left. It also hurts the user."
    };

    /// <summary>How one target of a move came out of it.</summary>
    private sealed class MoveHit
    {
        public required Battler Target;
        public bool Missed, Protected, OutOfReach, Immune, Absorbed, Endured, EnduredByMove;

        /// <summary>The Substitute took it, and whether that was the end of the Substitute.</summary>
        public bool IntoSubstitute, BrokeSubstitute;
        public DamageCalculator.DamageResult Damage;

        /// <summary>HP taken: from the Pokémon, or from its Substitute.</summary>
        public int Dealt;
        public List<BattleEvent> Notes = new();
        public bool Landed => !Missed && !Protected && !OutOfReach && !Immune && !Absorbed;

        /// <summary>It landed on the Pokémon itself: what a side effect needs.</summary>
        public bool Touched => Landed && !IntoSubstitute;
    }

    /// <summary>How a Pokémon leaves the field by its own move, once the move is done.</summary>
    private enum Leaving { No, Returns, PassesTheBaton, MakesAWish, DancesAway }

    /// <summary>One use of a move: who, what, at whom, and what came of it.</summary>
    private sealed class MoveUse
    {
        public required Battler User;
        public required Move Move;
        public required MoveEffect Effect;
        public MoveData Data => Move.Data;
        public List<Battler> Targets = new();

        /// <summary>The line "X used Y!", which the move's effects are shown with.</summary>
        public Said Line = null!;

        /// <summary>The second turn of a move that takes two: the strike.</summary>
        public bool Strikes;
        public List<MoveHit> Hits = new();

        /// <summary>The HP the move took, Substitutes' included: what draining and recoil go by.</summary>
        public int DamageDealt;
        public Leaving Leaves;

        /// <summary>The power of the move in tenths, before its own effect has its say: 20 for a Pursuit that catches its target leaving.</summary>
        public int Boost = 10;

        /// <summary>The move its user got to use before this one (Protect is less sure after another Protect).</summary>
        public MoveData? MoveBefore;
    }

    private IEnumerable<BattleRequest> ExecuteMove(Act act)
    {
        var user = act.User;
        var v = user.Volatile;
        var move = act.Move!;
        var target = act.Target;
        bool goesOn = act.Choice.Move == BattleChoice.HeldMove;

        // Under an Encore whatever was chosen comes out as the encored move
        if (!goesOn && move.Data != StruggleData && v.Encored != null && user.Pokemon!.Moves.FirstOrDefault(m => m.Data == v.Encored) is { } encored && encored != move)
        {
            move = encored;
            target = null;
        }

        user.MovedThisTurn = true;
        if (!CanMove(user, move))
        {
            v.LastMove = null;
            yield break;
        }
        // A rage lasts only as long as Rage is what it uses
        if (move.Data.Effect != "RaiseAtkWhenHit") v.Rage = false;

        var use = new MoveUse { User = user, Move = move, Effect = EffectOf(move.Data), Strikes = v.Charging, MoveBefore = v.LastMove };
        use.Targets = ResolveTargets(user, move, target);

        // PP: one, plus one for each foe with Pressure it is aimed at. A move it is only going on with costs nothing
        if (!goesOn && move.Data != StruggleData)
        {
            if (move.CurrentPP <= 0)
            {
                Say($"{user.Name} used {move.Name}!");
                Say("But there was no PP left for the move!");
                v.LastMove = null;
                yield break;
            }
            int pp = 1 + use.Targets.Where(t => t.Side != user.Side).Sum(t => BattleEffects.Of(t).Sum(e => e.ExtraPpUsed));
            move.CurrentPP = Math.Max(0, move.CurrentPP - pp);
            if (user.IsPlayerSide) Evolution.CountMoveUse(user.Pokemon!, move.Name);
        }
        if (BattleEffects.Of(user).Any(e => e.LocksMoveChoice) && user.Pokemon!.Moves.Contains(move)) user.ChoiceLock = move;
        v.LastMove = move.Data == StruggleData ? null : move.Data;

        // A move that takes a turn to get ready spends it now
        if (use.Effect.Charge is { } charge && !use.Strikes && !SkipsTheCharge(use, charge))
        {
            BeginCharge(use, charge);
            yield break;
        }

        // A move that goes on by itself may tell its later turns in its own words (Bide)
        bool ownLines = goesOn && use.Effect.OwnLinesAfterFirst;
        use.Line = ownLines ? new Said("") : Say($"{user.Name} used {move.Name}!");

        // The second turn of a move that takes two: it is back where it can be seen, and hit
        if (use.Strikes && use.Effect.Charge != null)
        {
            v.Charging = false;
            if (user.IsElsewhere)
            {
                v.Elsewhere = Elsewhere.No;
                use.Line.With(new Reappeared(user.Place));
            }
            if (!user.IsHeldToItsMove) v.LockedMove = null;
        }

        if (use.Targets.Count == 0)
        {
            Say("But there was no target...");
            Unlock(user);
            yield break;
        }

        foreach (var t in use.Targets) t.Turn.SubstituteHit = false;
        if (use.Effect.Does != null)
        {
            // A move that is more than a hit, or no hit at all
            if (!ownLines) use.Line.With(new Lunged(user.Place, move.Category));
            use.Effect.Use(this, use);
        }
        else if (move.Category != MoveCategory.Status && move.Power > 0 && (move.Data.Support != MoveEffectSupport.None || use.Effect != Plain))
        {
            Strike(use);
        }
        else
        {
            use.Line.With(new Lunged(user.Place, move.Category));
            if (move.Data.Support == MoveEffectSupport.None && use.Effect == Plain)
            {
                // A move whose effect isn't in the engine yet does nothing rather than a made-up something
                use.Line.With(Shown(user, user, move, null));
                Say("But nothing happened!");
            }
            else ByItsFields(use);
        }

        foreach (var request in Leave(use)) yield return request;
    }

    // ---------------------------------------------------------------- what can stop a move

    /// <summary>
    /// Whether the Pokémon gets to move at all (<c>BattleControllerPlayer_CheckStatusDisruption</c>, in its order):
    /// sleep, a freeze, recharging, a flinch, Disable, Taunt, Imprison, Gravity, Heal Block, confusion, paralysis,
    /// love.
    /// </summary>
    private bool CanMove(Battler user, Move move)
    {
        var p = user.Pokemon!;
        var v = user.Volatile;
        var effects = BattleEffects.Of(user).ToList();

        // Whatever it was holding on to from its last move is over
        v.DestinyBond = false;
        v.Grudge = false;

        if (p.Status == StatusCondition.Sleep)
        {
            if (UproarIsOn && !Has(user, "Soundproof")) WakeUp(user, "The uproar woke up {0}!");
            else
            {
                p.SleepTurns = Math.Max(0, p.SleepTurns - effects.Select(e => e.SleepCountdownRate).DefaultIfEmpty(1).Max());
                if (p.SleepTurns == 0) WakeUp(user, "{0} woke up!");
                else
                {
                    // Snore and Sleep Talk are told the same way, and then go on
                    Say($"{user.Name} is fast asleep.");
                    if (!UsableAsleep(move.Data)) return false;
                }
            }
        }

        if (p.Status == StatusCondition.Freeze)
        {
            // One time in five it thaws; a move that burns thaws its user whatever the roll says
            if (rng.Roll(RollKind.Thaw, 5) == 0 || move.Data.ThawsUser)
            {
                p.Status = StatusCondition.None;
                Say(move.Data.ThawsUser ? $"{user.Name}'s {move.Name} melted the ice!" : $"{user.Name} thawed out!").With(new StatusChanged(user.Place, StatusCondition.None));
            }
            else
            {
                Say($"{user.Name} is frozen solid!");
                return false;
            }
        }

        if (v.Recharging)
        {
            v.Recharging = false;
            Say($"{user.Name} must recharge!");
            Unlock(user);
            return false;
        }

        if (user.Flinched)
        {
            user.Flinched = false;
            Say($"{user.Name} flinched!");
            Unlock(user);
            foreach (var e in effects) e.OnFlinch(this, user);
            return Lost(user);
        }

        if (v.Disabled != null && v.Disabled == move.Data)
        {
            Say($"{user.Name}'s {move.Name} is disabled!");
            Unlock(user);
            return Lost(user);
        }

        if (v.TauntTurns > 0 && move.Power == 0)
        {
            Say($"{user.Name} can't use {move.Name} after the taunt!");
            Unlock(user);
            return Lost(user);
        }

        if (IsSealed(user, move.Data))
        {
            Say($"{user.Name} can't use the sealed {move.Name}!");
            Unlock(user);
            return Lost(user);
        }

        if (Field.Gravity && FailsUnderGravity(move.Data))
        {
            Say($"{user.Name} can't use {move.Name} because of gravity!");
            return Lost(user);
        }

        if (v.HealBlockTurns > 0 && IsHealingMove(move.Data))
        {
            Say($"{user.Name} can't use {move.Name} while it is kept from healing!");
            return false;
        }

        if (user.IsConfused)
        {
            if (--user.ConfusionTurns <= 0)
            {
                Say($"{user.Name} snapped out of confusion!");
            }
            else
            {
                Say($"{user.Name} is confused!");
                // It hurts itself one time in the rules' odds (the die's first face): a typeless 40-power hit
                if (rng.Roll(RollKind.ConfusionSelfHit, Rules.ConfusionSelfHitOdds) == 0)
                {
                    var self = DamageCalculator.Calculate(user, user, move, rng, spread: false, powerOverride: 40, rules: Rules);
                    p.CurrentHP -= Math.Min(p.CurrentHP, self.Damage);
                    Say("It hurt itself in its confusion!")
                        .AtImpact(new Struck(user.Place, p.CurrentHP, Hard: false)).AtImpact(new HitSounded(false));
                    Unlock(user);
                    return Lost(user);
                }
            }
        }

        // One time in four; a Pokémon with Magic Guard is never held by it, as in Platinum
        if (p.Status == StatusCondition.Paralyze && !effects.Any(e => e.PreventsIndirectDamage) && rng.Roll(RollKind.FullParalysis, 4) == 0)
        {
            Say($"{user.Name} is paralyzed! It can't move!");
            Unlock(user);
            return Lost(user);
        }

        if (v.InLoveWith is { } beloved)
        {
            Say($"{user.Name} is in love with {At(beloved).Name}!");
            if (rng.Roll(RollKind.Infatuation, 2) == 0)
            {
                Say($"{user.Name} is immobilized by love!");
                Unlock(user);
                return Lost(user);
            }
        }
        return true;
    }

    /// <summary>The move was lost in a way a rampage or an uproar ends on.</summary>
    private static bool Lost(Battler user)
    {
        user.Turn.MoveFailed = true;
        return false;
    }

    private void WakeUp(Battler b, string line)
    {
        b.Pokemon!.Status = StatusCondition.None;
        b.Pokemon.SleepTurns = 0;
        b.Volatile.Nightmare = false;
        Say(string.Format(line, b.Name)).With(new StatusChanged(b.Place, StatusCondition.None));
    }

    private static bool Has(Battler b, string ability) => b.Ability?.Name == ability;

    /// <summary>Snore and Sleep Talk are for a sleeping Pokémon to use.</summary>
    private static bool UsableAsleep(MoveData move) => move.Effect is "DamageWhileAsleep" or "UseRandomLearnedMoveSleep";

    /// <summary>The moves that can't be used while Gravity holds (the original's list: Fly, Bounce, the Jump Kicks, Splash, Magnet Rise).</summary>
    private static bool FailsUnderGravity(MoveData move) => move.Effect is "Fly" or "Bounce" or "CrashOnMiss" or "DoNothing" or "GiveGroundImmunity";

    /// <summary>The moves Heal Block stops (the original's list: Recover and its like, Rest, the sun's three, Swallow, Roost, Wish and the two wishes that end their user).</summary>
    private static bool IsHealingMove(MoveData move) =>
        (move.Category == MoveCategory.Status && move.HealPercent > 0 && move.Effect is null or "HealHalfMoreInSun" or "HealHalfRemoveFlyingType") ||
        move.Effect is "Rest" or "Swallow" or "HealIn3Turns" or "FaintAndFullHealNextMon" or "FaintFullRestoreNextMon";

    /// <summary>A foe that used Imprison knows the move too.</summary>
    private bool IsSealed(Battler user, MoveData move) =>
        ActiveFoes(user).Any(foe => foe.Volatile.Imprisoning && foe.Pokemon!.Moves.Any(m => m.Data == move));

    /// <summary>
    /// Frees a Pokémon from a move it was in the middle of (<c>Battler_UnlockMoveChoice</c>): the strike it owed is
    /// off, it is back where it can be hit, and a Bide is over.
    /// </summary>
    private void Unlock(Battler b)
    {
        var v = b.Volatile;
        v.Charging = false;
        v.BideTurns = 0;
        ComeBack(b);
        if (!b.IsHeldToItsMove) v.LockedMove = null;
    }

    /// <summary>Back from the air, the ground or wherever a two-turn move took it.</summary>
    private void ComeBack(Battler b)
    {
        if (!b.IsElsewhere) return;
        b.Volatile.Elsewhere = Elsewhere.No;
        Emit(new Reappeared(b.Place));
    }

    // ---------------------------------------------------------------- a turn to get ready

    /// <summary>The sun charges Solar Beam at once, and a Power Herb any move that would wait (it is used up).</summary>
    private bool SkipsTheCharge(MoveUse use, ChargeTurn charge)
    {
        if (charge.NotInTheSun && Field.WeatherInEffect == BattleWeather.Sun) return true;
        if (!BattleEffects.Of(use.User).Any(e => e.SkipsChargeTurn)) return false;

        Say(string.Format(charge.Line, use.User.Name));
        if (charge.RaisesDefense) ChangeStat(use.User, StatType.Defense, 1, use.User);
        Say($"{use.User.Name} became fully charged due to its {use.User.Pokemon!.HeldItem!.Name}!");
        ConsumeItem(use.User);
        return true;
    }

    private void BeginCharge(MoveUse use, ChargeTurn charge)
    {
        var user = use.User;
        var said = Say(string.Format(charge.Line, user.Name));
        said.With(new Lunged(user.Place, MoveCategory.Status));
        user.Volatile.Charging = true;
        user.Volatile.LockedMove = use.Data;
        if (charge.Where != Elsewhere.No)
        {
            user.Volatile.Elsewhere = charge.Where;
            said.With(new Vanished(user.Place));
        }
        if (charge.RaisesDefense) ChangeStat(user, StatType.Defense, 1, user);
    }

    // ---------------------------------------------------------------- at whom

    /// <summary>Who a move hits: its target type, the chosen target (or a stand-in if that one is gone).</summary>
    private List<Battler> ResolveTargets(Battler user, Move move, Battler? chosen)
    {
        var foes = SlotsOf(Other(user.Side)).Where(b => b.IsActive).ToList();
        switch (EffectOf(move.Data).TargetFor(user, move.Target))
        {
            case MoveTarget.User:
            case MoveTarget.UserSide:
            case MoveTarget.UserOrAlly:
            case MoveTarget.Field:
            case MoveTarget.FoeSide:
                return new List<Battler> { user };
            case MoveTarget.UserAndAllies:
                return SlotsOf(user.Side).Where(b => b.IsActive).ToList();
            case MoveTarget.Ally:
            case MoveTarget.Allies:
                return SlotsOf(user.Side).Where(b => b.IsActive && b != user).ToList();
            case MoveTarget.AllPokemon:
                return BySpeed().Where(b => b.IsActive).ToList();
            case MoveTarget.AllFoes:
                return speedOrder.Where(foes.Contains).Concat(foes).Distinct().ToList();
            case MoveTarget.AllOthers:
                return speedOrder.Concat(AllBattlers).Distinct().Where(b => b.IsActive && b != user).ToList();
            case MoveTarget.RandomFoe:
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Roll(RollKind.Target, foes.Count)] };
            default:
                if (chosen != null && chosen.IsActive && chosen != user) return new List<Battler> { chosen };
                // The chosen foe is gone: the move goes to the other one (an ally that fainted leaves nothing to hit)
                if (chosen != null && chosen.Side == user.Side) return new List<Battler>();
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Roll(RollKind.Target, foes.Count)] };
        }
    }

    /// <summary>The effect a move plays on one target: what kind of move it is and, for a damaging move, how it landed.</summary>
    private static MoveShown Shown(Battler user, Battler target, Move move, MoveHit? hit) => new(
        move.Name, move.Type, move.Category, user.Place, target.Place,
        Missed: hit != null && (hit.Missed || hit.OutOfReach),
        Blocked: hit != null && (hit.Immune || hit.Absorbed || hit.Protected),
        Critical: hit is { Landed: true } && hit.Damage.IsCritical,
        SuperEffective: hit is { Landed: true } && hit.Damage.IsSuperEffective);

    // ---------------------------------------------------------------- getting there

    /// <summary>
    /// Whether a move gets to its target (<c>CheckMoveHitAccuracy</c>, then <c>CheckMoveHitOverrides</c>): the roll
    /// is made first; then Protect stops what it can stop, Lock-On and No Guard make a hit of anything, rain makes
    /// Thunder sure and hail Blizzard, and a target in the air, under the ground or the water or out of sight is
    /// missed by whatever can't follow it there.
    /// </summary>
    private bool Reaches(MoveUse use, Battler target, MoveHit hit)
    {
        var user = use.User;
        if (target == user) return true;
        bool missed = !Hits(use, target);

        if (target.Turn.Protecting && use.Data.Flags.HasFlag(MoveFlags.Protect) && use.Effect.StoppedByProtect(user))
        {
            hit.Protected = true;
            Unlock(user);
            return false;
        }

        bool aimed = target.Volatile.LockOnTurns > 0 && target.Volatile.LockedOnBy == user.Place;
        if (aimed || BattleEffects.Of(user).Concat(BattleEffects.Of(target, includeAbility: !BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility))).Any(e => e.MovesNeverMiss))
            return true;

        if (use.Effect.SureUnder(Field.WeatherInEffect)) missed = false;

        if (target.IsElsewhere && use.Data.Target != MoveTarget.FoeSide && !use.Effect.FollowsTo(target.Volatile.Elsewhere))
        {
            hit.OutOfReach = true;
            return false;
        }

        hit.Missed = missed;
        return !missed;
    }

    /// <summary>
    /// The accuracy roll (<c>BattleControllerPlayer_CheckMoveHitAccuracy</c>): the move's accuracy by the stages,
    /// then by the user's ability, the weather over the target's ability, the fog, the target's item, the user's
    /// item and Gravity, each rounded down in turn; it hits when a roll of 100 comes out below the result.
    /// </summary>
    private bool Hits(MoveUse use, Battler target)
    {
        var user = use.User;
        var move = use.Move;
        if (move.Accuracy <= 0 || !use.Effect.RollsAccuracy) return true;
        // From Generation 6 a Poison type's Toxic can't miss
        if (Rules.PoisonTypesNeverMissToxic && move.Category == MoveCategory.Status && move.Data.InflictStatus == StatusCondition.Toxic && user.HasType(PokemonType.Poison)) return true;
        var weather = Field.WeatherInEffect;
        var userEffects = BattleEffects.Of(user).ToList();
        bool breaks = userEffects.Any(e => e.IgnoresTargetAbility);
        var targetEffects = BattleEffects.Of(target, includeAbility: !breaks).ToList();

        int accuracy = targetEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : user.Pokemon!.StatStages.GetValueOrDefault(StatType.Accuracy);
        int evasion = userEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : target.Pokemon!.StatStages.GetValueOrDefault(StatType.Evasion);
        // A target that has been identified can't hide behind raised evasion
        if ((target.Volatile.Identified || target.Volatile.MiracleEye) && evasion > 0) evasion = 0;
        int rate = Formulas.HitRate(use.Effect.AccuracyUnder(weather, move.Accuracy), accuracy, evasion);

        var userAbility = user.Ability?.Effect;
        var userItem = ItemOf(user);
        var targetAbility = breaks ? null : target.Ability?.Effect;
        var targetItem = ItemOf(target);
        if (userAbility != null) rate = Formulas.Scale(rate, userAbility.AccuracyMultiplier(user, move));
        if (targetAbility != null) rate = Formulas.Scale(rate, targetAbility.EvasionMultiplier(target));
        if (weather == BattleWeather.Fog) rate = rate * 6 / 10;
        if (targetItem != null) rate = Formulas.Scale(rate, targetItem.EvasionMultiplier(target));
        if (userItem != null) rate = Formulas.Scale(rate, userItem.AccuracyMultiplier(user, move));
        if (Field.Gravity) rate = rate * 10 / 6;

        return rng.Roll(RollKind.Accuracy, 100) < rate;
    }

    /// <summary>The held item's effect, unless an Embargo has put it out of use.</summary>
    private static BattleEffect? ItemOf(Battler b) =>
        b.Volatile.EmbargoTurns > 0 ? null : HeldItemEffects.For(b.Pokemon?.HeldItem);

    // ---------------------------------------------------------------- a damaging move

    private void Strike(MoveUse use)
    {
        var user = use.User;
        var effect = use.Effect;
        if (!effect.Begins(this, use))
        {
            Unlock(user);
            return;
        }

        // Its effect may have made another move of it by now (Weather Ball takes its type from the sky)
        var move = use.Move;
        use.Line.With(new Lunged(user.Place, move.Category));

        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        foreach (var t in use.Targets)
        {
            var hit = new MoveHit { Target = t };
            use.Hits.Add(hit);
            if (!Reaches(use, t, hit)) continue;
            effect.OnReach(this, use, t);

            float effectiveness = use.Data == StruggleData ? 1f : DamageCalculator.Effectiveness(user, t, move, Rules);
            var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
            hit.Notes.AddRange(Capture(() => hit.Absorbed = guards.Any(e => e.AbsorbsMove(this, t, user, move, effectiveness))));
            if (hit.Absorbed) continue;
            if (effectiveness == 0f)
            {
                hit.Immune = true;
                continue;
            }

            hit.Damage = DamageCalculator.Calculate(user, t, move, rng, spread: use.Targets.Count > 1, rules: Rules,
                powerTenths: effect.PowerTenths(this, use, t), critBonus: effect.CritBonus, pastScreens: effect.PastScreens);
            int damage = hit.Damage.Damage;

            // A Substitute takes the hit in the Pokémon's place
            if (t.HasSubstitute && t != user)
            {
                hit.IntoSubstitute = true;
                hit.Dealt = Math.Min(t.Volatile.SubstituteHp, damage);
                t.Volatile.SubstituteHp -= hit.Dealt;
                hit.BrokeSubstitute = t.Volatile.SubstituteHp == 0;
                t.Turn.SubstituteHit = true;
                continue;
            }

            hit.Dealt = Math.Min(t.Pokemon!.CurrentHP, damage);
            if (hit.Dealt >= t.Pokemon.CurrentHP)
            {
                if (t.Turn.Enduring) hit.Endured = hit.EnduredByMove = true;
                else hit.Notes.AddRange(Capture(() => hit.Endured = guards.Any(e => e.EnduresHit(this, t, hit.Dealt))));
                if (hit.Endured) hit.Dealt = t.Pokemon.CurrentHP - 1;
            }
        }

        // The move's effect flies to each target and lands with the damage
        foreach (var hit in use.Hits) use.Line.With(Shown(user, hit.Target, move, hit));
        foreach (var hit in use.Hits.Where(h => h.Touched && h.Dealt > 0))
        {
            var t = hit.Target;
            t.Pokemon!.CurrentHP -= hit.Dealt;
            t.Volatile.LastHitBy = user.Place;
            if (t.IsPlayerSide) Evolution.CountDamageTaken(t.Pokemon, hit.Dealt);
            if (user.IsPlayerSide && hit.Damage.IsCritical) Evolution.CountCriticalHit(user.Pokemon!);
            if (t.Volatile.BideTurns > 0) t.Volatile.BideDamage += hit.Dealt;
            use.Line.AtImpact(new Struck(t.Place, t.Pokemon.CurrentHP, hit.Damage.IsCritical || hit.Damage.IsSuperEffective));
        }
        if (use.Hits.Any(h => h.Landed)) use.Line.AtImpact(new HitSounded(use.Hits.Any(h => h.Landed && h.Damage.IsSuperEffective)));
        use.DamageDealt = use.Hits.Where(h => h.Landed).Sum(h => h.Dealt);

        AfterStrike(use);
    }

    /// <summary>
    /// What each target makes of a damaging move, then what its user gets out of it: in the original's order, with
    /// the user's recoil and the like told before anyone the move knocked out goes down.
    /// </summary>
    private void AfterStrike(MoveUse use)
    {
        var user = use.User;
        var move = use.Move;
        var data = move.Data;
        bool several = use.Hits.Count > 1;
        var userEffects = BattleEffects.Of(user).ToList();
        bool userBreaks = userEffects.Any(e => e.IgnoresTargetAbility);
        int chanceMult = userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();

        foreach (var hit in use.Hits)
        {
            var t = hit.Target;
            if (hit.Protected)
            {
                Say($"{t.Name} protected itself!");
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (hit.Missed || hit.OutOfReach)
            {
                Say(several || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!");
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (!hit.Endured) log.AddRange(hit.Notes);
            if (hit.Absorbed) continue;
            if (hit.Immune)
            {
                Say($"It doesn't affect {t.Name}...");
                user.Turn.MoveFailed = true;
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }

            if (hit.IntoSubstitute)
            {
                var took = Say($"The substitute took damage for {t.Name}!");
                if (hit.BrokeSubstitute)
                {
                    took.With(new SubstituteChanged(t.Place, Up: false));
                    Say($"{t.Name}'s substitute faded!");
                }
            }
            if (hit.Damage.IsCritical) Say(several ? $"A critical hit on {t.Name}!" : "A critical hit!");
            if (hit.Damage.IsSuperEffective) Say(several ? $"It's super effective on {t.Name}!" : "It's super effective!");
            if (hit.Damage.IsNotVeryEffective) Say(several ? $"It's not very effective on {t.Name}..." : "It's not very effective...");
            if (hit.EnduredByMove) Say($"{t.Name} endured the hit!");
            else if (hit.Endured) log.AddRange(hit.Notes);
            if (hit.Damage.IsCritical && hit.Touched) t.TookCriticalHit = true;

            bool standing = t.Pokemon!.CurrentHP > 0;

            // Side effects on the target: it has to be standing, hit itself and not behind Shield Dust
            bool shielded = BattleEffects.Of(t, includeAbility: !userBreaks).Any(e => e.BlocksSideEffects);
            if (standing && hit.Touched && !shielded && t != user)
            {
                if (data.InflictStatus != StatusCondition.None && Chance(data.StatusChancePercent * chanceMult))
                    TryInflictStatus(t, data.InflictStatus, user, false, By.SideEffect);
                if (data.TargetStatChange is { } stat && !data.StatChangeTargetSelf && Chance(data.StatChangeChancePercent * chanceMult))
                {
                    ChangeStat(t, stat, data.StatStageAmount, user, false, By.SideEffect);
                    foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(t, also, data.StatStageAmount, user, false, By.SideEffect);
                }
                if (data.ConfuseChancePercent > 0 && Chance(data.ConfuseChancePercent * chanceMult)) Confuse(t, user, false, By.SideEffect);
                if (data.FlinchChancePercent > 0 && !t.MovedThisTurn && Chance(data.FlinchChancePercent * chanceMult) &&
                    !BattleEffects.Of(t).Any(e => e.BlocksFlinch))
                    t.Flinched = true;
            }

            // What the move does of its own to whoever it hit
            use.Effect.OnHit(this, use, hit);

            // A Pokémon in a rage grows angrier with every hit it takes
            if (standing && hit.Touched && hit.Dealt > 0 && t != user && t.Volatile.Rage && t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) < 6)
            {
                int stage = t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) + 1;
                t.Pokemon.StatStages[StatType.Attack] = stage;
                Say($"{t.Name}'s rage is building!").With(new StageChanged(t.Place, StatType.Attack, stage, Rose: true));
            }

            // The target's ability reacts to the hit (Static, Rough Skin), and berries check its HP
            if (hit.Touched && hit.Dealt > 0)
            {
                foreach (var e in BattleEffects.Of(t, includeAbility: !userBreaks).ToList())
                    e.AfterHit(this, t, user, move, hit.Dealt, hit.Damage.IsCritical);
                CheckConditionHooks(t, user);
            }

            // A Fire move thaws a frozen target
            if (t.Pokemon.CurrentHP > 0 && hit.Touched && hit.Dealt > 0 && t != user && move.Type == PokemonType.Fire && t.Pokemon.Status == StatusCondition.Freeze)
            {
                t.Pokemon.Status = StatusCondition.None;
                Say($"{t.Name} thawed out!").With(new StatusChanged(t.Place, StatusCondition.None));
            }
        }

        var landed = use.Hits.Where(h => h.Landed).Select(h => (h.Target, h.Dealt)).ToList();
        int total = use.DamageDealt;

        if (user.IsActive && landed.Count > 0)
        {
            // Draining heals the user by what the move took (or hurts it, against Liquid Ooze)
            if (data.DrainPercent > 0 && total > 0)
            {
                int amount = Formulas.Divide(total * data.DrainPercent, 100);
                amount = amount * userEffects.Select(e => e.DrainHundredths).DefaultIfEmpty(100).Max() / 100;
                bool ooze = landed.Any(h => BattleEffects.Of(h.Target).Any(e => e.HurtsDrainers));
                if (ooze) LoseHp(user, amount, $"{user.Name} sucked up the liquid ooze!");
                else if (user.Volatile.HealBlockTurns > 0) Say($"{user.Name} was kept from healing!");
                else RestoreHp(user, amount, $"{landed[0].Target.Name} had its energy drained!");
            }

            // Recoil
            int beforeRecoil = user.Pokemon!.CurrentHP;
            if (data == StruggleData) LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, 4), $"{user.Name} is hit with recoil!", direct: true);
            else if (data.RecoilPercent > 0 && total > 0 && !userEffects.Any(e => e.PreventsRecoil))
                LoseHp(user, RecoilFor(total, data.RecoilPercent), $"{user.Name} is hit with recoil!");
            if (user.IsPlayerSide) Evolution.CountRecoil(user.Pokemon!, beforeRecoil - user.Pokemon!.CurrentHP);

            // Changes to the user's own stats (Close Combat)
            if (user.IsActive && data.TargetStatChange is { } selfStat && data.StatChangeTargetSelf &&
                Chance(data.StatChangeChancePercent * (data.StatChangeChancePercent >= 100 ? 1 : chanceMult)))
            {
                ChangeStat(user, selfStat, data.StatStageAmount, user);
                foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(user, also, data.StatStageAmount, user);
            }

            if (user.IsActive) use.Effect.AfterHits(this, use);
        }

        // Now whoever the move knocked out goes down
        ResolveFaints(use);
        if (Result != BattleResult.None) return;

        if (user.IsActive && landed.Count > 0) foreach (var e in userEffects) e.AfterAttacking(this, user, move, landed);
        CheckConditionHooks(user, null);
    }

    /// <summary>A quarter, a third or a half of the damage done, as the original divides it (never nothing).</summary>
    private static int RecoilFor(int damage, int percent) => percent switch
    {
        25 => Formulas.Divide(damage, 4),
        33 => Formulas.Divide(damage, 3),
        50 => Formulas.Divide(damage, 2),
        _ => Math.Max(1, damage * percent / 100)
    };

    // ---------------------------------------------------------------- a status move, by its data

    /// <summary>
    /// A status move with nothing of its own to it: the condition, the stat changes and the healing its data
    /// names, each on whoever the move reaches.
    /// </summary>
    private void ByItsFields(MoveUse use)
    {
        var user = use.User;
        var move = use.Move;
        var data = move.Data;
        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);

        foreach (var t in use.Targets) use.Line.With(Shown(user, t, move, null));
        foreach (var t in use.Targets)
        {
            if (!t.IsActive) continue;
            if (t != user)
            {
                if (!Arrives(use, t)) continue;
                var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
                if (guards.Any(e => e.AbsorbsMove(this, t, user, move, DamageCalculator.Effectiveness(user, t, move, Rules)))) continue;

                // Thunder Wave is the one status move here that types can be immune to
                if (move.Type == PokemonType.Electric && data.InflictStatus == StatusCondition.Paralyze &&
                    DamageCalculator.Effectiveness(user, t, move, Rules) == 0f)
                {
                    Say($"It doesn't affect {t.Name}...");
                    continue;
                }
            }

            bool did = false;
            if (data.InflictStatus != StatusCondition.None) did |= TryInflictStatus(t, data.InflictStatus, user, true, By.Move);
            if (data.ConfuseChancePercent > 0) did |= Confuse(t, user, true, By.Move);
            if (data.TargetStatChange is { } stat)
            {
                var who = data.StatChangeTargetSelf ? user : t;
                did |= ChangeStat(who, stat, data.StatStageAmount, user, true, By.Move);
                foreach (var also in data.AlsoChangesStats ?? []) did |= ChangeStat(who, also, data.StatStageAmount, user, true, By.Move);
            }
            if (data.HealPercent > 0)
            {
                var p = t.Pokemon!;
                if (p.CurrentHP >= p.MaxHP) Say($"{t.Name}'s HP is full!");
                else RestoreHp(t, Formulas.Divide(p.MaxHP * data.HealPercent, 100), $"{t.Name} regained health!");
                did = true;
            }
            if (!did && data.InflictStatus == StatusCondition.None && data.TargetStatChange == null && data.ConfuseChancePercent == 0)
                Say("But nothing happened!");
        }
    }

    /// <summary>
    /// A status move's way to one target: Protect, the accuracy roll and a target out of reach, each with its
    /// line. A move's effect asks this before doing anything to another Pokémon.
    /// </summary>
    private bool Arrives(MoveUse use, Battler target)
    {
        var hit = new MoveHit { Target = target };
        if (Reaches(use, target, hit))
        {
            // From Generation 6 powders and spores do nothing to a Grass type
            if (!Rules.GrassTypesIgnorePowder || !use.Data.Flags.HasFlag(MoveFlags.Powder) || !target.HasType(PokemonType.Grass)) return true;
            Say($"It doesn't affect {target.Name}...");
            return false;
        }
        if (hit.Protected) Say($"{target.Name} protected itself!");
        else Say(use.Targets.Count > 1 || IsDouble ? $"{target.Name} avoided the attack!" : $"{use.User.Name}'s attack missed!");
        return false;
    }

    /// <summary>A side effect with this chance in a hundred happens (a roll of 100 below the chance).</summary>
    private bool Chance(int percent) => percent >= 100 || (percent > 0 && rng.Roll(RollKind.SideEffect, 100) < percent);

    /// <summary>Lets the Pokémon's ability and item react to a change in its HP or condition (berries, Synchronize).</summary>
    private void CheckConditionHooks(Battler b, Battler? cause)
    {
        if (b.Pokemon == null || b.Pokemon.IsFainted) return;
        foreach (var e in BattleEffects.Of(b).ToList()) e.OnConditionChanged(this, b, cause);
    }
}
