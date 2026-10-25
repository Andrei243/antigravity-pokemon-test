using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Mega Evolution and Primal Reversion (plan 06 · R20; docs/mechanics/rulings.md, "Mega Evolution"), the first of the
// four later mechanics and the pattern R21 to R23 follow: a flag on the move's choice (BattleChoice.Mega), refused by
// WhyNot where it can't be; the change carried out at its moment of the turn, in the core alone, with its line and an
// event of its own on the line (MegaEvolved); the trainer AI's use of it (TrainerAi.WithTheLaterMechanics); all of it
// behind the Ruleset (Ruleset.MegaEvolution), so a battle of Platinum's rules never comes here. The rules are Pokémon
// Showdown's at the pinned commit: sim/battle-actions.ts (canMegaEvo, runMegaEvo), sim/battle-queue.ts (a Mega
// Evolution comes after the switches and before the moves, by Speed), sim/battle.ts (from Generation 8 the turn's
// order is sorted again by the Speed a Mega Evolution gave), sim/side.ts (once a battle for each trainer) and
// data/items.ts (the stones' and the orbs' onTakeItem, the orbs' onSwitchIn).
public sealed partial class BattleCore
{
    /// <summary>The item in the bag that lets the player's Pokémon Mega Evolve, by name.</summary>
    public const string KeyStoneItem = "Key Stone";

    /// <summary>The player has a Key Stone (<see cref="CoreSetup.KeyStone"/>).</summary>
    public bool KeyStone { get; }

    /// <summary>Whoever has Mega Evolved this battle: a trainer, or the player for their own Pokémon (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> megaUsed = new();

    /// <summary>The trainers whose Mega Evolution has been chosen for the turn being chosen.</summary>
    private readonly HashSet<object> megaChosen = new();

    /// <summary>The form and ability each Pokémon had before a Mega Evolution or a Primal Reversion: given back as the battle ends.</summary>
    private readonly Dictionary<Pokemon, (string? Form, string? Ability)> beforeMega = new();

    /// <summary>The player's own side, as the one who Mega Evolves for their own Pokémon.</summary>
    private static readonly object PlayersOwn = new();

    /// <summary>Who a Mega Evolution counts against: the place's trainer, the player for their own; a wild Pokémon is its own and has no Key Stone.</summary>
    private static object MegaKey(Battler b) => (object?)b.Trainer ?? (b.IsPlayerSide ? PlayersOwn : b);

    /// <summary>The trainer of the place has a Key Stone: the player's own (<see cref="KeyStone"/>), a trainer's or a partner's.</summary>
    private bool HasKeyStone(Battler b) => b.Trainer != null ? b.Trainer.KeyStone : b.IsPlayerSide ? KeyStone : MegaMaxDen;

    /// <summary>
    /// The Mega form a Pokémon would take, whatever the battle: a Rayquaza that knows Dragon Ascent and holds no
    /// Z-Crystal, or one holding its own species' Mega Stone (a stone for one form of its species, the Eternal Flower's,
    /// only in that form); null for none, and for one already in a Mega or Primal form (Showdown's <c>canMegaEvo</c>).
    /// </summary>
    public static string? MegaFormOf(Pokemon p)
    {
        if (p.IsEgg || p.FormData?.Kind is FormKind.Mega or FormKind.Primal or FormKind.MegaDynamax) return null;
        if (p.Species.Name == "Rayquaza" && p.HeldItem?.ZCrystal == null && p.Moves.Any(m => m.Name == "Dragon Ascent") && p.Species.Form("Rayquaza-Mega") != null)
            return "Rayquaza-Mega";
        if (p.HeldItem?.MegaStone is not { } stone || stone.Species != p.Species.Name) return null;
        if (stone.HeldByForm != null && p.Form != stone.HeldByForm) return null;
        return p.Species.Form(stone.Form) != null ? stone.Form : null;
    }

