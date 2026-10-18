using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later generations' moves, batch 3, its second half (plan 06 · R26b): Generation 9's, Scarlet and Violet's and
// their two expansions'. The rules are Pokémon Showdown's (data/moves.ts at the commit in
// tools/DataImporter/Sources.cs); each entry names what it was read from. A move whose data fields say all it does
// (their flags and side effects are Showdown's, through the importer) has an empty entry, so the importer marks it
// run. Hydro Steam's sun and Glaive Rush's price are DamageCalculator's (DamageCalculator.Later5.cs), Flower Trick's
// critical hit DamageCalculator.AlwaysCritical's, the two shields' answer ShieldBites', Salt Cure's and Syrup Bomb's
// turn's end the turn's (BattleCore.Turn.cs), Gigaton Hammer's and Blood Moon's refusal WhyNotMove's. Tera Blast and
// Tera Starstorm are R23's (Terastallization). They join the table when the class is first used (the static
// constructor in BattleCore.Families.cs), after LaterEffects4.
public sealed partial class BattleCore
{
    /// <summary>The Pokémon of each side that have fainted in this battle, each faint counted (Showdown's <c>side.totalFainted</c>): Last Respects.</summary>
    private readonly Dictionary<BattleSide, int> fallen = new() { [BattleSide.Player] = 0, [BattleSide.Enemy] = 0 };

    /// <summary>How many hits each Pokémon has taken from moves in this battle, a Substitute's not counted, kept through a switch (<c>timesAttacked</c>): Rage Fist.</summary>
    private readonly Dictionary<Pokemon, int> timesAttacked = new();

