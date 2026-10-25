using PokemonPlatinumEngine.Core;
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
        // The data's own words, as every other move's description is (plan 17 · N3 gives them in every language)
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
    private enum Leaving { No, Returns, PassesTheBaton, MakesAWish, DancesAway, ShedsItsTail }

    /// <summary>One use of a move: who, what, at whom, and what came of it.</summary>
    private sealed class MoveUse
    {
        public required Battler User;
        public required Move Move;
        public required MoveEffect Effect;
        public MoveData Data => Move.Data;

        /// <summary>The move's data as the Pokémon knows it, before Normalize made it Normal: what Mirror Move copies and Magic Coat sends back.</summary>
        public MoveData? Own;
        public List<Battler> Targets = new();

        /// <summary>A status move missed a target by its accuracy roll (plan 06 · R28b): Blunder Policy goes off for it.</summary>
        public bool StatusMissed;

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
        // (A Dynamaxed Pokémon's Max Move isn't turned aside: plan 06 · R22)
        if (!goesOn && move.Data != StruggleData && !v.Dynamaxed && v.Encored != null && user.Pokemon!.Moves.FirstOrDefault(m => m.Data == v.Encored) is { } encored && encored != move)
        {
            move = encored;
            target = null;
        }

        user.MovedThisTurn = true;
        PaysForTheRush(user);
        ItemLetItMoveFirst(act);
        if (!CanMove(user, move) || (!goesOn && Disobeys(user, ref move, ref target)))
        {
            // Stomping Tantrum counts a move lost this way as a move that failed (plan 06 · R25)
            failedOnTurn[user.Pokemon!] = Turn;
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
                Say(Text.Of("battle.used", ("user", user.Name), ("move", move.DisplayName)));
                Say(Text.Of("battle.butThereWasNo"));
                v.LastMove = null;
                lastMoveShown = null;
                yield break;
            }
            int pp = 1 + use.Targets.Where(t => t.Side != user.Side).Sum(t => BattleEffects.Of(t).Sum(e => e.ExtraPpUsed));
            move.CurrentPP = Math.Max(0, move.CurrentPP - pp);
            if (user.IsPlayerSide) Evolution.CountMoveUse(user.Pokemon!, move.Name);
        }
        if (BattleEffects.Of(user).Any(e => e.LocksMoveChoice) && user.Pokemon!.Moves.Contains(move) && !v.Dynamaxed) user.ChoiceLock = move;
        v.LastMove = move.Data == StruggleData ? null : move.Data;
        v.LastMoveTarget = target?.Place;
        // Using a move of its own forgets the one that hit it (BattleControllerPlayer_MoveEnd)
        v.LastHitMove = null;
        v.LastHitMoveBy = null;
        CountForMetronome(user, move);
        int slot = user.Pokemon!.Moves.IndexOf(move);
        if (slot >= 0 && move.Data.Effect != "FailIfNotUsedAllOtherMoves") v.UsedMoveSlots |= 1 << slot;

        // What Copycat copies next is the move chosen, even one that went on to call another (the original's
        // UpdateMoveBuffers, once the move is done)
        // Whether the move failed, as Stomping Tantrum asks next turn (plan 06 · R25): a "But it failed!" said for
        // it, or every target missed, protected or immune (Showdown's moveThisTurnResult)
        // A move chosen as a Z-Move becomes it, now that it is paid for (plan 06 · R21)
        if (act.Choice.ZMove && !goesOn) BecomeZMove(use, target);
        // A Dynamaxed Pokémon's move becomes its Max Move (plan 06 · R22)
        else if (v.Dynamaxed && !goesOn) BecomeMaxMove(use, target);
        failedNow = false;
        exits.Clear();
        bool wentOn = Perform(use, goesOn);
        bool failed = failedNow || (use.Hits.Count > 0 && !use.Hits.Any(h => h.Landed));
        if (failed) failedOnTurn[user.Pokemon!] = Turn;
        failedNow = false;
        lastMoveShown = move.Data;
        if (!wentOn)
        {
            ForgetItemsAfterTheMove();
            yield break;
        }
        // A dance danced again, and whoever the move sent running (plan 06 · R27b)
        if (!failed) Dance(use);
        foreach (var request in EmergencyExits(use)) yield return request;
        // What the later held items do once the move is done (plan 06 · R28a): Red Card, Eject Button, Eject Pack and the rest
        foreach (var request in ItemsAfterTheMove(use)) yield return request;
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
        use.Own ??= move.Data;
        use.Move = move = Normalized(user, move);
        // Ion Deluge, Plasma Fists and Electrify make it Electric (plan 06 · R25)
        use.Move = move = Electrified(user, move);
        // The later abilities' say on the move (plan 06 · R27): its type, its touch, its side effects, Protean
        ShapedByAbility(use);
        // And the later held items' (plan 06 · R28a): a Punching Glove's or Protective Pads' touch
        ShapedByItem(use);
        move = use.Move;

        // Aegislash draws or sheathes its blade before the move (plan 06 · R19b)
        StanceChange(user, move);

        // A move that takes a turn to get ready spends it now
        if (use.Effect.Charge is { } charge && !use.Strikes && !SkipsTheCharge(use, charge))
        {
            BeginCharge(use, charge);
            return false;
        }

        // A move that goes on by itself may tell its later turns in its own words (Bide); a bounced or snatched
        // move was told with the line that took it
        bool ownLines = goesOn && use.Effect.OwnLinesAfterFirst;
        use.Line ??= ownLines ? new Said("") : Say(Text.Of("battle.used", ("user", user.Name), ("move", move.DisplayName)));

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

        // A Fire move set off the powder on its user (plan 06 · R25)
        if (Explodes(use)) return true;

        if (use.Targets.Count == 0)
        {
            Say(Text.Of("battle.butThereWasNoTarget"));
            Unlock(user);
            return false;
        }

        // Lightning Rod or Storm Drain drew it away from where it was aimed (subscript_lightning_rod_redirected)
        foreach (var t in use.Targets.Where(t => t.Turn.DrewTheMove))
        {
            t.Turn.DrewTheMove = false;
            Say(Text.Of("battle.tookTheAttack", ("target", t.Name), ("ability", AbilityOf(t))));
        }

        // Whoever it is aimed at can send it back with Mirror Move (the original's moveCopied)
        if (move.Data.Flags.HasFlag(MoveFlags.Mirror))
            foreach (var t in use.Targets.Where(t => t != user)) t.Volatile.MirrorMove = use.Own;

        // (An ally's Helping Hand is a step of the damage formula's own: DamageCalculator reads the turn's flag)
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
                Say(Text.Of("battle.butNothingHappened"));
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
        var data = use.Own ?? use.Data;
        // Magic Bounce sends it back before Magic Coat would (plan 06 · R27)
        if (BouncedByAbility(use)) return true;
        if (data.Flags.HasFlag(MoveFlags.Reflectable) && use.Targets.FirstOrDefault(t => t != user && t.IsActive && t.Turn.MagicCoat) is { } coat)
        {
            coat.Turn.MagicCoat = false;
            UseAs(coat, data, user, Say(Text.Of("battle.wasBouncedBackBy", ("user", user.Name), ("move", use.Move.DisplayName))));
            return true;
        }
        if (data.Flags.HasFlag(MoveFlags.Snatch) && speedOrder.Concat(AllBattlers).Distinct().FirstOrDefault(b => b != user && b.IsActive && b.Turn.Snatching) is { } snatcher)
        {
            snatcher.Turn.Snatching = false;
            UseAs(snatcher, data, snatcher, Say(Text.Of("battle.snatchedMove", ("snatcher", snatcher.Name), ("user", user.Name))));
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
        var v = target.Volatile;
        // What the trainer AI looks at when it thinks of switching (the original's moveHit and moveHitBattler)
        if (target != user)
        {
            v.LastHitMove = move.Data;
            v.LastHitMoveBy = user.Place;
        }
        if (target == user || target.Side == user.Side) return;
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

        // Carried into the sky by a foe's Sky Drop, it does nothing, and nothing is said (plan 06 · R25's leftovers)
        if (v.LiftedBy != null) return false;
        // A Tatsugiri in its Dondozo's mouth does nothing (plan 06 · R27b; Showdown's commanding onBeforeTurn)
        if (v.Commanding != null) return false;

        if (p.Status == StatusCondition.Sleep)
        {
            if (UproarIsOn && !Has(user, "Soundproof")) WakeUp(user, "battle.theUproarWokeUp");
            else
            {
                p.SleepTurns = Math.Max(0, p.SleepTurns - effects.Select(e => e.SleepCountdownRate).DefaultIfEmpty(1).Max());
                if (p.SleepTurns == 0) WakeUp(user, "battle.wokeUp");
                else
                {
                    // Snore and Sleep Talk are told the same way, and then go on
                    Say(Text.Of("battle.isFastAsleep", ("user", user.Name))).With(Moment(user, BattleMoment.Asleep));
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
                Say(move.Data.ThawsUser ? Text.Of("battle.meltedTheIce", ("user", user.Name), ("move", move.DisplayName)) : Text.Of("battle.thawedOut", ("user", user.Name))).With(new StatusChanged(user.Place, StatusCondition.None));
            }
            else
            {
                Say(Text.Of("battle.isFrozenSolid", ("user", user.Name))).With(Moment(user, BattleMoment.Frozen));
                return false;
            }
        }

        // Truant loafs every other turn, from the one after its first
        if (Loafs(user))
        {
            Say(Text.Of("battle.isLoafingAround", ("user", user.Name)));
            Unlock(user);
            return false;
        }

        if (v.Recharging)
        {
            v.Recharging = false;
            Say(Text.Of("battle.mustRecharge", ("user", user.Name)));
            Unlock(user);
            return false;
        }

        if (user.Flinched)
        {
            user.Flinched = false;
            Say(Text.Of("battle.flinched", ("user", user.Name)));
            Unlock(user);
            foreach (var e in effects) e.OnFlinch(this, user);
            return Lost(user);
        }

        if (v.Disabled != null && v.Disabled == move.Data)
        {
            Say(Text.Of("battle.isDisabled", ("pokemon", user.Name), ("move", move.DisplayName)));
            Unlock(user);
            return Lost(user);
        }

        if (v.TauntTurns > 0 && move.Category == MoveCategory.Status)
        {
            Say(Text.Of("battle.cantUseAfterThe", ("pokemon", user.Name), ("move", move.DisplayName)));
            Unlock(user);
            return Lost(user);
        }

        // Throat Chop keeps a sound move from it for two turns (plan 06 · R25)
        if (ThroatChopped(user, move.Data))
        {
            Say(Text.Of("battle.cantUseBecauseOfThroatChop", ("pokemon", user.Name), ("move", move.DisplayName)));
            Unlock(user);
            return Lost(user);
        }

        if (IsSealed(user, move.Data))
        {
            Say(Text.Of("battle.cantUseTheSealed", ("pokemon", user.Name), ("move", move.DisplayName)));
            Unlock(user);
            return Lost(user);
        }

        if (Field.Gravity && FailsUnderGravity(move.Data))
        {
            Say(Text.Of("battle.cantUseBecauseOf", ("pokemon", user.Name), ("move", move.DisplayName)));
            return Lost(user);
        }

        if (v.HealBlockTurns > 0 && IsHealingMove(move.Data))
        {
            Say(Text.Of("battle.cantUseWhileIt", ("pokemon", user.Name), ("move", move.DisplayName)));
            return false;
        }

        if (user.IsConfused)
        {
            if (--user.ConfusionTurns <= 0)
            {
                Say(Text.Of("battle.snappedOutOfConfusion", ("user", user.Name))).With(Passing(user));
            }
            else
            {
                Say(Text.Of("battle.isConfused", ("user", user.Name))).With(new Confused(user.Place)).With(Moment(user, BattleMoment.Confused));
                // It hurts itself one time in the rules' odds (the die's first face): a typeless 40-power hit
                if (rng.Roll(RollKind.ConfusionSelfHit, Rules.ConfusionSelfHitOdds) == 0)
                {
                    var self = DamageCalculator.Calculate(user, user, move, rng, spread: false, powerOverride: 40, rules: Rules);
                    p.CurrentHP -= Math.Min(p.CurrentHP, self.Damage);
                    user.Turn.TookDamage = true;
                    Say(Text.Of("battle.itHurtItselfIn"))
                        .AtImpact(new Struck(user.Place, p.CurrentHP, Hard: false)).AtImpact(new HitSounded(false));
                    Unlock(user);
                    return Lost(user);
                }
            }
        }

        // One time in four; a Pokémon with Magic Guard is never held by it, as in Platinum
        if (p.Status == StatusCondition.Paralyze && !effects.Any(e => e.PreventsIndirectDamage) && rng.Roll(RollKind.FullParalysis, 4) == 0)
        {
            Say(Text.Of("battle.isParalyzedItCant", ("user", user.Name))).With(Moment(user, BattleMoment.Paralyzed));
            Unlock(user);
            return Lost(user);
        }

        if (v.InLoveWith is { } beloved)
        {
            Say(Text.Of("battle.isInLoveWith", ("user", user.Name), ("beloved", At(beloved).Name))).With(Moment(user, BattleMoment.Infatuated));
            if (rng.Roll(RollKind.Infatuation, 2) == 0)
            {
                Say(Text.Of("battle.isImmobilizedByLove", ("user", user.Name)));
                Unlock(user);
                return Lost(user);
            }
        }
        return true;
    }

    /// <summary>
    /// <c>BattleControllerPlayer_CheckObedience</c>, after what can stop a move and before its PP (plan 06 · R10):
    /// a Pokémon of the player's whose original trainer is someone else, over the level the badges command
    /// (<see cref="Obedience"/>), may not do as it's told. Each test is a roll of 256 against its level and the
    /// cap. Failing the first, it ignores an order to Snore or Sleep Talk while asleep; passing the second, it uses
    /// another of its moves it could pick instead (true is not returned: the move and target are changed);
    /// otherwise, by how far over the cap it is, it lies down for a nap, hurts itself as a confused Pokémon does,
    /// or does nothing. True when it doesn't use a move at all.
    /// </summary>
    private bool Disobeys(Battler user, ref Move move, ref Battler? target)
    {
        var p = user.Pokemon!;
        if (!user.IsPlayerSide || user.Trainer != null || Kind == BattleKind.CatchingLesson || Frontier) return false;
        if (!Obedience.IsOutsider(p, Conditions.Player)) return false;
        if (Obedience.LevelCap(Conditions.Badges) is not { } cap || p.Level <= cap) return false;
        if (Obedience.Obeys(p.Level, cap, rng.Roll(RollKind.Obedience, 256))) return false;

        if (move.Data.Effect == "RaiseAtkWhenHit") user.Volatile.Rage = false;
        if (p.Status == StatusCondition.Sleep && UsableAsleep(move.Data))
        {
            Say(Text.Of("battle.ignoredTheOrderAnd", ("user", user.Name)));
            return true;
        }

        if (Obedience.Obeys(p.Level, cap, rng.Roll(RollKind.Obedience, 256)))
        {
            var chosen = move;
            var others = p.Moves.Where(m => m != chosen && WhyNotMove(user, m) == null).ToList();
            if (others.Count == 0)
            {
                DoesNothing(user);
                return true;
            }
            // The original draws a place of four until it lands on one it could pick
            Move other;
            do
            {
                int slot = rng.Roll(RollKind.Obedience, 4);
                other = slot < p.Moves.Count ? p.Moves[slot] : chosen;
            }
            while (!others.Contains(other));
            Say(Text.Of("battle.ignoredTheOrderAndDid", ("user", user.Name)));
            move = other;
            target = null;
            return false;
        }

        int over = p.Level - cap;
        int roll = rng.Roll(RollKind.Obedience, 256);
        if (roll < over && p.Status == StatusCondition.None && !Has(user, "Vital Spirit") && !Has(user, "Insomnia") && !UproarIsOn)
        {
            Say(Text.Of("battle.layDownForA", ("user", user.Name)));
            TryInflictStatus(user, StatusCondition.Sleep, null, false, By.Other);
            return true;
        }
        if (roll - over < over)
        {
            // A typeless hit of 40 on itself, as a confused Pokémon's
            Say(Text.Of("battle.wontDoAsIt", ("user", user.Name)));
            var self = DamageCalculator.Calculate(user, user, move, rng, spread: false, powerOverride: 40, rules: Rules);
            p.CurrentHP -= Math.Min(p.CurrentHP, self.Damage);
            user.Turn.TookDamage = true;
            Say(Text.Of("battle.itHurtItselfIn"))
                .AtImpact(new Struck(user.Place, p.CurrentHP, Hard: false)).AtImpact(new HitSounded(false));
            Unlock(user);
            return true;
        }
        DoesNothing(user);
        return true;
    }

    /// <summary>One of four ways of not listening (<c>subscript_disobey_do_nothing</c>), drawn at random.</summary>
    private void DoesNothing(Battler user) => Say(rng.Roll(RollKind.Obedience, 4) switch
    {
        1 => Text.Of("battle.wontListen", ("user", user.Name)),
        2 => Text.Of("battle.lookedTheOtherWay", ("user", user.Name)),
        3 => Text.Of("battle.actedAsIfIt", ("user", user.Name)),
        _ => Text.Of("battle.isLazingAbout", ("user", user.Name))
    });

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
        Say(string.Format(Text.Of(line), b.Name)).With(new StatusChanged(b.Place, StatusCondition.None));
    }

    private static bool Has(Battler b, string ability) => b.Ability?.Name == ability;

    /// <summary><c>Battler_CheckTruant</c>: a Pokémon with Truant is loafing this turn. It then doesn't move, tighten its focus or pursue.</summary>
    private bool Loafs(Battler b) => Has(b, "Truant") && (Turn & 1) != b.Volatile.TruantParity;

    /// <summary>Snore and Sleep Talk are for a sleeping Pokémon to use.</summary>
    private static bool UsableAsleep(MoveData move) => move.Effect is "DamageWhileAsleep" or "UseRandomLearnedMoveSleep";

    /// <summary>The moves that can't be used while Gravity holds (the original's list: Fly, Bounce, the Jump Kicks, Splash, Magnet Rise).</summary>
    private static bool FailsUnderGravity(MoveData move) => move.Effect is "Fly" or "Bounce" or "CrashOnMiss" or "DoNothing" or "GiveGroundImmunity" or "SkyDrop" or "Telekinesis" or "FlyingPress";

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
        // Sky Drop lets go of whoever it carried, or of whoever carried it (plan 06 · R25's leftovers)
        LetGoOfTheSky(b);
        if (!b.IsElsewhere) return;
        b.Volatile.Elsewhere = Elsewhere.No;
        Emit(new Reappeared(b.Place));
    }

    // ---------------------------------------------------------------- a turn to get ready

    /// <summary>The sun charges Solar Beam at once, and a Power Herb any move that would wait (it is used up).</summary>
    private bool SkipsTheCharge(MoveUse use, ChargeTurn charge)
    {
        if (charge.NotInTheSun && Field.WeatherInEffect == BattleWeather.Sun) return true;
        // Electro Shot gathers itself and strikes at once in the rain (plan 06 · R26b)
        if (charge.NotInTheRain && Field.WeatherInEffect is BattleWeather.Rain or BattleWeather.HeavyRain) return ChargesInTheRain(use, charge);
        if (!BattleEffects.Of(use.User).Any(e => e.SkipsChargeTurn)) return false;

        Say(string.Format(Text.Of(charge.Line), use.User.Name));
        if (charge.RaisesDefense) ChangeStat(use.User, StatType.Defense, 1, use.User);
        if (charge.RaisesSpAttack) ChangeStat(use.User, StatType.SpAttack, 1, use.User);
        Say(Text.Of("battle.becameFullyChargedDue", ("user", use.User.Name), ("item", ItemAtWork(use.User))));
        ConsumeItem(use.User);
        return true;
    }

    private void BeginCharge(MoveUse use, ChargeTurn charge)
    {
        var user = use.User;
        var said = Say(string.Format(Text.Of(charge.Line), user.Name));
        said.With(new Lunged(user.Place, MoveCategory.Status));
        // The body gathers itself until the move is let go (plan 25 · Y5): the screen's alone, the rules know nothing of it
        said.With(new Charging(user.Place, (use.Own ?? use.Data).Name));
        user.Volatile.Charging = true;
        user.Volatile.LockedMove = use.Own ?? use.Data;
        if (charge.Where != Elsewhere.No)
        {
            user.Volatile.Elsewhere = charge.Where;
            said.With(new Vanished(user.Place));
        }
        if (charge.RaisesDefense) ChangeStat(user, StatType.Defense, 1, user);
        if (charge.RaisesSpAttack) ChangeStat(user, StatType.SpAttack, 1, user);
        // Dive's plunge brings a Cramorant up with something in its beak (plan 06 · R19b)
        if (use.Data.Name == "Dive") Gulp(user, use.Data);
    }

    // ---------------------------------------------------------------- at whom

    /// <summary>Who a move hits: its target type, the chosen target (or a stand-in if that one is gone).</summary>
    private List<Battler> ResolveTargets(Battler user, Move move, Battler? chosen)
    {
        // In a triple battle only the places in its user's reach, unless the move reaches anywhere (plan 14 · B6);
        // in every other format everything is in reach
        bool Near(Battler b) => GetsTo(user, b, move.Data);
        var foes = SlotsOf(Other(user.Side)).Where(b => b.IsActive && Near(b)).ToList();
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
                return SlotsOf(user.Side).Where(b => b.IsActive && b != user && Near(b)).ToList();
            case MoveTarget.Allies:
                return SlotsOf(user.Side).Where(b => b.IsActive && b != user).ToList();
            case MoveTarget.AllPokemon:
                return BySpeed().Where(b => b.IsActive).ToList();
            case MoveTarget.AllFoes:
                return speedOrder.Where(foes.Contains).Concat(foes).Distinct().ToList();
            case MoveTarget.AllOthers:
                return speedOrder.Concat(AllBattlers).Distinct().Where(b => b.IsActive && b != user && Near(b)).ToList();
            case MoveTarget.RandomFoe:
                if (Centre() is { } drawnTo) return new List<Battler> { drawnTo };
                return foes.Count == 0 ? foes : Drawn(foes[rng.Roll(RollKind.Target, foes.Count)]);
            default:
                // Follow Me on the other side draws every move aimed at one Pokémon to its user, while that one stands
                if (Centre() is { } centre) return new List<Battler> { centre };
                if (chosen != null && chosen.IsActive && chosen != user && Near(chosen)) return Drawn(chosen);
                // The chosen foe is gone: the move goes to the other one (an ally that fainted leaves nothing to hit)
                if (chosen != null && chosen.Side == user.Side) return new List<Battler>();
                return foes.Count == 0 ? foes : Drawn(foes[rng.Roll(RollKind.Target, foes.Count)]);
        }

        Battler? Centre() => !(TracksItsTarget(move.Data) || KeepsItsAim(user)) && Field.Side(Other(user.Side)).FollowMe is { } place && At(place) is { IsActive: true } centre && Near(centre) ? centre : null;

        // Lightning Rod and Storm Drain (BattleSystem_CheckRedirectionAbilities): an Electric or a Water move aimed
        // at one Pokémon goes to the fastest holder that isn't its user, unless it is on its turn of getting ready
        // or its user has Normalize or Mold Breaker. Nothing is said when it was aimed at the holder already.
        List<Battler> Drawn(Battler target)
        {
            if (Has(user, "Normalize") || Has(user, "Mold Breaker") || (TracksItsTarget(move.Data) || KeepsItsAim(user)) || move.Type is not (PokemonType.Electric or PokemonType.Water)) return new List<Battler> { target };
            if (EffectOf(move.Data).Charge != null && !user.Volatile.Charging) return new List<Battler> { target };
            var holder = BySpeed().FirstOrDefault(b => b.IsActive && b != user && Near(b) && b.Ability?.Effect?.DrawsMovesOf(move.Type) == true);
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
            if (Guarded(use, target, hit))
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

        // A later ability on the target's side stops it before it lands (plan 06 · R27)
        if (StoppedByAbility(use, target, hit)) return false;

        // Psychic Terrain keeps a move that goes first off a foe on the ground (plan 06 · R19b; Showdown's psychicterrain onTryHit)
        if (Field.Has(FieldCondition.PsychicTerrain) && use.Data.Priority > 0 && target.Side != user.Side && DamageCalculator.OnTheTerrain(target))
        {
            hit.Missed = true;
            hit.FailLine = Text.Of("battle.isProtectedByPsychicTerrain", ("target", target.Name));
            return false;
        }

        bool missed = !Hits(use, target);

        if (Guarded(use, target, hit))
        {
            hit.Protected = true;
            Unlock(user);
            return false;
        }

        // Telekinesis holds its target where every move finds it (plan 06 · R25; a one-hit knockout decided above)
        bool aimed = (target.Volatile.LockOnTurns > 0 && target.Volatile.LockedOnBy == user.Place) || target.Volatile.TelekinesisTurns > 0 || target.Volatile.GlaiveRush;
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
        // Nothing misses in the catching lesson (BattleControllerPlayer_CheckMoveHitAccuracy)
        if (Kind == BattleKind.CatchingLesson) return true;
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
        // (Mind's Eye too: plan 06 · R27b's audit, Showdown's ignoreEvasion)
        if (userEffects.Any(e => e.IgnoresOthersStatStages) || DamageCalculator.IgnoresDefenseStages(move.Data) || Has(user, "Mind's Eye")) evasion = 0;
        // A target that has been identified can't hide behind raised evasion
        if ((target.Volatile.Identified || target.Volatile.MiracleEye) && evasion > 0) evasion = 0;
        int rate = Formulas.HitRate(use.Effect.AccuracyUnder(weather, move.Accuracy), accuracy, evasion);

        var userAbility = user.Ability?.Effect;
        var targetAbility = breaks ? null : target.Ability?.Effect;
        if (userAbility != null) rate = Formulas.Scale(rate, userAbility.AccuracyMultiplier(user, move));
        if (targetAbility != null) rate = Formulas.Scale(rate, targetAbility.EvasionMultiplier(target));
        if (weather == BattleWeather.Fog) rate = rate * 6 / 10;
        // The items, by hold effect (BattleControllerPlayer_CheckMoveHit): the target's Bright Powder or Lax
        // Incense, the user's Wide Lens, its Zoom Lens against a target that has moved, and a Micle Berry eaten,
        // which is good for one move
        string? theirHold = BattleEffects.HoldEffectOf(target), myHold = BattleEffects.HoldEffectOf(user);
        if (theirHold == "AccReduce") rate = rate * (100 - BattleEffects.HoldParamOf(target)) / 100;
        if (myHold == "AccuracyUp") rate = rate * (100 + BattleEffects.HoldParamOf(user)) / 100;
        if (myHold == "AccuracyUpSlower" && target.MovedThisTurn) rate = rate * (100 + BattleEffects.HoldParamOf(user)) / 100;
        if (user.Volatile.MicleBerry)
        {
            user.Volatile.MicleBerry = false;
            rate = rate * 120 / 100;
        }
        if (Field.Gravity) rate = rate * 10 / 6;
        rate = LaterAccuracy(use, target, rate);

        return rng.Roll(RollKind.Accuracy, 100) < rate;
    }

    /// <summary>The held item's effect, unless an Embargo or Klutz has put it out of use.</summary>
    private static BattleEffect? ItemEffectOf(Battler b) => BattleEffects.ItemOf(b);

    /// <summary>The move as its user makes it: Normal, whatever it was, for a user with Normalize (a copy; the Pokémon's own move is left as it is).</summary>
    private static Move Normalized(Battler user, Move move) =>
        Has(user, "Normalize") && move.Type != PokemonType.Normal ? new Move(move.Data.OfType(PokemonType.Normal), move.CurrentPP) : move;

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

        // The harsh sun burns a Water move away and the heavy rain puts a Fire move out (plan 06 · R19b)
        if (WashedOut(move))
        {
            Unlock(user);
            return;
        }
        // Surf brings a Cramorant up with something in its beak (plan 06 · R19b)
        if (move.Data.Name == "Surf") Gulp(user, move.Data);

        if (!use.Line.Shows.OfType<Lunged>().Any()) use.Line.With(new Lunged(user.Place, move.Category));

        // Explosion and its kind cost their user everything before the hit, whatever comes of it
        if (effect.SelfKo)
        {
            user.Pokemon!.CurrentHP = 0;
            use.Line.With(new HpChanged(user.Place, 0, Healed: false));
        }

        use.StrikeCount = Math.Max(1, effect.Strikes?.Invoke(this, use) ?? 1);
        ParentalBond(use);
        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility) || DamageCalculator.IgnoresAbilities(use.Data);
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
                    if (effect.EachHitRollsAccuracy && RollsEachHit(use) && !Hits(use, t))
                    {
                        hit.Disrupted = true;
                        break;
                    }
                }
                if (effect.StrikeLine?.Invoke(this, use) is { } line) Say(line);
                StrikingAs(use, n);
                if (!StrikeOnce(use, hit, userBreaks)) break;
                // A Cramorant with its catch spits it back (plan 06 · R19b)
                SpitsTheCatch(use, hit);
                if (use.StrikeCount > 1) AfterOneOfSeveral(use, hit, n == use.StrikeCount);
            }
            if (hit.Landed) RememberHit(t, user, move);
        }

        // The move's effect flies to each target and lands with the damage
        foreach (var hit in use.Hits) use.Line.With(Shown(user, hit.Target, move, hit));
        if (use.Hits.Any(h => h.Landed))
        {
            bool super = use.Hits.Any(h => h.Landed && h.SuperEffective);
            use.Line.AtImpact(new HitSounded(super, !super && use.Hits.Where(h => h.Landed).All(h => h.NotVeryEffective)));
        }
        use.DamageDealt = use.Hits.Where(h => h.Landed).Sum(h => h.Dealt);
        NoteLanded(use);

        AfterStrike(use);
        // Relic Song turns Meloetta (plan 06 · R19b)
        if (Result == BattleResult.None) Sings(use);
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
            // A Gem of the move's type goes off as its first hit is worked out (plan 06 · R28a)
            GemGoesOff(use);
            result = DamageCalculator.Calculate(user, t, move, rng, spread: use.Targets.Count > 1 && !StrikesEachAlone(use.Data), rules: Rules,
                powerTenths: effect.PowerTenths(this, use, t), critBonus: effect.CritBonus, pastScreens: effect.PastScreens,
                // (a Terastallized user's weak move of its Tera Type hits as 60: plan 06 · R23)
                basePower: TeraPower(use, effect.BasePower?.Invoke(this, use, t)), noVariance: effect.NoVariance, damageTenths: use.DamageTenths,
                // The catching lesson and the first battle have no critical hits (BtlCmd_CalcCrit)
                noCrit: NoCriticalHits);
            // A Z-Move through a protection does a quarter (plan 06 · R21)
            if (zUses.Count > 0) result = ThroughProtection(use, t, result);
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
        if (t.HasSubstitute && t != user && !Infiltrates(use))
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

        // A disguise or an icy face takes the hit instead (plan 06 · R19b)
        if (effect.Deals == null && ShapeTakesTheHit(use, hit, userBreaks))
        {
            hit.LastDealt = 0;
            hit.Strikes++;
            return true;
        }

        var p = t.Pokemon!;
        // A berry against the move's type halves the hit as it lands, and is eaten (subscript_type_resist_berry)
        if (!hit.OneHitKo && effect.Deals == null && WeakenedByBerry(t, move, result, out var berry))
        {
            // (a quarter with Ripen: plan 06 · R27b)
            damage = Formulas.Divide(damage, Has(t, "Ripen") ? 4 : 2);
            hit.Notes.AddRange(Capture(() =>
            {
                Say(Text.Of("battle.theWeakenedPower", ("item", ItemAtWork(t, berry)), ("move", move.DisplayName)));
                ConsumeItem(t);
            }));
        }
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
        CountTheBlow(t);
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
        if (hit.IntoSubstitute) PopUnderTheDoll(hit);
        if (hit.LastCritical) Say(use.Hits.Count > 1 ? Text.Of("battle.aCriticalHitOn", ("target", t.Name)) : Text.Of("battle.aCriticalHit"));
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
        var took = Say(Text.Of("battle.theSubstituteTookDamage", ("target", t.Name)));
        if (!hit.BrokeSubstitute || t.HasSubstitute) return;
        took.With(new SubstituteChanged(t.Place, Up: false));
        Say(Text.Of("battle.substituteFaded", ("target", t.Name)));
        hit.BrokeSubstitute = false;
    }

    /// <summary>What the type made of the hit (<c>subscript_move_followup_message</c>): a one-hit knockout says so instead.</summary>
    private void EffectivenessLine(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        bool several = use.Hits.Count > 1;
        if (hit.OneHitKo && !hit.Endured) Say(Text.Of("battle.itAOneHit"));
        else if (hit.SuperEffective) Say(several ? Text.Of("battle.itSuperEffectiveOn", ("target", t.Name)) : Text.Of("battle.itSuperEffective"));
        else if (hit.NotVeryEffective) Say(several ? Text.Of("battle.itNotVeryEffective", ("target", t.Name)) : Text.Of("battle.itNotVeryEffective2"));
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
        // A rainbow over its user's side doubles them again, but a flinch for one with Serene Grace (plan 06 · R25's leftovers)
        int flinchMult = Rainbowed(user) && chanceMult > 1 ? chanceMult : chanceMult * RainbowOver(user);
        chanceMult *= RainbowOver(user);
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
            if (data.FlinchChancePercent > 0 && !t.MovedThisTurn && Chance(data.FlinchChancePercent * flinchMult) &&
                !BattleEffects.Of(t).Any(e => e.BlocksFlinch))
                t.Flinched = true;
        }

        // What the move does of its own to whoever it hit
        use.Effect.OnHit(this, use, hit);
        // A beak heating up, a shell trap set (plan 06 · R25)
        HitAnswers(use, hit);

        // A Pokémon in a rage grows angrier with every hit it takes
        if (standing && hit.Touched && dealt > 0 && t != user && t.Volatile.Rage && t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) < 6)
        {
            int stage = t.Pokemon.StatStages.GetValueOrDefault(StatType.Attack) + 1;
            t.Pokemon.StatStages[StatType.Attack] = stage;
            Say(Text.Of("battle.rageIsBuilding", ("target", t.Name))).With(new StageChanged(t.Place, StatType.Attack, stage, Rose: true));
        }

        // The target's ability reacts to the hit (Static, Rough Skin), berries check its HP, and then the items
        // that answer a hit (a Sticky Barb, a Jaboca or Rowap Berry, an Enigma Berry), in the original's order
        if (hit.Touched && dealt > 0)
        {
            foreach (var e in BattleEffects.Of(t, includeAbility: !userBreaks).ToList())
                e.AfterHit(this, t, user, move, dealt, hit.LastCritical);
            // Wimp Out and Emergency Exit leave once the move is done (plan 06 · R27b)
            NoteTheExit(use, hit);
            CheckConditionHooks(t, user);
            ItemsOnHit(use, hit);
        }

        // A Fire move thaws a frozen target
        if (t.Pokemon.CurrentHP > 0 && hit.Touched && dealt > 0 && t != user && (move.Type == PokemonType.Fire || ThawsItsTarget(move.Data)) && t.Pokemon.Status == StatusCondition.Freeze)
        {
            t.Pokemon.Status = StatusCondition.None;
            Say(Text.Of("battle.thawedOut", ("user", t.Name))).With(new StatusChanged(t.Place, StatusCondition.None));
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
        int chanceMult = userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max() * RainbowOver(user);

        foreach (var hit in use.Hits)
        {
            var t = hit.Target;
            if (hit.Protected)
            {
                // A side's guard says whose it was, and a shield answers whoever touched it (plan 06 · R25)
                Say(hit.FailLine ?? Text.Of("battle.protectedItself", ("user", t.Name)));
                ShieldBites(use, hit);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (hit.Missed || hit.OutOfReach)
            {
                Say(hit.FailLine ?? (several || !IsSingle ? Text.Of("battle.avoidedTheAttack", ("target", t.Name)) : Text.Of("battle.attackMissed", ("user", user.Name))));
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (hit.Failed)
            {
                Say(Text.Of("battle.butItFailed"));
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }
            if (!hit.Endured) log.AddRange(hit.Notes);
            if (hit.Absorbed) continue;
            if (hit.Immune)
            {
                Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
                user.Turn.MoveFailed = true;
                Unlock(user);
                use.Effect.OnMiss(this, use, hit);
                continue;
            }

            if (use.StrikeCount > 1)
            {
                // Each hit told its own; the type's effect waits for the last, and then comes the count
                if (!hit.MessagesTold) EffectivenessLine(use, hit);
                if (!use.Effect.NoHitCount) Say(Text.Of("battle.hitTimes", ("n", hit.Strikes)));
                continue;
            }

            if (hit.IntoSubstitute) SubstituteLines(hit);
            if (hit.IntoSubstitute) PopUnderTheDoll(hit);
            if (hit.LastCritical) Say(several ? Text.Of("battle.aCriticalHitOn", ("target", t.Name)) : Text.Of("battle.aCriticalHit"));
            if (hit.EnduredByMove) Say(Text.Of("battle.enduredTheHit", ("target", t.Name)));
            else if (hit.Endured) log.AddRange(hit.Notes);
            EffectivenessLine(use, hit);
            if (hit.LastCritical && hit.Touched) t.TookCriticalHit = true;
            TargetEffects(use, hit);
        }

        if (use.AfterLine != null) Say(use.AfterLine);

        var landed = use.Hits.Where(h => h.Landed).Select(h => (h.Target, h.Dealt)).ToList();
        int total = use.DamageDealt;
        if (landed.Count == 0) MetronomeMissed(user);

        if (user.IsActive && landed.Count > 0)
        {
            // Draining heals the user by what the move took (or hurts it, against Liquid Ooze)
            if (data.DrainPercent > 0 && total > 0)
            {
                int amount = Formulas.Divide(total * data.DrainPercent, 100);
                amount = amount * userEffects.Select(e => e.DrainHundredths).DefaultIfEmpty(100).Max() / 100;
                bool ooze = landed.Any(h => BattleEffects.Of(h.Target).Any(e => e.HurtsDrainers));
                if (ooze) LoseHp(user, amount, Text.Of("battle.suckedUpTheLiquid", ("user", user.Name)));
                else if (user.Volatile.HealBlockTurns > 0) Say(Text.Of("battle.wasKeptFromHealing", ("user", user.Name)));
                else RestoreHp(user, amount, Text.Of("battle.hadItsEnergyDrained", ("target", landed[0].Target.Name)));
            }

            // Recoil
            int beforeRecoil = user.Pokemon!.CurrentHP;
            if (data == StruggleData) LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, 4), Text.Of("battle.isHitWithRecoil", ("user", user.Name)), direct: true);
            else if (data.RecoilPercent > 0 && total > 0 && !userEffects.Any(e => e.PreventsRecoil))
                LoseHp(user, RecoilFor(total, data.RecoilPercent), Text.Of("battle.isHitWithRecoil", ("user", user.Name)));
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
                    Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
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
                if (p.CurrentHP >= p.MaxHP) Say(Text.Of("battle.hpIsFull", ("user", t.Name)));
                else RestoreHp(t, Formulas.Divide(p.MaxHP * data.HealPercent, 100), Text.Of("battle.regainedHealth", ("user", t.Name)));
                did = true;
            }
            if (!did && data.InflictStatus == StatusCondition.None && data.TargetStatChange == null && data.ConfuseChancePercent == 0)
                Say(Text.Of("battle.butNothingHappened"));
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
            // (and Safety Goggles keep them off whoever wears them: plan 06 · R28a)
            bool powder = use.Data.Flags.HasFlag(MoveFlags.Powder);
            if (!powder || !((Rules.GrassTypesIgnorePowder && target.HasType(PokemonType.Grass)) || BattleEffects.HoldEffectOf(target) == "SafetyGoggles")) return true;
            Say(Text.Of("battle.itDoesntAffect", ("target", target.Name)));
            return false;
        }
        if (hit.Protected)
        {
            Say(hit.FailLine ?? Text.Of("battle.protectedItself", ("user", target.Name)));
            ShieldBites(use, hit);
        }
        else
        {
            if (hit.Missed && hit.FailLine == null) use.StatusMissed = true;
            Say(hit.FailLine ?? (use.Targets.Count > 1 || !IsSingle ? Text.Of("battle.avoidedTheAttack", ("target", target.Name)) : Text.Of("battle.attackMissed", ("user", use.User.Name))));
        }
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
        // What answers a berry eaten (plan 06 · R27b)
        Digest();
    }
}
