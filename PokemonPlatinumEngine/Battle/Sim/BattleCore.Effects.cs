using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// What a move does of its own, by the name of its effect in moves.json (the original's BATTLE_EFFECT_*). A move
// whose data fields say everything has no entry here. Plan 06 · R3 wrote the conditions and the field: the passing
// states (Substitute, Taunt, Encore, Leech Seed…), the weather, the screens and hazards, protection, the moves that
// take two turns or hold their user, and the ways of leaving the field. R4 to R6 add the rest.
public sealed partial class BattleCore
{
    /// <summary>A turn spent getting ready: the line it is told with, and where it takes the user meanwhile.</summary>
    private sealed record ChargeTurn(string Line, Elsewhere Where = Elsewhere.No, bool RaisesDefense = false, bool NotInTheSun = false);

    /// <summary>
    /// One move effect: only the parts it has are set. <see cref="Does"/> is a whole move in itself (a status
    /// move, or one that isn't a plain hit); the rest shape a hit.
    /// </summary>
    private sealed class MoveEffect
    {
        /// <summary>The whole of the move, once "X used Y!" is said and its targets are found.</summary>
        public Action<BattleCore, MoveUse>? Does { get; init; }

        public ChargeTurn? Charge { get; init; }

        /// <summary>Before a hit is tried: false when the move fails (and has said so).</summary>
        public Func<BattleCore, MoveUse, bool>? Start { get; init; }

        /// <summary>
        /// For each target the move gets to, before the damage and even if its type can't hurt it (Brick Break
        /// breaks the screens of whoever it reaches).
        /// </summary>
        public Action<BattleCore, MoveUse, Battler>? Reach { get; init; }

        /// <summary>The move's power against one target, in tenths of what its data says (20: double).</summary>
        public Func<BattleCore, MoveUse, Battler, int>? Power { get; init; }

        /// <summary>The move's power worked out afresh for this hit, in place of its data's (Flail, Gyro Ball, Rollout).</summary>
        public Func<BattleCore, MoveUse, Battler, int>? BasePower { get; init; }

        /// <summary>
        /// A fixed amount of damage in place of the formula's (Seismic Toss, Counter): no bonus for the type, no
        /// critical hit, no screen, only an immunity in its way. Null when the move fails for this target.
        /// </summary>
        public Func<BattleCore, MoveUse, Battler, int?>? Deals { get; init; }

        /// <summary>How many times the move hits, drawn once a use (two to five, Triple Kick's three, Beat Up's party).</summary>
        public Func<BattleCore, MoveUse, int>? Strikes { get; init; }

        /// <summary>Each hit after the first rolls its own accuracy, and a miss ends them (Triple Kick).</summary>
        public bool EachHitRollsAccuracy { get; init; }

        /// <summary>A line before each hit (Beat Up names whose it is).</summary>
        public Func<BattleCore, MoveUse, string?>? StrikeLine { get; init; }

        /// <summary>Several hits with no count told after them (Beat Up).</summary>
        public bool NoHitCount { get; init; }

        /// <summary>No type is immune to it (Beat Up).</summary>
        public bool HitsAnything { get; init; }

        /// <summary>Decides for itself whether it gets there, in place of the accuracy roll (a one-hit knockout); Protect and a target out of reach still stand in its way.</summary>
        public Func<BattleCore, MoveUse, Battler, MoveHit, bool>? Decides { get; init; }

        /// <summary>The hit takes the formula's whole number, with no roll off it (Spit Up).</summary>
        public bool NoVariance { get; init; }

        /// <summary>Its user's HP goes to nothing before the hit, whatever comes of it (Explosion).</summary>
        public bool SelfKo { get; init; }

        /// <summary>It always leaves its target at least 1 HP (False Swipe).</summary>
        public bool LeavesOneHp { get; init; }

        /// <summary>It hurts, whatever its data's power says: by a power or an amount of its own.</summary>
        public bool Damaging => Deals != null || BasePower != null;

        public int CritBonus { get; init; }
        public bool PastScreens { get; init; }

        /// <summary>Where the move can follow a target that most moves can't reach.</summary>
        public Elsewhere[] Follows { get; init; } = Array.Empty<Elsewhere>();

        /// <summary>The weather that makes the move sure to hit, and what its accuracy is under another.</summary>
        public BattleWeather SureIn { get; init; }
        public Func<BattleWeather, int, int>? Accuracy { get; init; }

        /// <summary>After the damage and the lines about it, for each target the move reached.</summary>
        public Action<BattleCore, MoveUse, MoveHit>? Hit { get; init; }

        /// <summary>Once after all targets, when the move landed on any and its user still stands.</summary>
        public Action<BattleCore, MoveUse>? Then { get; init; }

        /// <summary>For a target the move didn't get to.</summary>
        public Action<BattleCore, MoveUse, MoveHit>? Miss { get; init; }

        /// <summary>Who the move is aimed at when that depends on its user (Curse).</summary>
        public Func<Battler, MoveTarget, MoveTarget>? Aim { get; init; }

        /// <summary>Whether Protect stops it, for a move it stops only some of the time (Curse from a Ghost).</summary>
        public Func<Battler, bool>? Protectable { get; init; }

        /// <summary>On the turns it goes on by itself the move says its own lines, not "X used Y!" (Bide).</summary>
        public bool OwnLinesAfterFirst { get; init; }

        public void Use(BattleCore b, MoveUse use) => Does!(b, use);

        public bool Begins(BattleCore b, MoveUse use) => Start?.Invoke(b, use) ?? true;
        public void OnReach(BattleCore b, MoveUse use, Battler target) => Reach?.Invoke(b, use, target);
        public int PowerTenths(BattleCore b, MoveUse use, Battler target) => use.Boost * (Power?.Invoke(b, use, target) ?? 10) / 10;
        public bool FollowsTo(Elsewhere where) => Array.IndexOf(Follows, where) >= 0;
        public bool SureUnder(BattleWeather weather) => SureIn != BattleWeather.None && SureIn == weather;
        public int AccuracyUnder(BattleWeather weather, int accuracy) => Accuracy?.Invoke(weather, accuracy) ?? accuracy;
        public bool RollsAccuracy => true;
        public void OnHit(BattleCore b, MoveUse use, MoveHit hit) => Hit?.Invoke(b, use, hit);
        public void AfterHits(BattleCore b, MoveUse use) => Then?.Invoke(b, use);
        public void OnMiss(BattleCore b, MoveUse use, MoveHit hit) => Miss?.Invoke(b, use, hit);
        public MoveTarget TargetFor(Battler user, MoveTarget target) => Aim?.Invoke(user, target) ?? target;
        public bool StoppedByProtect(Battler user) => Protectable?.Invoke(user) ?? true;
    }

