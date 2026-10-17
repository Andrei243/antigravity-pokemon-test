using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The shapes the later games' Pokémon change into in the middle of a battle (plan 06 · R19b; docs/mechanics/
// rulings.md, "Shapes that change in battle"): what BattleCore.CheckShapes grew into. Each goes by its ability's name,
// as the original's own shapes go by species (Showdown's data/abilities.ts and data/moves.ts at the pinned commit:
// stancechange, zenmode, schooling, shieldsdown, disguise, gulpmissile, iceface, hungerswitch, zerotohero, and
// relicsong's own). None of Platinum's species has any of these abilities, so its battles never come here.
//
// Two kinds. A passing shape (Aegislash's blade, Darmanitan's Zen Mode, Meloetta's Pirouette, Wishiwashi's school,
// Minior's core, Cramorant's catch, Morpeko's hunger) is kept the first time in Volatiles.Original and given back by
// Revert when the Pokémon leaves the field or the battle ends, as Showdown's formeChange that isn't permanent is. A
// lasting one (Mimikyu busted, Eiscue's face broken, Palafin the hero) stays through switching and is given back as
// the battle ends (lastingShapes). Either takes the form's own stats (not its HP, which no such form changes).
public sealed partial class BattleCore
{
    /// <summary>Each Pokémon a lasting shape was given to this battle, with the form it is given back as the battle ends.</summary>
    private readonly Dictionary<Pokemon, string?> lastingShapes = new();

    /// <summary>The Palafin told of their heroic transformation (once a battle, as Showdown's heroMessageDisplayed).</summary>
    private readonly HashSet<Pokemon> heroesTold = new();

    /// <summary>
    /// Takes the Pokémon into a form of its species for the battle (null: the species' own), with the form's stats.
    /// False when it is in that form already, or its species has no such form.
    /// </summary>
    private bool Become(Battler b, string? form, bool lasting = false)
    {
        var p = b.Pokemon!;
        if (p.Form == form || form != null && p.Species.Form(form) == null) return false;
        if (lasting) lastingShapes.TryAdd(p, p.Form);
        else
        {
            var o = Keep(b);
            if (o.Species == null)
            {
                o.Species = p.Species;
                o.Form = p.Form;
            }
            if (!o.StatsKept)
            {
                o.StatsKept = true;
                (o.Attack, o.Defense, o.SpAttack, o.SpDefense, o.Speed) = (p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed);
            }
        }
        p.Form = form;
        p.RecalculateStats();
        p.CurrentHP = Math.Min(p.CurrentHP, p.MaxHP);
        return true;
    }

    /// <summary>Says a shape's line with the event the screen takes the new shape from.</summary>
    private void Reshape(Battler b, string? form, string line, bool lasting = false)
    {
        if (Become(b, form, lasting)) Say(line).With(new Reshaped(b.Place));
    }

    private static bool Is(Battler b, string species, string ability) =>
        b.Pokemon!.Species.Name == species && Has(b, ability) && !b.Volatile.Transformed;

    /// <summary>The lasting shapes go back to what they were as the battle ends (Disguise, Ice Face, Zero to Hero).</summary>
    private void GiveBackLastingShapes()
    {
        foreach (var (pokemon, form) in lastingShapes)
        {
            pokemon.Form = form;
            pokemon.RecalculateStats();
            pokemon.CurrentHP = Math.Min(pokemon.CurrentHP, pokemon.MaxHP);
        }
        lastingShapes.Clear();
    }

    // ---------------------------------------------------------------- as it comes in

    /// <summary>
    /// The later shapes a Pokémon takes as it comes in, once a stay (Showdown's onStart): Wishiwashi schools and
    /// Minior's shell holds above their share of HP, Eiscue's face forms again in the snow, and a Palafin back from
    /// its first time out is a hero.
    /// </summary>
    private void ShapeOnEntry(Battler b)
    {
        var v = b.Volatile;
        if ((v.Announced & EntryCheck.Shape) != 0) return;
        v.Announced |= EntryCheck.Shape;
        var p = b.Pokemon!;
        if (Is(b, "Palafin", "Zero to Hero") && p.Form == "Palafin-Hero")
        {
            var said = heroesTold.Add(p) ? Say(Text.Of("battle.heroicTransformation", ("pokemon", b.Name))) : null;
            if (said != null) said.With(new Reshaped(b.Place));
            else Emit(new Reshaped(b.Place));
        }
        if (Is(b, "Eiscue", "Ice Face") && p.Form == "Eiscue-Noice" && Field.WeatherInEffect == BattleWeather.Hail)
            Reshape(b, null, Text.Of("battle.iceFaceRestored", ("pokemon", b.Name)), lasting: true);
        HpShapes(b);
    }

