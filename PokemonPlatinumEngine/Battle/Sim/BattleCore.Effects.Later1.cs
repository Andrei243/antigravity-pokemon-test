using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later generations' moves, batch 1 (plan 06 · R24): Generation 5's own effects, the moves that lay the field
// R19b built (the four terrains, Sticky Web, Aurora Veil) and Photon Geyser. The rules are Pokémon Showdown's (the
// third pinned source, data/moves.ts at the commit in tools/DataImporter/Sources.cs); each entry names the move it
// was read from. A move that hits with other numbers (Foul Play, Psyshock, Chip Away, Photon Geyser) has an empty
// entry here and its rule in DamageCalculator, which reads the move's effect. They join the table in
// BattleCore.Effects.cs when the class is first used (the static constructor in BattleCore.Families.cs).
public sealed partial class BattleCore
{
    /// <summary>The turn a Pokémon of each side last fainted on (Retaliate's <c>side.faintedLastTurn</c>).</summary>
    private readonly Dictionary<BattleSide, int> faintedOnTurn = new() { [BattleSide.Player] = -1, [BattleSide.Enemy] = -1 };

    /// <summary>The last damaging move that landed this turn, and the turn (Fusion Flare's and Fusion Bolt's <c>lastSuccessfulMoveThisTurn</c>).</summary>
    private (int Turn, string Move) landedLast = (-1, "");

    /// <summary>Echoed Voice's field condition (Showdown's <c>echoedvoice</c> pseudo-weather): the turn it was last used on, and its multiplier, 1 to 5.</summary>
    private int echoedOnTurn = -2, echoMultiplier = 1;

    /// <summary>The abilities Simple Beam and Entrainment can't replace (Showdown's <c>cantsuppress</c>), and Truant.</summary>
    private static readonly HashSet<string> CantBeReplaced = new()
    {
        "Multitype", "Stance Change", "Schooling", "Comatose", "Shields Down", "Disguise", "RKS System", "Battle Bond",
        "Power Construct", "Ice Face", "Gulp Missile", "As One (Glastrier)", "As One (Spectrier)", "Zero to Hero", "Tera Shift", "Truant",
        // The data's name for both riders' (plan 06 · R27b's audit)
        "As One"
    };

    /// <summary>The abilities Entrainment can't hand on (Showdown's <c>noentrain</c>, with <c>cantsuppress</c>).</summary>
    private static readonly HashSet<string> NotHandedOn = new()
    {
        "Trace", "Forecast", "Flower Gift", "Illusion", "Imposter", "Zen Mode", "Power of Alchemy", "Receiver", "Hunger Switch",
        "Neutralizing Gas", "Commander", "Protosynthesis", "Quark Drive", "Embody Aspect (Cornerstone)", "Embody Aspect (Hearthflame)",
        "Embody Aspect (Teal)", "Embody Aspect (Wellspring)",
        // The data's name for all four masks' (plan 06 · R27b's audit)
        "Embody Aspect"
    };