    /// <summary>A move with nothing of its own: it hits, or does what its data fields say.</summary>
    private static readonly MoveEffect Plain = new();

    private static MoveEffect EffectOf(MoveData move) =>
        move.Effect != null && MoveEffects.TryGetValue(move.Effect, out var effect) ? effect : Plain;

    /// <summary>Whether the engine runs a move effect of this name: what the data importer marks its moves by.</summary>
    public static bool HasMoveEffect(string name) => MoveEffects.ContainsKey(name);

    /// <summary>The names of every move effect the engine runs.</summary>
    public static IReadOnlyCollection<string> MoveEffectNames => MoveEffects.Keys;

    private static readonly Elsewhere[] IntoTheAir = { Elsewhere.InTheAir };

    private static readonly Dictionary<string, MoveEffect> MoveEffects = new()
    {
        // ---- protection
        ["Protect"] = new() { Does = (b, use) => b.Brace(use, endure: false) },
        ["SurviveWith1Hp"] = new() { Does = (b, use) => b.Brace(use, endure: true) },
        ["RemoveProtect"] = new()
        {
            Protectable = _ => false,
            Start = (b, use) => use.Targets.Any(t => t.Turn.Protecting) || b.Fails(),
            Hit = (b, use, hit) =>
            {
                if (!hit.Landed) return;
                hit.Target.Turn.Protecting = false;
                b.Say($"{hit.Target.Name} fell for the feint!");
            }
        },
        ["SetSubstitute"] = new() { Does = (b, use) => b.MakeSubstitute(use) },

        // ---- the weather
        ["WeatherRain"] = new() { Does = (b, use) => b.CallWeather(use, BattleWeather.Rain, "Rain") },
        ["WeatherSun"] = new() { Does = (b, use) => b.CallWeather(use, BattleWeather.Sun, "Sun") },
        ["WeatherSandstorm"] = new() { Does = (b, use) => b.CallWeather(use, BattleWeather.Sandstorm, "Sandstorm") },
        ["WeatherHail"] = new() { Does = (b, use) => b.CallWeather(use, BattleWeather.Hail, "Hail") },
        ["Thunder"] = new() { SureIn = BattleWeather.Rain, Follows = IntoTheAir, Accuracy = (weather, accuracy) => weather == BattleWeather.Sun ? 50 : accuracy },
        ["Blizzard"] = new() { SureIn = BattleWeather.Hail },
        ["SkipChargeTurnInSun"] = new() { Charge = new("{0} took in sunlight!", NotInTheSun: true) },
        ["HealHalfMoreInSun"] = new() { Does = (b, use) => b.HealByTheSky(use) },
        ["ChangeTypeWithWeather"] = new()
        {
            Start = (b, use) =>
            {
                var type = b.Field.WeatherInEffect switch
                {
                    BattleWeather.Rain => PokemonType.Water,
                    BattleWeather.Sun => PokemonType.Fire,
                    BattleWeather.Sandstorm => PokemonType.Rock,
                    BattleWeather.Hail => PokemonType.Ice,
                    _ => use.Move.Type
                };
                Retype(use, type);
                return true;
            },
            Power = (b, use, t) => b.Field.WeatherInEffect != BattleWeather.None ? 20 : 10
        },

        // ---- what a side puts up, and the rooms
        ["SetReflect"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.Reflect, (s, t) => s.ReflectTurns = t, "Screens", "Reflect raised {0}'s Defense!") },
        ["SetLightScreen"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.LightScreen, (s, t) => s.LightScreenTurns = t, "Screens", "Light Screen raised {0}'s Special Defense!") },
        ["PreventStatReduction"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.Mist, (s, t) => s.MistTurns = t, "", "{1} became shrouded in mist!") },
        ["PreventStatus"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.Safeguard, (s, t) => s.SafeguardTurns = t, "", "{1} became cloaked in a mystical veil!") },
        ["PreventCrits"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.LuckyChant, (s, t) => s.LuckyChantTurns = t, "", "The Lucky Chant shielded {0} from critical hits!") },
        ["DoubleSpeed3Turns"] = new() { Does = (b, use) => b.RaiseScreen(use, s => s.Tailwind, (s, t) => s.TailwindTurns = t, "Tailwind", "The tailwind blew from behind {0}!") },
        ["TrickRoom"] = new() { Does = (b, use) => b.TwistTheRoom(use) },
        ["Gravity"] = new() { Does = (b, use) => b.Intensify(use) },
        ["RemoveScreens"] = new()
        {
            PastScreens = true,
            Reach = (b, use, t) =>
            {
                var side = b.Field.Side(t.Side);
                if (!side.Reflect && !side.LightScreen) return;
                side.ReflectTurns = side.LightScreenTurns = 0;
                b.Say("It shattered the barrier!");
            }
        },
        ["RemoveHazardsScreensEvaDown"] = new() { Does = (b, use) => b.Defog(use) },
        ["RemoveHazardsAndBinding"] = new() { Then = (b, use) => b.SpinFree(use.User) },

        // ---- what lies in wait
        ["SetSpikes"] = new() { Does = (b, use) => b.Scatter(use, s => s.Spikes < 3, s => s.Spikes++, "Spikes were scattered all around {0}'s feet!") },
        ["ToxicSpikes"] = new() { Does = (b, use) => b.Scatter(use, s => s.ToxicSpikes < 2, s => s.ToxicSpikes++, "Poison spikes were scattered all around {0}'s feet!") },
        ["StealthRock"] = new() { Does = (b, use) => b.Scatter(use, s => !s.StealthRock, s => s.StealthRock = true, "Pointed stones float in the air around {0}!") },

        // ---- sent ahead
        ["HealIn3Turns"] = new() { Does = (b, use) => b.MakeAWish(use) },
        ["HitIn3Turns"] = new() { Does = (b, use) => b.SendAhead(use) },
        ["FaintAndFullHealNextMon"] = new() { Does = (b, use) => b.GiveItsAll(use, Leaving.MakesAWish) },
        ["FaintFullRestoreNextMon"] = new() { Does = (b, use) => b.GiveItsAll(use, Leaving.DancesAway) },
        ["HealHalfRemoveFlyingType"] = new() { Does = (b, use) => b.Roost(use) },

        // ---- two turns
        ["Fly"] = new() { Charge = new("{0} flew up high!", Elsewhere.InTheAir) },
        ["Bounce"] = new() { Charge = new("{0} sprang up!", Elsewhere.InTheAir) },
        ["Dig"] = new() { Charge = new("{0} burrowed its way under the ground!", Elsewhere.Underground) },
        ["Dive"] = new() { Charge = new("{0} hid underwater!", Elsewhere.Underwater) },
        ["ShadowForce"] = new()
        {
            Charge = new("{0} vanished instantly!", Elsewhere.Vanished),
            Protectable = _ => false,
            Hit = (b, use, hit) =>
            {
                if (!hit.Landed || !hit.Target.Turn.Protecting) return;
                hit.Target.Turn.Protecting = false;
                b.Say($"It broke through {hit.Target.Name}'s protection!");
            }
        },
        ["ChargeTurnHighCrit"] = new() { Charge = new("{0} whipped up a whirlwind!") },
        ["ChargeTurnHighCritFlinch"] = new() { Charge = new("{0} is glowing!") },
        ["ChargeTurnDefUp"] = new() { Charge = new("{0} lowered its head!", RaisesDefense: true) },
        ["DoubleDamageDig"] = new() { Follows = new[] { Elsewhere.Underground }, Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.Underground ? 20 : 10 },
        ["DoubleDamageDive"] = new() { Follows = new[] { Elsewhere.Underwater }, Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.Underwater ? 20 : 10 },
        ["DoubleDamageFlyOrBounce"] = new() { Follows = IntoTheAir, Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.InTheAir ? 20 : 10 },
        ["FlinchDoubleDamageFlyOrBounce"] = new() { Follows = IntoTheAir, Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.InTheAir ? 20 : 10 },
        ["HitFly"] = new() { Follows = IntoTheAir },
        ["CrashOnMiss"] = new() { Miss = (b, use, hit) => b.Crash(use, hit) },

        // ---- held to a move
        ["RechargeAfter"] = new()
        {
            Then = (b, use) =>
            {
                var v = use.User.Volatile;
                v.Recharging = true;
                v.RechargeTurn = b.Turn;
                v.LockedMove = use.Data;
            }
        },
        ["ContinueAndConfuseSelf"] = new()
        {
            Then = (b, use) =>
            {
                var v = use.User.Volatile;
                if (v.RampageTurns > 0) return;
                v.RampageTurns = 2 + b.rng.Roll(RollKind.Duration, 2);
                v.LockedMove = use.Data;
            }
        },
        ["Uproar"] = new()
        {
            Then = (b, use) =>
            {
                var v = use.User.Volatile;
                if (v.UproarTurns > 0) return;
                v.UproarTurns = b.Lasting(b.Rules.UproarTurns);
                v.LockedMove = use.Data;
                b.Say($"{use.User.Name} caused an uproar!");
            }
        },
        ["Bide"] = new() { OwnLinesAfterFirst = true, Does = (b, use) => b.Bide(use) },
        ["RaiseAtkWhenHit"] = new() { Then = (b, use) => use.User.Volatile.Rage = true },

        // ---- leaving
        ["ForceSwitch"] = new() { Does = (b, use) => b.BlowAway(use) },
        ["HitBeforeSwitch"] = new(),
        ["SwitchHit"] = new() { Then = (b, use) => use.Leaves = Leaving.Returns },
        ["PassStatsAndStatus"] = new()
        {
            Does = (b, use) =>
            {
                if (!b.HasReplacement(use.User)) b.Fails();
                else use.Leaves = Leaving.PassesTheBaton;
            }
        },
        ["FleeFromWildBattle"] = new()
        {
            Does = (b, use) =>
            {
                if (b.IsTrainerBattle || b.IsDouble || b.TrapOn(use.User, forRunning: true) != null)
                {
                    b.Fails();
                    return;
                }
                b.Say($"{use.User.Name} fled from battle!");
                b.End(BattleResult.PlayerRan);
            }
        },

        // ---- conditions given to another
        ["Infatuate"] = new() { Does = (b, use) => b.OnEach(use, t => b.Infatuate(use.User, t)) },
        ["StatusLeechSeed"] = new() { Does = (b, use) => b.OnEach(use, t => b.Seed(use.User, t)) },
        ["StatusNightmare"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.HasSubstitute || t.Volatile.Nightmare || t.Pokemon!.Status != StatusCondition.Sleep) return b.Fails();
                t.Volatile.Nightmare = true;
                b.Say($"{t.Name} began having a nightmare!");
                return true;
            })
        },
        ["Curse"] = new()
        {
            Aim = (user, target) => user.HasType(PokemonType.Ghost) ? MoveTarget.RandomFoe : MoveTarget.User,
            Protectable = user => user.HasType(PokemonType.Ghost),
            Does = (b, use) => b.Curse(use)
        },
        ["AllFaint3Turns"] = new() { Does = (b, use) => b.PerishSong(use) },
        ["Taunt"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.Volatile.TauntTurns > 0) return b.Fails();
                t.Volatile.TauntTurns = b.Lasting(b.Rules.TauntTurns);
                b.Say($"{t.Name} fell for the taunt!");
                return true;
            })
        },
        ["Torment"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.Volatile.Tormented) return b.Fails();
                t.Volatile.Tormented = true;
                b.Say($"{t.Name} was subjected to torment!");
                return true;
            })
        },
        ["Encore"] = new() { Does = (b, use) => b.OnEach(use, t => b.Encore(t)) },
        ["Disable"] = new() { Does = (b, use) => b.OnEach(use, t => b.Disable(t)) },
        ["StatusSleepNextTurn"] = new() { Does = (b, use) => b.OnEach(use, t => b.Yawn(use.User, t)) },
        ["PreventHealing"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.HasSubstitute || t.Volatile.HealBlockTurns > 0)
                {
                    b.Say($"It failed to affect {t.Name}!");
                    return false;
                }
                t.Volatile.HealBlockTurns = 5;
                b.Say($"{t.Name} was prevented from healing!");
                return true;
            })
        },
        ["PreventItemUse"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.HasSubstitute || t.Volatile.EmbargoTurns > 0) return b.Fails();
                t.Volatile.EmbargoTurns = 5;
                b.Say($"{t.Name} can't use items anymore!");
                return true;
            })
        },
        ["BindHit"] = new() { Hit = (b, use, hit) => b.Bind(use, hit) },
        ["Whirlpool"] = new()
        {
            Follows = new[] { Elsewhere.Underwater },
            Power = (b, use, t) => t.Volatile.Elsewhere == Elsewhere.Underwater ? 20 : 10,
            Hit = (b, use, hit) => b.Bind(use, hit)
        },
        ["PreventEscape"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.Volatile.TrappedBy != null || t.HasSubstitute) return b.Fails();
                t.Volatile.TrappedBy = use.User.Place;
                b.Say($"{t.Name} can no longer escape!");
                return true;
            })
        },
        ["NextAttackAlwaysHits"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.HasSubstitute) return b.Fails();
                t.Volatile.LockedOnBy = use.User.Place;
                t.Volatile.LockOnTurns = 2;
                b.Say($"{use.User.Name} took aim at {t.Name}!");
                return true;
            })
        },
        ["Foresight"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                t.Volatile.Identified = true;
                b.Say($"{use.User.Name} identified {t.Name}!");
                return true;
            })
        },
        ["IgnoreEvationRemoveDarkImmune"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                t.Volatile.MiracleEye = true;
                b.Say($"{use.User.Name} identified {t.Name}!");
                return true;
            })
        },
        ["SupressAbility"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                if (t.HasSubstitute || t.Volatile.AbilitySuppressed || t.Pokemon!.AbilityName == "Multitype") return b.Fails();
                t.Volatile.AbilitySuppressed = true;
                b.Say($"{t.Name}'s ability was suppressed!");
                return true;
            })
        },
        ["TransferStatus"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                var mine = use.User.Pokemon!;
                if (mine.Status == StatusCondition.None || t.Pokemon!.Status != StatusCondition.None) return b.Fails();
                if (!b.TryInflictStatus(t, mine.Status, use.User, true, By.Move)) return false;
                b.CureStatus(use.User, $"{use.User.Name} moved its condition onto {t.Name}!");
                return true;
            })
        },

        // ---- conditions of its own
        ["CritUp2"] = new() { Does = (b, use) => b.Once(use.User.Volatile.FocusEnergy, () => use.User.Volatile.FocusEnergy = true, $"{use.User.Name} is getting pumped!") },
        ["GroundTrapUserContinuousHeal"] = new() { Does = (b, use) => b.Once(use.User.Volatile.Ingrained, () => use.User.Volatile.Ingrained = true, $"{use.User.Name} planted its roots!") },
        ["RestoreHpEveryTurn"] = new() { Does = (b, use) => b.Once(use.User.Volatile.AquaRing, () => use.User.Volatile.AquaRing = true, $"{use.User.Name} surrounded itself with a veil of water!") },
        ["GiveGroundImmunity"] = new()
        {
            Does = (b, use) => b.Once(use.User.Volatile.MagnetRiseTurns > 0 || use.User.Volatile.Ingrained || BattleEffects.Of(use.User).Any(e => e.Levitates),
                () => use.User.Volatile.MagnetRiseTurns = 5, $"{use.User.Name} levitated on electromagnetism!")
        },
        ["KoMonThatDefeatedUser"] = new() { Does = (b, use) => b.Once(false, () => use.User.Volatile.DestinyBond = true, $"{use.User.Name} is trying to take its foe down with it!") },
        ["RemoveAllPpOnDefeat"] = new() { Does = (b, use) => b.Once(use.User.Volatile.Grudge, () => use.User.Volatile.Grudge = true, $"{use.User.Name} wants its foe to bear a grudge!") },
        ["MakeSharedMovesUnuseable"] = new()
        {
            Does = (b, use) => b.Once(use.User.Volatile.Imprisoning || !b.ActiveFoes(use.User).Any(foe => foe.Pokemon!.Moves.Any(m => use.User.Pokemon!.Moves.Any(own => own.Data == m.Data))),
                () => use.User.Volatile.Imprisoning = true, $"{use.User.Name} sealed the moves it shares with its foe!")
        },
        ["HalveElectricDamage"] = new() { Does = (b, use) => b.Once(use.User.Volatile.MudSport, () => use.User.Volatile.MudSport = true, "Electricity's power was weakened!") },
        ["HalveFireDamage"] = new() { Does = (b, use) => b.Once(use.User.Volatile.WaterSport, () => use.User.Volatile.WaterSport = true, "Fire's power was weakened!") },
        ["EvaUp2Minimize"] = new()
        {
            Does = (b, use) =>
            {
                use.User.Volatile.Minimized = true;
                b.ChangeStat(use.User, StatType.Evasion, b.Rules.MinimizeStages, use.User, true, By.Move);
            }
        },
        ["FlinchMinimizeDoubleHit"] = new() { Power = (b, use, t) => t.Volatile.Minimized ? 20 : 10 },
        ["DefUpDoubleRolloutPower"] = new()
        {
            Does = (b, use) =>
            {
                use.User.Volatile.DefenseCurl = true;
                b.ByItsFields(use);
            }
        },
        ["SpDefUpDoubleElectricPower"] = new()
        {
            Does = (b, use) =>
            {
                use.User.Volatile.ChargeTurns = 2;
                b.Say($"{use.User.Name} began charging power!");
                b.ByItsFields(use);
            }
        },

        // ---- sleep, and what cures
        ["Rest"] = new() { Does = (b, use) => b.Rest(use) },
        ["HealStatus"] = new()
        {
            Does = (b, use) =>
            {
                if (use.User.Pokemon!.Status is StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn or StatusCondition.Paralyze)
                    b.CureStatus(use.User, $"{use.User.Name}'s status returned to normal!");
                else b.Fails();
            }
        },
        ["CurePartyStatus"] = new() { Does = (b, use) => b.HealBell(use) },
        ["ResetStatChanges"] = new()
        {
            Does = (b, use) =>
            {
                foreach (var each in b.AllBattlers.Where(o => o.IsActive)) each.Pokemon!.ResetStatStages();
                b.Say("All stat changes were eliminated!");
            }
        },
        ["DoublePowerAndCureParalysis"] = new()
        {
            Power = (b, use, t) => t.Pokemon!.Status == StatusCondition.Paralyze && !t.HasSubstitute ? 20 : 10,
            Hit = (b, use, hit) =>
            {
                if (hit.Touched && hit.Target.IsActive && hit.Target.Pokemon!.Status == StatusCondition.Paralyze)
                    b.CureStatus(hit.Target, $"{hit.Target.Name} was healed of paralysis!");
            }
        },
        ["DoublePowerHealSleep"] = new()
        {
            Power = (b, use, t) => t.Pokemon!.Status == StatusCondition.Sleep && !t.HasSubstitute ? 20 : 10,
            Hit = (b, use, hit) =>
            {
                if (hit.Touched && hit.Target.IsActive && hit.Target.Pokemon!.Status == StatusCondition.Sleep)
                    b.WakeUp(hit.Target, "{0} woke up!");
            }
        },
        ["DamageWhileAsleep"] = new()
        {
            Start = (b, use) => use.User.Pokemon!.Status == StatusCondition.Sleep || b.Fails(),
            Hit = (b, use, hit) =>
            {
                var t = hit.Target;
                int mult = BattleEffects.Of(use.User).Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();
                if (hit.Touched && t.IsActive && !t.MovedThisTurn && b.Chance(use.Data.EffectChance * mult) &&
                    !BattleEffects.Of(t).Any(e => e.BlocksFlinch || e.BlocksSideEffects))
                    t.Flinched = true;
            }
        },
        ["RecoverDamageSleep"] = new()
        {
            Start = (b, use) =>
            {
                var t = use.Targets[0];
                if (t.Pokemon!.Status == StatusCondition.Sleep && !t.HasSubstitute) return true;
                b.Say($"{t.Name} wasn't affected!");
                return false;
            },
            Then = (b, use) =>
            {
                var user = use.User;
                if (use.DamageDealt == 0) return;
                if (user.Volatile.HealBlockTurns > 0)
                {
                    b.Say($"{user.Name} was kept from healing!");
                    return;
                }
                int amount = Formulas.Divide(use.DamageDealt, 2) * BattleEffects.Of(user).Select(e => e.DrainHundredths).DefaultIfEmpty(100).Max() / 100;
                b.RestoreHp(user, amount, $"{use.Hits[0].Target.Name}'s dream was eaten!");
            }
        },
        ["DoublePowerWhenStatused"] = new()
        {
            Power = (b, use, t) => use.User.Pokemon!.Status is StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn or StatusCondition.Paralyze ? 20 : 10
        },
        ["DoNothing"] = new() { Does = (b, use) => b.Say("But nothing happened!") }
    };

    // ---------------------------------------------------------------- shared by several effects

    /// <summary>"But it failed!" Returns false, so an effect can end on it.</summary>
    private bool Fails()
    {
        Say("But it failed!");
        return false;
    }

    /// <summary>Something a Pokémon does for itself once: it fails while it already holds.</summary>
    private void Once(bool already, Action set, string line)
    {
        if (already)
        {
            Fails();
            return;
        }
        set();
        Say(line);
    }

    /// <summary>A status move's effect on each target it gets to (Protect, accuracy and a target out of reach have their lines).</summary>
    private void OnEach(MoveUse use, Func<Battler, bool> effect)
    {
        bool breaks = BattleEffects.Of(use.User).Any(e => e.IgnoresTargetAbility);
        foreach (var t in use.Targets) use.Line.With(Shown(use.User, t, use.Move, null));
        foreach (var t in use.Targets.Where(t => t.IsActive))
        {
            if (Result != BattleResult.None) return;
            if (t != use.User)
            {
                if (!Arrives(use, t)) continue;
                // Soundproof against Roar, and the like
                if (BattleEffects.Of(t, includeAbility: !breaks).ToList().Any(e => e.AbsorbsMove(this, t, use.User, use.Move, 1f))) continue;
            }
            if (effect(t)) RememberHit(t, use.User, use.Move);
        }
    }

    /// <summary>A length drawn evenly from the rules' range for it.</summary>
    private int Lasting((int Min, int Max) range) =>
        range.Min + (range.Max > range.Min ? rng.Roll(RollKind.Duration, range.Max - range.Min + 1) : 0);

    // ---------------------------------------------------------------- protection and Substitute

    /// <summary>
    /// <c>BtlCmd_TryProtection</c>: Protect, Detect and Endure are less likely each time they work in a row, and
    /// never work for the last Pokémon to act in a turn.
    /// </summary>
    private void Brace(MoveUse use, bool endure)
    {
        var user = use.User;
        var v = user.Volatile;
        use.Line.With(Shown(user, user, use.Move, null));
        if (use.MoveBefore?.Effect is not ("Protect" or "SurviveWith1Hp")) v.ProtectChain = 0;

        var rates = Rules.ProtectRates;
        bool rolled = rates[Math.Min(v.ProtectChain, rates.Count - 1)] >= rng.Roll(RollKind.Protect, 65536);
        bool othersToAct = waiting.Any(a => a.User.IsActive && a.User.Pokemon == a.Actor);
        if (!rolled || !othersToAct)
        {
            v.ProtectChain = 0;
            Fails();
            return;
        }

        if (endure) user.Turn.Enduring = true;
        else user.Turn.Protecting = true;
        if (v.ProtectChain < rates.Count - 1) v.ProtectChain++;
        Say(endure ? $"{user.Name} braced itself!" : $"{user.Name} protected itself!");
    }

    /// <summary><c>BtlCmd_TrySubstitute</c>: a quarter of its HP makes a Substitute of as much, and frees it from a binding move.</summary>
    private void MakeSubstitute(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        if (user.HasSubstitute)
        {
            Say($"{user.Name} already has a substitute!");
            return;
        }
        int cost = Formulas.Divide(p.MaxHP, 4);
        if (p.CurrentHP <= cost)
        {
            Say("It was too weak to make a substitute!");
            return;
        }
        p.CurrentHP -= cost;
        user.Volatile.SubstituteHp = cost;
        user.Volatile.BindTurns = 0;
        user.Volatile.BoundBy = null;
        Say($"{user.Name} made a substitute!").With(new HpChanged(user.Place, p.CurrentHP, Healed: false)).With(new SubstituteChanged(user.Place, Up: true));
    }

    // ---------------------------------------------------------------- the weather and the field

    private void CallWeather(MoveUse use, BattleWeather weather, string rock)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        int turns = 5 + BattleEffects.Of(use.User).Sum(e => e.ExtraTurns(rock));
        if (!SetWeather(weather, turns, Starts(weather))) Fails();
    }

    /// <summary><c>BtlCmd_WeatherHPRecovery</c>: half its HP under a clear sky, two thirds in the sun, a quarter under any other.</summary>
    private void HealByTheSky(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (p.CurrentHP >= p.MaxHP)
        {
            Say($"{user.Name}'s HP is full!");
            return;
        }
        int amount = Field.WeatherInEffect switch
        {
            BattleWeather.None => p.MaxHP / 2,
            BattleWeather.Sun => Formulas.Divide(p.MaxHP * 20, 30),
            _ => Formulas.Divide(p.MaxHP, 4)
        };
        RestoreHp(user, Math.Max(1, amount), $"{user.Name} regained health!");
    }

    /// <summary>Reflect, Light Screen, Mist, Safeguard, Lucky Chant and Tailwind: each fails while it is up. In the line {0} is "your team", {1} "Your team".</summary>
    private void RaiseScreen(MoveUse use, Func<SideState, bool> isUp, Action<SideState, int> raise, string kind, string line)
    {
        var user = use.User;
        var side = Field.Side(user.Side);
        use.Line.With(Shown(user, user, use.Move, null));
        if (isUp(side))
        {
            Fails();
            return;
        }
        int turns = kind == "Tailwind" ? Rules.TailwindTurns : 5 + (kind.Length > 0 ? BattleEffects.Of(user).Sum(e => e.ExtraTurns(kind)) : 0);
        raise(side, turns);
        Say(string.Format(line, TeamOf(user.Side), TeamOf(user.Side, capital: true)));
    }

    /// <summary>Trick Room for five turns; used again while it is up, it takes it down. Either way the rest of the turn is put in order again.</summary>
    private void TwistTheRoom(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        if (Field.TrickRoom)
        {
            Field.TrickRoomTurns = 0;
            Say($"{use.User.Name} restored the twisted dimensions!");
        }
        else
        {
            Field.TrickRoomTurns = 5;
            Say($"{use.User.Name} twisted the dimensions!");
        }
        orderChanged = true;
    }

    /// <summary><c>subscript_gravity_start</c>: five turns; whoever was off the ground comes down, by whatever it was up.</summary>
    private void Intensify(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        if (Field.Gravity)
        {
            Fails();
            return;
        }
        Field.GravityTurns = 5;
        Say("Gravity intensified!");
        foreach (var b in BySpeed().Where(b => b.IsActive))
        {
            var v = b.Volatile;
            bool floated = v.MagnetRiseTurns > 0 || v.Elsewhere == Elsewhere.InTheAir ||
                (!v.Ingrained && (b.HasType(PokemonType.Flying) || BattleEffects.Of(b).Any(e => e.Levitates)));
            v.MagnetRiseTurns = 0;
            if (v.Elsewhere == Elsewhere.InTheAir) Unlock(b);
            if (floated) Say($"{b.Name} couldn't stay airborne because of gravity!");
        }
    }

    /// <summary>Spikes, Toxic Spikes and Stealth Rock on the other side of the field; each has its limit.</summary>
    private void Scatter(MoveUse use, Func<SideState, bool> room, Action<SideState> add, string line)
    {
        var theirs = Other(use.User.Side);
        var side = Field.Side(theirs);
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        if (!room(side))
        {
            Fails();
            return;
        }
        add(side);
        Say(string.Format(line, TeamOf(theirs)));
    }

    /// <summary><c>subscript_defog</c>: the target's evasion falls, and its side loses its screens, its veils and what lies at its feet; the fog lifts.</summary>
    private void Defog(MoveUse use)
    {
        OnEach(use, t =>
        {
            ChangeStat(t, StatType.Evasion, -1, use.User, true, By.Move);
            var side = Field.Side(t.Side);
            string team = TeamOf(t.Side, capital: true);
            if (side.Reflect) Say($"{team}'s Reflect was blown away!");
            if (side.LightScreen) Say($"{team}'s Light Screen was blown away!");
            if (side.Mist) Say($"{team}'s Mist was blown away!");
            if (side.Safeguard) Say($"{team}'s Safeguard was blown away!");
            if (side.Spikes > 0) Say($"The spikes around {TeamOf(t.Side)} were blown away!");
            if (side.ToxicSpikes > 0) Say($"The poison spikes around {TeamOf(t.Side)} were blown away!");
            if (side.StealthRock) Say($"The pointed stones around {TeamOf(t.Side)} were blown away!");
            side.ReflectTurns = side.LightScreenTurns = side.MistTurns = side.SafeguardTurns = 0;
            side.Spikes = side.ToxicSpikes = 0;
            side.StealthRock = false;
            return true;
        });
        if (Field.Weather == BattleWeather.Fog)
        {
            Field.Weather = BattleWeather.None;
            Field.WeatherLasts = false;
            Say($"{use.User.Name} blew away the deep fog!").With(new WeatherChanged(BattleWeather.None));
        }
    }

    /// <summary><c>BtlCmd_RapidSpin</c>: the user is free of what binds it and of Leech Seed, and its side of what lies at its feet.</summary>
    private void SpinFree(Battler user)
    {
        var v = user.Volatile;
        var side = Field.Side(user.Side);
        if (v.BindTurns > 0)
        {
            Say($"{user.Name} got free of {v.BindingMove}!");
            v.BindTurns = 0;
            v.BoundBy = null;
        }
        if (v.SeededBy != null)
        {
            v.SeededBy = null;
            Say($"{user.Name} blew away Leech Seed!");
        }
        if (side.Spikes > 0)
        {
            side.Spikes = 0;
            Say($"{user.Name} blew away Spikes!");
        }
        if (side.ToxicSpikes > 0)
        {
            side.ToxicSpikes = 0;
            Say($"{user.Name} blew away Toxic Spikes!");
        }
        if (side.StealthRock)
        {
            side.StealthRock = false;
            Say($"{user.Name} blew away Stealth Rock!");
        }
    }

    // ---------------------------------------------------------------- sent ahead

    private void MakeAWish(MoveUse use)
    {
        var place = Field.At(use.User.Place);
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        if (place.WishTurns > 0)
        {
            Fails();
            return;
        }
        place.WishTurns = 2;
        place.WishFrom = use.User.Name;
        Say($"{use.User.Name} made a wish!");
    }

    /// <summary>
    /// <c>BtlCmd_TryFutureSight</c>: the damage is worked out now, with no type to it and no chance of a critical
    /// hit, and lands at the end of the turn after next on whoever stands there.
    /// </summary>
    private void SendAhead(MoveUse use)
    {
        var user = use.User;
        var t = use.Targets[0];
        var place = Field.At(t.Place);
        use.Line.With(Shown(user, user, use.Move, null));
        if (place.DoomTurns > 0)
        {
            Fails();
            return;
        }
        place.DoomTurns = 3;
        place.DoomMove = use.Data;
        place.DoomFrom = user.Place;
        place.DoomDamage = DamageCalculator.Calculate(user, t, use.Move, rng, spread: false, rules: Rules, noCrit: true, typeless: true).Damage;
        Say(use.Data.Name == "Doom Desire" ? $"{user.Name} chose Doom Desire as its destiny!" : $"{user.Name} foresaw an attack!");
    }

    /// <summary>Healing Wish and Lunar Dance: the user faints so that whoever takes its place comes in whole. They need someone to come in.</summary>
    private void GiveItsAll(MoveUse use, Leaving how)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (!HasReplacement(user))
        {
            Fails();
            return;
        }
        user.Pokemon!.CurrentHP = 0;
        Emit(new HpChanged(user.Place, 0, Healed: false));
        ResolveFaints();
        use.Leaves = how;
    }

    private void Roost(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (p.CurrentHP >= p.MaxHP)
        {
            Say($"{user.Name}'s HP is full!");
            return;
        }
        RestoreHp(user, Formulas.Divide(p.MaxHP, 2), $"{user.Name} regained health!");
        user.Turn.Roosting = true;
    }

    // ---------------------------------------------------------------- crashing, biding

    /// <summary>
    /// <c>subscript_crash_on_miss</c>: a kick that doesn't land (a miss, a Protect) hurts its user by half of what
    /// it would have done to that target, and by no more than half the target's HP. Against a target its type
    /// can't hurt that damage is nothing, and so is the fall. Magic Guard lands on its feet.
    /// </summary>
    private void Crash(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        if (!user.IsActive || BattleEffects.Of(user).Any(e => e.PreventsIndirectDamage)) return;
        if (hit.Immune)
        {
            Say($"{user.Name} kept going and crashed!");
            return;
        }
        int would = DamageCalculator.Calculate(user, hit.Target, use.Move, rng, spread: false, rules: Rules,
            powerTenths: use.Effect.PowerTenths(this, use, hit.Target)).Damage;
        int crash = Math.Min(Formulas.Divide(would, 2), Formulas.Divide(hit.Target.Pokemon!.MaxHP, 2));
        LoseHp(user, crash, $"{user.Name} kept going and crashed!", direct: true);
    }

    /// <summary>
    /// Bide: two turns of taking it, then twice what was taken, given back to whoever hit it last. It has no type
    /// and can't be a critical hit.
    /// </summary>
    private void Bide(MoveUse use)
    {
        var user = use.User;
        var v = user.Volatile;
        if (!v.Charging)
        {
            v.Charging = true;
            v.LockedMove = use.Data;
            v.BideTurns = 2;
            v.BideDamage = 0;
            use.Line.With(Shown(user, user, use.Move, null));
            return;
        }

        if (--v.BideTurns > 0)
        {
            Say($"{user.Name} is storing energy!");
            return;
        }

        int damage = v.BideDamage * 2;
        var last = v.LastHitBy is { } from && At(from).IsActive && At(from).Side != user.Side ? At(from) : null;
        var target = last ?? ActiveFoes(user).OrderBy(Number).FirstOrDefault();
        v.Charging = false;
        v.BideDamage = 0;
        if (!user.IsHeldToItsMove) v.LockedMove = null;
        Say($"{user.Name} unleashed energy!");
        if (damage == 0 || target == null)
        {
            Fails();
            return;
        }

        var hit = new MoveHit { Target = target };
        if (!Reaches(use, target, hit))
        {
            Say(hit.Protected ? $"{target.Name} protected itself!" : $"{user.Name}'s attack missed!");
            return;
        }
        var struck = Say($"{target.Name} took the energy!");
        if (target.HasSubstitute)
        {
            target.Volatile.SubstituteHp = Math.Max(0, target.Volatile.SubstituteHp - damage);
            Say($"The substitute took damage for {target.Name}!");
            if (!target.HasSubstitute) Say($"{target.Name}'s substitute faded!").With(new SubstituteChanged(target.Place, Up: false));
        }
        else
        {
            var p = target.Pokemon!;
            int dealt = Math.Min(p.CurrentHP, damage);
            if (dealt >= p.CurrentHP && target.Turn.Enduring) dealt = p.CurrentHP - 1;
            p.CurrentHP -= dealt;
            target.Volatile.LastHitBy = user.Place;
            struck.AtImpact(new Struck(target.Place, p.CurrentHP, Hard: false)).AtImpact(new HitSounded(false));
            CheckConditionHooks(target, user);
        }
        ResolveFaints();
    }

    // ---------------------------------------------------------------- made to leave

    /// <summary>
    /// Roar and Whirlwind (<c>subscript_force_target_to_switch_or_flee</c>, <c>BattleSystem_CanWhirlwind</c>): a
    /// trainer's Pokémon is dragged out for another of the party, picked by chance; a wild battle ends. Against a
    /// foe of a higher level it only sometimes works.
    /// </summary>
    private void BlowAway(MoveUse use)
    {
        var user = use.User;
        OnEach(use, t =>
        {
            bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
            if (!breaks && t.Ability?.Effect is { HoldsItsGround: true })
            {
                Say($"{t.Name} anchors itself with {t.Ability.Name}!");
                return false;
            }
            if (t.Volatile.Ingrained)
            {
                Say($"{t.Name} anchored itself with its roots!");
                return false;
            }
            if (!IsTrainerBattle && IsDouble) return Fails();

            int mine = user.Pokemon!.Level, theirs = t.Pokemon!.Level;
            bool moves = mine >= theirs || (rng.Roll(RollKind.Whirlwind, 256) * (mine + theirs) >> 8) + 1 > theirs / 4;
            if (!moves || (IsTrainerBattle && !HasReplacement(t))) return Fails();

            if (IsTrainerBattle)
            {
                DragOut(t);
                return true;
            }
            // Against or from a wild Pokémon it ends the meeting
            Say($"{t.Name} was driven off!");
            End(BattleResult.PlayerRan);
            return true;
        });
    }

    // ---------------------------------------------------------------- conditions given to another

    /// <summary><c>BtlCmd_TryAttract</c>: of the other gender, not Oblivious, and not in love already.</summary>
    private bool Infatuate(Battler user, Battler t)
    {
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (!breaks && t.Ability?.Effect is { BlocksInfatuation: true })
        {
            Say($"{t.Name}'s {t.Ability.Name} prevents romance!");
            return false;
        }
        var mine = user.Pokemon!.Gender;
        var theirs = t.Pokemon!.Gender;
        if (mine == theirs || mine == Gender.Genderless || theirs == Gender.Genderless || t.Volatile.InLoveWith != null) return Fails();
        t.Volatile.InLoveWith = user.Place;
        Say($"{t.Name} fell in love!");
        return true;
    }

    /// <summary><c>subscript_leech_seed_start</c>: not through a Substitute, not on a Grass type, not twice.</summary>
    private bool Seed(Battler user, Battler t)
    {
        if (t.HasSubstitute) return Fails();
        if (t.HasType(PokemonType.Grass))
        {
            Say($"It doesn't affect {t.Name}...");
            return false;
        }
        if (t.Volatile.SeededBy != null)
        {
            Say($"{t.Name} evaded the attack!");
            return false;
        }
        t.Volatile.SeededBy = user.Place;
        Say($"{t.Name} was seeded!");
        return true;
    }

    /// <summary>
    /// Curse: from anyone but a Ghost it slows the user and raises its Attack and Defense; from a Ghost it costs
    /// half the user's HP and lays a curse on a foe.
    /// </summary>
    private void Curse(MoveUse use)
    {
        var user = use.User;
        if (!user.HasType(PokemonType.Ghost))
        {
            use.Line.With(Shown(user, user, use.Move, null));
            ChangeStat(user, StatType.Speed, -1, user, true, By.Move);
            ChangeStat(user, StatType.Attack, 1, user, true, By.Move);
            ChangeStat(user, StatType.Defense, 1, user, true, By.Move);
            return;
        }
        OnEach(use, t =>
        {
            if (t.HasSubstitute || t.Volatile.Cursed) return Fails();
            t.Volatile.Cursed = true;
            LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, 2), $"{user.Name} cut its own HP and laid a curse on {t.Name}!", direct: true);
            return true;
        });
    }

    /// <summary><c>BtlCmd_TryPerishSong</c>: everyone on the field who hears it gets a count of three.</summary>
    private void PerishSong(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        var deaf = new List<Battler>();
        bool any = false;
        foreach (var b in BySpeed().Where(b => b.IsActive))
        {
            if (b.Volatile.PerishCount >= 0) continue;
            if (Has(b, "Soundproof") && !(breaks && b != user))
            {
                deaf.Add(b);
                continue;
            }
            b.Volatile.PerishCount = 3;
            any = true;
        }
        if (!any)
        {
            Fails();
            return;
        }
        Say("All Pokémon hearing the song will faint in three turns!");
        foreach (var b in deaf) Say($"{b.Name}'s Soundproof blocks {use.Move.Name}!");
    }

    private static readonly HashSet<string> NotForEncore = new() { "Transform", "CopyMoveForBattle", "LearnMovePermanent", "CopyMove", "Encore" };

    /// <summary><c>BtlCmd_TryEncore</c>: the last move it used, if it still knows it and has PP for it.</summary>
    private bool Encore(Battler t)
    {
        var v = t.Volatile;
        var last = v.LastMove;
        var known = last == null ? null : t.Pokemon!.Moves.FirstOrDefault(m => m.Data == last);
        if (v.Encored != null || known == null || known.CurrentPP == 0 || (last!.Effect != null && NotForEncore.Contains(last.Effect))) return Fails();
        v.Encored = last;
        v.EncoreTurns = Lasting(Rules.EncoreTurns);
        Say($"{t.Name} received an encore!");
        return true;
    }

    /// <summary><c>BtlCmd_TryDisable</c>: the last move it used, if it still knows it and has PP for it.</summary>
    private bool Disable(Battler t)
    {
        var v = t.Volatile;
        var last = v.LastMove;
        var known = last == null ? null : t.Pokemon!.Moves.FirstOrDefault(m => m.Data == last);
        if (v.Disabled != null || known == null || known.CurrentPP == 0) return Fails();
        v.Disabled = last;
        v.DisableTurns = Lasting(Rules.DisableTurns);
        Say($"{t.Name}'s {last!.Name} was disabled!");
        return true;
    }

    /// <summary><c>subscript_yawn</c>: whatever would keep it from sleeping keeps it from growing drowsy.</summary>
    private bool Yawn(Battler user, Battler t)
    {
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (!breaks && t.Ability?.Effect is { } guard && guard.BlocksStatus(t, StatusCondition.Sleep))
        {
            Say($"{t.Name}'s {t.Ability.Name} made it ineffective!");
            return false;
        }
        if (t.HasSubstitute || t.Volatile.YawnTurns > 0 || t.Pokemon!.Status != StatusCondition.None || (UproarIsOn && !Has(t, "Soundproof"))) return Fails();
        if (Field.Side(t.Side).Safeguard)
        {
            Say($"{t.Name} is protected by Safeguard!");
            return false;
        }
        t.Volatile.YawnTurns = 2;
        Say($"{user.Name} made {t.Name} drowsy!");
        return true;
    }

    /// <summary><c>subscript_bind_start</c>: a hit that reached the Pokémon itself holds it for a number of turns.</summary>
    private void Bind(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || t.Volatile.BindTurns > 0) return;
        t.Volatile.BindTurns = BattleEffects.Of(use.User).Any(e => e.BindsToTheEnd) ? Rules.GripClawBindTurns : Lasting(Rules.BindTurns);
        t.Volatile.BoundBy = use.User.Place;
        t.Volatile.BindingMove = use.Data.Name;
        Say($"{t.Name} was caught in {use.User.Name}'s {use.Data.Name}!");
    }

    // ---------------------------------------------------------------- sleep, and what cures

    /// <summary><c>subscript_rest</c>: full HP and no other condition, for two turns asleep.</summary>
    private void Rest(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        // Insomnia and Vital Spirit; Leaf Guard doesn't stop a Rest in Platinum
        if (Has(user, "Insomnia") || Has(user, "Vital Spirit"))
        {
            Say($"{user.Name} stayed awake because of its {user.Ability!.Name}!");
            return;
        }
        if (p.Status == StatusCondition.Sleep)
        {
            Say($"{user.Name} is already asleep!");
            return;
        }
        if (UproarIsOn && !Has(user, "Soundproof"))
        {
            Say($"But the uproar kept {user.Name} awake!");
            return;
        }
        if (p.CurrentHP >= p.MaxHP)
        {
            Say($"{user.Name}'s HP is full!");
            return;
        }
        bool ailing = p.Status != StatusCondition.None;
        p.Status = StatusCondition.Sleep;
        p.SleepTurns = 3;
        p.ToxicCounter = 0;
        p.CurrentHP = p.MaxHP;
        Say(ailing ? $"{user.Name} slept and became healthy!" : $"{user.Name} went to sleep!")
            .With(new StatusChanged(user.Place, StatusCondition.Sleep)).With(new HpChanged(user.Place, p.CurrentHP, Healed: true));
    }

    /// <summary><c>BtlCmd_TryPartyStatusRefresh</c>: the whole party is cured, but a Pokémon on the field that is Soundproof doesn't hear the bell.</summary>
    private void HealBell(MoveUse use)
    {
        var user = use.User;
        bool bell = use.Data.Flags.HasFlag(MoveFlags.Sound);
        use.Line.With(Shown(user, user, use.Move, null));
        Say(bell ? "A bell chimed!" : "A soothing aroma wafted through the area!");

        var onTheField = SlotsOf(user.Side).Where(b => b.IsActive).ToList();
        var party = user.Roster?.Members ?? onTheField.Select(b => b.Pokemon!).ToList();
        foreach (var p in party)
        {
            var standing = onTheField.FirstOrDefault(b => b.Pokemon == p);
            if (standing != null && bell && Has(standing, "Soundproof"))
            {
                Say($"{standing.Name}'s Soundproof blocks {use.Move.Name}!");
                continue;
            }
            if (p.IsFainted || p.Status == StatusCondition.None) continue;
            p.Status = StatusCondition.None;
            p.SleepTurns = 0;
            p.ToxicCounter = 0;
            if (standing == null) continue;
            standing.Volatile.Nightmare = false;
            Emit(new StatusChanged(standing.Place, StatusCondition.None));
        }
    }
}
