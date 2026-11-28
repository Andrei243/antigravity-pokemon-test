using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The winter masks (plan 46 · CP13, our own; docs/mechanics/rulings.md, "The winter masks"), Carpathia's second gimmick,
// on the Tales' pattern: a mask's id on the move's choice (BattleChoice.Mask), refused by WhyNotMask where it can't be;
// put on at its moment of the turn (after the tales, the fastest first), with its line and an event of its own (MaskWorn);
// the trainer AI's use of it (TrainerAi.MaskNow); all of it behind the Ruleset (Ruleset.Masks) and the run's "No later
// mechanics", so a battle of Platinum's rules, or with no mask on either side, never comes here and is what it was to
// the bit. A trainer masks one Pokémon a battle (decision 15). A mask lasts through switches to the battle's end, kept on
// the Pokémon (Pokemon.WornMask) as a Terastallization is: its second type (Battler.Types) and its ability (kept like
// Embody Aspect's, given back as the battle ends) are the mask's, and its role is played where the field's rules already
// bite: the bear through Follow Me's draw (ResolveTargets' Centre), the goat through Spectral Thief's (StealBoosts), the
// horse through U-turn's way out (Leave), the stag at the turn's end beside War Cry. One mechanic a Pokémon: a masked
// Pokémon neither Mega Evolves, Ultra Bursts, Dynamaxes, Terastallizes nor wakes to a tale, and one that has can't be masked.
public sealed partial class BattleCore
{
    /// <summary>The winter masks the player holds (<see cref="CoreSetup.Masks"/>): null with none.</summary>
    public IReadOnlyList<string>? PlayerMasks { get; }

    /// <summary>Whoever has masked a Pokémon this battle, and whoever has chosen to this turn (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> maskUsed = new(), maskChosen = new();

    /// <summary>Each masked Pokémon and the ability it had before: given back as the battle ends.</summary>
    private readonly Dictionary<Pokemon, string?> beforeMask = new();

    /// <summary>How many turn ends each stag has led its side's dance (<see cref="WinterMasks.StagRounds"/> at most).</summary>
    private readonly Dictionary<Pokemon, int> stagRounds = new();

    /// <summary>The masks a place's trainer holds: a trainer's own, the player's; null for none.</summary>
    private IReadOnlyList<string>? MasksOf(Battler b) => b.Trainer != null ? b.Trainer.Masks : b.IsPlayerSide ? PlayerMasks : null;

    /// <summary>The mask the Pokémon in a place wears, or null.</summary>
    public static WinterMask? MaskOf(Battler b) => WinterMasks.Get(b.Pokemon?.WornMask);

    // ---------------------------------------------------------------- what may be chosen