    private static readonly Dictionary<string, MoveEffect> LaterEffects1 = new()
    {
        // ---- hitting with other numbers: the rule is DamageCalculator's, by the move's effect
        ["Psyshock"] = new(),
        ["FoulPlay"] = new(),
        ["ChipAway"] = new(),
        ["SunsteelStrike"] = new(),
        ["PhotonGeyser"] = new()
        {
            // The move is physical for the whole of its use, Counter included, when the user's Attack is the higher
            Start = (b, use) =>
            {
                if (DamageCalculator.PhysicalByStats(use.User, use.Data))
                    use.Move = new Move(use.Data.OfCategory(MoveCategory.Physical), use.Move.CurrentPP);
                return true;
            }
        },

        // ---- power by what holds
        ["Venoshock"] = new() { Power = (b, use, t) => t.Pokemon!.Status is StatusCondition.Poison or StatusCondition.Toxic ? 20 : 10 },
        ["Hex"] = new() { Power = (b, use, t) => t.Pokemon!.Status is not (StatusCondition.None or StatusCondition.Faint) ? 20 : 10 },
        ["Acrobatics"] = new() { Power = (b, use, t) => use.User.Pokemon!.HeldItem == null ? 20 : 10 },
        ["Retaliate"] = new() { Power = (b, use, t) => b.faintedOnTurn[use.User.Side] == b.Turn - 1 ? 20 : 10 },
        ["FusionFlare"] = new() { Power = (b, use, t) => b.landedLast == (b.Turn, "Fusion Bolt") ? 20 : 10 },
        ["FusionBolt"] = new() { Power = (b, use, t) => b.landedLast == (b.Turn, "Fusion Flare") ? 20 : 10 },
        ["StoredPower"] = new() { BasePower = (b, use, t) => use.Data.Power + 20 * use.User.Pokemon!.StatStages.Values.Where(s => s > 0).Sum() },
        ["HeavySlam"] = new() { BasePower = (b, use, t) => HeavySlamPower(WeightOf(use.User), WeightOf(t)) },
        ["ElectroBall"] = new()
        {
            // [40, 60, 80, 120, 150] by the user's Speed over the target's, rounded down
            BasePower = (b, use, t) =>
            {
                int theirs = b.EffectiveSpeed(t);
                int ratio = theirs == 0 ? 0 : b.EffectiveSpeed(use.User) / theirs;
                return new[] { 40, 60, 80, 120, 150 }[Math.Min(ratio, 4)];
            }
        },
        ["EchoedVoice"] = new()
        {
            // Its field condition lasts two turns: used again on the next turn, by anyone, it grows by one, to five times
            Start = (b, use) =>
            {
                if (b.echoedOnTurn == b.Turn - 1) b.echoMultiplier = Math.Min(5, b.echoMultiplier + 1);
                else if (b.echoedOnTurn != b.Turn) b.echoMultiplier = 1;
                b.echoedOnTurn = b.Turn;
                return true;
            },
            BasePower = (b, use, t) => use.Data.Power * b.echoMultiplier
        },

        // ---- how it hits
        ["StormThrow"] = new(), // every hit is critical: DamageCalculator.AlwaysCritical
        ["Hurricane"] = new()
        {
            // Sure under rain, 50 under the sun, and it reaches one in the air (Showdown's fly condition lets it)
            SureIn = BattleWeather.Rain,
            Follows = new[] { Elsewhere.InTheAir },
            Accuracy = (weather, accuracy) => weather == BattleWeather.Sun ? 50 : accuracy
        },
        ["FinalGambit"] = new()
        {
            // As much as its user has left; its user faints once it has hit (selfdestruct: "ifHit")
            Deals = (b, use, t) => use.User.Pokemon!.CurrentHP,
            Then = (b, use) =>
            {
                use.User.Pokemon!.CurrentHP = 0;
                b.Emit(new HpChanged(use.User.Place, 0, Healed: false));
            }
        },
        ["Synchronoise"] = new()
        {
            // Only a Pokémon that shares a type with its user is hurt; to the rest it is as a type they are immune to
            Start = (b, use) =>
            {
                foreach (var t in use.Targets.ToList())
                {
                    if (use.User.Types.Any(t.HasType)) continue;
                    b.Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
                    use.Targets.Remove(t);
                }
                return use.Targets.Count > 0;
            }
        },
        ["FreezeShock"] = new() { Charge = new("battle.becameCloakedInAFreezingLight") },
        ["IceBurn"] = new() { Charge = new("battle.becameCloakedInFreezingAir") },

        // ---- what the hit does
        ["ClearSmog"] = new()
        {
            Hit = (b, use, hit) =>
            {
                var t = hit.Target;
                if (!hit.Touched || !t.IsActive || t.Pokemon!.StatStages.Values.All(s => s == 0)) return;
                t.Pokemon.ResetStatStages();
                b.Say(Text.Of("battle.statChangesWereRemoved", ("target", t.Name)));
            }
        },
        ["Incinerate"] = new() { Hit = (b, use, hit) => b.Incinerate(use, hit) },
        ["FlameBurst"] = new() { Hit = (b, use, hit) => b.FlameBurst(use, hit) },
        ["SmackDown"] = new() { Follows = new[] { Elsewhere.InTheAir }, Hit = (b, use, hit) => b.SmackDown(hit) },
        ["CircleThrow"] = new() { Then = (b, use) => b.ThrowOut(use) },

        // ---- status moves
        ["ShellSmash"] = new() { Does = (b, use) => b.Raise(use, (StatType.Defense, -1), (StatType.SpDefense, -1), (StatType.Attack, 2), (StatType.SpAttack, 2), (StatType.Speed, 2)) },
        ["ShiftGear"] = new() { Does = (b, use) => b.Raise(use, (StatType.Attack, 1), (StatType.Speed, 2)) },
        ["Autotomize"] = new() { Does = (b, use) => b.Autotomize(use) },
        ["HealPulse"] = new() { Does = (b, use) => b.OnEach(use, t => b.HealPulse(t)) },
        ["GuardSplit"] = new() { Does = (b, use) => b.OnEach(use, t => b.Split(use.User, t, guard: true)) },
        ["PowerSplit"] = new() { Does = (b, use) => b.OnEach(use, t => b.Split(use.User, t, guard: false)) },
        ["Soak"] = new() { Does = (b, use) => b.OnEach(use, t => b.Soak(t)) },
        ["ReflectType"] = new() { Does = (b, use) => b.OnEach(use, t => b.ReflectType(use.User, t)) },
        ["SimpleBeam"] = new() { Does = (b, use) => b.OnEach(use, t => b.SimpleBeam(t)) },
        ["Entrainment"] = new() { Does = (b, use) => b.OnEach(use, t => b.Entrain(use.User, t)) },
        ["Bestow"] = new() { Does = (b, use) => b.OnEach(use, t => b.Bestow(use, t)) },

        // ---- the field of the later games, laid through R19b's entry points
        ["ElectricTerrain"] = new() { Does = (b, use) => b.LayTerrain(use, FieldCondition.ElectricTerrain) },
        ["GrassyTerrain"] = new() { Does = (b, use) => b.LayTerrain(use, FieldCondition.GrassyTerrain) },
        ["MistyTerrain"] = new() { Does = (b, use) => b.LayTerrain(use, FieldCondition.MistyTerrain) },
        ["PsychicTerrain"] = new() { Does = (b, use) => b.LayTerrain(use, FieldCondition.PsychicTerrain) },
        ["StickyWeb"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                b.LayStickyWeb(Other(use.User.Side));
            }
        },
        ["AuroraVeil"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                b.RaiseAuroraVeil(use.User);
            }
        }
    };

    // ---------------------------------------------------------------- shared by the batch

    /// <summary>A Pokémon's weight in battle: its own, less 100 kg for each Autotomize, never under 0.1 kg (Showdown's <c>getWeight</c>).</summary>
    internal static float WeightOf(Battler b)
    {
        // (Heavy Metal and Light Metal first: plan 06 · R27)
        float kg = AbilityWeight(b, b.Pokemon!.Weight);
        // A Float Stone halves it (plan 06 · R28a; Showdown's floatstone onModifyWeight)
        if (BattleEffects.HoldEffectOf(b) == "FloatStone") kg /= 2f;
        if (b.Volatile.Autotomized == 0) return kg;
        return Math.Max(0.1f, kg - 100f * b.Volatile.Autotomized);
    }

    /// <summary>Heavy Slam and Heat Crash: 120, 100, 80 or 60 for a user five, four, three or twice as heavy as its target; 40 otherwise.</summary>
    internal static int HeavySlamPower(float mine, float theirs)
    {
        // In hectograms, as the games weigh (pokemon.getWeight)
        int user = (int)Math.Round(mine * 10f), target = (int)Math.Round(theirs * 10f);
        return user >= target * 5 ? 120 : user >= target * 4 ? 100 : user >= target * 3 ? 80 : user >= target * 2 ? 60 : 40;
    }

    /// <summary>The terrain moves: the terrain for five turns (eight with a Terrain Extender); the same one again fails.</summary>
    private void LayTerrain(MoveUse use, FieldCondition terrain)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        if (!SetTerrain(terrain, use.User)) Fails();
    }

    /// <summary>A status move's changes to its user's own stats, in the order Showdown's <c>boosts</c> lists them.</summary>
    private void Raise(MoveUse use, params (StatType Stat, int By)[] changes)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        bool any = false;
        foreach (var (stat, by) in changes) any |= ChangeStat(user, stat, by, user, false, By.Move);
        if (!any) Fails();
    }

    /// <summary>Autotomize: two stages of Speed, and 100 kg lighter while it stays in (it fails at the sixth stage already).</summary>
    private void Autotomize(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (!ChangeStat(user, StatType.Speed, 2, user, true, By.Move)) return;
        if (WeightOf(user) <= 0.1f) return;
        user.Volatile.Autotomized++;
        Say(Text.Of("battle.becameNimble", ("user", user.Name)));
    }

    /// <summary>Heal Pulse: half the target's HP, rounded up; not through a Substitute nor a Heal Block, nor to one whose HP is full.</summary>
    private bool HealPulse(Battler t)
    {
        var p = t.Pokemon!;
        if (t.HasSubstitute || t.Volatile.HealBlockTurns > 0 || p.CurrentHP >= p.MaxHP) return Fails();
        RestoreHp(t, (p.MaxHP + 1) / 2, Text.Of("battle.regainedHealth", ("user", t.Name)));
        return true;
    }

    /// <summary>
    /// Guard Split and Power Split: the user's and the target's Defense and Sp. Def (or Attack and Sp. Atk) each
    /// become the average of the two, rounded down, for as long as each stays in; not through a Substitute.
    /// </summary>
    private bool Split(Battler user, Battler t, bool guard)
    {
        if (t.HasSubstitute) return Fails();
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        KeepStats(Keep(user), p);
        KeepStats(Keep(t), q);
        if (guard)
        {
            p.Defense = q.Defense = (p.Defense + q.Defense) / 2;
            p.SpDefense = q.SpDefense = (p.SpDefense + q.SpDefense) / 2;
            Say(Text.Of("battle.sharedItsGuardWith", ("user", user.Name)));
        }
        else
        {
            p.Attack = q.Attack = (p.Attack + q.Attack) / 2;
            p.SpAttack = q.SpAttack = (p.SpAttack + q.SpAttack) / 2;
            Say(Text.Of("battle.sharedItsPowerWith", ("user", user.Name)));
        }
        return true;
    }

    /// <summary>Soak: the target is a Water type and nothing else; not one that is that already, nor Arceus or Silvally, nor through a Substitute.</summary>
    private bool Soak(Battler t)
    {
        if (t.HasSubstitute || t.Types.SequenceEqual(new[] { PokemonType.Water }) || t.Pokemon!.AbilityName is "Multitype" or "RKS System") return Fails();
        t.Volatile.Types = new List<PokemonType> { PokemonType.Water };
        Say(Text.Of("battle.transformedIntoTheType", ("user", t.Name), ("pick", PokemonType.Water)));
        return true;
    }

    /// <summary>Reflect Type: the user takes the target's types; not Arceus or Silvally (it goes through a Substitute).</summary>
    private bool ReflectType(Battler user, Battler t)
    {
        if (user.Pokemon!.Species.Name is "Arceus" or "Silvally") return Fails();
        user.Volatile.Types = t.Types.ToList();
        Say(Text.Of("battle.typeBecameTheSameAs", ("user", user.Name), ("target", t.Name)));
        return true;
    }

    /// <summary>Simple Beam: the target's ability is Simple while it stays in; not one that is Simple already or can't be replaced, nor through a Substitute.</summary>
    private bool SimpleBeam(Battler t)
    {
        var q = t.Pokemon!;
        if (t.HasSubstitute || q.AbilityName == "Simple" || (q.AbilityName != null && CantBeReplaced.Contains(q.AbilityName))) return Fails();
        KeepAbility(Keep(t), q);
        q.AbilityName = "Simple";
        Say(Text.Of("battle.acquired", ("target", t.Name), ("ability", AbilityOf(t))));
        return true;
    }

    /// <summary>
    /// Entrainment: the target takes the user's ability while it stays in; not the same one, not an ability that
    /// can't be replaced or Truant, nor one the user can't hand on, nor through a Substitute.
    /// </summary>
    private bool Entrain(Battler user, Battler t)
    {
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (t == user || t.HasSubstitute || p.AbilityName == null || q.AbilityName == p.AbilityName) return Fails();
        if ((q.AbilityName != null && CantBeReplaced.Contains(q.AbilityName)) || CantBeReplaced.Contains(p.AbilityName) || NotHandedOn.Contains(p.AbilityName)) return Fails();
        KeepAbility(Keep(t), q);
        q.AbilityName = p.AbilityName;
        Say(Text.Of("battle.acquired", ("target", t.Name), ("ability", AbilityOf(t))));
        return true;
    }

    /// <summary>
    /// Bestow: the user's item goes to a target that holds none, for good; not mail, Arceus's plate or Giratina's
    /// orb, nor an item knocked off (it goes through a Substitute).
    /// </summary>
    private bool Bestow(MoveUse use, Battler t)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        if (q.HeldItem != null || p.HeldItem is not { } item || knockedOff.ContainsKey(p) || knockedOff.ContainsKey(q)) return Fails();
        if (item.Pocket == ItemPocket.Mail || p.AbilityName == "Multitype" || q.AbilityName == "Multitype" || item.Name == "Griseous Orb") return Fails();
        if (ClingsTo(item, p) || ClingsTo(item, q)) return Fails();
        p.HeldItem = null;
        q.HeldItem = item;
        Say(Text.Of("battle.receivedFrom", ("target", t.Name), ("item", ItemOf(t)), ("user", user.Name)));
        return true;
    }

    /// <summary>Incinerate: the berry a target hit holds is burnt up, and can't be brought back; Sticky Hold keeps it.</summary>
    private void Incinerate(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || t.Pokemon!.HeldItem is not { Pocket: ItemPocket.Berries } berry) return;
        if (KeptByStickyHold(use.User, t, use.Move)) return;
        t.Pokemon.HeldItem = null;
        Say(Text.Of("battle.wasBurnedUp", ("target", t.Name), ("item", ItemOf(t, berry))));
    }

    /// <summary>Flame Burst: a sixteenth of their HP from each Pokémon beside its target, whether the hit went into a Substitute or not.</summary>
    private void FlameBurst(MoveUse use, MoveHit hit)
    {
        if (!hit.Landed) return;
        foreach (var ally in SlotsOf(hit.Target.Side).Where(o => o != hit.Target && o.IsActive).ToList())
            LoseHp(ally, Math.Max(1, ally.Pokemon!.MaxHP / 16), Text.Of("battle.theBurstingFlameHit", ("pokemon", ally.Name)));
    }

    /// <summary>
    /// Smack Down: a target in the air, or floating by its type, Levitate or Magnet Rise, falls to the ground and
    /// stays there while it stays in; one that was flying up loses its strike.
    /// </summary>
    private void SmackDown(MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive) return;
        var v = t.Volatile;
        bool flying = v.Elsewhere == Elsewhere.InTheAir;
        bool applies = flying || v.MagnetRiseTurns > 0 ||
            ((t.HasType(PokemonType.Flying) || BattleEffects.Of(t).Any(e => e.Levitates)) && !v.Ingrained && !Field.Gravity && !BattleEffects.Of(t).Any(e => e.GroundsHolder));
        if (!applies) return;
        v.SmackedDown = true;
        v.MagnetRiseTurns = 0;
        if (flying) Unlock(t);
        Say(Text.Of("battle.fellStraightDown", ("target", t.Name)));
    }

    /// <summary>
    /// Circle Throw and Dragon Tail (<c>forceSwitch</c>): a target the hit reached is dragged out for another of its
    /// party, picked by chance; in a wild battle a target of no higher level is driven off and the battle ends.
    /// Ingrain and Suction Cups hold it where it is.
    /// </summary>
    private void ThrowOut(MoveUse use)
    {
        var user = use.User;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        foreach (var hit in use.Hits.Where(h => h.Touched && h.Target.IsActive))
        {
            if (Result != BattleResult.None) return;
            var t = hit.Target;
            if (t.Volatile.Ingrained || (!breaks && t.Ability?.Effect is { HoldsItsGround: true })) continue;
            if (IsTrainerBattle)
            {
                if (HasReplacement(t)) DragOut(t);
                continue;
            }
            if (IsDouble || user.Pokemon!.Level < t.Pokemon!.Level) continue;
            Say(Text.Of("battle.wasDrivenOff", ("target", t.Name)));
            End(BattleResult.PlayerRan);
        }
    }

    /// <summary>What the batch's moves remember of a damaging move that landed (Fusion Flare and Fusion Bolt ask).</summary>
    private void NoteLanded(MoveUse use)
    {
        if (use.Hits.Any(h => h.Landed)) landedLast = (Turn, use.Data.Name);
    }
}
