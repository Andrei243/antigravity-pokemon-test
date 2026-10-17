using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later generations' moves, batch 3, its first half (plan 06 · R26a): Generation 8's, Sword and Shield's and
// Legends: Arceus's. The rules are Pokémon Showdown's (the third pinned source, data/moves.ts at the commit in
// tools/DataImporter/Sources.cs); each entry names the move it was read from. A move whose data fields say all it
// does (Legends: Arceus's take theirs from Showdown's table, through the importer) has an empty entry here, so the
// importer marks it run; a move that hits with other numbers (Body Press, Surging Strikes, Tar Shot's mark) has its
// rule in DamageCalculator, which reads the move's effect. Grassy Glide's priority is PriorityOf's, Snipe Shot's aim
// ResolveTargets', Octolock's grip TrapOn's and the turn's end's. Dynamax Cannon, Behemoth Blade and Behemoth Bash are
// left to R22 (their doubling against a Dynamaxed target). They join the table in BattleCore.Effects.cs when the class
// is first used (the static constructor in BattleCore.Families.cs).
public sealed partial class BattleCore
{
    private static readonly Dictionary<string, MoveEffect> LaterEffects4 = new()
    {
        // ---- Sword and Shield
        ["SnipeShot"] = new(), // goes where it was aimed, whatever draws moves: ResolveTargets
        ["BodyPress"] = new(), // its user's Defense and its stage: DamageCalculator.OffensiveStat
        ["GrassyGlide"] = new(), // a step of priority on Grassy Terrain: PriorityOf
        ["JawLock"] = new() { Hit = (b, use, hit) => b.JawLock(use, hit) },
        ["StuffCheeks"] = new() { Does = (b, use) => b.StuffCheeks(use) },
        ["NoRetreat"] = new() { Does = (b, use) => b.NoRetreat(use) },
        ["TarShot"] = new() { Does = (b, use) => b.OnEach(use, t => b.TarShot(use, t)) },
        ["MagicPowder"] = new() { Does = (b, use) => b.OnEach(use, t => b.MagicPowder(t)) },
        ["DragonDarts"] = new()
        {
            // smartTarget: in a double battle with both foes standing, a dart for each; otherwise two at the one
            Start = (b, use) =>
            {
                b.AimTheDarts(use);
                return true;
            },
            Strikes = (b, use) => use.Targets.Count > 1 ? 1 : 2
        },
        ["Teatime"] = new() { Does = (b, use) => b.Teatime(use) },
        ["Octolock"] = new() { Does = (b, use) => b.OnEach(use, t => b.Octolock(use, t)) },
        ["BoltBeak"] = new()
        {
            // Twice as strong against a target still to move this turn, or one that has just come in (basePowerCallback)
            Power = (b, use, t) => !t.Turn.Acted || (t.Volatile.EnteredOnTurn == b.Turn && t.Volatile.FirstTurn > b.Turn) ? 20 : 10
        },
        ["CourtChange"] = new() { Does = (b, use) => b.CourtChange(use) },
        ["ClangorousSoul"] = new() { Does = (b, use) => b.ClangorousSoul(use) },
        ["AuraWheel"] = new()
        {
            // Only Morpeko's, and Dark in its Hangry Mode (onTry, onModifyType)
            Start = (b, use) =>
            {
                var p = use.User.Pokemon!;
                if (p.Species.Name != "Morpeko") return b.Fails();
                Retype(use, p.Form == "Morpeko-Hangry" ? PokemonType.Dark : PokemonType.Electric);
                return true;
            }
        },
        ["GravApple"] = new() { Power = (b, use, t) => b.Field.Gravity ? 15 : 10 },
        ["LifeDew"] = new() { Does = (b, use) => b.LifeDew(use) },
        ["Obstruct"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.Obstruct) },
        ["ExpandingForce"] = new()
        {
            // On Psychic Terrain, from a user on the ground: every foe beside it, and half as strong again
            Start = (b, use) =>
            {
                if (b.Field.Has(FieldCondition.PsychicTerrain) && DamageCalculator.IsGrounded(use.User))
                {
                    var foes = b.SlotsOf(Other(use.User.Side)).Where(f => f.IsActive).ToList();
                    if (foes.Count > 0) use.Targets = foes;
                }
                return true;
            },
            Power = (b, use, t) => b.Field.Has(FieldCondition.PsychicTerrain) && DamageCalculator.IsGrounded(use.User) ? 15 : 10
        },
        ["SteelRoller"] = new()
        {
            // Nothing without a terrain; the terrain is gone once it has hit, a Substitute included (onTry, onHit)
            Start = (b, use) => b.Field.Terrain != null || b.Fails(),
            Then = (b, use) =>
            {
                if (b.Field.Terrain is { } terrain) b.EndTerrain(terrain);
            }
        },
        ["ScaleShot"] = new()
        {
            // Two to five scales, then a stage of Speed up and one of Defense down for its user (selfBoost)
            Strikes = (b, use) => b.MultiHitCount(use.User),
            Then = (b, use) =>
            {
                b.ChangeStat(use.User, StatType.Defense, -1, use.User, false, By.Move);
                b.ChangeStat(use.User, StatType.Speed, 1, use.User, false, By.Move);
            }
        },
        ["MeteorBeam"] = new() { Charge = new("battle.isOverflowingWithSpacePower", RaisesSpAttack: true) },
        ["ShellSideArm"] = new() { Start = (b, use) => b.ShellSideArm(use) },
        ["MistyExplosion"] = new()
        {
            // Explosion's, Damp included, and half as strong again on Misty Terrain from a user on the ground
            Start = (b, use) => MoveEffects["HalveDefense"].Begins(b, use),
            SelfKo = true,
            Power = (b, use, t) => b.Field.Has(FieldCondition.MistyTerrain) && DamageCalculator.IsGrounded(use.User) ? 15 : 10
        },
        ["RisingVoltage"] = new()
        {
            // Twice as strong against a target on the ground under Electric Terrain (basePowerCallback)
            Power = (b, use, t) => b.Field.Has(FieldCondition.ElectricTerrain) && DamageCalculator.IsGrounded(t) ? 20 : 10
        },
        ["TerrainPulse"] = new()
        {
            // From a user on the ground under a terrain: the terrain's type, and twice the power (onModifyType, onModifyMove)
            Start = (b, use) =>
            {
                if (b.TerrainPulses(use.User) is { } type) Retype(use, type);
                return true;
            },
            BasePower = (b, use, t) => b.TerrainPulses(use.User) != null ? use.Data.Power * 2 : use.Data.Power
        },
        ["BurningJealousy"] = new() { Hit = (b, use, hit) => b.BurningJealousy(use, hit) },
        ["LashOut"] = new() { Power = (b, use, t) => use.User.Turn.StatsLowered ? 20 : 10 },
        ["Poltergeist"] = new() { Start = (b, use) => b.Poltergeist(use) },
        ["Coaching"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                bool attack = b.ChangeStat(t, StatType.Attack, 1, use.User, true, By.Move);
                bool defense = b.ChangeStat(t, StatType.Defense, 1, use.User, true, By.Move);
                return attack || defense;
            })
        },
        ["JungleHealing"] = new() { Does = (b, use) => b.HealAndCure(use) },
        ["SurgingStrikes"] = new() { Strikes = (b, use) => 3 }, // and every one critical: DamageCalculator.AlwaysCritical
        ["EerieSpell"] = new() { Hit = (b, use, hit) => b.EerieSpell(use, hit) },

        // ---- Legends: Arceus (their side effects are their data's, from Showdown's table)
        ["PsyshieldBash"] = new(),
        ["SpringtideStorm"] = new(),
        ["MysticalPower"] = new(),
        ["WaveCrash"] = new(),
        ["MountainGale"] = new(),
        ["VictoryDance"] = new(),
        ["HeadlongRush"] = new(),
        ["EsperWing"] = new(),
        ["BitterMalice"] = new(),
        ["Shelter"] = new(),
        ["TripleArrows"] = new(),
        ["DireClaw"] = new() { Hit = (b, use, hit) => b.DireClaw(use, hit) },
        ["StoneAxe"] = new() { Hit = (b, use, hit) => b.LeaveBehind(use, hit, SideCondition.StealthRock) },
        ["CeaselessEdge"] = new() { Hit = (b, use, hit) => b.LeaveBehind(use, hit, SideCondition.Spikes) },
        ["Chloroblast"] = new()
        {
            // Half its user's HP, rounded, once it has hit (chloroblastRecoil: Math.round(maxhp / 2))
            Then = (b, use) => b.LoseHp(use.User, (use.User.Pokemon!.MaxHP + 1) / 2, Text.Of("battle.hurtByItsOwnChloroblast", ("user", use.User.Name), ("move", use.Move.DisplayName)))
        },
        ["BarbBarrage"] = new() { Power = (b, use, t) => t.Pokemon!.Status is StatusCondition.Poison or StatusCondition.Toxic ? 20 : 10 },
        ["InfernalParade"] = new()
        {
            // Twice as strong against a target with a condition, or with Comatose (basePowerCallback)
            Power = (b, use, t) => t.Pokemon!.Status is not (StatusCondition.None or StatusCondition.Faint) || Has(t, "Comatose") ? 20 : 10
        },
        // Sure to hit in the rain (onModifyMove: the target's weather), its chance its data's
        ["BleakwindStorm"] = new() { SureIn = BattleWeather.Rain },
        ["WildboltStorm"] = new() { SureIn = BattleWeather.Rain },
        ["SandsearStorm"] = new() { SureIn = BattleWeather.Rain },
        ["LunarBlessing"] = new() { Does = (b, use) => b.HealAndCure(use) },
        ["TakeHeart"] = new() { Does = (b, use) => b.TakeHeart(use) }
    };

    // ---------------------------------------------------------------- the turn's order and the aim

    /// <summary>A move's priority this turn: Grassy Glide's goes up a step on Grassy Terrain from a user on the ground (onModifyPriority).</summary>
    private int PriorityOf(Act act)
    {
        int priority = act.Move?.Priority ?? 0;
        // Prankster, Gale Wings, Triage (plan 06 · R27)
        priority += AbilityPriority(act.User, act.Move);
        if (act.Move?.Data.Effect == "GrassyGlide" && Field.Has(FieldCondition.GrassyTerrain) && DamageCalculator.IsGrounded(act.User)) priority++;
        return priority;
    }

    /// <summary>Snipe Shot goes where it was aimed: Follow Me, Lightning Rod and Storm Drain don't draw it (<c>tracksTarget</c>).</summary>
    private static bool TracksItsTarget(MoveData move) => move.Effect == "SnipeShot";

    /// <summary>
    /// Dragon Darts in a double battle (<c>smartTarget</c>, <c>getSmartTargets</c>): with the foe beside its target
    /// standing too, a dart for each; otherwise both at the one.
    /// </summary>
    private void AimTheDarts(MoveUse use)
    {
        if (!IsDouble || use.Targets.Count != 1) return;
        var target = use.Targets[0];
        var beside = SlotsOf(target.Side).FirstOrDefault(o => o != target && o != use.User && o.IsActive);
        if (beside != null && target.IsActive) use.Targets.Add(beside);
    }

    /// <summary>Dragon Darts' two darts are two moves at one target each, not a move at several: no spread's quarter off.</summary>
    private static bool StrikesEachAlone(MoveData move) => move.Effect == "DragonDarts";

    // ---------------------------------------------------------------- what holds a Pokémon on the field

    /// <summary>No Retreat or Octolock holds it (each condition's <c>onTrapPokemon</c>; a Ghost type goes free, TrapOn's first).</summary>
    private static bool HeldByALaterMove(Battler b) => b.Volatile.NoRetreat || b.Volatile.OctolockedBy != null;

    /// <summary>Jaw Lock: once it has hit, neither its user nor its target can leave while the other stays (each a <c>trapped</c> volatile, linked).</summary>
    private void JawLock(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || !user.IsActive) return;
        bool held = false;
        if (user.Volatile.TrappedBy == null)
        {
            user.Volatile.TrappedBy = t.Place;
            held = true;
        }
        if (t.Volatile.TrappedBy == null)
        {
            t.Volatile.TrappedBy = user.Place;
            held = true;
        }
        if (held) Say(Text.Of("battle.neitherCanRunAway"));
    }

    /// <summary>
    /// No Retreat: a stage of every stat, and its user can't leave; not again while it holds (onTry). Already held
    /// by another, it raises its stats all the same.
    /// </summary>
    private void NoRetreat(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Volatile.NoRetreat)
        {
            Fails();
            return;
        }
        bool any = false;
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            any |= ChangeStat(user, stat, 1, user, false, By.Move);
        if (user.Volatile.TrappedBy != null) return;
        user.Volatile.NoRetreat = true;
        Say(Text.Of("battle.cantEscapeNoRetreat", ("user", user.Name), ("move", use.Move.DisplayName)));
    }

    /// <summary>
    /// Octolock: its target can't leave while its user stays, and loses a stage of Defense and of Sp. Def at each
    /// turn's end (onResidualOrder 14); a Ghost type is immune, and a target already held fails.
    /// </summary>
    private bool Octolock(MoveUse use, Battler t)
    {
        if (Rules.GhostTypesCantBeTrapped && t.HasType(PokemonType.Ghost))
        {
            Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
            return false;
        }
        if (t.Volatile.OctolockedBy != null) return Fails();
        t.Volatile.OctolockedBy = use.User.Place;
        Say(Text.Of("battle.caughtInOctolock", ("target", t.Name), ("move", use.Move.DisplayName)));
        return true;
    }

    /// <summary>The turn's end for one held by Octolock: two stages lost while its user stands; its user gone, the grip is too.</summary>
    private void Octolocked(Battler b)
    {
        if (b.Volatile.OctolockedBy is not { } from) return;
        var source = At(from);
        if (!source.IsActive)
        {
            b.Volatile.OctolockedBy = null;
            return;
        }
        ChangeStat(b, StatType.Defense, -1, source, true, By.Move);
        ChangeStat(b, StatType.SpDefense, -1, source, true, By.Move);
    }

    // ---------------------------------------------------------------- berries

    /// <summary>A berry eaten whole by its holder, whatever an Unnerve says (Showdown's <c>eatItem(true)</c>): it is gone and gives what it gives.</summary>
    private void EatsItsBerry(Battler holder)
    {
        if (holder.Pokemon?.HeldItem is not { Pocket: ItemPocket.Berries } berry) return;
        ConsumeItem(holder);
        Say(Text.Of("battle.ateIts", ("pokemon", holder.Name), ("item", ItemOf(holder, berry))));
        if (berry.PluckEffect != null) EatBerry(holder, berry, berry.PluckEffect);
    }

    /// <summary>Stuff Cheeks: two stages of Defense and the berry it holds eaten; nothing without a berry, or when its Defense can't rise (onTry, onHit).</summary>
    private void StuffCheeks(MoveUse use)
    {
        var user = use.User;
        use.Line.With(Shown(user, user, use.Move, null));
        if (user.Pokemon!.HeldItem is not { Pocket: ItemPocket.Berries } || !ChangeStat(user, StatType.Defense, 2, user, false, By.Move))
        {
            Fails();
            return;
        }
        EatsItsBerry(user);
    }

    /// <summary>Teatime: everyone on the field with a berry eats it, from the fastest; it fails when nobody holds one (onHitField).</summary>
    private void Teatime(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        var eaters = BySpeed().Where(b => b.IsActive && b.Pokemon!.HeldItem is { Pocket: ItemPocket.Berries } && !b.IsElsewhere).ToList();
        if (eaters.Count == 0)
        {
            Fails();
            return;
        }
        Say(Text.Of("battle.itsTeatime"));
        foreach (var b in eaters) EatsItsBerry(b);
    }

    // ---------------------------------------------------------------- moves on another Pokémon

    /// <summary>Tar Shot: a stage of Speed off, and Fire moves twice as strong against it while it stays in (its condition's onEffectiveness).</summary>
    private bool TarShot(MoveUse use, Battler t)
    {
        bool slowed = ChangeStat(t, StatType.Speed, -1, use.User, true, By.Move);
        if (t.Volatile.TarShot) return slowed;
        t.Volatile.TarShot = true;
        Say(Text.Of("battle.becameWeakerToFire", ("target", t.Name)));
        return true;
    }

    /// <summary>Magic Powder: the target is a Psychic type and nothing else; not one that is that already, nor Arceus or Silvally (setType).</summary>
    private bool MagicPowder(Battler t)
    {
        if (t.Types.SequenceEqual(new[] { PokemonType.Psychic }) || t.Pokemon!.Species.Name is "Arceus" or "Silvally") return Fails();
        t.Volatile.Types = new List<PokemonType> { PokemonType.Psychic };
        t.Volatile.AddedType = null;
        Say(Text.Of("battle.transformedIntoTheType", ("user", t.Name), ("pick", PokemonType.Psychic)));
        return true;
    }

    /// <summary>Burning Jealousy: a target whose stats rose this turn is burned, if its side effects reach it (secondary.onHit).</summary>
    private void BurningJealousy(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit) || !t.Turn.StatsRaised) return;
        TryInflictStatus(t, StatusCondition.Burn, use.User, false, By.SideEffect);
    }

    /// <summary>Dire Claw: one time in two, poison, paralysis or sleep, one drawn of the three (secondary, <c>this.sample</c>).</summary>
    private void DireClaw(MoveUse use, MoveHit hit)
    {
        if (!SideEffectReaches(use, hit) || !Chance(50 * SideEffectOdds(use.User))) return;
        var status = new[] { StatusCondition.Poison, StatusCondition.Paralyze, StatusCondition.Sleep }[rng.Roll(RollKind.SideEffect, 3)];
        TryInflictStatus(hit.Target, status, use.User, false, By.SideEffect);
    }

    /// <summary>Eerie Spell: the target's last move loses 3 PP, or what it has left (secondary.onHit, deductPP).</summary>
    private void EerieSpell(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!SideEffectReaches(use, hit)) return;
        var last = t.Volatile.LastMove;
        var move = last == null ? null : t.Pokemon!.Moves.FirstOrDefault(m => m.Data == last);
        if (move == null || move.CurrentPP == 0) return;
        int taken = Math.Min(3, move.CurrentPP);
        move.CurrentPP -= taken;
        Say(Text.Of("battle.itReducedThePp", ("target", t.Name), ("move", move.DisplayName), ("taken", taken)));
    }

    /// <summary>A move's own side effect gets to a target: it stands, the hit reached it and not its Substitute, no Shield Dust, no Sheer Force.</summary>
    private bool SideEffectReaches(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || t == use.User || Has(use.User, "Sheer Force")) return false;
        bool breaks = BattleEffects.Of(use.User).Any(e => e.IgnoresTargetAbility);
        return !BattleEffects.Of(t, includeAbility: !breaks).Any(e => e.BlocksSideEffects);
    }

    /// <summary>What a side effect's chance is multiplied by: Serene Grace, and a rainbow over its user's side.</summary>
    private int SideEffectOdds(Battler user) => BattleEffects.Of(user).Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max() * RainbowOver(user);

    /// <summary>
    /// Stone Axe and Ceaseless Edge: what they leave about the target's side once they hit, a Substitute included,
    /// unless Sheer Force took it (onAfterHit, onAfterSubDamage): Stealth Rock, or a layer of Spikes to three.
    /// </summary>
    private void LeaveBehind(MoveUse use, MoveHit hit, SideCondition what)
    {
        if (!hit.Landed || !use.User.IsActive || Has(use.User, "Sheer Force")) return;
        var theirs = hit.Target.Side;
        var side = Field.Side(theirs);
        if (what == SideCondition.StealthRock)
        {
            if (side.StealthRock) return;
            side.StealthRock = true;
            Say(string.Format(Text.Of("battle.pointedStonesFloatIn"), TeamOf(theirs))).With(Now(theirs, what));
            return;
        }
        if (side.Spikes >= 3) return;
        side.Spikes++;
        Say(string.Format(Text.Of("battle.spikesWereScatteredAll"), TeamOf(theirs))).With(Now(theirs, what));
    }

    /// <summary>Poltergeist: nothing against a target that holds no item; one that does is told of it (onTry, onTryHit).</summary>
    private bool Poltergeist(MoveUse use)
    {
        var t = use.Targets.FirstOrDefault();
        if (t?.Pokemon?.HeldItem is not { } item) return Fails();
        Say(Text.Of("battle.aboutToBeAttackedByItsItem", ("target", t.Name), ("item", ItemOf(t, item))));
        return true;
    }

    // ---------------------------------------------------------------- the field

    /// <summary>The conditions Court Change carries from one side to the other (its <c>sideConditions</c> list, G-Max's for R22).</summary>
    private static readonly SideCondition[] CourtConditions =
    {
        SideCondition.Mist, SideCondition.LightScreen, SideCondition.Reflect, SideCondition.Spikes, SideCondition.Safeguard, SideCondition.Tailwind,
        SideCondition.ToxicSpikes, SideCondition.StealthRock, SideCondition.Rainbow, SideCondition.SeaOfFire, SideCondition.Swamp,
        SideCondition.StickyWeb, SideCondition.AuroraVeil, SideCondition.LuckyChant
    };

    /// <summary>
    /// Court Change: what lies on each side and what each side has put up change sides, turns and layers with them;
    /// it fails when neither side has anything (onHitField). The line carries each change for the look.
    /// </summary>
    private void CourtChange(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        SideState a = Field.Side(BattleSide.Player), b = Field.Side(BattleSide.Enemy);
        var before = CourtConditions.Select(c => (c, Mine: a.Layers(c), Theirs: b.Layers(c))).ToList();
        if (before.All(x => x.Mine == 0 && x.Theirs == 0))
        {
            Fails();
            return;
        }
        (a.MistTurns, b.MistTurns) = (b.MistTurns, a.MistTurns);
        (a.LightScreenTurns, b.LightScreenTurns) = (b.LightScreenTurns, a.LightScreenTurns);
        (a.ReflectTurns, b.ReflectTurns) = (b.ReflectTurns, a.ReflectTurns);
        (a.Spikes, b.Spikes) = (b.Spikes, a.Spikes);
        (a.SafeguardTurns, b.SafeguardTurns) = (b.SafeguardTurns, a.SafeguardTurns);
        (a.TailwindTurns, b.TailwindTurns) = (b.TailwindTurns, a.TailwindTurns);
        (a.ToxicSpikes, b.ToxicSpikes) = (b.ToxicSpikes, a.ToxicSpikes);
        (a.StealthRock, b.StealthRock) = (b.StealthRock, a.StealthRock);
        (a.RainbowTurns, b.RainbowTurns) = (b.RainbowTurns, a.RainbowTurns);
        (a.SeaOfFireTurns, b.SeaOfFireTurns) = (b.SeaOfFireTurns, a.SeaOfFireTurns);
        (a.SwampTurns, b.SwampTurns) = (b.SwampTurns, a.SwampTurns);
        (a.StickyWeb, b.StickyWeb) = (b.StickyWeb, a.StickyWeb);
        (a.AuroraVeilTurns, b.AuroraVeilTurns) = (b.AuroraVeilTurns, a.AuroraVeilTurns);
        (a.LuckyChantTurns, b.LuckyChantTurns) = (b.LuckyChantTurns, a.LuckyChantTurns);
        var line = Say(Text.Of("battle.swappedFieldEffects", ("user", use.User.Name)));
        foreach (var (c, mine, theirs) in before.Where(x => x.Mine != x.Theirs))
        {
            line.With(Now(BattleSide.Player, c));
            line.With(Now(BattleSide.Enemy, c));
        }
        // A Tailwind that changed sides changes who is faster
        orderChanged = true;
    }

    /// <summary>The type Terrain Pulse takes from the terrain under a user on the ground; null for none.</summary>
    private PokemonType? TerrainPulses(Battler user)
    {
        if (!DamageCalculator.IsGrounded(user) || Field.Terrain is not { } terrain || !Field.Has(terrain)) return null;
        return terrain switch
        {
            FieldCondition.ElectricTerrain => PokemonType.Electric,
            FieldCondition.GrassyTerrain => PokemonType.Grass,
            FieldCondition.MistyTerrain => PokemonType.Fairy,
            FieldCondition.PsychicTerrain => PokemonType.Psychic,
            _ => null
        };
    }

    // ---------------------------------------------------------------- moves on its own side

    /// <summary>
    /// Clangorous Soul: a stage of every stat for a third of its HP (33 hundredths, rounded down); not with that much
    /// or less left, and nothing is lost when no stat could rise (onTry, onTryHit, onHit).
    /// </summary>
    private void ClangorousSoul(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (p.CurrentHP * 100 <= p.MaxHP * 33 || p.MaxHP == 1)
        {
            Fails();
            return;
        }
        bool any = false;
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            any |= ChangeStat(user, stat, 1, user, false, By.Move);
        if (!any)
        {
            Fails();
            return;
        }
        LoseHp(user, Math.Max(1, p.MaxHP * 33 / 100), Text.Of("battle.gaveItsHpToItsSoul", ("user", user.Name)), direct: true);
    }

    /// <summary>Life Dew: a quarter of their HP, rounded, to its user and its allies; it fails when nobody needed it (heal: [1, 4]).</summary>
    private void LifeDew(MoveUse use)
    {
        bool any = false;
        foreach (var t in use.Targets) use.Line.With(Shown(use.User, t, use.Move, null));
        foreach (var t in use.Targets.Where(t => t.IsActive))
        {
            var p = t.Pokemon!;
            if (p.CurrentHP >= p.MaxHP || t.Volatile.HealBlockTurns > 0) continue;
            RestoreHp(t, (p.MaxHP + 2) / 4, Text.Of("battle.regainedHealth", ("user", t.Name)));
            any = true;
        }
        if (!any) Fails();
    }

    /// <summary>
    /// Jungle Healing and Lunar Blessing: a quarter of their HP (<c>this.modify</c>'s 4096ths) and their condition
    /// cured, for its user and its allies; it fails when neither did anything for anyone.
    /// </summary>
    private void HealAndCure(MoveUse use)
    {
        bool any = false;
        foreach (var t in use.Targets) use.Line.With(Shown(use.User, t, use.Move, null));
        foreach (var t in use.Targets.Where(t => t.IsActive))
        {
            var p = t.Pokemon!;
            if (p.CurrentHP < p.MaxHP && t.Volatile.HealBlockTurns == 0)
            {
                RestoreHp(t, Math.Max(1, Formulas.Modify(p.MaxHP, 1024)), Text.Of("battle.regainedHealth", ("user", t.Name)));
                any = true;
            }
            if (p.Status is not (StatusCondition.None or StatusCondition.Faint))
            {
                CureStatus(t, Text.Of("battle.wasCuredOf", ("pokemon", t.Name), ("status", BattleText.StatusName(p.Status))));
                any = true;
            }
        }
        if (!any) Fails();
    }

    /// <summary>Take Heart: a stage of Sp. Atk and of Sp. Def, and its condition cured; it fails when neither happened.</summary>
    private void TakeHeart(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        bool special = ChangeStat(user, StatType.SpAttack, 1, user, false, By.Move);
        bool guard = ChangeStat(user, StatType.SpDefense, 1, user, false, By.Move);
        bool cured = false;
        if (p.Status is not (StatusCondition.None or StatusCondition.Faint))
        {
            CureStatus(user, Text.Of("battle.wasCuredOf", ("pokemon", user.Name), ("status", BattleText.StatusName(p.Status))));
            cured = true;
        }
        if (!special && !guard && !cured) Fails();
    }

    // ---------------------------------------------------------------- the category by the numbers

    /// <summary>
    /// Shell Side Arm: physical, and touching, when the damage a physical hit of 90 would do by its user's Attack
    /// against the target's Defense (both at their stages, nothing else) is the greater; a coin between equals (onModifyMove).
    /// </summary>
    private bool ShellSideArm(MoveUse use)
    {
        var t = use.Targets.FirstOrDefault();
        if (t?.Pokemon is not { } q) return true;
        var p = use.User.Pokemon!;
        int Staged(Pokemon who, StatType stat, int value) => Formulas.Staged(value, who.StatStages.GetValueOrDefault(stat));
        int Rough(int attack, int defense) => (2 * p.Level / 5 + 2) * 90 * attack / Math.Max(1, defense) / 50;
        int physical = Rough(Staged(p, StatType.Attack, p.Attack), Staged(q, StatType.Defense, q.Defense));
        int special = Rough(Staged(p, StatType.SpAttack, p.SpAttack), Staged(q, StatType.SpDefense, q.SpDefense));
        if (physical > special || (physical == special && rng.Roll(RollKind.Other, 2) == 0))
            use.Move = new Move(use.Data.CopyWith(m =>
            {
                m.Category = MoveCategory.Physical;
                m.Flags |= MoveFlags.Contact;
            }), use.Move.CurrentPP);
        return true;
    }
}
