using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later generations' moves, R25's leftovers (plan 06): Sky Drop, Ally Switch, the three Pledges and Sappy Seed,
// with Defog's and Feint's later reach (Ruleset.LaterMoveReach). The rules are Pokémon Showdown's (data/moves.ts at the
// commit in tools/DataImporter/Sources.cs); each names what it was read from. They join the table in
// BattleCore.Effects.cs when the class is first used (the static constructor in BattleCore.Families.cs).
public sealed partial class BattleCore
{
    /// <summary>
    /// A Pledge that waits for its user's ally's (Showdown's <c>-waiting</c> and <c>sourceEffect</c>): the turn, the
    /// Pokémon whose Pledge now goes next as the two moves together, and the effect of the Pledge that waits.
    /// </summary>
    private (int Turn, Pokemon? Who, string With) pledged = (-1, null, "");

    /// <summary>The Pledges, which combine with one another (Showdown's <c>pledgecombo</c> flag).</summary>
    private static bool IsPledge(string? effect) => effect is "WaterPledge" or "FirePledge" or "GrassPledge";

    /// <summary>What two Pledges together are: the type of the move and what it leaves, on whose side.</summary>
    private sealed record PledgeCombo(PokemonType Type, SideCondition Leaves, bool OnUsersSide);

    private static PledgeCombo CombinationOf(string first, string second)
    {
        bool Has(string effect) => first == effect || second == effect;
        // Water and Fire make a rainbow over the user's side, Grass and Water a swamp, Fire and Grass a sea of fire
        // round the target's (each Pledge's onModifyMove)
        if (Has("WaterPledge") && Has("FirePledge")) return new(PokemonType.Water, SideCondition.Rainbow, true);
        if (Has("GrassPledge") && Has("WaterPledge")) return new(PokemonType.Grass, SideCondition.Swamp, false);
        return new(PokemonType.Fire, SideCondition.SeaOfFire, false);
    }

    private static MoveEffect Pledge() => new()
    {
        Start = (b, use) => b.Pledges(use),
        // 150 together (basePowerCallback)
        BasePower = (b, use, t) => use.Scratch is PledgeCombo ? 150 : use.Data.Power,
        Then = (b, use) => b.PledgeLeaves(use)
    };

    private static readonly Dictionary<string, MoveEffect> LaterEffects3 = new()
    {
        ["SkyDrop"] = new() { Does = (b, use) => b.SkyDrop(use) },
        ["AllySwitch"] = new() { Does = (b, use) => b.AllySwitch(use) },
        ["WaterPledge"] = Pledge(),
        ["FirePledge"] = Pledge(),
        ["GrassPledge"] = Pledge(),
        ["SappySeed"] = new()
        {
            // Leech Seed on a target the hit reached, but a Grass type; one already seeded is left as it is (onHit)
            Hit = (b, use, hit) =>
            {
                var t = hit.Target;
                if (!hit.Touched || !t.IsActive || !use.User.IsActive || t.HasType(PokemonType.Grass) || t.Volatile.SeededBy != null) return;
                t.Volatile.SeededBy = use.User.Place;
                b.Say(Text.Of("battle.wasSeeded", ("target", t.Name)));
            }
        }
    };

    // ---------------------------------------------------------------- Sky Drop

