using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The move families whose rule is a formula or a condition on the battle's numbers (plan 06 · R4): moves that hit
// several times, fixed damage, one-hit knockouts, power by HP, weight, Speed, friendship or what went before,
// moves that cost their user everything, the counters, Stockpile's three, and the hits that only work in some
// turn. Each entry names the original's script command or effect script it was read from. They join the table
// in BattleCore.Effects.cs when the class is first used.
public sealed partial class BattleCore
{
    static BattleCore()
    {
        foreach (var (name, effect) in FamilyEffects) MoveEffects[name] = effect;
    }

    private static readonly Elsewhere[] Underground = { Elsewhere.Underground };

    /// <summary>The original's own numbering of the types, which Hidden Power's formula indexes (<c>generated/pokemon_types.txt</c>; 9 is its "mystery" type).</summary>
    private static readonly PokemonType[] PlatinumTypeOrder =
    {
        PokemonType.Normal, PokemonType.Fighting, PokemonType.Flying, PokemonType.Poison, PokemonType.Ground, PokemonType.Rock,
        PokemonType.Bug, PokemonType.Ghost, PokemonType.Steel, PokemonType.Normal, PokemonType.Fire, PokemonType.Water,
        PokemonType.Grass, PokemonType.Electric, PokemonType.Psychic, PokemonType.Ice, PokemonType.Dragon, PokemonType.Dark
    };