    private static readonly Dictionary<string, MoveEffect> LaterEffects5 = new()
    {
        // ---- by their data alone (flags and side effects Showdown's, through the importer)
        ["JetPunch"] = new(),
        ["LuminaCrash"] = new(),
        ["SpinOut"] = new(),
        ["KowtowCleave"] = new(),
        ["FlowerTrick"] = new(), // every hit critical: DamageCalculator.AlwaysCritical
        ["TorchSong"] = new(),
        ["AquaStep"] = new(),
        ["MakeItRain"] = new(),
        ["Pounce"] = new(),
        ["Trailblaze"] = new(),
        ["ChillingWater"] = new(),
        ["HyperDrill"] = new(), // no protect flag: through Protect
        ["MightyCleave"] = new(),
        ["ArmorCannon"] = new(),
        ["BitterBlade"] = new(),
        ["AquaCutter"] = new(),
        ["BlazingTorque"] = new(),
        ["WickedTorque"] = new(),
        ["NoxiousTorque"] = new(),
        ["CombatTorque"] = new(),
        ["MagicalTorque"] = new(),
        ["MalignantChain"] = new(),
        ["MatchaGotcha"] = new(), // and it thaws its target: ThawsItsTarget
        ["GigatonHammer"] = new(), // not twice in a row: WhyNotMove
        ["BloodMoon"] = new(),
        // Order Up's boost comes from a Tatsugiri in its user's mouth (Commander, plan 06 · R27b): without one it is a
        // plain hit (onAfterMoveSecondarySelf returns at once)
        ["OrderUp"] = new() { Then = (b, use) => b.OrderUp(use) },

        // ---- several hits
        ["TwinBeam"] = new() { Strikes = (b, use) => 2 },
        ["TachyonCutter"] = new() { Strikes = (b, use) => 2 },
        ["TripleDive"] = new() { Strikes = (b, use) => 3 },
        // Ten hits, each rolling its accuracy (multihit: 10, multiaccuracy; Skill Link takes the rolls away)
        ["PopulationBomb"] = new() { Strikes = (b, use) => 10, EachHitRollsAccuracy = true },

        // ---- shields
        ["SilkTrap"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.SilkTrap) },
        ["BurningBulwark"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.BurningBulwark) },

        // ---- power and damage of their own
        ["LastRespects"] = new() { BasePower = (b, use, t) => 50 + 50 * b.fallen[use.User.Side] },
        ["RageFist"] = new() { BasePower = (b, use, t) => Math.Min(350, 50 + 50 * b.timesAttacked.GetValueOrDefault(use.User.Pokemon!)) },
        ["Ruination"] = new() { Deals = (b, use, t) => Formulas.Divide(t.Pokemon!.CurrentHP, 2) },
        ["Comeuppance"] = new() { Start = (b, use) => b.CounterBack(use, null), Deals = (b, use, t) => (int)use.Scratch! },
        ["HardPress"] = new()
        {
            // basePowerCallback: 100 × the target's share of its HP, in 4096ths, rounded as Showdown rounds it; at least 1
            BasePower = (b, use, t) =>
            {
                var q = t.Pokemon!;
                long share = (long)q.CurrentHP * 4096 / Math.Max(1, q.MaxHP);
                return Math.Max(1, (int)((100 * (100 * share) + 2048 - 1) / 4096 / 100));
            }
        },
        ["TemperFlare"] = new()
        {
            // Twice as strong when its user's move failed the turn before (moveLastTurnResult, as Stomping Tantrum)
            BasePower = (b, use, t) => b.failedOnTurn.TryGetValue(use.User.Pokemon!, out int turn) && turn == b.Turn - 1 ? use.Data.Power * 2 : use.Data.Power
        },
        ["Psyblade"] = new() { Power = (b, use, t) => b.Field.Has(FieldCondition.ElectricTerrain) ? 15 : 10 },
        // Half as strong again against a target it does well against (onBasePower: chainModify(5461/4096))
        ["CollisionCourse"] = new() { BasePower = (b, use, t) => b.SuperEffectiveBoost(use, t) },
        ["ElectroDrift"] = new() { BasePower = (b, use, t) => b.SuperEffectiveBoost(use, t) },
        ["FickleBeam"] = new()
        {
            // Three times in ten it goes all out, twice as strong (onBasePower: randomChance(3, 10))
            Start = (b, use) =>
            {
                bool allOut = b.rng.Roll(RollKind.Power, 10) < 3;
                use.Scratch = allOut;
                if (allOut) b.Say(Text.Of("battle.isGoingAllOut", ("user", use.User.Name)));
                return true;
            },
            Power = (b, use, t) => use.Scratch is true ? 20 : 10
        },
        // R26a's three left to R22: twice as strong against a Dynamaxed target (Showdown's gen8 mod, basePowerCallback;
        // its generation 9 table has no Dynamax and no such rule). One effect for the three, as the data names it
        ["DynamaxCannon"] = new() { Power = (b, use, t) => IsDynamaxed(t) ? 20 : 10 },
        ["HydroSteam"] = new(), // stronger in the sun: DamageCalculator.SteamsInTheSun; it thaws its target: ThawsItsTarget

        // ---- the type by its user
        ["RagingBull"] = new()
        {
            // Paldean Tauros's type, and the screens on the target's side broken before it hits (onModifyType, onTryHit)
            Start = (b, use) =>
            {
                switch (use.User.Pokemon!.Form)
                {
                    case "Tauros-Paldea-Combat": Retype(use, PokemonType.Fighting); break;
                    case "Tauros-Paldea-Blaze": Retype(use, PokemonType.Fire); break;
                    case "Tauros-Paldea-Aqua": Retype(use, PokemonType.Water); break;
                }
                return true;
            },
            PastScreens = true,
            Reach = (b, use, t) => MoveEffects["RemoveScreens"].OnReach(b, use, t)
        },
        ["IvyCudgel"] = new()
        {
            // Ogerpon's mask gives it its type (onModifyType), Terastallized or not
            Start = (b, use) =>
            {
                var form = use.User.Pokemon!.Form ?? "";
                if (form.StartsWith("Ogerpon-Wellspring")) Retype(use, PokemonType.Water);
                else if (form.StartsWith("Ogerpon-Hearthflame")) Retype(use, PokemonType.Fire);
                else if (form.StartsWith("Ogerpon-Cornerstone")) Retype(use, PokemonType.Rock);
                return true;
            }
        },

        // ---- when it may be used
        ["Thunderclap"] = new() { Start = (b, use) => b.SuckerPunch(use) }, // as Sucker Punch (onTry)
        ["UpperHand"] = new() { Start = (b, use) => b.UpperHand(use) },
        ["DoubleShock"] = new()
        {
            // Only from an Electric type, which it then isn't (onTryMove, self.onHit)
            Start = (b, use) => use.User.HasType(PokemonType.Electric) || b.Fails(),
            Then = (b, use) =>
            {
                use.User.Volatile.Types = use.User.Types.Where(t => t != PokemonType.Electric).ToList();
                b.Say(Text.Of("battle.usedUpAllItsElectricity", ("user", use.User.Name)));
            }
        },
        ["ElectroShot"] = new() { Charge = new("battle.absorbedElectricity", RaisesSpAttack: true, NotInTheRain: true) },

        // ---- a price or a crash
        ["AxeKick"] = new() { Miss = (b, use, hit) => b.CrashByHalf(use) },
        ["SupercellSlam"] = new() { Miss = (b, use, hit) => b.CrashByHalf(use) },
        ["GlaiveRush"] = new() { Then = (b, use) => use.User.Volatile.GlaiveRush = true },

        // ---- what clears the field
        ["IceSpinner"] = new()
        {
            // The terrain is gone once it has hit (onAfterHit, onAfterSubDamage)
            Then = (b, use) =>
            {
                if (b.Field.Terrain is { } terrain && b.Field.Has(terrain)) b.EndTerrain(terrain);
            }
        },
        ["MortalSpin"] = new() { Then = (b, use) => b.MortalSpin(use) },
        ["TidyUp"] = new() { Does = (b, use) => b.TidyUp(use) },

        // ---- what lasts on the target
        ["SaltCure"] = new() { Hit = (b, use, hit) => b.SaltCure(use, hit) },
        ["SyrupBomb"] = new() { Hit = (b, use, hit) => b.SyrupBomb(use, hit) },
        ["PsychicNoise"] = new() { Hit = (b, use, hit) => b.PsychicNoise(use, hit) },
        ["AlluringVoice"] = new() { Hit = (b, use, hit) => b.AlluringVoice(use, hit) },

        // ---- status moves
        ["SpicyExtract"] = new() { Does = (b, use) => b.OnEach(use, t => b.SpicyExtract(use, t)) },
        ["FilletAway"] = new() { Does = (b, use) => b.FilletAway(use) },
        ["Doodle"] = new() { Does = (b, use) => b.OnEach(use, t => b.Doodle(use, t)) },
        ["DragonCheer"] = new() { Does = (b, use) => b.DragonCheer(use) },
        ["Snowscape"] = new() { Does = (b, use) => b.CallWeather(use, BattleWeather.Snow, "Hail") },
        ["ChillyReception"] = new() { Does = (b, use) => b.ChillyReception(use) },
        ["ShedTail"] = new() { Does = (b, use) => b.ShedTail(use) },
        ["RevivalBlessing"] = new() { Does = (b, use) => b.RevivalBlessing(use) }
    };

    // ---------------------------------------------------------------- what the battle counts

    /// <summary>A Pokémon has fainted: one more for its side (Last Respects).</summary>
    private void CountTheFallen(Battler down) => fallen[down.Side]++;

    /// <summary>A move's hit has reached a Pokémon itself (not its Substitute): one more for Rage Fist.</summary>
    private void CountTheBlow(Battler t)
    {
        if (t.Pokemon is { } p) timesAttacked[p] = timesAttacked.GetValueOrDefault(p) + 1;
    }

    // ---------------------------------------------------------------- the rules other files ask

    /// <summary>Gigaton Hammer and Blood Moon can't be used twice in a row (their <c>cantusetwice</c> flag): the move its user used last.</summary>
    private static bool CantUseTwice(Battler b, MoveData data) => data.Effect is "GigatonHammer" or "BloodMoon" && b.Volatile.LastMove == data;

    /// <summary>Hydro Steam and Matcha Gotcha thaw a frozen target they hit, as a Fire move does (<c>thawsTarget</c>).</summary>
    private static bool ThawsItsTarget(MoveData data) => data.Effect is "HydroSteam" or "MatchaGotcha";

    /// <summary>
    /// Whether a move that rolls each hit's accuracy goes on rolling: by the later rules Skill Link takes the rolls
    /// away (Showdown's skilllink <c>onModifyMove</c> deletes <c>multiaccuracy</c>).
    /// </summary>
    private bool RollsEachHit(MoveUse use) => !(Rules.LaterMoveReach && Has(use.User, "Skill Link"));

    /// <summary>Electro Shot in the rain: its line and its stage of Sp. Atk, then the strike at once (onTryMove). True: no turn to wait.</summary>
    private bool ChargesInTheRain(MoveUse use, ChargeTurn charge)
    {
        Say(string.Format(Text.Of(charge.Line), use.User.Name));
        if (charge.RaisesSpAttack) ChangeStat(use.User, StatType.SpAttack, 1, use.User);
        return true;
    }

    /// <summary>Glaive Rush's price is paid until its user next tries to move (its condition's <c>onBeforeMove</c>, priority 100).</summary>
    private static void PaysForTheRush(Battler user) => user.Volatile.GlaiveRush = false;

    /// <summary>The power of Collision Course and Electro Drift against one target: 5461/4096 of it where the move does well.</summary>
    private int SuperEffectiveBoost(MoveUse use, Battler t)
    {
        int power = use.Data.Power;
        return DamageCalculator.Effectiveness(use.User, t, use.Move, Rules) > 1f ? Formulas.Modify(power, 5461) : power;
    }

    /// <summary>The two shields of Generation 9 answer whoever touched them: Silk Trap a stage of Speed, Burning Bulwark a burn.</summary>
    private void LaterShieldBites(MoveUse use, Battler t)
    {
        var user = use.User;
        if (t.Turn.Shield == Shield.SilkTrap) ChangeStat(user, StatType.Speed, -1, t, false, By.Other);
        else if (t.Turn.Shield == Shield.BurningBulwark) TryInflictStatus(user, StatusCondition.Burn, t, false, By.Other);
    }

    // ---------------------------------------------------------------- when it may be used

    /// <summary>
    /// Upper Hand: only against a target still to use a damaging move that goes first this turn (onTry: the
    /// queued move's priority above 0, not a status move).
    /// </summary>
    private bool UpperHand(MoveUse use)
    {
        var t = use.Targets.FirstOrDefault();
        var pending = t == null ? null : waiting.FirstOrDefault(a => a.User == t && a.Actor == t.Pokemon);
        if (pending == null || pending.Choice.Kind != ChoiceKind.Fight || pending.Move == null) return Fails();
        if (pending.Move.Category == MoveCategory.Status || PriorityOf(pending) <= 0) return Fails();
        return true;
    }

    /// <summary>Axe Kick and Supercell Slam: a kick that doesn't land costs its user half its HP (<c>onMoveFail</c>), Magic Guard aside.</summary>
    private void CrashByHalf(MoveUse use)
    {
        var user = use.User;
        if (!user.IsActive) return;
        LoseHp(user, Formulas.Divide(user.Pokemon!.MaxHP, 2), Text.Of("battle.keptGoingAndCrashed", ("user", user.Name)));
    }

    // ---------------------------------------------------------------- the field

    /// <summary>
    /// Mortal Spin: what Rapid Spin frees its user of (binding, Leech Seed, what lies on its side, G-Max Steelsurge
    /// too), unless Sheer Force took it (onAfterHit).
    /// </summary>
    private void MortalSpin(MoveUse use)
    {
        var user = use.User;
        if (Has(user, "Sheer Force")) return;
        SpinFree(user);
        if (Field.Side(user.Side).GMaxSteelsurge) ClearLaterSide(user.Side, Say(Text.Of("battle.blewAwaySteelsurge", ("user", user.Name))), veil: false, web: true);
    }

    /// <summary>
    /// Tidy Up: every Substitute on the field gone, and every hazard on both sides; then a stage of Attack and of
    /// Speed. It fails only when nothing was tidied and neither stat could rise (onHit).
    /// </summary>
    private void TidyUp(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        bool tidied = false;
        foreach (var b in BySpeed().Where(b => b.IsActive && b.HasSubstitute))
        {
            b.Volatile.SubstituteHp = 0;
            Say(Text.Of("battle.substituteFaded", ("target", b.Name))).With(new SubstituteChanged(b.Place, Up: false));
            tidied = true;
        }
        foreach (var side in new[] { user.Side, Other(user.Side) })
        {
            var state = Field.Side(side);
            if (state.Spikes == 0 && state.ToxicSpikes == 0 && !state.StealthRock && !state.StickyWeb && !state.GMaxSteelsurge) continue;
            var line = Say(string.Format(Text.Of("battle.theHazardsAroundWereCleared"), TeamOf(side)));
            if (state.Spikes > 0)
            {
                state.Spikes = 0;
                line.With(Now(side, SideCondition.Spikes));
            }
            if (state.ToxicSpikes > 0)
            {
                state.ToxicSpikes = 0;
                line.With(Now(side, SideCondition.ToxicSpikes));
            }
            if (state.StealthRock)
            {
                state.StealthRock = false;
                line.With(Now(side, SideCondition.StealthRock));
            }
            ClearLaterSide(side, line, veil: false, web: true);
            tidied = true;
        }
        if (tidied) Say(Text.Of("battle.tidyingUpComplete"));
        bool attack = ChangeStat(user, StatType.Attack, 1, user, false, By.Move);
        bool speed = ChangeStat(user, StatType.Speed, 1, user, false, By.Move);
        if (!tidied && !attack && !speed) Fails();
    }

    // ---------------------------------------------------------------- what lasts on the target

    /// <summary>Salt Cure: the target is cured with salt while it stays in, losing HP at each turn's end (secondary: <c>saltcure</c>).</summary>
    private void SaltCure(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit) || t.Volatile.SaltCured) return;
        t.Volatile.SaltCured = true;
        Say(Text.Of("battle.isBeingSaltCured", ("target", t.Name))).With(new MarkChanged(t.Place, MoveMark.SaltCure, On: true));
    }

    /// <summary>
    /// The turn's end for one cured with salt (onResidualOrder 13): an eighth of its HP, a quarter for a Water or a
    /// Steel type, Magic Guard aside.
    /// </summary>
    private void SaltCures(Battler b)
    {
        if (!b.Volatile.SaltCured) return;
        var p = b.Pokemon!;
        int share = b.HasType(PokemonType.Water) || b.HasType(PokemonType.Steel) ? 4 : 8;
        LoseHp(b, Formulas.Divide(p.MaxHP, share), Text.Of("battle.isHurtBySaltCure", ("pokemon", b.Name)));
    }

    /// <summary>Syrup Bomb: the target is covered in syrup for three turns' ends, while its user stays (secondary: <c>syrupbomb</c>, duration 4).</summary>
    private void SyrupBomb(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit) || t.Volatile.SyrupBombBy != null) return;
        t.Volatile.SyrupBombBy = use.User.Place;
        t.Volatile.SyrupBombTurns = 4;
        Say(Text.Of("battle.gotCoveredInSyrup", ("target", t.Name))).With(new MarkChanged(t.Place, MoveMark.SyrupBomb, On: true));
    }

    /// <summary>
    /// The turn's end for one covered in syrup (onResidualOrder 14): its count goes down first, and at nothing the
    /// syrup is gone with no line; otherwise a stage of Speed. Its user gone, so is the syrup (onUpdate).
    /// </summary>
    private void Syrupy(Battler b)
    {
        if (b.Volatile.SyrupBombBy is not { } from) return;
        var source = At(from);
        if (!source.IsActive || --b.Volatile.SyrupBombTurns <= 0)
        {
            WipeTheSyrup(b);
            return;
        }
        ChangeStat(b, StatType.Speed, -1, source, false, By.Move);
    }

    /// <summary>The syrup is gone, with no line: the event stands by itself.</summary>
    private void WipeTheSyrup(Battler b)
    {
        b.Volatile.SyrupBombBy = null;
        b.Volatile.SyrupBombTurns = 0;
        Emit(new MarkChanged(b.Place, MoveMark.SyrupBomb, On: false));
    }

    /// <summary>Psychic Noise: the target can't heal for two turns (secondary: <c>healblock</c>, whose duration is 2 for this move).</summary>
    private void PsychicNoise(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit) || t.Volatile.HealBlockTurns > 0) return;
        t.Volatile.HealBlockTurns = 2;
        Say(Text.Of("battle.wasPreventedFromHealing", ("target", t.Name)));
    }

    /// <summary>Alluring Voice: a target whose stats rose this turn is confused, if its side effects reach it (secondary.onHit).</summary>
    private void AlluringVoice(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit) || !t.Turn.StatsRaised) return;
        Confuse(t, use.User, false, By.SideEffect);
    }

    // ---------------------------------------------------------------- status moves

    /// <summary>Spicy Extract: two stages of Attack up and two of Defense down for the target; it fails when neither moved (boosts).</summary>
    private bool SpicyExtract(MoveUse use, Battler t)
    {
        bool attack = ChangeStat(t, StatType.Attack, 2, use.User, true, By.Move);
        bool defense = ChangeStat(t, StatType.Defense, -2, use.User, true, By.Move);
        return attack || defense;
    }

    /// <summary>
    /// Fillet Away: two stages of Attack, Sp. Atk and Speed for half its HP; not with half or less left, and
    /// nothing is lost when no stat could rise (onTry, onTryHit, onHit).
    /// </summary>
    private void FilletAway(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (p.CurrentHP * 2 <= p.MaxHP || p.MaxHP == 1)
        {
            Fails();
            return;
        }
        bool any = false;
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed })
            any |= ChangeStat(user, stat, 2, user, false, By.Move);
        if (!any)
        {
            Fails();
            return;
        }
        LoseHp(user, p.MaxHP / 2, Text.Of("battle.cutItsOwnHpToPowerUp", ("user", user.Name)), direct: true);
    }

    /// <summary>
    /// The abilities Doodle can't copy (Showdown's <c>failroleplay</c>): those Simple Beam can't replace and those
    /// Entrainment can't hand on (R24's two lists, Truant aside), and four more.
    /// </summary>
    // Made when first asked: the two lists are another file's statics
    private static HashSet<string> NotDrawn => notDrawn ??= CantBeReplaced.Where(a => a != "Truant").Concat(NotHandedOn)
        .Concat(new[] { "Wonder Guard", "Poison Puppeteer", "Tera Shell", "Teraform Zero" }).ToHashSet();

    private static HashSet<string>? notDrawn;

    /// <summary>
    /// Doodle: its user and its allies take the target's ability, each that hasn't it already and whose own can be
    /// replaced (onHit; <c>failroleplay</c>, <c>cantsuppress</c>).
    /// </summary>
    private bool Doodle(MoveUse use, Battler t)
    {
        var drawn = t.Pokemon!.AbilityName;
        if (drawn == null || NotDrawn.Contains(drawn)) return Fails();
        bool any = false;
        foreach (var b in SlotsOf(use.User.Side).Where(b => b.IsActive))
        {
            var p = b.Pokemon!;
            if (p.AbilityName == drawn || (p.AbilityName != null && CantBeReplaced.Contains(p.AbilityName) && p.AbilityName != "Truant")) continue;
            if (ShieldsItsAbility(b)) continue;
            KeepAbility(Keep(b), p);
            p.AbilityName = drawn;
            Say(Text.Of("battle.acquired", ("target", b.Name), ("ability", AbilityOf(b))));
            any = true;
        }
        return any || Fails();
    }

    /// <summary>
    /// Dragon Cheer: the ally beside its user is a stage likelier to land a critical hit, two for a Dragon type (as it
    /// is when cheered); not one that is pumped up by Focus Energy already, nor twice (its condition's onStart).
    /// </summary>
    private void DragonCheer(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        var allies = use.Targets.Where(t => t.IsActive && t != use.User).ToList();
        if (allies.Count == 0)
        {
            Fails();
            return;
        }
        foreach (var t in allies)
        {
            if (t.Volatile.FocusEnergy || t.Volatile.DragonCheer > 0)
            {
                Fails();
                continue;
            }
            t.Volatile.DragonCheer = t.HasType(PokemonType.Dragon) ? 2 : 1;
            Say(Text.Of("battle.isGettingPumped", ("user", t.Name)));
        }
    }

    /// <summary>
    /// Chilly Reception: a joke first, then snow, then its user goes back for another (<c>weather: 'snowscape'</c>,
    /// <c>selfSwitch</c>); it fails only when neither the snow came nor anyone can come in.
    /// </summary>
    private void ChillyReception(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        Say(Text.Of("battle.isPreparingAJoke", ("user", user.Name)));
        int turns = 5 + BattleEffects.Of(user).Sum(e => e.ExtraTurns("Hail"));
        bool snowed = SetWeather(BattleWeather.Snow, turns, Starts(BattleWeather.Snow));
        bool leaves = HasReplacement(user);
        if (leaves) use.Leaves = Leaving.Returns;
        else if (!snowed) Fails();
    }

    /// <summary>
    /// Shed Tail: half its HP, rounded up, makes a Substitute of a quarter, and it leaves for another, which takes
    /// the Substitute and nothing else (onTryHit, onHit, <c>selfSwitch: 'shedtail'</c>). Nobody to come in, a
    /// Substitute up already or no more than half its HP left, it fails.
    /// </summary>
    private void ShedTail(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (!HasReplacement(user) || user.HasSubstitute)
        {
            Fails();
            return;
        }
        int cost = (p.MaxHP + 1) / 2;
        if (p.CurrentHP <= cost)
        {
            Say(Text.Of("battle.itWasTooWeak"));
            failedNow = true;
            return;
        }
        p.CurrentHP -= cost;
        user.Volatile.SubstituteHp = Math.Max(1, p.MaxHP / 4);
        Say(Text.Of("battle.shedItsTail", ("user", user.Name)))
            .With(new HpChanged(user.Place, p.CurrentHP, Healed: false)).With(new SubstituteChanged(user.Place, Up: true));
        use.Leaves = Leaving.ShedsItsTail;
    }

    /// <summary>What Shed Tail hands on as its user leaves: its Substitute, and nothing more.</summary>
    private Baton HandOnTheDoll(Battler place)
    {
        int doll = place.Volatile.SubstituteHp;
        Withdraw(place);
        return new Baton(new Volatiles { SubstituteHp = doll }, new Dictionary<StatType, int>(), 0);
    }

    /// <summary>
    /// Revival Blessing: a fainted Pokémon of its user's side is back with half its HP (side.ts's revival); nothing
    /// when none has fainted (onTryHit). Showdown lets whoever uses it choose; here the first fainted in its party's
    /// order is chosen, for both sides.
    /// </summary>
    private void RevivalBlessing(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        var onTheField = SlotsOf(user.Side).Select(b => b.Pokemon).ToHashSet();
        var revived = user.Roster?.Members.FirstOrDefault(m => !m.IsEgg && m.CurrentHP <= 0 && !onTheField.Contains(m));
        if (revived == null)
        {
            Fails();
            return;
        }
        revived.Status = StatusCondition.None;
        revived.SleepTurns = 0;
        revived.ToxicCounter = 0;
        revived.CurrentHP = Math.Max(1, revived.MaxHP / 2);
        Say(Text.Of("battle.wasRevivedAndReadyToFight", ("pokemon", revived.DisplayName)));
    }
}
