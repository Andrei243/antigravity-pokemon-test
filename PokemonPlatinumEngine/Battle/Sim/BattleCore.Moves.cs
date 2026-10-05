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

        /// <summary>The move failed for this target (nothing to give back, nothing to take): "But it failed!".</summary>
        public bool Failed;

        /// <summary>What is said in place of "the attack missed" (a one-hit knockout against a higher level, Sturdy).</summary>
        public string? FailLine;

        /// <summary>The last hit went into the Substitute, and whether a hit was the end of the Substitute.</summary>
        public bool IntoSubstitute, BrokeSubstitute;

        /// <summary>The last hit's damage; <see cref="Critical"/> and the two type flags hold for any of several hits.</summary>
        public DamageCalculator.DamageResult Damage;
        public bool Critical, SuperEffective, NotVeryEffective, LastCritical, OneHitKo;

        /// <summary>HP taken by all its hits (from the Pokémon or its Substitute), and by the last one.</summary>
        public int Dealt, LastDealt;

        /// <summary>Hits that landed; a later one that missed ended them (Triple Kick).</summary>
        public int Strikes;
        public bool Disrupted;

        /// <summary>Its lines about the type were said with one of its hits.</summary>
        public bool MessagesTold;
        public List<BattleEvent> Notes = new();
        public bool Landed => !Missed && !Protected && !OutOfReach && !Immune && !Absorbed && !Failed;

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

        /// <summary>How many hits the move makes on each target, and which of them is being made.</summary>
        public int StrikeCount = 1, HitNumber = 1;

        /// <summary>What an effect worked out for the use and reads back (a power, a party).</summary>
        public object? Scratch;

        /// <summary>A hit worked out by an effect of its own was critical.</summary>
        public bool StrikeCritical;

        /// <summary>A line said once the hits are told, before what the user gets out of them (Spit Up's stockpile is gone).</summary>
        public string? AfterLine;

        /// <summary>It is used by way of another move (Metronome, Sleep Talk…), bounced back by Magic Coat or snatched: no PP, and no record as the move chosen.</summary>
        public bool Called;

        /// <summary>What the damage is multiplied by before the roll, in tenths (Me First: 15).</summary>
        public int DamageTenths = 10;
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
            lastMoveShown = null;
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
                lastMoveShown = null;
                yield break;
            }
            int pp = 1 + use.Targets.Where(t => t.Side != user.Side).Sum(t => BattleEffects.Of(t).Sum(e => e.ExtraPpUsed));
            move.CurrentPP = Math.Max(0, move.CurrentPP - pp);
            if (user.IsPlayerSide) Evolution.CountMoveUse(user.Pokemon!, move.Name);
        }
        if (BattleEffects.Of(user).Any(e => e.LocksMoveChoice) && user.Pokemon!.Moves.Contains(move)) user.ChoiceLock = move;
        v.LastMove = move.Data == StruggleData ? null : move.Data;
        int slot = user.Pokemon!.Moves.IndexOf(move);
        if (slot >= 0 && move.Data.Effect != "FailIfNotUsedAllOtherMoves") v.UsedMoveSlots |= 1 << slot;

        // What Copycat copies next is the move chosen, even one that went on to call another (the original's
        // UpdateMoveBuffers, once the move is done)
        bool wentOn = Perform(use, goesOn);
        lastMoveShown = move.Data;
        if (!wentOn) yield break;
        foreach (var request in Leave(use)) yield return request;
    }

    /// <summary>
    /// The move from its turn to get ready, or "X used Y!", to what follows its hits. A move called by another
    /// (Metronome), bounced back by Magic Coat or snatched goes through here too. False when it ended before
    /// anything happened: a turn spent charging, or no target left.
    /// </summary>
    private bool Perform(MoveUse use, bool goesOn = false)
    {
        var user = use.User;
        var v = user.Volatile;
        var move = use.Move;

        // Normalize: every move its user makes is Normal, Hidden Power and Weather Ball included (the original
        // reads the ability wherever it reads a move's type; Retype keeps those moves' own types from winning)
        if (Has(user, "Normalize") && move.Type != PokemonType.Normal) use.Move = move = new Move(move.Data.OfType(PokemonType.Normal), move.CurrentPP);

        // A move that takes a turn to get ready spends it now
        if (use.Effect.Charge is { } charge && !use.Strikes && !SkipsTheCharge(use, charge))
        {
            BeginCharge(use, charge);
            return false;
        }

        // A move that goes on by itself may tell its later turns in its own words (Bide); a bounced or snatched
        // move was told with the line that took it
        bool ownLines = goesOn && use.Effect.OwnLinesAfterFirst;
        use.Line ??= ownLines ? new Said("") : Say($"{user.Name} used {move.Name}!");

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
            return false;
        }

        // Lightning Rod or Storm Drain drew it away from where it was aimed (subscript_lightning_rod_redirected)
        foreach (var t in use.Targets.Where(t => t.Turn.DrewTheMove))
        {
            t.Turn.DrewTheMove = false;
            Say($"{t.Name}'s {t.Ability?.Name} took the attack!");
        }

        // Whoever it is aimed at can send it back with Mirror Move (the original's moveCopied)
        if (move.Data.Flags.HasFlag(MoveFlags.Mirror))
            foreach (var t in use.Targets.Where(t => t != user)) t.Volatile.MirrorMove = move.Data;

        // Its ally's Helping Hand makes it half as strong again
        if (user.Turn.HelpingHand) use.Boost = use.Boost * 15 / 10;

        foreach (var t in use.Targets) t.Turn.SubstituteHit = false;
        if (BouncedOrSnatched(use)) return true;

        if (use.Effect.Does != null)
        {
            // A move that is more than a hit, or no hit at all
            if (!ownLines) use.Line.With(new Lunged(user.Place, move.Category));
            use.Effect.Use(this, use);
        }
        else if (move.Category != MoveCategory.Status && (move.Power > 0 || use.Effect.Damaging) && (move.Data.Support != MoveEffectSupport.None || use.Effect != Plain))
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
        return true;
    }

    /// <summary>
    /// A move used by way of another (Metronome, Mirror Move, Sleep Talk, Assist, Copycat, Me First; the original's
    /// <c>GoToMoveScript</c>): it costs no PP, has its own "used" line, and goes where the calling move was aimed,
    /// or at a foe when it wasn't. What takes its user off the field (a called U-turn) is left to the calling
    /// move's end.
    /// </summary>
    private void CallMove(MoveUse outer, MoveData data, Battler? target, int damageTenths = 10)
    {
        var user = outer.User;
        var inner = new MoveUse { User = user, Move = new Move(data), Effect = EffectOf(data), MoveBefore = outer.MoveBefore, Called = true, DamageTenths = damageTenths };
        inner.Targets = ResolveTargets(user, inner.Move, target);
        if (Perform(inner)) outer.Leaves = inner.Leaves;
    }

    /// <summary>
    /// Magic Coat sends a status move that can be reflected back at whoever used it, and Snatch takes one its
    /// user would have used on itself, the fastest snatcher first (<c>BattleControllerPlayer_MoveStolen</c>). The
    /// move then runs as the other's, under the line that took it, and the one it was taken from has had its turn.
    /// </summary>
    private bool BouncedOrSnatched(MoveUse use)
    {
        var user = use.User;
        var data = use.Data;
        if (data.Flags.HasFlag(MoveFlags.Reflectable) && use.Targets.FirstOrDefault(t => t != user && t.IsActive && t.Turn.MagicCoat) is { } coat)
        {
            coat.Turn.MagicCoat = false;
            UseAs(coat, data, user, Say($"{user.Name}'s {use.Move.Name} was bounced back by Magic Coat!"));
            return true;
        }
        if (data.Flags.HasFlag(MoveFlags.Snatch) && speedOrder.Concat(AllBattlers).Distinct().FirstOrDefault(b => b != user && b.IsActive && b.Turn.Snatching) is { } snatcher)
        {
            snatcher.Turn.Snatching = false;
            UseAs(snatcher, data, snatcher, Say($"{snatcher.Name} snatched {user.Name}'s move!"));
            return true;
        }
        return false;
    }

    /// <summary>Runs a move as another Pokémon's, aimed where it is told, under a line already said in place of "X used Y!".</summary>
    private void UseAs(Battler asUser, MoveData data, Battler target, Said line)
    {
        var inner = new MoveUse { User = asUser, Move = new Move(data), Effect = EffectOf(data), MoveBefore = asUser.Volatile.LastMove, Called = true, Line = line };
        inner.Targets = ResolveTargets(asUser, inner.Move, target);
        Perform(inner);
    }

    /// <summary>
    /// A move landed on a Pokémon from across the field: what Conversion 2 answers to (the original's
    /// <c>conversion2Move</c>, kept in <c>UpdateFlagsWhenHit</c> for a move that succeeded and wasn't aimed at
    /// its user's own side).
    /// </summary>
    private static void RememberHit(Battler target, Battler user, Move move)
    {
        if (target == user || target.Side == user.Side) return;
        var v = target.Volatile;
        v.Conversion2Move = move.Data;
        v.Conversion2Type = move.Type;
        v.Conversion2By = user.Place;
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

        // Truant loafs every other turn, from the one after its first (Battler_CheckTruant)
        if (Has(user, "Truant") && (Turn & 1) != v.TruantParity)
        {
            Say($"{user.Name} is loafing around!");
            Unlock(user);
            return false;
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

        if (v.TauntTurns > 0 && move.Category == MoveCategory.Status)
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
                    user.Turn.TookDamage = true;
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
        v.RolloutTurns = 0;
        v.FuryCutterCount = 0;
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
                if (Centre() is { } drawnTo) return new List<Battler> { drawnTo };
                return foes.Count == 0 ? foes : Drawn(foes[rng.Roll(RollKind.Target, foes.Count)]);
            default:
                // Follow Me on the other side draws every move aimed at one Pokémon to its user, while that one stands
                if (Centre() is { } centre) return new List<Battler> { centre };
                if (chosen != null && chosen.IsActive && chosen != user) return Drawn(chosen);
                // The chosen foe is gone: the move goes to the other one (an ally that fainted leaves nothing to hit)
                if (chosen != null && chosen.Side == user.Side) return new List<Battler>();
                return foes.Count == 0 ? foes : Drawn(foes[rng.Roll(RollKind.Target, foes.Count)]);
        }

        Battler? Centre() => Field.Side(Other(user.Side)).FollowMe is { } place && At(place) is { IsActive: true } centre ? centre : null;

        // Lightning Rod and Storm Drain (BattleSystem_CheckRedirectionAbilities): an Electric or a Water move aimed
        // at one Pokémon goes to the fastest holder that isn't its user, unless it is on its turn of getting ready
        // or its user has Normalize or Mold Breaker. Nothing is said when it was aimed at the holder already.
        List<Battler> Drawn(Battler target)
        {
            if (Has(user, "Normalize") || Has(user, "Mold Breaker") || move.Type is not (PokemonType.Electric or PokemonType.Water)) return new List<Battler> { target };
            if (EffectOf(move.Data).Charge != null && !user.Volatile.Charging) return new List<Battler> { target };
            var holder = BySpeed().FirstOrDefault(b => b.IsActive && b != user && b.Ability?.Effect?.DrawsMovesOf(move.Type) == true);
            if (holder == null || holder == target) return new List<Battler> { target };
            holder.Turn.DrewTheMove = true;
            return new List<Battler> { holder };
        }
    }

    /// <summary>The effect a move plays on one target: what kind of move it is and, for a damaging move, how it landed.</summary>
    private static MoveShown Shown(Battler user, Battler target, Move move, MoveHit? hit) => new(
        move.Name, move.Type, move.Category, user.Place, target.Place,
        Missed: hit != null && (hit.Missed || hit.OutOfReach),
        Blocked: hit != null && (hit.Immune || hit.Absorbed || hit.Protected),
        Critical: hit is { Landed: true } && hit.Critical,
        SuperEffective: hit is { Landed: true } && hit.SuperEffective);

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

        if (use.Effect.Decides != null)
        {
            // A one-hit knockout decides for itself whether it lands; Protect and a target out of reach still stand in its way
            if (target.Turn.Protecting && use.Data.Flags.HasFlag(MoveFlags.Protect) && use.Effect.StoppedByProtect(user))
            {
                hit.Protected = true;
                Unlock(user);
                return false;
            }
            if (target.IsElsewhere && !use.Effect.FollowsTo(target.Volatile.Elsewhere))
            {
                hit.OutOfReach = true;
                return false;
            }
            return use.Effect.Decides(this, use, target, hit);
        }

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

        int accuracy = user.Pokemon!.StatStages.GetValueOrDefault(StatType.Accuracy);
        int evasion = target.Pokemon!.StatStages.GetValueOrDefault(StatType.Evasion);
        // Under Platinum's rules Simple's stages count double; Unaware takes no notice of the other's
        if (!Rules.SimpleDoublesChanges)
        {
            if (Has(user, "Simple")) accuracy *= 2;
            if (!breaks && Has(target, "Simple")) evasion *= 2;
        }
        if (targetEffects.Any(e => e.IgnoresOthersStatStages)) accuracy = 0;
        if (userEffects.Any(e => e.IgnoresOthersStatStages)) evasion = 0;
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

    /// <summary>The held item's effect, unless an Embargo or Klutz has put it out of use.</summary>
    private static BattleEffect? ItemOf(Battler b) => BattleEffects.ItemOf(b);

    /// <summary>
    /// A move takes the type its own rule gives it (Hidden Power, Judgment, Weather Ball, Natural Gift), unless its
    /// user has Normalize, whose Normal wins over every one of them.
    /// </summary>
    private static void Retype(MoveUse use, PokemonType type)
    {
        if (Has(use.User, "Normalize") || type == use.Move.Type) return;
        use.Move = new Move(use.Data.OfType(type), use.Move.CurrentPP);
    }

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
        if (!use.Line.Shows.OfType<Lunged>().Any()) use.Line.With(new Lunged(user.Place, move.Category));

        // Explosion and its kind cost their user everything before the hit, whatever comes of it
        if (effect.SelfKo)
        {
            user.Pokemon!.CurrentHP = 0;
            use.Line.With(new HpChanged(user.Place, 0, Healed: false));
        }

        use.StrikeCount = Math.Max(1, effect.Strikes?.Invoke(this, use) ?? 1);
        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        foreach (var t in use.Targets)
        {
            var hit = new MoveHit { Target = t };
            use.Hits.Add(hit);
            if (!Reaches(use, t, hit)) continue;
            effect.OnReach(this, use, t);

            float effectiveness = use.Data == StruggleData || effect.HitsAnything ? 1f : DamageCalculator.Effectiveness(user, t, move, Rules);
            var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
            hit.Notes.AddRange(Capture(() => hit.Absorbed = guards.Any(e => e.AbsorbsMove(this, t, user, move, effectiveness))));
            if (hit.Absorbed) continue;
            if (effectiveness == 0f)
            {
                hit.Immune = true;
                continue;
            }

            for (int n = 1; n <= use.StrikeCount; n++)
            {
                use.HitNumber = n;
                if (n > 1)
                {
                    // The hits stop once the target is down or their user has been put to sleep (Effect Spore); Triple Kick rolls each kick
                    if (!t.IsActive || user.Pokemon!.Status == StatusCondition.Sleep) break;
                    if (effect.EachHitRollsAccuracy && !Hits(use, t))
                    {
                        hit.Disrupted = true;
                        break;
                    }
                }
                if (effect.StrikeLine?.Invoke(this, use) is { } line) Say(line);
                if (!StrikeOnce(use, hit, userBreaks)) break;
                if (use.StrikeCount > 1) AfterOneOfSeveral(use, hit, n == use.StrikeCount);
            }
            if (hit.Landed) RememberHit(t, user, move);
        }

        // The move's effect flies to each target and lands with the damage
        foreach (var hit in use.Hits) use.Line.With(Shown(user, hit.Target, move, hit));
        if (use.Hits.Any(h => h.Landed)) use.Line.AtImpact(new HitSounded(use.Hits.Any(h => h.Landed && h.SuperEffective)));
        use.DamageDealt = use.Hits.Where(h => h.Landed).Sum(h => h.Dealt);

        AfterStrike(use);
    }

    /// <summary>
    /// One hit on one target: the damage (the formula's, or an amount of the move's own), where it goes (the
    /// Substitute, or the Pokémon), what holds it off, and what the target records of it. False when the move
    /// fails for this target.
    /// </summary>
    private bool StrikeOnce(MoveUse use, MoveHit hit, bool userBreaks)
    {
        var user = use.User;
        var t = hit.Target;
        var move = use.Move;
        var effect = use.Effect;

        DamageCalculator.DamageResult result;
        if (effect.Deals != null)
        {
            int? amount = effect.Deals(this, use, t);
            if (amount == null)
            {
                hit.Failed = true;
                return false;
            }
            result = new DamageCalculator.DamageResult { Damage = Math.Max(1, amount.Value), TypeMultiplier = 1f };
        }
        else
        {
            result = DamageCalculator.Calculate(user, t, move, rng, spread: use.Targets.Count > 1, rules: Rules,
                powerTenths: effect.PowerTenths(this, use, t), critBonus: effect.CritBonus, pastScreens: effect.PastScreens,
                basePower: effect.BasePower?.Invoke(this, use, t), noVariance: effect.NoVariance, damageTenths: use.DamageTenths);
        }
        bool critical = result.IsCritical || use.StrikeCritical;
        use.StrikeCritical = false;
        hit.Damage = result;
        hit.LastCritical = critical;
        hit.Critical |= critical;
        hit.SuperEffective |= result.IsSuperEffective;
        hit.NotVeryEffective |= result.IsNotVeryEffective;
        int damage = result.Damage;

        // A Substitute takes the hit in the Pokémon's place
        if (t.HasSubstitute && t != user)
        {
            int taken = Math.Min(t.Volatile.SubstituteHp, damage);
            t.Volatile.SubstituteHp -= taken;
            hit.IntoSubstitute = true;
            hit.BrokeSubstitute = t.Volatile.SubstituteHp == 0;
            hit.Dealt += taken;
            hit.LastDealt = taken;
            hit.Strikes++;
            t.Turn.SubstituteHit = true;
            return true;
        }
        hit.IntoSubstitute = false;

        var p = t.Pokemon!;
        if (hit.OneHitKo) damage = p.MaxHP;
        // False Swipe leaves one
        if (effect.LeavesOneHp && damage >= p.CurrentHP) damage = p.CurrentHP - 1;
        int dealt = Math.Min(p.CurrentHP, damage);
        bool endured = false;
        if (dealt >= p.CurrentHP)
        {
            if (t.Turn.Enduring) hit.Endured = hit.EnduredByMove = endured = true;
            else
            {
                var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
                hit.Notes.AddRange(Capture(() => endured = guards.Any(e => e.EnduresHit(this, t, dealt))));
                if (endured) hit.Endured = true;
            }
            if (endured) dealt = p.CurrentHP - 1;
        }

        // What the target took this turn, for Counter, Mirror Coat, Metal Burst, Revenge, Assurance and Focus Punch
        t.Turn.Took(user.Place, move.Category, endured ? dealt : damage);
        p.CurrentHP -= dealt;
        t.Volatile.LastHitBy = user.Place;
        if (t.IsPlayerSide) Evolution.CountDamageTaken(p, dealt);
        if (user.IsPlayerSide && critical) Evolution.CountCriticalHit(user.Pokemon!);
        if (t.Volatile.BideTurns > 0) t.Volatile.BideDamage += dealt;
        use.Line.AtImpact(new Struck(t.Place, p.CurrentHP, critical || result.IsSuperEffective));
        hit.Dealt += dealt;
        hit.LastDealt = dealt;
        hit.Strikes++;
        return true;
    }

    /// <summary>
    /// After one hit of a move that hits several times (the original's <c>AfterMoveMessage</c> for each): what the
    /// Substitute took, a critical hit, the type's effect on the last hit only, then the hit's own side effects.
    /// </summary>
    private void AfterOneOfSeveral(MoveUse use, MoveHit hit, bool lastPlanned)
    {
        var t = hit.Target;
        log.AddRange(hit.Notes);
        hit.Notes.Clear();
        if (hit.IntoSubstitute) SubstituteLines(hit);
        if (hit.LastCritical) Say(use.Hits.Count > 1 ? $"A critical hit on {t.Name}!" : "A critical hit!");
        if (lastPlanned || !t.IsActive || use.User.Pokemon!.Status == StatusCondition.Sleep)
        {
            EffectivenessLine(use, hit);
            hit.MessagesTold = true;
        }
        if (hit.LastCritical && hit.Touched) t.TookCriticalHit = true;
        TargetEffects(use, hit);
    }

    /// <summary>"The substitute took damage", and that it is gone when a hit was the end of it.</summary>
    private void SubstituteLines(MoveHit hit)
    {
        var t = hit.Target;
        var took = Say($"The substitute took damage for {t.Name}!");
        if (!hit.BrokeSubstitute || t.HasSubstitute) return;
        took.With(new SubstituteChanged(t.Place, Up: false));
        Say($"{t.Name}'s substitute faded!");
        hit.BrokeSubstitute = false;
    }

    /// <summary>What the type made of the hit (<c>subscript_move_followup_message</c>): a one-hit knockout says so instead.</summary>
    private void EffectivenessLine(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        bool several = use.Hits.Count > 1;
        if (hit.OneHitKo && !hit.Endured) Say("It's a one-hit KO!");
        else if (hit.SuperEffective) Say(several ? $"It's super effective on {t.Name}!" : "It's super effective!");
        else if (hit.NotVeryEffective) Say(several ? $"It's not very effective on {t.Name}..." : "It's not very effective...");
    }

    /// <summary>
    /// What a hit does to its target beyond the damage: the move's side effects, what it does of its own, Rage,
    /// the target's ability and items, a thaw.
    /// </summary>
    private void TargetEffects(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        var move = use.Move;
        var data = move.Data;
        int dealt = hit.LastDealt;
        var userEffects = BattleEffects.Of(user).ToList();
        bool userBreaks = userEffects.Any(e => e.IgnoresTargetAbility);
        int chanceMult = userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();
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
        if (standing && hit.Touched && dealt > 0 && t != user && t.Volatile.Rage && t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) < 6)
        {
            int stage = t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) + 1;
            t.Pokemon.StatStages[StatType.Attack] = stage;
            Say($"{t.Name}'s rage is building!").With(new StageChanged(t.Place, StatType.Attack, stage, Rose: true));
        }

        // The target's ability reacts to the hit (Static, Rough Skin), and berries check its HP
        if (hit.Touched && dealt > 0)
        {
            foreach (var e in BattleEffects.Of(t, includeAbility: !userBreaks).ToList())
                e.AfterHit(this, t, user, move, dealt, hit.LastCritical);
            CheckConditionHooks(t, user);
        }

        // A Fire move thaws a frozen target
        if (t.Pokemon.CurrentHP > 0 && hit.Touched && dealt > 0 && t != user && move.Type == PokemonType.Fire && t.Pokemon.Status == StatusCondition.Freeze)
        {
            t.Pokemon.Status = StatusCondition.None;
            Say($"{t.Name} thawed out!").With(new StatusChanged(t.Place, StatusCondition.None));
        }
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
                Say(hit.FailLine ?? (several || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!"));
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (hit.Failed)
            {
                Say("But it failed!");
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

            if (use.StrikeCount > 1)
            {
                // Each hit told its own; the type's effect waits for the last, and then comes the count
                if (!hit.MessagesTold) EffectivenessLine(use, hit);
                if (!use.Effect.NoHitCount) Say($"Hit {hit.Strikes} time{(hit.Strikes == 1 ? "" : "s")}!");
                continue;
            }

            if (hit.IntoSubstitute) SubstituteLines(hit);
            if (hit.LastCritical) Say(several ? $"A critical hit on {t.Name}!" : "A critical hit!");
            if (hit.EnduredByMove) Say($"{t.Name} endured the hit!");
            else if (hit.Endured) log.AddRange(hit.Notes);
            EffectivenessLine(use, hit);
            if (hit.LastCritical && hit.Touched) t.TookCriticalHit = true;
            TargetEffects(use, hit);
        }

        if (use.AfterLine != null) Say(use.AfterLine);

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

        // Now whoever the move knocked out goes down; the user of Explosion after its targets
        ResolveFaints(use, use.Effect.SelfKo ? user : null);
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
        // Unburden is armed again by an item in its hands (the original's RecoverStatusByAbility, after every move)
        if (b.Pokemon.HeldItem != null) b.Volatile.CanUnburden = true;
        foreach (var e in BattleEffects.Of(b).ToList()) e.OnConditionChanged(this, b, cause);
    }
}