    private static readonly Dictionary<string, MoveEffect> FamilyEffects = new()
    {
        // ---- several hits (BtlCmd_SetMultiHit, BattleControllerPlayer_LoopMultiHit)
        ["MultiHit"] = new() { Strikes = (b, use) => b.MultiHitCount(use.User) },
        ["HitTwice"] = new() { Strikes = (b, use) => 2 },
        ["PoisonMultiHit"] = new() { Strikes = (b, use) => 2 },
        ["HitThreeTimes"] = new()
        {
            // Triple Kick: each kick rolls its own accuracy and is 10 stronger than the one before
            Strikes = (b, use) => 3,
            EachHitRollsAccuracy = true,
            BasePower = (b, use, t) => use.Data.Power * use.HitNumber
        },
        ["BeatUp"] = new()
        {
            // BtlCmd_BeatUp: one hit for each of the party that stands without a condition (the user always), each
            // by that member's base Attack and level against the target's base Defense, with no type to it
            Start = (b, use) => { use.Scratch = b.BeatUpParty(use.User); return true; },
            Strikes = (b, use) => ((List<Pokemon>)use.Scratch!).Count,
            StrikeLine = (b, use) => NameOf(use.User, ((List<Pokemon>)use.Scratch!)[use.HitNumber - 1]) + "'s attack!",
            Deals = (b, use, t) => b.BeatUpHit(use, ((List<Pokemon>)use.Scratch!)[use.HitNumber - 1], t),
            HitsAnything = true,
            NoHitCount = true
        },

        // ---- a fixed amount (the effect scripts set the damage themselves; their types count only for immunity)
        ["20DamageFlat"] = new() { Deals = (b, use, t) => 20 },
        ["40DamageFlat"] = new() { Deals = (b, use, t) => 40 },
        ["LevelDamageFlat"] = new() { Deals = (b, use, t) => use.User.Pokemon!.Level },
        ["HalveHp"] = new() { Deals = (b, use, t) => Formulas.Divide(t.Pokemon!.CurrentHP, 2) },
        ["SetHpEqualToUser"] = new()
        {
            // Endeavor: down to the user's own HP; nothing when the target is already as low
            Deals = (b, use, t) => t.Pokemon!.CurrentHP > use.User.Pokemon!.CurrentHP ? t.Pokemon.CurrentHP - use.User.Pokemon.CurrentHP : null
        },
        ["RandomDamage1To150Level"] = new()
        {
            // Psywave: the level times five to fifteen tenths (Random 10, 5)
            Deals = (b, use, t) => Math.Max(1, use.User.Pokemon!.Level * (b.rng.Roll(RollKind.Power, 11) + 5) / 10)
        },
        ["OneHitKo"] = new()
        {
            Decides = (b, use, t, hit) => b.OneHitKo(use, t, hit),
            Deals = (b, use, t) => t.Pokemon!.MaxHP
        },

        // ---- power by the numbers
        ["IncreasePowerWithLessHp"] = new() { BasePower = (b, use, t) => FlailPower(use.User.Pokemon!) },
        ["IncreasePowerWithMoreHp"] = new() { BasePower = (b, use, t) => 1 + 120 * t.Pokemon!.CurrentHP / t.Pokemon.MaxHP },
        ["DecreasePowerWithLessUserHp"] = new() { BasePower = (b, use, t) => Math.Max(1, use.Data.Power * use.User.Pokemon!.CurrentHP / use.User.Pokemon.MaxHP) },
        ["IncreasePowerWithWeight"] = new() { BasePower = (b, use, t) => WeightPower(t.Pokemon!) },
        ["PowerBasedOnLowSpeed"] = new()
        {
            // Gyro Ball: 1 + 25 × the target's Speed over the user's, no more than 150
            BasePower = (b, use, t) => Math.Min(150, 1 + 25 * b.EffectiveSpeed(t) / b.EffectiveSpeed(use.User))
        },
        ["PowerBasedOnFriendship"] = new() { BasePower = (b, use, t) => use.User.Pokemon!.Friendship * 10 / 25 },
        ["PowerBasedOnLowFriendship"] = new() { BasePower = (b, use, t) => (255 - use.User.Pokemon!.Friendship) * 10 / 25 },
        ["IncreasePowerWithMoreStatUp"] = new()
        {
            // Punishment: 60 and 20 for each stage the target has raised, no more than 200
            BasePower = (b, use, t) => Math.Min(200, 60 + 20 * t.Pokemon!.StatStages.Values.Where(s => s > 0).Sum())
        },
        ["HigherPowerWhenLowPp"] = new() { BasePower = (b, use, t) => TrumpCardPower[Math.Min(4, use.Move.CurrentPP)] },
        ["RandomPowerBasedOnIvs"] = new()
        {
            // Hidden Power: the second bit of each IV makes the power, the first bit the type (BtlCmd_CalcHiddenPowerParams)
            Start = (b, use) =>
            {
                var p = use.User.Pokemon!;
                int powerBits = ((p.IvHP & 2) >> 1) | (p.IvAttack & 2) | ((p.IvDefense & 2) << 1) | ((p.IvSpeed & 2) << 2) | ((p.IvSpAttack & 2) << 3) | ((p.IvSpDefense & 2) << 4);
                int typeBits = (p.IvHP & 1) | ((p.IvAttack & 1) << 1) | ((p.IvDefense & 1) << 2) | ((p.IvSpeed & 1) << 3) | ((p.IvSpAttack & 1) << 4) | ((p.IvSpDefense & 1) << 5);
                use.Scratch = b.Rules.HiddenPowerPower > 0 ? b.Rules.HiddenPowerPower : powerBits * 40 / 63 + 30;
                int id = typeBits * 15 / 63 + 1;
                if (id >= 9) id++;
                var type = PlatinumTypeOrder[id];
                if (type != use.Move.Type) use.Move = new Move(use.Data.OfType(type), use.Move.CurrentPP);
                return true;
            },
            BasePower = (b, use, t) => (int)use.Scratch!
        },
        ["RandomPowerMaybeHeal"] = new() { Does = (b, use) => b.Present(use) },
        ["Judgement"] = new()
        {
            // The type of the plate its user holds (the plates' hold effects are named for it)
            Start = (b, use) =>
            {
                string? hold = use.User.Volatile.EmbargoTurns > 0 ? null : use.User.Pokemon!.HeldItem?.HoldEffect;
                if (hold != null && hold.StartsWith("Arceus") && Enum.TryParse(hold["Arceus".Length..], out PokemonType type) && type != use.Move.Type)
                    use.Move = new Move(use.Data.OfType(type), use.Move.CurrentPP);
                return true;
            }
        },
        ["DoublePowerWhenBelowHalf"] = new() { Power = (b, use, t) => t.Pokemon!.CurrentHP <= t.Pokemon.MaxHP / 2 ? 20 : 10 },
        ["DoublePowerIfMovingSecond"] = new() { Power = (b, use, t) => t.Turn.Acted ? 20 : 10 },
        ["DoublePowerIfTargetHit"] = new() { Power = (b, use, t) => t.Turn.TookDamage ? 20 : 10 },
        ["DoublePowerIfHit"] = new()
        {
            // Revenge, Avalanche: twice as strong against whoever hurt the user this turn
            Power = (b, use, t) => use.User.Turn.PhysicalFrom[t.Place.Number] > 0 || use.User.Turn.SpecialFrom[t.Place.Number] > 0 ? 20 : 10
        },
        ["DoublePowerEachTurn"] = new()
        {
            // Fury Cutter: doubles with each use, up to 160, until something stops its user (a miss, a flinch…)
            BasePower = (b, use, t) =>
            {
                var v = use.User.Volatile;
                v.FuryCutterCount = Math.Min(5, v.FuryCutterCount + 1);
                return Math.Min(160, use.Data.Power << (v.FuryCutterCount - 1));
            }
        },
        ["DoublePowerEachTurnLockInto"] = new()
        {
            // Rollout, Ice Ball: five turns held to the move, each twice the last; Defense Curl doubles it all
            BasePower = (b, use, t) =>
            {
                var v = use.User.Volatile;
                if (v.RolloutTurns == 0)
                {
                    v.RolloutTurns = 5;
                    v.LockedMove = use.Data;
                }
                v.RolloutTurns--;
                int power = use.Data.Power << (5 - v.RolloutTurns - 1);
                if (v.DefenseCurl) power *= 2;
                if (v.RolloutTurns == 0 && !use.User.IsHeldToItsMove) v.LockedMove = null;
                return power;
            }
        },

        // ---- what costs the user everything
        ["HalveDefense"] = new()
        {
            // Explosion, Self-Destruct: Damp anywhere stops it; the user's HP goes to nothing before the hit
            Start = (b, use) =>
            {
                bool breaks = BattleEffects.Of(use.User).Any(e => e.IgnoresTargetAbility);
                var damp = breaks ? null : b.AllBattlers.FirstOrDefault(o => o.IsActive && Has(o, "Damp"));
                if (damp == null) return true;
                b.Say($"{damp.Name}'s Damp prevents {use.User.Name} from using {use.Move.Name}!");
                return false;
            },
            SelfKo = true
        },
        ["FaintAndAtkSpAtkDown2"] = new() { Does = (b, use) => b.Memento(use), Follows = new[] { Elsewhere.InTheAir, Elsewhere.Underground, Elsewhere.Underwater, Elsewhere.Vanished } },
        ["MaxAtkLoseHalfMaxHp"] = new() { Does = (b, use) => b.BellyDrum(use) },

        // ---- hits that go by the turn
        ["LeaveWith1Hp"] = new() { LeavesOneHp = true },
        ["TriAttack"] = new() { Hit = (b, use, hit) => b.TriAttack(use, hit) },
        ["AlwaysFlinchFirstTurnOnly"] = new()
        {
            // Fake Out: only on the first turn its user gets to act after coming in
            Start = (b, use) => b.Turn == use.User.Volatile.FirstTurn || b.Fails(),
            Hit = (b, use, hit) =>
            {
                var t = hit.Target;
                if (!hit.Touched || !t.IsActive || t.MovedThisTurn) return;
                bool breaks = BattleEffects.Of(use.User).Any(e => e.IgnoresTargetAbility);
                if (BattleEffects.Of(t, includeAbility: !breaks).Any(e => e.BlocksFlinch || e.BlocksSideEffects)) return;
                t.Flinched = true;
            }
        },
        ["HitFirstIfTargetAttacking"] = new() { Start = (b, use) => b.SuckerPunch(use) },
        ["HitLastWhiffIfHit"] = new()
        {
            // Focus Punch: the focus is lost once anything hurts its user this turn
            Start = (b, use) =>
            {
                if (use.User.Turn.PhysicalTaken == 0 && use.User.Turn.SpecialTaken == 0) return true;
                b.Say($"{use.User.Name} lost its focus and couldn't move!");
                return false;
            }
        },
        ["FailIfNotUsedAllOtherMoves"] = new()
        {
            // Last Resort: every other move must have been used once (BtlCmd_TryLastResort)
            Start = (b, use) =>
            {
                var moves = use.User.Pokemon!.Moves;
                int mine = moves.IndexOf(use.Move);
                int others = Enumerable.Range(0, moves.Count).Count(i => i != mine && (use.User.Volatile.UsedMoveSlots & (1 << i)) != 0);
                return moves.Count >= 2 && others >= moves.Count - 1 || b.Fails();
            }
        },
        ["IncreasePrizeMoney"] = new()
        {
            Hit = (b, use, hit) =>
            {
                if (!hit.Touched) return;
                if (use.User.IsPlayerSide) b.PayDayMoney += use.User.Pokemon!.Level * 5;
                b.Say("Coins scattered everywhere!");
            }
        },

        // ---- the counters (BtlCmd_Counter, BtlCmd_MirrorCoat, BtlCmd_TryMetalBurst)
        ["Counter"] = new() { Start = (b, use) => b.CounterBack(use, MoveCategory.Physical), Deals = (b, use, t) => (int)use.Scratch! },
        ["MirrorCoat"] = new() { Start = (b, use) => b.CounterBack(use, MoveCategory.Special), Deals = (b, use, t) => (int)use.Scratch! },
        ["MetalBurst"] = new() { Start = (b, use) => b.CounterBack(use, null), Deals = (b, use, t) => (int)use.Scratch! },

        // ---- Stockpile's three
        ["Stockpile"] = new() { Does = (b, use) => b.Stockpile(use) },
        ["SpitUp"] = new() { Does = (b, use) => b.SpitUp(use), BasePower = (b, use, t) => (int)use.Scratch!, NoVariance = true },
        ["Swallow"] = new() { Does = (b, use) => b.Swallow(use) },

        // ---- the rest of the family
        ["AverageHp"] = new() { Does = (b, use) => b.PainSplit(use) },
        ["RandomStatUp2"] = new() { Does = (b, use) => b.Acupressure(use) },
        ["CopyStatChanges"] = new() { Does = (b, use) => b.PsychUp(use) },
        ["SpAtkDown2OppositeGender"] = new() { Does = (b, use) => b.Captivate(use) },
        ["Psywave"] = new()
        {
            // Magnitude (the original names its effect after Psywave): a strength drawn once a use, twice as hard on a target underground
            Start = (b, use) =>
            {
                int roll = b.rng.Roll(RollKind.Power, 100);
                (int magnitude, int power) = roll < 5 ? (4, 10) : roll < 15 ? (5, 30) : roll < 35 ? (6, 50) : roll < 65 ? (7, 70) : roll < 85 ? (8, 90) : roll < 95 ? (9, 110) : (10, 150);
                use.Scratch = power;
                b.Say($"Magnitude {magnitude}!");
                return true;
            },
            BasePower = (b, use, t) => (int)use.Scratch!,
            Follows = Underground,
            Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.Underground ? 20 : 10
        },
        // What a Pokémon does with no PP left is its own move (StruggleData); the data's entry only stands for it
        ["Struggle"] = new()
    };