    /// <summary>
    /// Sky Drop (Showdown's <c>skydrop</c>): the first turn it carries its target up with it, where neither can be
    /// reached but by what follows into the air, and the target does nothing and can't be called back; the second it
    /// lets it fall, a hit a Flying type isn't touched by. Not an ally, nor one behind a Substitute, nor one of 200 kg
    /// or more; its first turn rolls no accuracy (onModifyMove), though Protect stops it.
    /// </summary>
    private void SkyDrop(MoveUse use)
    {
        var user = use.User;
        var v = user.Volatile;
        if (v.Charging && v.Lifting is { } carried)
        {
            // The second turn: both come down, and the target takes the fall
            v.Charging = false;
            v.Lifting = null;
            if (!user.IsHeldToItsMove) v.LockedMove = null;
            if (user.IsElsewhere)
            {
                v.Elsewhere = Elsewhere.No;
                use.Line.With(new Reappeared(user.Place));
            }
            var t = At(carried);
            if (t.Volatile.LiftedBy == user.Place)
            {
                t.Volatile.LiftedBy = null;
                if (t.IsElsewhere)
                {
                    t.Volatile.Elsewhere = Elsewhere.No;
                    use.Line.With(new Reappeared(t.Place));
                }
            }
            if (!t.IsActive)
            {
                Say(Text.Of("battle.butThereWasNoTarget"));
                return;
            }
            if (t.HasType(PokemonType.Flying))
            {
                use.Line.With(Shown(user, t, use.Move, new MoveHit { Target = t, Immune = true }));
                Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
                return;
            }
            use.Targets = new List<Battler> { t };
            Strike(use);
            return;
        }

        var target = use.Targets.FirstOrDefault();
        if (target == null || !target.IsActive)
        {
            Say(Text.Of("battle.butThereWasNoTarget"));
            return;
        }
        var hit = new MoveHit { Target = target };
        if (target.IsElsewhere)
        {
            Say(!IsSingle ? Text.Of("battle.avoidedTheAttack", ("target", target.Name)) : Text.Of("battle.attackMissed", ("user", user.Name)));
            failedNow = true;
            return;
        }
        if (Guarded(use, target, hit))
        {
            // (Its first turn touches nobody: no shield bites back)
            Say(hit.FailLine ?? Text.Of("battle.protectedItself", ("user", target.Name)));
            failedNow = true;
            return;
        }
        if (target.HasSubstitute || target.Side == user.Side)
        {
            Fails();
            return;
        }
        if (WeightOf(target) >= 200f)
        {
            Say(Text.Of("battle.tooHeavyToLift", ("target", target.Name)));
            failedNow = true;
            return;
        }
        v.Charging = true;
        v.LockedMove = use.Own ?? use.Data;
        v.Lifting = target.Place;
        v.Elsewhere = Elsewhere.InTheAir;
        target.Volatile.LiftedBy = user.Place;
        target.Volatile.Elsewhere = Elsewhere.InTheAir;
        use.Line.With(Shown(user, target, use.Move, null));
        Say(Text.Of("battle.tookIntoTheSky", ("user", user.Name), ("target", target.Name)))
            .With(new Charging(user.Place, (use.Own ?? use.Data).Name))
            .With(new Vanished(user.Place))
            .With(new Vanished(target.Place));
    }

    /// <summary>
    /// Sky Drop is let go of as either of the two leaves the sky some other way (a faint, a switch, Gravity, a move
    /// lost to a condition): whoever was carried is set down, and whoever carried it comes down free of its move
    /// (Showdown's onMoveFail, onFaint and gravity's onFieldStart). Nothing is said: each is back in sight.
    /// </summary>
    private void LetGoOfTheSky(Battler b)
    {
        var v = b.Volatile;
        if (v.Lifting is { } carried)
        {
            v.Lifting = null;
            var t = At(carried);
            if (t.Volatile.LiftedBy == b.Place)
            {
                t.Volatile.LiftedBy = null;
                if (t.IsElsewhere)
                {
                    t.Volatile.Elsewhere = Elsewhere.No;
                    Emit(new Reappeared(t.Place));
                }
            }
        }
        if (v.LiftedBy is { } by)
        {
            v.LiftedBy = null;
            var carrier = At(by);
            var cv = carrier.Volatile;
            if (cv.Lifting == b.Place)
            {
                cv.Lifting = null;
                cv.Charging = false;
                if (!carrier.IsHeldToItsMove) cv.LockedMove = null;
                if (carrier.IsElsewhere)
                {
                    cv.Elsewhere = Elsewhere.No;
                    Emit(new Reappeared(carrier.Place));
                }
            }
        }
    }