    /// <summary>The primal form a Kyogre holding the Blue Orb or a Groudon holding the Red Orb takes as it comes in; null for anyone else.</summary>
    public static string? PrimalFormOf(Pokemon p)
    {
        if (p.IsEgg || p.FormData?.Kind == FormKind.Primal) return null;
        string? form = (p.Species.Name, p.HeldItem?.Name) switch
        {
            ("Kyogre", "Blue Orb") => "Kyogre-Primal",
            ("Groudon", "Red Orb") => "Groudon-Primal",
            _ => null
        };
        return form != null && p.Species.Form(form) != null ? form : null;
    }

    /// <summary>
    /// An item that never leaves the hands it is in, nor goes into them by a move (Showdown's <c>onTakeItem</c> of the
    /// Mega Stones and the orbs): a Mega Stone held by its own species, the Blue Orb by a Kyogre, the Red Orb by a
    /// Groudon. Knock Off, Thief, Covet, Trick, Bestow and Fling ask it of both hands; only rules with Mega
    /// Evolution have it.
    /// </summary>
    private bool ClingsTo(ItemData? item, Pokemon? holder)
    {
        // A Z-Crystal in anyone's hands (plan 06 · R21; the crystals' onTakeItem is false)
        if (Rules.ZMoves && item?.ZCrystal != null && holder != null) return true;
        if (!Rules.MegaEvolution || item == null || holder == null) return false;
        if (item.MegaStone is { } stone) return stone.Species == holder.Species.Name;
        return (item.Name, holder.Species.Name) is ("Blue Orb", "Kyogre") or ("Red Orb", "Groudon");
    }