    /// <summary>Schooling and Shields Down, which ask the share of HP as the Pokémon comes in and at every turn's end.</summary>
    private void HpShapes(Battler b)
    {
        var p = b.Pokemon!;
        if (Is(b, "Wishiwashi", "Schooling") && p.Level >= 20)
        {
            if (p.CurrentHP * 4 > p.MaxHP) Reshape(b, "Wishiwashi-School", Text.Of("battle.formedASchool", ("pokemon", b.Name)));
            else Reshape(b, null, Text.Of("battle.stoppedSchooling", ("pokemon", b.Name)));
        }
        if (Is(b, "Minior", "Shields Down"))
        {
            bool meteor = p.Form == null || p.Form.EndsWith("-Meteor");
            if (p.CurrentHP * 2 > p.MaxHP && !meteor) Reshape(b, MeteorOf(p.Form), Text.Of("battle.shieldsWentUp", ("pokemon", b.Name)));
            else if (p.CurrentHP * 2 <= p.MaxHP && meteor) Reshape(b, CoreOf(p.Form), Text.Of("battle.shieldsWentDown", ("pokemon", b.Name)));
        }
    }

    /// <summary>Minior's core under its shell: the species' own shell is the red core's, "Minior-Blue-Meteor" the blue's.</summary>
    internal static string CoreOf(string? meteor) => meteor == null ? "Minior-Red" : meteor[..^"-Meteor".Length];

    internal static string? MeteorOf(string? core) => core is null or "Minior-Red" ? null : core + "-Meteor";

    // ---------------------------------------------------------------- at the turn's end

    /// <summary>Zen Mode, Schooling, Shields Down and Hunger Switch at the end of each turn (Showdown's onResidual, order 29).</summary>
    private void ShapeAtTurnEnd(Battler b)
    {
        var p = b.Pokemon!;
        if (Is(b, "Darmanitan", "Zen Mode"))
        {
            bool galar = p.Form is "Darmanitan-Galar-Standard" or "Darmanitan-Galar-Zen";
            bool zen = p.Form is "Darmanitan-Zen" or "Darmanitan-Galar-Zen";
            if (p.CurrentHP * 2 <= p.MaxHP && !zen)
                Reshape(b, galar ? "Darmanitan-Galar-Zen" : "Darmanitan-Zen", Text.Of("battle.zenModeTriggered", ("pokemon", b.Name)));
            else if (p.CurrentHP * 2 > p.MaxHP && zen)
                Reshape(b, galar ? "Darmanitan-Galar-Standard" : null, Text.Of("battle.zenModeEnded", ("pokemon", b.Name)));
        }
        HpShapes(b);
        // Hunger Switch turns Morpeko about every turn, with nothing said (Showdown's hungerswitch onResidual)
        if (Is(b, "Morpeko", "Hunger Switch") && Become(b, p.Form == "Morpeko-Hangry" ? null : "Morpeko-Hangry")) Emit(new Reshaped(b.Place));
    }

    // ---------------------------------------------------------------- moves

    /// <summary>
    /// Stance Change as a move begins (Showdown's stancechange onModifyMove): Aegislash draws its blade for any move
    /// that hits and sheathes it for King's Shield, before its line.
    /// </summary>
    private void StanceChange(Battler user, Move move)
    {
        if (!Is(user, "Aegislash", "Stance Change")) return;
        if (move.Category != MoveCategory.Status) Reshape(user, "Aegislash-Blade", Text.Of("battle.changedToBladeForme", ("pokemon", user.Name)));
        else if (move.Data.Name == "King's Shield") Reshape(user, null, Text.Of("battle.changedToShieldForme", ("pokemon", user.Name)));
    }

    /// <summary>Relic Song that landed turns Meloetta between its two formes (Showdown's relicsong onAfterMoveSecondarySelf).</summary>
    private void Sings(MoveUse use)
    {
        var user = use.User;
        if (use.Data.Name != "Relic Song" || !user.IsActive || user.Pokemon!.Species.Name != "Meloetta" || user.Volatile.Transformed) return;
        if (!use.Hits.Any(h => h.Landed)) return;
        Reshape(user, user.Pokemon.Form == "Meloetta-Pirouette" ? null : "Meloetta-Pirouette", Text.Of("battle.transformed", ("pokemon", user.Name)));
    }

