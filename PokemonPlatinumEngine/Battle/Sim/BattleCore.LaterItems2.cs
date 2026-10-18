using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later held items R28a left (plan 06 · R28b), by Pokémon Showdown's data/items.ts and sim/battle.ts at the pinned
// commit: the Ability Shield against whatever would change or take its holder's ability, the Rusted Sword and Shield's
// Crowned forms with Behemoth Blade and Bash, the Legend Plate's Judgment, Booster Energy as the sun or the terrain ends,
// an Eject Pack and a Mirror Herb after what a Pokémon's coming in did (an Intimidate, a Sticky Web), and an Air Balloon
// popped through a Substitute. docs/mechanics/rulings.md, "The later games' held items, medicine and balls", has them.
public sealed partial class BattleCore
{
    // ---------------------------------------------------------------- the Ability Shield

    /// <summary>
    /// Whether its Ability Shield keeps its ability as it is (Showdown's <c>onSetAbility</c> and Gastro Acid's check):
    /// says so when it does. The shield works under Klutz (<c>ignoreKlutz</c>), not under an Embargo or in Magic Room.
    /// </summary>
    private bool ShieldsItsAbility(Battler b)
    {
        if (!LaterItems.AbilityShielded(b)) return false;
        Say(Text.Of("battle.abilityShield", ("pokemon", b.Name), ("item", ItemOf(b))));
        return true;
    }

    // ---------------------------------------------------------------- the Rusted Sword and Shield

    /// <summary>
    /// Zacian holding the Rusted Sword is Zacian-Crowned in a battle, and Zamazenta holding the Rusted Shield
    /// Zamazenta-Crowned (Showdown's battle-only formes with their required items), from the moment it comes in; its
    /// Iron Head is Behemoth Blade or Behemoth Bash, with the PP it had. Both are given back as it leaves, as a shape is.
    /// </summary>
    private void Crown(Battler b)
    {
        var p = b.Pokemon!;
        if (p.Form != null || b.Volatile.Transformed) return;
        (string? form, string? move) = (p.Species.Name, p.HeldItem?.HoldEffect) switch
        {
            ("Zacian", "RustedSword") => ("Zacian-Crowned", "Behemoth Blade"),
            ("Zamazenta", "RustedShield") => ("Zamazenta-Crowned", "Behemoth Bash"),
            _ => (null, null)
        };
        if (form == null || !Become(b, form)) return;
        int slot = p.Moves.FindIndex(m => m.Name == "Iron Head");
        if (slot >= 0 && MoveDatabase.Get(move!) is { } crowned)
        {
            var o = Keep(b);
            o.MoveSlots ??= new Dictionary<int, (MoveData, int, int)>();
            o.MoveSlots.TryAdd(slot, (p.Moves[slot].Data, p.Moves[slot].CurrentPP, p.Moves[slot].PPUps));
            p.Moves[slot] = new Move(crowned, Math.Min(p.Moves[slot].CurrentPP, crowned.MaxPP), p.Moves[slot].PPUps);
        }
        Emit(new Reshaped(b.Place));
    }

    // ---------------------------------------------------------------- the Legend Plate

    private static readonly PokemonType[] JudgedTypes =
    {
        PokemonType.Normal, PokemonType.Fire, PokemonType.Water, PokemonType.Grass, PokemonType.Electric, PokemonType.Ice,
        PokemonType.Fighting, PokemonType.Poison, PokemonType.Ground, PokemonType.Flying, PokemonType.Psychic, PokemonType.Bug,
        PokemonType.Rock, PokemonType.Ghost, PokemonType.Dragon, PokemonType.Dark, PokemonType.Steel, PokemonType.Fairy
    };

    /// <summary>
    /// The Legend Plate (Legends: Arceus's; Showdown has no battle of it): Judgment takes the type that does the most to
    /// its first target, the first in the types' order where two do as much; none with nobody to aim at.
    /// </summary>
    private PokemonType? LegendPlateType(MoveUse use)
    {
        var target = use.Targets.FirstOrDefault(t => t != use.User && t.IsActive);
        if (target == null) return null;
        var types = target.Types;
        if (types.Count == 0) return null;
        var second = types.Count > 1 ? types[1] : (PokemonType?)null;
        return JudgedTypes.OrderByDescending(t => TypeChart.GetEffectiveness(t, types[0], second, Rules, Field.Inverse)).First();
    }

    // ---------------------------------------------------------------- Booster Energy

    /// <summary>
    /// The sun or the Electric Terrain has gone (Showdown's Booster Energy <c>onUpdate</c>): a Protosynthesis or Quark
    /// Drive whose holder holds Booster Energy is set going by it at once, where R28a set it going only as the holder came in.
    /// </summary>
    private void BoostersWake()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive).ToList())
            if (BattleEffects.ItemOf(b) is BoosterEnergy booster) booster.OnEntry(this, b);
    }

    // ---------------------------------------------------------------- an Eject Pack and a Mirror Herb outside a move

    /// <summary>
    /// What a Pokémon's coming in did (an Intimidate, a Sticky Web, a foe's Download or Intrepid Sword) once everyone
    /// has come in and the switch-in checks are done (Showdown's <c>onAnySwitchIn</c>): a Mirror Herb takes on the stages
    /// its foe gained, and an Eject Pack's holder whose stage fell leaves, its side choosing who comes in. Outside a move
    /// alone; what a move did waits for its own end (<see cref="ItemsAfterTheMove"/>).
    /// </summary>
    private IEnumerable<BattleRequest> ItemsAfterTheEntry()
    {
        // Each leaving brings someone in, whose coming in may set off another: never more than the field holds
        for (int round = 0; round < 8 && !itemsArmed && Result == BattleResult.None && (mirrored.Count > 0 || ejectPacks.Count > 0); round++)
        {
            foreach (var holder in mirrored.Select(m => m.Holder).Distinct().ToList())
            {
                if (!holder.IsActive || !Holds(holder, "MirrorHerb")) continue;
                Say(Text.Of("battle.mirroredStats", ("pokemon", holder.Name), ("item", ItemAtWork(holder))));
                ConsumeItem(holder);
                foreach (var (_, stat, stages) in mirrored.Where(m => m.Holder == holder).ToList()) ChangeStat(holder, stat, stages, holder, false, By.Other);
            }
            mirrored.Clear();
            var packs = ejectPacks.ToList();
            ejectPacks.Clear();
            foreach (var holder in packs)
            {
                if (!holder.IsActive || !Holds(holder, "EjectPack") || !HasReplacement(holder)) continue;
                Say(Text.Of("battle.ejectPack", ("pokemon", holder.Name), ("item", ItemAtWork(holder))));
                ConsumeItem(holder);
                foreach (var request in SwitchOut(holder)) yield return request;
                break;
            }
        }
        mirrored.Clear();
        ejectPacks.Clear();
    }

    // ---------------------------------------------------------------- an Air Balloon behind a Substitute

    /// <summary>A hit its Substitute took pops its holder's Air Balloon all the same (Showdown's <c>onAfterSubDamage</c>).</summary>
    private void PopUnderTheDoll(MoveHit hit)
    {
        var t = hit.Target;
        if (!t.IsActive || !Holds(t, "AirBalloon")) return;
        Say(Text.Of("battle.balloonPopped", ("pokemon", t.Name), ("item", ItemOf(t))));
        ConsumeItem(t);
    }
}