    // ---------------------------------------------------------------- tables

    /// <summary>Flail and Reversal by the HP bar's 64 pixels (<c>sHPPixelsToFlailPower</c>, <c>App_PixelCount</c>).</summary>
    internal static int FlailPower(Pokemon p)
    {
        int pixels = p.CurrentHP * 64 / p.MaxHP;
        if (pixels == 0 && p.CurrentHP > 0) pixels = 1;
        return pixels <= 1 ? 200 : pixels <= 5 ? 150 : pixels <= 12 ? 100 : pixels <= 21 ? 80 : pixels <= 42 ? 40 : 20;
    }

    /// <summary>Low Kick and Grass Knot by the target's weight in tenths of a kilogram (<c>sWeightToPower</c>).</summary>
    internal static int WeightPower(Pokemon p)
    {
        int weight = (int)Math.Round(p.Species.Weight * 10f);
        return weight <= 100 ? 20 : weight <= 250 ? 40 : weight <= 500 ? 60 : weight <= 1000 ? 80 : weight <= 2000 ? 100 : 120;
    }

    /// <summary>Trump Card by the PP it has left once used (<c>sCurrentPPScaledPower</c>).</summary>
    private static readonly int[] TrumpCardPower = { 200, 80, 60, 50, 40 };

    // ---------------------------------------------------------------- several hits

