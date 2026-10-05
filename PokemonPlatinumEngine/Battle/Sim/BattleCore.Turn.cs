using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The end of a turn, in the original's order (BattleControllerPlayer_CheckFieldConditions, _CheckMonConditions and
// _CheckSideConditions): first what the two sides and the sky have (screens run down, a Wish comes true, the
// weather passes), then each Pokémon from the fastest to the slowest with everything that is its own, then what
// was sent ahead (Future Sight, a perish count) and Trick Room. Whoever is brought to nothing faints at once,
// before the next thing happens.
public sealed partial class BattleCore
{
    /// <summary>Someone on the field is making an uproar: nobody can sleep.</summary>
    private bool UproarIsOn => AllBattlers.Any(b => b.IsActive && b.Volatile.UproarTurns > 0);

    private static string Starts(BattleWeather weather) => weather switch
    {
        BattleWeather.Rain => "It started to rain!",
        BattleWeather.Sandstorm => "A sandstorm kicked up!",
        BattleWeather.Sun => "The sunlight turned harsh!",
        BattleWeather.Hail => "It started to hail!",
        _ => "The fog is deep..."
    };

    private static string GoesOn(BattleWeather weather) => weather switch
    {
        BattleWeather.Rain => "Rain continues to fall.",
        BattleWeather.Sandstorm => "The sandstorm rages.",
        BattleWeather.Sun => "The sunlight is strong.",
        BattleWeather.Hail => "Hail continues to fall.",
        _ => "The fog is deep..."
    };

    private static string Stops(BattleWeather weather) => weather switch
    {
        BattleWeather.Rain => "The rain stopped.",
        BattleWeather.Sandstorm => "The sandstorm subsided.",
        BattleWeather.Sun => "The sunlight faded.",
        _ => "The hail stopped."
    };

    /// <summary>
    /// A battle opens under the sky of the place it is fought in, and that sky stays (the original's
    /// <c>subscript_overworld_*</c>); a place that bends the order of things gives it five turns of Trick Room.
    /// </summary>
    private void OpenUnderTheSky()
    {
        if (Conditions.Weather != BattleWeather.None) SetWeather(Conditions.Weather, 0, Starts(Conditions.Weather));
        if (Conditions.TrickRoom)
        {
            Field.TrickRoomTurns = 5;
            Say("The dimensions became distorted!");
        }
    }

    /// <summary>Faints whoever has fallen; false when that ended the battle.</summary>
    private bool Settle()
    {
        ResolveFaints();
        return Result == BattleResult.None;
    }

    private void EndOfTurn()
    {
        speedOrder = BySpeed();
        if (FieldConditions() && PokemonConditions()) SideConditions();
    }

    // ---------------------------------------------------------------- the sides and the sky