    /// <summary>
    /// Why the trainer of a place can't put <paramref name="mask"/> on its Pokémon with its move this turn; null when it
    /// can. Under rules without the masks, under a run with no later mechanics, with the mask not held, once the trainer
    /// has masked a Pokémon, or for a Pokémon masked already, transformed, Dynamaxed, Terastallized, in a Tale Form or in
    /// a Mega, primal or Ultra form.
    /// </summary>
    public string? WhyNotMask(Battler b, string? mask)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantWearMask", ("pokemon", mine.Name));
        if (!Rules.Masks || LaterMechanicsBarred || mask == null || mine.Pokemon is not { IsEgg: false } p) return no;
        if (MasksOf(mine) is not { } held || !held.Contains(mask) || WinterMasks.Get(mask) == null) return no;
        if (maskUsed.Contains(MegaKey(mine))) return Text.Of("battle.maskOnlyOnce");
        if (p.WornMask != null || mine.Volatile.Transformed || mine.Volatile.Dynamaxed || p.Terastallized != null || beforeTale.ContainsKey(p)) return no;
        if (p.FormData?.Kind is FormKind.Mega or FormKind.Primal or FormKind.MegaDynamax || p.Form == UltraForm) return no;
        return null;
    }

    /// <summary>Whether the trainer of a place may put this mask on with the move it chooses now (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanWearMask(Battler b, string mask) => WhyNotMask(b, mask) == null && !maskChosen.Contains(MegaKey(At(b.Place)));

    /// <summary>The masks the Pokémon in a place could put on now, in the dances' order (what the move menu's MASK button turns through).</summary>
    public IReadOnlyList<string> MasksToWear(Battler b) =>
        MasksOf(At(b.Place)) is { } held ? WinterMasks.All.Select(m => m.Id).Where(id => held.Contains(id) && CanWearMask(b, id)).ToList() : Array.Empty<string>();

    /// <summary>A choice that masks: with a move, alone of the transforming mechanics, and where <see cref="WhyNotMask"/> lets it.</summary>
    private string? WhyNotMaskChoice(BattleChoice choice, Battler b)
    {
        if (choice.Kind != ChoiceKind.Fight || choice.Mega || choice.UltraBurst || choice.Dynamax || choice.Terastallize)
            return Text.Of("battle.cantWearMask", ("pokemon", b.Name));
        return WhyNotMask(b, choice.Mask);
    }

    /// <summary>What a chooser inside may not have: a mask the rules don't let it put on.</summary>
    private BattleChoice AsTheMaskRulesLet(Battler b, BattleChoice choice) =>
        choice.Mask != null && (WhyNotMaskChoice(choice, b) != null || !CanWearMask(b, choice.Mask)) ? choice with { Mask = null } : choice;

    /// <summary>Two masks of one trainer in one answer are refused.</summary>
    private void RefuseTwoMasks(IReadOnlyList<BattleChoice> choices)
    {
        if (choices.Where(c => c.Mask != null).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.maskOnlyOnce"));
    }

    /// <summary>Notes the masks an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseMask(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen.Where(c => c.Mask != null))
            if (!maskChosen.Add(MegaKey(At(choice.Who)))) throw new ArgumentException(Text.Of("battle.maskOnlyOnce"));
    }

    // ---------------------------------------------------------------- putting it on

    /// <summary>
    /// The turn's masks, after its tales, the fastest first: every Pokémon still on the field whose move came with one.
    /// True when anyone put one on, so that the moves still to come are put in order again (a goat's Prankster).
    /// </summary>
    private bool WearMasks(IReadOnlyList<Act> acts)
    {
        maskChosen.Clear();
        var those = acts.Where(a => a.Choice is { Kind: ChoiceKind.Fight, Mask: not null } && a.User.IsActive && a.User.Pokemon == a.Actor).ToList();
        if (those.Count == 0) return false;
        bool any = false;
        foreach (var b in BySpeed().Where(b => those.Any(a => a.User == b)))
        {
            if (Result != BattleResult.None) break;
            string mask = those.First(a => a.User == b).Choice.Mask!;
            if (!b.IsActive || WhyNotMask(b, mask) != null) continue;
            Wear(b, mask);
            any = true;
        }
        return any;
    }

    /// <summary>
    /// A mask put on: spent for its trainer, kept on the Pokémon with the ability it had, the mask's ability taken (a
    /// copy kept for the battle is the mask's too, so a switch keeps it), the line with its event, and the role's line
    /// naming the ability. A bear draws the foes' moves from this turn on.
    /// </summary>
    private void Wear(Battler b, string id)
    {
        var mask = WinterMasks.Get(id)!;
        var p = b.Pokemon!;
        maskUsed.Add(MegaKey(b));
        beforeMask[p] = p.AbilityName;
        p.WornMask = id;
        p.AbilityName = mask.Ability;
        if (b.Volatile.Original is { AbilityKept: true } o) o.AbilityName = mask.Ability;
        string who = b.Trainer != null ? b.Trainer.FullTitle : playerName;
        Say(Text.Of("battle.maskWorn", ("trainer", who), ("mask", WinterMasks.NameOf(id)), ("pokemon", b.Name))).With(new MaskWorn(b.Place, id));
        Say(Text.Of("battle.maskRole", ("pokemon", b.Name), ("role", WinterMasks.RoleOf(id)), ("ability", AbilityOf(b))));
        if (id == WinterMasks.Bear) Field.Side(b.Side).FollowMe ??= b.Place;
    }

    /// <summary>Every mask comes off with the battle, with nothing said: each wearer's ability is given back.</summary>
    private void TakeOffMasks()
    {
        foreach (var (pokemon, ability) in beforeMask)
        {
            pokemon.WornMask = null;
            pokemon.AbilityName = ability;
        }
        beforeMask.Clear();
        stagRounds.Clear();
    }

    // ---------------------------------------------------------------- the roles

    /// <summary>
    /// The bear (ursul) takes the hits meant for an ally: as each turn begins, a Pokémon of a side wearing the bear's mask
    /// draws the foes' single-target moves to itself, as Follow Me does (the fastest bear of a side, where two stand). A
    /// Follow Me used in the turn goes over it. In a single battle there is no ally, and nothing changes.
    /// </summary>
    private void BearsDrawTheFoes()
    {
        if (beforeMask.Count == 0) return;
        foreach (var b in BySpeed().Where(b => b.IsActive && MaskOf(b)?.Id == WinterMasks.Bear))
            Field.Side(b.Side).FollowMe ??= b.Place;
    }

    /// <summary>The goat (capra) steals: a damaging move of a Pokémon in the goat's mask takes the target's raised stats before it lands, as Spectral Thief does.</summary>
    private void GoatSteals(MoveUse use, Battler target)
    {
        if (beforeMask.Count == 0 || MaskOf(use.User)?.Id != WinterMasks.Goat || use.Data.Category == MoveCategory.Status) return;
        if (target == use.User || !target.IsActive || !AreFoes(use.User, target)) return;
        StealBoosts(use.User, target);
    }

    /// <summary>
    /// The horse (căluțul) carries its rider out: once a damaging move of a Pokémon in the horse's mask has landed, it goes
    /// back as U-turn's user does and its trainer sends another in, a free switch each turn it attacks. Nothing where the
    /// move takes it out already or nobody could come in.
    /// </summary>
    private void HorseCarries(MoveUse use)
    {
        if (beforeMask.Count == 0 || MaskOf(use.User)?.Id != WinterMasks.Horse || use.Data.Category == MoveCategory.Status) return;
        if (use.Leaves != Leaving.No || !HasReplacement(use.User)) return;
        use.Leaves = Leaving.Returns;
    }

    /// <summary>
    /// The stag (cerbul) leads: at each turn's end while it stands, for <see cref="WinterMasks.StagRounds"/> turn ends,
    /// every Pokémon of its side on the field raises a stat a stage, Attack, Defense, Sp. Atk, Sp. Def and Speed in turn.
    /// </summary>
    private void StagLeads(Battler b)
    {
        if (beforeMask.Count == 0 || MaskOf(b)?.Id != WinterMasks.Stag) return;
        var p = b.Pokemon!;
        int round = stagRounds.GetValueOrDefault(p);
        if (round >= WinterMasks.StagRounds) return;
        stagRounds[p] = round + 1;
        Say(Text.Of("battle.stagLeads", ("pokemon", b.Name)));
        foreach (var ally in AllBattlers.Where(x => x.Side == b.Side && x.IsActive).ToList())
            ChangeStat(ally, WinterMasks.StagStats[round], 1, b);
    }
}