    /// <summary>
    /// How many times a move of two to five hits does (<c>BtlCmd_SetMultiHit</c>): in Platinum two or three on
    /// half the draws and then any of the four on the rest; by the modern rules out of a hundred. Skill Link
    /// makes it five.
    /// </summary>
    private int MultiHitCount(Battler user)
    {
        if (BattleEffects.Of(user).Any(e => e.AlwaysHitsFiveTimes)) return 5;
        if (Rules.MultiHitByHundredths)
        {
            int r = rng.Roll(RollKind.HitCount, 100);
            return r < 35 ? 2 : r < 70 ? 3 : r < 85 ? 4 : 5;
        }
        int hits = rng.Roll(RollKind.HitCount, 4);
        return hits < 2 ? hits + 2 : rng.Roll(RollKind.HitCount, 4) + 2;
    }

    /// <summary>Who joins a Beat Up: the user, and every other member of its party that stands with no condition.</summary>
    private List<Pokemon> BeatUpParty(Battler user)
    {
        var party = user.Roster?.Members ?? new List<Pokemon> { user.Pokemon! };
        return party.Where(p => p == user.Pokemon || (!p.IsFainted && p.Status == StatusCondition.None)).ToList();
    }

    /// <summary>One member's part of a Beat Up: its base Attack and level against the target's base Defense, a critical hit and the roll, nothing else.</summary>
    private int BeatUpHit(MoveUse use, Pokemon member, Battler target)
    {
        int damage = member.Species.BaseAttack * use.Data.Power * (member.Level * 2 / 5 + 2) / Math.Max(1, target.Pokemon!.Species.BaseDefense) / 50 + 2;
        if (DamageCalculator.RollsCritical(use.User, target, use.Move, rng, Rules))
        {
            damage = Formulas.Scale(damage, Rules.CriticalMultiplier * BattleEffects.Of(use.User).Aggregate(1f, (m, e) => Math.Max(m, e.CriticalBoost)));
            use.Hits[^1].Critical = true;
        }
        return Formulas.Variance(damage, rng.Roll(RollKind.Damage, 16));
    }