    private bool FieldConditions()
    {
        var sides = new[] { BattleSide.Player, BattleSide.Enemy };
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).ReflectTurns)) Say($"{TeamOf(side, true)}'s Reflect wore off!");
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).LightScreenTurns)) Say($"{TeamOf(side, true)}'s Light Screen wore off!");
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).MistTurns)) Say($"{TeamOf(side, true)}'s Mist wore off!");
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).SafeguardTurns)) Say($"{TeamOf(side, true)} is no longer protected by Safeguard!");
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).TailwindTurns)) Say($"{TeamOf(side, true)}'s tailwind petered out!");
        foreach (var side in sides) if (RunsOut(ref Field.Side(side).LuckyChantTurns)) Say($"{TeamOf(side, true)}'s Lucky Chant wore off!");

        // A Wish comes true for whoever stands where it was made
        foreach (var b in speedOrder)
        {
            var place = Field.At(b.Place);
            if (!RunsOut(ref place.WishTurns) || !b.IsActive) continue;
            Say($"{place.WishFrom}'s wish came true!");
            var p = b.Pokemon!;
            if (b.Volatile.HealBlockTurns > 0) Say($"{b.Name} was kept from healing!");
            else if (p.CurrentHP >= p.MaxHP) Say($"{b.Name}'s HP is full!");
            else RestoreHp(b, Formulas.Divide(p.MaxHP, 2), $"{b.Name} regained health!");
        }

        // The weather: it runs down unless it is there to stay, and passes over everyone
        if (Field.Weather is not (BattleWeather.None or BattleWeather.Fog) && !Field.WeatherLasts && --Field.WeatherTurns <= 0)
        {
            Say(Stops(Field.Weather)).With(new WeatherChanged(BattleWeather.None));
            Field.Weather = BattleWeather.None;
            Field.WeatherTurns = 0;
        }
        else if (Field.Weather != BattleWeather.None)
        {
            Say(GoesOn(Field.Weather));
            if (!WeatherPasses()) return false;
        }

        if (Field.GravityTurns > 0 && --Field.GravityTurns == 0) Say("Gravity returned to normal!");
        return true;
    }

    /// <summary>Counts a turn off; true when that was the last.</summary>
    private static bool RunsOut(ref int turns) => turns > 0 && --turns == 0;

    /// <summary>
    /// <c>BtlCmd_EndOfTurnWeatherEffect</c>, for each Pokémon from the fastest: a sandstorm wears at whatever isn't
    /// Rock, Steel or Ground and hail at whatever isn't Ice, a sixteenth of its HP, unless it is under the ground
    /// or the water or its ability shelters it; then what its ability makes of the weather.
    /// </summary>
    private bool WeatherPasses()
    {
        var weather = Field.WeatherInEffect;
        if (weather == BattleWeather.None) return true;
        foreach (var b in speedOrder.ToList())
        {
            if (!b.IsActive) continue;
            var effects = BattleEffects.Of(b).ToList();
            bool hidden = b.Volatile.Elsewhere is Elsewhere.Underground or Elsewhere.Underwater;
            bool wears = weather switch
            {
                BattleWeather.Sandstorm => !b.HasType(PokemonType.Rock) && !b.HasType(PokemonType.Steel) && !b.HasType(PokemonType.Ground),
                BattleWeather.Hail => !b.HasType(PokemonType.Ice),
                _ => false
            };
            if (wears && !hidden && !effects.Any(e => e.ShelteredFrom(weather) || e.PreventsIndirectDamage))
                LoseHp(b, Formulas.Divide(b.Pokemon!.MaxHP, 16), weather == BattleWeather.Sandstorm ? $"{b.Name} is buffeted by the sandstorm!" : $"{b.Name} is pelted by the hail!");
            if (b.IsActive && !hidden) foreach (var e in effects) e.UnderTheWeather(this, b, weather);
            if (!Settle()) return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- each Pokémon

    private bool PokemonConditions()
    {
        foreach (var b in speedOrder.ToList())
        {
            if (!b.IsActive) continue;
            var p = b.Pokemon!;
            var v = b.Volatile;
            bool guarded = BattleEffects.Of(b).Any(e => e.PreventsIndirectDamage);
            int drain = BattleEffects.Of(b).Select(e => e.DrainHundredths).DefaultIfEmpty(100).Max();

            // Roots and a ring of water give a sixteenth back
            foreach (var (has, line) in new[] { (v.Ingrained, "{0} absorbed nutrients with its roots!"), (v.AquaRing, "{0}'s veil of water restored its HP!") })
            {
                if (!has || p.CurrentHP >= p.MaxHP) continue;
                if (v.HealBlockTurns > 0) Say($"{b.Name} was kept from healing!");
                else RestoreHp(b, Formulas.Divide(p.MaxHP, 16) * drain / 100, string.Format(line, b.Name));
            }

            // Its ability, then its item (Speed Boost, Shed Skin; Leftovers, Black Sludge)
            b.Ability?.Effect?.AtEndOfTurn(this, b);
            if (b.IsActive) ItemOf(b)?.AtEndOfTurn(this, b);

            // Leech Seed: an eighth of its HP goes to whoever stands where the seeder stood
            if (b.IsActive && v.SeededBy is { } to && At(to).IsActive && !guarded)
            {
                var taker = At(to);
                int taken = Math.Min(p.CurrentHP, Formulas.Divide(p.MaxHP, 8));
                LoseHp(b, taken, $"{b.Name}'s health is sapped by Leech Seed!");
                int given = taken * BattleEffects.Of(taker).Select(e => e.DrainHundredths).DefaultIfEmpty(100).Max() / 100;
                if (BattleEffects.Of(b).Any(e => e.HurtsDrainers)) LoseHp(taker, given, $"{taker.Name} sucked up the liquid ooze!");
                else if (taker.Volatile.HealBlockTurns == 0 && taker.Pokemon!.CurrentHP < taker.Pokemon.MaxHP)
                    RestoreHp(taker, given, $"{taker.Name} took in the sapped health!");
            }

            if (b.IsActive)
            {
                switch (p.Status)
                {
                    case StatusCondition.Poison:
                        LoseHp(b, Formulas.Divide(p.MaxHP, 8), $"{b.Name} is hurt by poison!");
                        break;
                    case StatusCondition.Toxic:
                        p.ToxicCounter = Math.Min(15, p.ToxicCounter + 1);
                        LoseHp(b, Formulas.Divide(p.MaxHP, 16) * p.ToxicCounter, $"{b.Name} is hurt by poison!");
                        break;
                    case StatusCondition.Burn:
                        LoseHp(b, Formulas.Divide(p.MaxHP, Rules.BurnDamageDivisor), $"{b.Name} is hurt by its burn!");
                        break;
                }
            }

            // A nightmare lasts as long as the sleep does
            if (b.IsActive && v.Nightmare)
            {
                if (p.Status == StatusCondition.Sleep) LoseHp(b, Formulas.Divide(p.MaxHP, 4), $"{b.Name} is locked in a nightmare!");
                else v.Nightmare = false;
            }

            if (b.IsActive && v.Cursed) LoseHp(b, Formulas.Divide(p.MaxHP, 4), $"{b.Name} is afflicted by the curse!");

            // A binding move wears at it until its count is done, and the last count is the one that frees
            if (b.IsActive && v.BindTurns > 0)
            {
                if (--v.BindTurns > 0) LoseHp(b, Formulas.Divide(p.MaxHP, Rules.BindDamageDivisor), $"{b.Name} is hurt by {v.BindingMove}!");
                else
                {
                    Say($"{b.Name} was freed from {v.BindingMove}!");
                    v.BoundBy = null;
                }
            }

            // Bad Dreams across the field wears at a sleeper
            if (b.IsActive && p.Status == StatusCondition.Sleep && !guarded && ActiveFoes(b).Any(foe => Has(foe, "Bad Dreams")))
                LoseHp(b, Formulas.Divide(p.MaxHP, 8), $"{b.Name} is tormented!");

            if (b.IsActive && v.UproarTurns > 0)
            {
                foreach (var sleeper in AllBattlers.Where(o => o.IsActive && o.Pokemon!.Status == StatusCondition.Sleep && !Has(o, "Soundproof")).ToList())
                    WakeUp(sleeper, "The uproar woke up {0}!");
                v.UproarTurns--;
                if (b.Turn.MoveFailed || v.UproarTurns == 0)
                {
                    v.UproarTurns = 0;
                    if (!b.IsHeldToItsMove) v.LockedMove = null;
                    Say($"{b.Name} calmed down.");
                }
                else Say($"{b.Name} is making an uproar!");
            }

            // A rampage ends in confusion, unless it was cut short
            if (b.IsActive && v.RampageTurns > 0)
            {
                v.RampageTurns--;
                if (b.Turn.MoveFailed) v.RampageTurns = 0;
                else if (v.RampageTurns == 0 && !b.IsConfused && Confuse(b, null, false, By.Other))
                    Say($"{b.Name} became confused due to fatigue!");
                if (v.RampageTurns == 0 && !b.IsHeldToItsMove) v.LockedMove = null;
            }

            if (b.IsActive && v.Disabled != null)
            {
                if (p.Moves.All(m => m.Data != v.Disabled)) v.DisableTurns = 0;
                if (v.DisableTurns > 0) v.DisableTurns--;
                else
                {
                    v.Disabled = null;
                    Say($"{b.Name} is disabled no more!");
                }
            }

            if (b.IsActive && v.Encored != null)
            {
                var encored = p.Moves.FirstOrDefault(m => m.Data == v.Encored);
                if (encored == null || encored.CurrentPP == 0) v.EncoreTurns = 0;
                if (v.EncoreTurns > 0) v.EncoreTurns--;
                else
                {
                    v.Encored = null;
                    Say($"{b.Name}'s encore ended!");
                }
            }

            if (RunsOut(ref v.LockOnTurns)) v.LockedOnBy = null;
            if (v.ChargeTurns > 0) v.ChargeTurns--;
            if (b.IsActive)
            {
                if (RunsOut(ref v.TauntTurns)) Say($"{b.Name}'s taunt wore off!");
                if (RunsOut(ref v.MagnetRiseTurns)) Say($"{b.Name}'s magnetism wore off!");
                if (RunsOut(ref v.HealBlockTurns)) Say($"{b.Name} can heal again!");
                if (RunsOut(ref v.EmbargoTurns)) Say($"{b.Name} can use items again!");

                // A yawn's sleep arrives
                if (RunsOut(ref v.YawnTurns)) TryInflictStatus(b, StatusCondition.Sleep, null, false, By.Other);
            }

            // Its berries look at what the turn left it with, and an orb does its harm last
            CheckConditionHooks(b, null);
            if (b.IsActive) ItemOf(b)?.AfterTheTurn(this, b);

            if (!Settle()) return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- what was sent ahead

    private void SideConditions()
    {
        // Future Sight and Doom Desire land where they were aimed, on whoever stands there now
        foreach (var b in speedOrder.ToList())
        {
            var place = Field.At(b.Place);
            if (!RunsOut(ref place.DoomTurns) || !b.IsActive) continue;
            var move = place.DoomMove!;
            Say($"{b.Name} took the {move.Name} attack!");

            // Its accuracy is rolled now, by the stages of whoever stands where it was sent from
            var sender = At(place.DoomFrom);
            int rate = move.Accuracy <= 0 ? 100 : Formulas.HitRate(move.Accuracy,
                sender.IsActive ? sender.Pokemon!.StatStages.GetValueOrDefault(StatType.Accuracy) : 0,
                b.Pokemon!.StatStages.GetValueOrDefault(StatType.Evasion));
            if (b.Turn.Protecting || rng.Roll(RollKind.Accuracy, 100) >= rate)
            {
                Say("But it failed!");
                continue;
            }
            LandAhead(b, place.DoomDamage);
            if (!Settle()) return;
        }

        // A perish count falls; at nothing its Pokémon faints
        foreach (var b in speedOrder.ToList())
        {
            if (!b.IsActive || b.Volatile.PerishCount < 0) continue;
            int count = b.Volatile.PerishCount;
            Say($"{b.Name}'s perish count fell to {count}!");
            if (count == 0)
            {
                b.Volatile.PerishCount = -1;
                b.Pokemon!.CurrentHP = 0;
                Emit(new HpChanged(b.Place, 0, Healed: false));
            }
            else b.Volatile.PerishCount = count - 1;
            if (!Settle()) return;
        }

        if (Field.TrickRoomTurns > 0 && --Field.TrickRoomTurns == 0) Say("The twisted dimensions returned to normal!");
    }

    /// <summary>Damage worked out turns ago arrives (<c>subscript_future_sight_damage</c>): a Substitute takes it if there is one, and a Focus Sash or Band can hold against it.</summary>
    private void LandAhead(Battler b, int damage)
    {
        var v = b.Volatile;
        var p = b.Pokemon!;
        if (b.HasSubstitute)
        {
            v.SubstituteHp = Math.Max(0, v.SubstituteHp - damage);
            var took = Say($"The substitute took damage for {b.Name}!");
            if (v.SubstituteHp == 0)
            {
                took.With(new SubstituteChanged(b.Place, Up: false));
                Say($"{b.Name}'s substitute faded!");
            }
            return;
        }

        int dealt = Math.Min(p.CurrentHP, damage);
        var notes = new List<BattleEvent>();
        if (dealt >= p.CurrentHP)
        {
            bool held = false;
            notes = Capture(() => held = BattleEffects.Of(b).Any(e => e.EnduresHit(this, b, dealt)));
            if (held) dealt = p.CurrentHP - 1;
        }
        p.CurrentHP -= dealt;
        if (v.BideTurns > 0) v.BideDamage += dealt;
        Emit(new Struck(b.Place, p.CurrentHP, Hard: false));
        Emit(new HitSounded(false));
        log.AddRange(notes);
        if (p.CurrentHP > 0 && dealt > 0 && v.Rage && p.StatStages.GetValueOrDefault(StatType.Attack) < 6)
        {
            int stage = p.StatStages.GetValueOrDefault(StatType.Attack) + 1;
            p.StatStages[StatType.Attack] = stage;
            Say($"{b.Name}'s rage is building!").With(new StageChanged(b.Place, StatType.Attack, stage, Rose: true));
        }
        CheckConditionHooks(b, null);
    }

    // ---------------------------------------------------------------- between turns

    /// <summary><c>BattleSystem_SetupNextTurn</c>: a sleeper is in the middle of nothing.</summary>
    private void SetUpNextTurn()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive))
        {
            var v = b.Volatile;
            if (v.Recharging && v.RechargeTurn + 1 < Turn) v.Recharging = false;
            if (b.Pokemon!.Status != StatusCondition.Sleep) continue;
            if (v.Charging) Unlock(b);
            if (v.RampageTurns > 0)
            {
                v.RampageTurns = 0;
                if (!b.IsHeldToItsMove) v.LockedMove = null;
            }
        }
    }
}