    // ---------------------------------------------------------------- Ally Switch

    /// <summary>
    /// Ally Switch (Showdown's <c>allyswitch</c>): its user and its ally change places, every move still to come
    /// aimed at a place finding whoever stands there now. Only in a double battle where one trainer has both, and not
    /// with an ally that has fainted. Used again the turn after it worked it is a third as likely each time, down to 1
    /// in 729 (its volatile's counter, kept whether or not the places changed).
    /// </summary>
    private void AllySwitch(MoveUse use)
    {
        var user = use.User;
        var v = user.Volatile;
        use.Line.With(Shown(user, user, use.Move, null));
        // The chain (onPrepareHit, onRestart): the volatile lasts the turn it is gained and the next
        if (v.AllySwitchTurn >= Turn - 1 && v.AllySwitchTurn >= 0)
        {
            if (rng.Roll(RollKind.Protect, v.AllySwitchOdds) != 0)
            {
                v.AllySwitchTurn = -1;
                v.AllySwitchOdds = 3;
                Fails();
                return;
            }
            v.AllySwitchOdds = Math.Min(729, v.AllySwitchOdds * 3);
        }
        else v.AllySwitchOdds = 3;
        v.AllySwitchTurn = Turn;

        // In a triple battle the Pokémon at the other end, and none for the centre (plan 14 · B6; Showdown's allyswitch)
        var ally = IsDouble ? SlotsOf(user.Side)[1 - user.Slot] : IsTriple && user.Slot != CentreSlot ? SlotsOf(user.Side)[2 - user.Slot] : null;
        if (ally?.Pokemon == null || ally.Pokemon.IsFainted || ally.Trainer != user.Trainer)
        {
            Fails();
            return;
        }
        string userName = user.Name, allyName = ally.Name;
        var (from, to) = (user.Place, ally.Place);
        Exchange(user, ally, AllBattlers);
        foreach (var act in waiting)
        {
            if (act.User == user) act.User = ally;
            else if (act.User == ally) act.User = user;
        }
        var side = Field.Side(user.Side);
        if (side.FollowMe == from) side.FollowMe = to;
        else if (side.FollowMe == to) side.FollowMe = from;
        orderChanged = true;
        Say(Text.Of("battle.switchedPlaces", ("user", userName), ("ally", allyName))).With(new PlacesSwapped(from, to));
    }

    /// <summary>
    /// Two places of a side change Pokémon with everything that lasts on each (Showdown's <c>swapPosition</c>), and
    /// every place anyone remembers follows the Pokémon that stood there. The rules' battlers and the screen's both
    /// go through here.
    /// </summary>
    internal static void Exchange(Battler a, Battler b, IEnumerable<Battler> everyone)
    {
        (a.Pokemon, b.Pokemon) = (b.Pokemon, a.Pokemon);
        (a.Roster, b.Roster) = (b.Roster, a.Roster);
        (a.Trainer, b.Trainer) = (b.Trainer, a.Trainer);
        (a.ConfusionTurns, b.ConfusionTurns) = (b.ConfusionTurns, a.ConfusionTurns);
        (a.Flinched, b.Flinched) = (b.Flinched, a.Flinched);
        (a.ChoiceLock, b.ChoiceLock) = (b.ChoiceLock, a.ChoiceLock);
        (a.FlashFire, b.FlashFire) = (b.FlashFire, a.FlashFire);
        (a.MovedThisTurn, b.MovedThisTurn) = (b.MovedThisTurn, a.MovedThisTurn);
        (a.TookCriticalHit, b.TookCriticalHit) = (b.TookCriticalHit, a.TookCriticalHit);
        (a.Volatile, b.Volatile) = (b.Volatile, a.Volatile);
        (a.Turn, b.Turn) = (b.Turn, a.Turn);
        var fought = a.FoughtAgainst.ToList();
        a.FoughtAgainst.Clear();
        a.FoughtAgainst.UnionWith(b.FoughtAgainst);
        b.FoughtAgainst.Clear();
        b.FoughtAgainst.UnionWith(fought);

        Place pa = a.Place, pb = b.Place;
        Place? Map(Place? p) => p == pa ? pb : p == pb ? pa : p;
        int na = pa.Number, nb = pb.Number;
        foreach (var x in everyone)
        {
            x.Volatile.Remap(Map);
            var turn = x.Turn;
            (turn.PhysicalFrom[na], turn.PhysicalFrom[nb]) = (turn.PhysicalFrom[nb], turn.PhysicalFrom[na]);
            (turn.SpecialFrom[na], turn.SpecialFrom[nb]) = (turn.SpecialFrom[nb], turn.SpecialFrom[na]);
            turn.LastPhysicalFrom = Map(turn.LastPhysicalFrom);
            turn.LastSpecialFrom = Map(turn.LastSpecialFrom);
            turn.LastHitFrom = Map(turn.LastHitFrom);
        }
    }