    /// <summary>A party member's name as the side's lines say it.</summary>
    private static string NameOf(Battler side, Pokemon member) => side.IsPlayerSide ? member.DisplayName : $"Foe {member.DisplayName}";

    // ---------------------------------------------------------------- a one-hit knockout

    /// <summary>
    /// <c>BtlCmd_TryOHKOMove</c>: Sturdy stands against it; it needs the user's level to be no lower than the
    /// target's, and then lands on a roll of 100 under its accuracy plus the levels' difference, or surely under
    /// Lock-On or No Guard. Against a higher level it never works, and says so.
    /// </summary>
    private bool OneHitKo(MoveUse use, Battler target, MoveHit hit)
    {
        var user = use.User;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (!breaks && Has(target, "Sturdy"))
        {
            hit.Missed = true;
            hit.FailLine = $"{target.Name} was protected by Sturdy!";
            return false;
        }
        int mine = user.Pokemon!.Level, theirs = target.Pokemon!.Level;
        bool sure = (target.Volatile.LockOnTurns > 0 && target.Volatile.LockedOnBy == user.Place) || Has(user, "No Guard") || Has(target, "No Guard");
        bool lands = sure && mine >= theirs || (rng.Roll(RollKind.Accuracy, 100) < use.Move.Accuracy + (mine - theirs) && mine >= theirs);
        if (lands)
        {
            hit.OneHitKo = true;
            return true;
        }
        hit.Missed = true;
        if (mine < theirs) hit.FailLine = $"{target.Name} is unaffected!";
        return false;
    }