    /// <summary>
    /// Gulp Missile: Surf, or Dive's plunge, and Cramorant comes up with something in its beak, an Arrokuda above
    /// half its HP and a Pikachu at half or under (Showdown's gulpmissile onSourceTryPrimaryHit and dive's onTryMove).
    /// </summary>
    private void Gulp(Battler user, MoveData move)
    {
        if (move.Name is not ("Surf" or "Dive") || !Is(user, "Cramorant", "Gulp Missile")) return;
        var p = user.Pokemon!;
        if (p.Form != null) return;
        if (Become(user, p.CurrentHP * 2 <= p.MaxHP ? "Cramorant-Gorging" : "Cramorant-Gulping")) Emit(new Reshaped(user.Place));
    }

    /// <summary>
    /// A Cramorant with its catch spits it at whoever hit it (Showdown's gulpmissile onDamagingHit): a quarter of the
    /// attacker's HP, and its Defense down a stage for an Arrokuda or paralysis for a Pikachu.
    /// </summary>
    private void SpitsTheCatch(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        var attacker = use.User;
        if (hit.IntoSubstitute || hit.LastDealt <= 0 || t == attacker || !t.IsActive || t.IsElsewhere || !attacker.IsActive) return;
        if (t.Pokemon!.Species.Name != "Cramorant" || t.Pokemon.Form is not ("Cramorant-Gulping" or "Cramorant-Gorging")) return;
        bool gorging = t.Pokemon.Form == "Cramorant-Gorging";
        hit.Notes.AddRange(Capture(() =>
        {
            Become(t, null);
            Say(Text.Of("battle.spatOutItsCatch", ("pokemon", t.Name))).With(new Reshaped(t.Place));
            LoseHp(attacker, Math.Max(1, attacker.Pokemon!.MaxHP / 4), Text.Of("battle.wasHitByTheCatch", ("pokemon", attacker.Name)));
            if (!attacker.IsActive || attacker.Pokemon.CurrentHP <= 0) return;
            if (gorging) TryInflictStatus(attacker, StatusCondition.Paralyze, t, false, By.Ability);
            else ChangeStat(attacker, StatType.Defense, -1, t, false, By.Ability);
        }));
    }

    /// <summary>
    /// A disguise or an icy face takes a hit in the Pokémon's place and breaks (Showdown's disguise and iceface
    /// onDamage): Mimikyu's any move's, losing an eighth of its HP for it; Eiscue's a physical move's. Mold Breaker
    /// goes past both. True when the hit was taken so.
    /// </summary>
    private bool ShapeTakesTheHit(MoveUse use, MoveHit hit, bool userBreaks)
    {
        var t = hit.Target;
        if (userBreaks || t == use.User || hit.OneHitKo) return false;
        var p = t.Pokemon!;
        if (Is(t, "Mimikyu", "Disguise") && p.Form == null)
        {
            // Nothing is said of how the type did against it (Showdown's disguise onEffectiveness)
            hit.SuperEffective = hit.NotVeryEffective = false;
            hit.Notes.AddRange(Capture(() =>
            {
                Become(t, "Mimikyu-Busted", lasting: true);
                Say(Text.Of("battle.disguiseServedAsADecoy")).With(new Reshaped(t.Place));
                LoseHp(t, Math.Max(1, p.MaxHP / 8), Text.Of("battle.disguiseWasBusted", ("pokemon", t.Name)));
            }));
            return true;
        }
        if (Is(t, "Eiscue", "Ice Face") && p.Form == null && use.Move.Category == MoveCategory.Physical)
        {
            hit.Notes.AddRange(Capture(() =>
            {
                Become(t, "Eiscue-Noice", lasting: true);
                Say(Text.Of("battle.iceFaceBroke", ("pokemon", t.Name))).With(new Reshaped(t.Place));
            }));
            return true;
        }
        return false;
    }

    /// <summary>Hail or snow come in: a broken icy face forms again (Showdown's iceface onWeatherChange).</summary>
    private void IceFaceInTheSnow()
    {
        if (Field.WeatherInEffect != BattleWeather.Hail) return;
        // In the places' order: sorting by Speed may roll for a tie, and a weather of Platinum's must roll nothing more
        foreach (var b in AllBattlers.Where(b => b.IsActive && Is(b, "Eiscue", "Ice Face") && b.Pokemon!.Form == "Eiscue-Noice").ToList())
            Reshape(b, null, Text.Of("battle.iceFaceRestored", ("pokemon", b.Name)), lasting: true);
    }

    /// <summary>Zero to Hero: a Palafin leaving in its Zero Form is a hero from then on (Showdown's zerotohero onSwitchOut).</summary>
    private void LeavesAHero(Battler place)
    {
        if (!place.IsActive || !Is(place, "Palafin", "Zero to Hero") || place.Pokemon!.Form == "Palafin-Hero") return;
        Become(place, "Palafin-Hero", lasting: true);
    }
}