    // ---------------------------------------------------------------- the Pledges

    /// <summary>
    /// A Pledge as it is tried (each Pledge's onPrepareHit and onModifyMove): with its user's ally still to use another
    /// Pledge it waits, and the ally's goes at once as the two together; the Pledge that goes after one that waited
    /// is that combined move, 150 strong, of the pair's type, with the same-type bonus whoever uses it.
    /// </summary>
    private bool Pledges(MoveUse use)
    {
        var user = use.User;
        if (!use.Called && pledged.Turn == Turn && pledged.Who == user.Pokemon && IsPledge(pledged.With) && pledged.With != use.Data.Effect)
        {
            var combo = CombinationOf(pledged.With, use.Data.Effect!);
            pledged = (-1, null, "");
            use.Move = new Move(use.Data.OfType(combo.Type).WithForcedStab(), use.Move.CurrentPP);
            use.Scratch = combo;
            Say(Text.Of("battle.movesBecameOne"));
            return true;
        }
        if (use.Called) return true;
        var partner = waiting.FirstOrDefault(a => a.Choice.Kind == ChoiceKind.Fight && a.User != user && a.User.Side == user.Side &&
            a.User.IsActive && a.User.Pokemon == a.Actor && IsPledge(a.Move?.Data.Effect) && a.Move!.Data.Effect != use.Data.Effect);
        if (partner == null) return true;
        waiting.Remove(partner);
        waiting.Insert(0, partner);
        pledged = (Turn, partner.Actor, use.Data.Effect!);
        Say(Text.Of("battle.waitingForMove", ("user", user.Name), ("ally", partner.User.Name)));
        return false;
    }

    /// <summary>
    /// What a combined Pledge leaves once it has landed, for four turns: a rainbow over its user's side, or a sea of
    /// fire or a swamp round its target's. One already there stays as it is (no restart), and nothing is said.
    /// </summary>
    private void PledgeLeaves(MoveUse use)
    {
        if (use.Scratch is not PledgeCombo combo) return;
        var side = combo.OnUsersSide ? use.User.Side : use.Targets.FirstOrDefault()?.Side ?? Other(use.User.Side);
        var state = Field.Side(side);
        string line;
        switch (combo.Leaves)
        {
            case SideCondition.Rainbow:
                if (state.Rainbow) return;
                state.RainbowTurns = 4;
                line = "battle.rainbowAppeared";
                break;
            case SideCondition.SeaOfFire:
                if (state.SeaOfFire) return;
                state.SeaOfFireTurns = 4;
                line = "battle.seaOfFireEnveloped";
                break;
            default:
                if (state.Swamp) return;
                state.SwampTurns = 4;
                line = "battle.swampEnveloped";
                break;
        }
        Say(Text.Of(line, ("team", TeamOf(side)))).With(Now(side, combo.Leaves));
    }