    // ---------------------------------------------------------------- the counters

    /// <summary>
    /// Counter, Mirror Coat and Metal Burst (null: either kind) turn on whoever last hurt the user that way
    /// this turn, if it stands on the other side and still stands: twice the damage, or half as much again for
    /// Metal Burst. Otherwise they fail.
    /// </summary>
    private bool CounterBack(MoveUse use, MoveCategory? category)
    {
        var tf = use.User.Turn;
        Place? from = category switch { MoveCategory.Physical => tf.LastPhysicalFrom, MoveCategory.Special => tf.LastSpecialFrom, _ => tf.LastHitFrom };
        int taken = from is { } f ? category switch { MoveCategory.Physical => tf.PhysicalFrom[f.Number], MoveCategory.Special => tf.SpecialFrom[f.Number], _ => tf.LastDamage } : 0;
        if (from is not { } place || taken <= 0 || place.Side == use.User.Side || !At(place).IsActive) return Fails();
        use.Targets = new List<Battler> { At(place) };
        use.Scratch = category == null ? taken * 15 / 10 : taken * 2;
        return true;
    }

    // ---------------------------------------------------------------- what costs the user everything

    /// <summary>Memento: the user is spent whatever comes of it; the target's Attack and Sp. Atk fall two stages each.</summary>
    private void Memento(MoveUse use)
    {
        var user = use.User;
        user.Pokemon!.CurrentHP = 0;
        use.Line.With(new HpChanged(user.Place, 0, Healed: false));
        OnEach(use, t =>
        {
            if (t.HasSubstitute) return false;
            var stages = t.Pokemon!.StatStages;
            if (stages.GetValueOrDefault(StatType.Attack) <= -6 && stages.GetValueOrDefault(StatType.SpAttack) <= -6) return Fails();
            ChangeStat(t, StatType.Attack, -2, user, true, By.Move);
            ChangeStat(t, StatType.SpAttack, -2, user, true, By.Move);
            return true;
        });
        ResolveFaints();
    }

    /// <summary><c>subscript_belly_drum</c>: half its HP for the most Attack there is; nothing with less than half left, or at the top already.</summary>
    private void BellyDrum(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        int half = Formulas.Divide(p.MaxHP, 2);
        if (p.StatStages.GetValueOrDefault(StatType.Attack) >= 6 || p.CurrentHP <= half)
        {
            Fails();
            return;
        }
        p.CurrentHP -= half;
        p.StatStages[StatType.Attack] = 6;
        Say($"{user.Name} cut its own HP and maximized its Attack!")
            .With(new HpChanged(user.Place, p.CurrentHP, Healed: false)).With(new StageChanged(user.Place, StatType.Attack, 6, Rose: true));
        CheckConditionHooks(user, null);
    }

    // ---------------------------------------------------------------- hits that go by the turn

    /// <summary>Tri Attack: by its chance, one of a burn, a freeze and paralysis, drawn evenly.</summary>
    private void TriAttack(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        var user = use.User;
        if (!hit.Touched || !t.IsActive || t == user) return;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (BattleEffects.Of(t, includeAbility: !breaks).Any(e => e.BlocksSideEffects)) return;
        int chance = use.Data.EffectChance * BattleEffects.Of(user).Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();
        if (!Chance(chance)) return;
        var status = rng.Roll(RollKind.Pick, 3) switch { 0 => StatusCondition.Burn, 1 => StatusCondition.Freeze, _ => StatusCondition.Paralyze };
        TryInflictStatus(t, status, user, false, By.SideEffect);
    }