    /// <summary>
    /// Why the Pokémon in a place can't Mega Evolve with its move this turn; null when it can. Under rules without it,
    /// with no Key Stone, transformed, without its stone, or once its trainer has Mega Evolved (or chosen to this turn).
    /// </summary>
    public string? WhyNotMega(Battler b)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantMegaEvolve", ("pokemon", mine.Name));
        if (!Rules.MegaEvolution || mine.Pokemon == null || !HasKeyStone(mine) || mine.Volatile.Transformed || MegaFormOf(mine.Pokemon) == null) return no;
        if (megaUsed.Contains(MegaKey(mine))) return Text.Of("battle.megaOnlyOnce");
        return null;
    }

    /// <summary>Whether the Pokémon in a place may Mega Evolve with the move it chooses now (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanMegaEvolve(Battler b) => WhyNotMega(b) == null && !megaChosen.Contains(MegaKey(At(b.Place)));

    /// <summary>Notes the Mega Evolutions an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseMegas(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen.Where(c => c.Mega))
            if (!megaChosen.Add(MegaKey(At(choice.Who)))) throw new ArgumentException(Text.Of("battle.megaOnlyOnce"));
        // And the Z-Moves and Ultra Bursts (plan 06 · R21)
        ChooseZ(chosen);
        // And the Dynamaxes (plan 06 · R22)
        ChooseDynamax(chosen);
        // And the Terastallizations (plan 06 · R23)
        ChooseTera(chosen);
    }

    /// <summary>
    /// The turn's Mega Evolutions, once its switches and items are done and before its first move (Showdown's
    /// <c>megaEvo</c> action, order 104, after <c>switch</c>'s 103): every Pokémon still on the field whose move came
    /// with one, the fastest first. True when anyone did, so that the moves still to come are put in order again by
    /// the new Speeds (Showdown's queue sorted again before each move from Generation 8).
    /// </summary>
    private bool MegaEvolutions(IReadOnlyList<Act> acts)
    {
        megaChosen.Clear();
        // An Ultra Burst comes with them (plan 06 · R21: Showdown's megaEvo action runs both)
        var megas = acts.Where(a => a.Choice is { Kind: ChoiceKind.Fight } c && (c.Mega || c.UltraBurst) && a.User.IsActive && a.User.Pokemon == a.Actor).ToList();
        if (megas.Count == 0) return false;
        bool any = false;
        foreach (var b in BySpeed().Where(b => megas.Any(a => a.User == b)))
        {
            if (Result != BattleResult.None) break;
            if (!b.IsActive) continue;
            if (megas.First(a => a.User == b).Choice.UltraBurst)
            {
                if (WhyNotUltraBurst(b) != null) continue;
                UltraBurstNow(b);
            }
            else
            {
                if (WhyNotMega(b) != null) continue;
                MegaEvolve(b);
            }
            any = true;
        }
        return any;
    }

    /// <summary>
    /// <c>runMegaEvo</c>: the stone's line (or Rayquaza's, its trainer's wish), the form taken with its ability, its
    /// stats and its types, the Mega Evolution spent for the trainer, and the new ability acting as it would on entry.
    /// </summary>
    private void MegaEvolve(Battler b)
    {
        var p = b.Pokemon!;
        string form = MegaFormOf(p)!;
        string holder = b.Trainer != null ? b.Trainer.FullTitle : playerName;
        // A den's wild boss has no trainer: its stone reacts to the den (plan 06 · R20c)
        if (b.Trainer == null && !b.IsPlayerSide) Say(Text.Of("battle.megaReactingDen", ("pokemon", b.Name), ("item", ItemOf(b))));
        else if (p.HeldItem?.MegaStone == null) Say(Text.Of("battle.megaWish", ("trainer", holder), ("pokemon", b.Name)));
        else Say(Text.Of("battle.megaReacting", ("pokemon", b.Name), ("item", ItemOf(b)), ("trainer", holder)));
        TakeForm(b, form);
        megaUsed.Add(MegaKey(b));
        Say(Text.Of("battle.megaEvolved", ("pokemon", b.Name), ("species", MegaName(p, form)))).With(new MegaEvolved(b.Place, form, Primal: false));
        // The new ability acts as one does on entry (Showdown's setAbility starts it), and once only
        b.Volatile.Announced &= EntryCheck.Item | EntryCheck.Shape;
        SwitchInChecks();
    }

    /// <summary>
    /// Primal Reversion as a Kyogre or a Groudon holding its orb comes in (the orbs' <c>onSwitchIn</c>), before any
    /// ability acts on entry, so that its new ability is the one that does. It spends no Mega Evolution.
    /// </summary>
    private void PrimalReversions(IEnumerable<Battler> order)
    {
        if (!Rules.MegaEvolution) return;
        foreach (var b in order)
        {
            if (!b.IsActive || b.Volatile.Transformed || PrimalFormOf(b.Pokemon!) is not { } form) continue;
            TakeForm(b, form);
            Say(Text.Of("battle.primalReversion", ("pokemon", b.Name))).With(new MegaEvolved(b.Place, form, Primal: true));
        }
    }

    /// <summary>
    /// The form taken for the rest of the battle, through switches (Showdown's permanent <c>formeChange</c>): its ability
    /// in the form's first slot, its stats and its types; whatever a move had changed of its types goes, and what is
    /// put back as it leaves the field is the new form's ability and stats.
    /// </summary>
    private void TakeForm(Battler b, string form)
    {
        var p = b.Pokemon!;
        beforeMega.TryAdd(p, (p.Form, p.AbilityName));
        p.ChangeForm(form);
        b.Volatile.Types = null;
        b.Volatile.AddedType = null;
        if (b.Volatile.Original is { } o)
        {
            if (o.AbilityKept) o.AbilityName = p.AbilityName;
            if (o.StatsKept) (o.Attack, o.Defense, o.SpAttack, o.SpDefense, o.Speed) = (p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed);
        }
    }

    /// <summary>A Mega form's name as the line says it after "Mega": the species in the language of the moment, and an X or a Y where the form has one.</summary>
    private static string MegaName(Pokemon p, string form)
    {
        string species = LocalNames.Species(p.Species.Name);
        int mega = form.IndexOf("-Mega", StringComparison.Ordinal);
        string rest = mega < 0 ? "" : form[(mega + "-Mega".Length)..].TrimStart('-');
        return rest.Length == 0 ? species : $"{species} {rest}";
    }

    /// <summary>Every Mega and primal form goes back to what it was as the battle ends (Showdown's <c>formeRegression</c>).</summary>
    private void GiveBackMegas()
    {
        foreach (var (pokemon, (form, ability)) in beforeMega)
        {
            pokemon.ChangeForm(form);
            pokemon.AbilityName = ability;
        }
        beforeMega.Clear();
    }
}