    /// <summary>A rainbow over a Pokémon's side: its moves' side effects are twice as likely (Showdown's waterpledge onModifyMove).</summary>
    private int RainbowOver(Battler user) => Rainbowed(user) ? 2 : 1;

    private bool Rainbowed(Battler user) => Field.Side(user.Side).Rainbow;

    /// <summary>A sea of fire burns whoever isn't a Fire type on its side for an eighth of its HP at a turn's end (firepledge onResidual).</summary>
    private void SeaOfFireBurns(Battler b)
    {
        if (!Field.Side(b.Side).SeaOfFire || b.HasType(PokemonType.Fire) || BattleEffects.Of(b).Any(e => e.PreventsIndirectDamage)) return;
        LoseHp(b, Math.Max(1, b.Pokemon!.MaxHP / 8), Text.Of("battle.hurtByTheSeaOfFire", ("pokemon", b.Name)));
    }

    /// <summary>The rainbow, the sea of fire and the swamp each run four turn ends, and say so as they go.</summary>
    private void PledgesRunDown()
    {
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var state = Field.Side(side);
            if (RunsOut(ref state.RainbowTurns)) Say(Text.Of("battle.rainbowDisappeared", ("team", TeamOf(side)))).With(Now(side, SideCondition.Rainbow));
            if (RunsOut(ref state.SeaOfFireTurns)) Say(Text.Of("battle.seaOfFireDisappeared", ("team", TeamOf(side)))).With(Now(side, SideCondition.SeaOfFire));
            if (RunsOut(ref state.SwampTurns)) Say(Text.Of("battle.swampDisappeared", ("team", TeamOf(side)))).With(Now(side, SideCondition.Swamp));
        }
    }

    // ---------------------------------------------------------------- Defog's and Feint's later reach

    /// <summary>
    /// Defog under the later rules (Showdown's <c>defog</c>): the hazards on its user's own side go too, each on a line
    /// of its own, and the terrain lifts.
    /// </summary>
    private void DefogsFurther(Battler user)
    {
        var side = Field.Side(user.Side);
        SideConditionChanged Gone(SideCondition what) => new(user.Side, what, 0);
        if (side.Spikes > 0)
        {
            side.Spikes = 0;
            Say(Text.Of("battle.theSpikesAroundWere", ("team", TeamOf(user.Side)))).With(Gone(SideCondition.Spikes));
        }
        if (side.ToxicSpikes > 0)
        {
            side.ToxicSpikes = 0;
            Say(Text.Of("battle.thePoisonSpikesAround", ("team", TeamOf(user.Side)))).With(Gone(SideCondition.ToxicSpikes));
        }
        if (side.StealthRock)
        {
            side.StealthRock = false;
            Say(Text.Of("battle.thePointedStonesAround", ("team", TeamOf(user.Side)))).With(Gone(SideCondition.StealthRock));
        }
        if (side.StickyWeb) ClearLaterSide(user.Side, Say(Text.Of("battle.theStickyWebAroundWas", ("team", TeamOf(user.Side)))), veil: false, web: true);
        if (Field.Terrain is { } terrain) EndTerrain(terrain);
    }

    /// <summary>
    /// Feint under the later rules (Showdown's <c>breaksProtect</c>): it lifts the target's Protect or shield and its
    /// side's guards, and says so only when there was one.
    /// </summary>
    private void Feinted(Battler t)
    {
        bool broke = t.Turn.Protecting || GuardsOf(t.Side) != SideGuard.None;
        t.Turn.Protecting = false;
        t.Turn.Shield = Shield.None;
        guards[(int)t.Side] = (-1, SideGuard.None);
        if (broke) Say(Text.Of("battle.fellForTheFeint", ("target", t.Name)));
    }
}