    /// <summary><c>BtlCmd_TrySuckerPunch</c>: only against a target that is still to use a damaging move this turn.</summary>
    private bool SuckerPunch(MoveUse use)
    {
        var t = use.Targets[0];
        var pending = waiting.FirstOrDefault(a => a.User == t && a.Actor == t.Pokemon);
        if (pending == null || pending.Choice.Kind != ChoiceKind.Fight || pending.Move == null) return Fails();
        var chosen = pending.Move;
        if (t.Volatile.Encored is { } encored && t.Pokemon!.Moves.FirstOrDefault(m => m.Data == encored) is { } instead) chosen = instead;
        return chosen.Power > 0 || chosen.Data == StruggleData || Fails();
    }

    /// <summary>Present: a gift of 40, 80 or 120 power, or a quarter of the target's HP given back (<c>BtlCmd_Present</c>).</summary>
    private void Present(MoveUse use)
    {
        int roll = rng.Roll(RollKind.Power, 256);
        if (roll < 255 * 80 / 100)
        {
            use.Scratch = roll < 255 * 40 / 100 ? 40 : roll < 255 * 70 / 100 ? 80 : 120;
            Strike(use);
            return;
        }
        OnEach(use, t =>
        {
            var p = t.Pokemon!;
            if (t.Volatile.HealBlockTurns > 0)
            {
                Say($"{t.Name} was prevented from healing!");
                return false;
            }
            if (p.CurrentHP >= p.MaxHP)
            {
                Say($"{t.Name}'s HP is full!");
                return false;
            }
            RestoreHp(t, Formulas.Divide(p.MaxHP, 4), $"{t.Name} regained health!");
            return true;
        });
    }

    // ---------------------------------------------------------------- Stockpile's three

    /// <summary><c>subscript_stockpile</c>: up to three, each raising Defense and Sp. Def a stage (those raises are what Spit Up and Swallow take back).</summary>
    private void Stockpile(MoveUse use)
    {
        var user = use.User;
        var v = user.Volatile;
        use.Line.With(Shown(user, user, use.Move, null));
        if (v.Stockpile >= 3)
        {
            Fails();
            return;
        }
        v.Stockpile++;
        Say($"{user.Name} stockpiled {v.Stockpile}!");
        if (ChangeStat(user, StatType.Defense, 1, user, false, By.Move)) v.StockpileDef++;
        if (ChangeStat(user, StatType.SpDefense, 1, user, false, By.Move)) v.StockpileSpDef++;
    }

    /// <summary>What Spit Up and Swallow take back: the count, and the stages Stockpile raised.</summary>
    private void Unstockpile(Battler user)
    {
        var v = user.Volatile;
        var p = user.Pokemon!;
        v.Stockpile = 0;
        foreach (var (stat, boosts) in new[] { (StatType.Defense, v.StockpileDef), (StatType.SpDefense, v.StockpileSpDef) })
        {
            if (boosts == 0) continue;
            int stage = Math.Max(-6, p.StatStages.GetValueOrDefault(stat) - boosts);
            p.StatStages[stat] = stage;
            Emit(new StageChanged(user.Place, stat, stage, Rose: false));
        }
        v.StockpileDef = v.StockpileSpDef = 0;
    }

    /// <summary>Spit Up: a hit of 100 power for each stockpile, with no roll to it, and the stockpile is gone.</summary>
    private void SpitUp(MoveUse use)
    {
        var user = use.User;
        if (user.Volatile.Stockpile == 0)
        {
            Say("But it failed to spit up a thing!");
            return;
        }
        use.Scratch = 100 * user.Volatile.Stockpile;
        Unstockpile(user);
        use.AfterLine = $"{user.Name}'s stockpiled effect wore off!";
        Strike(use);
    }

    /// <summary>Swallow: a quarter, a half or all of its HP back, by the stockpile, which is gone.</summary>
    private void Swallow(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Volatile.Stockpile == 0)
        {
            Say("But it failed to swallow a thing!");
            return;
        }
        int amount = Formulas.Divide(p.MaxHP, 1 << (3 - user.Volatile.Stockpile));
        Unstockpile(user);
        if (p.CurrentHP >= p.MaxHP) Say($"{user.Name}'s HP is full!");
        else RestoreHp(user, amount, $"{user.Name} regained health!");
    }

    // ---------------------------------------------------------------- the rest

    /// <summary><c>subscript_pain_split</c>: both at the average of their HP; not through a Substitute.</summary>
    private void PainSplit(MoveUse use)
    {
        var user = use.User;
        OnEach(use, t =>
        {
            if (t.HasSubstitute) return Fails();
            var mine = user.Pokemon!;
            var theirs = t.Pokemon!;
            int average = (mine.CurrentHP + theirs.CurrentHP) / 2;
            mine.CurrentHP = Math.Min(mine.MaxHP, average);
            theirs.CurrentHP = Math.Min(theirs.MaxHP, average);
            Say("The battlers shared their pain!")
                .With(new HpChanged(user.Place, mine.CurrentHP, Healed: true)).With(new HpChanged(t.Place, theirs.CurrentHP, Healed: false));
            CheckConditionHooks(user, null);
            CheckConditionHooks(t, user);
            return true;
        });
    }

    /// <summary><c>BtlCmd_BoostRandomStatBy2</c>: one of the stats not at the top yet, drawn evenly, up two.</summary>
    private void Acupressure(MoveUse use)
    {
        var user = use.User;
        OnEach(use, t =>
        {
            if (t != user && t.HasSubstitute) return Fails();
            var stages = t.Pokemon!.StatStages;
            var open = new[] { StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense, StatType.Accuracy, StatType.Evasion }
                .Where(s => stages.GetValueOrDefault(s) < 6).ToList();
            if (open.Count == 0) return Fails();
            ChangeStat(t, open[rng.Roll(RollKind.Pick, open.Count)], 2, user, true, By.Move);
            return true;
        });
    }

    /// <summary><c>BtlCmd_CopyStatStages</c>: the target's stages, and its Focus Energy, become the user's.</summary>
    private void PsychUp(MoveUse use)
    {
        var user = use.User;
        OnEach(use, t =>
        {
            var mine = user.Pokemon!;
            foreach (var stat in Enum.GetValues<StatType>())
            {
                int stage = t.Pokemon!.StatStages.GetValueOrDefault(stat);
                if (stage == mine.StatStages.GetValueOrDefault(stat)) continue;
                mine.StatStages[stat] = stage;
                Emit(new StageChanged(user.Place, stat, stage, Rose: stage > 0));
            }
            if (t.Volatile.FocusEnergy) user.Volatile.FocusEnergy = true;
            Say($"{user.Name} copied {t.Name}'s stat changes!");
            return true;
        });
    }

    /// <summary>Captivate: two stages off the Sp. Atk of a target of the other gender; Oblivious is above it.</summary>
    private void Captivate(MoveUse use)
    {
        var user = use.User;
        OnEach(use, t =>
        {
            bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
            if (!breaks && Has(t, "Oblivious"))
            {
                Say($"{t.Name}'s Oblivious made {use.Move.Name} ineffective!");
                return false;
            }
            var mine = user.Pokemon!.Gender;
            var theirs = t.Pokemon!.Gender;
            if (mine == Gender.Genderless || theirs == Gender.Genderless || mine == theirs)
            {
                Say($"It failed to affect {t.Name}!");
                return false;
            }
            return ChangeStat(t, StatType.SpAttack, -2, user, true, By.Move);
        });
    }
}
