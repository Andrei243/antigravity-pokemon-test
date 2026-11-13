using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The Tales (plan 46 · CP11, our own; docs/mechanics/rulings.md, "The Tales"), Carpathia's primary gimmick, on the later
// mechanics' pattern: a tale's id on the move's choice (BattleChoice.Tale), refused by WhyNotTale where it can't be; told
// at its moment of the turn (after the Terastallizations, the fastest first), with its line and an event of its own
// (TaleTold, TaleAnswered); the trainer AI's use of it (TrainerAi.TaleNow, TaleToAnswer); all of it behind the Ruleset
// (Ruleset.Tales) and the run's "No later mechanics", so a battle of Platinum's rules, or with no Book of Tales on
// either side, never comes here and is what it was to the bit. A tale's rule is Data/tales.json's (TaleTable), read where
// the field's rules already bite: a move's power in both of DamageCalculator's paths (TaleBoostPercent), accuracy in
// Hits, a weather through SetWeather. A trainer tells one tale a battle and answers one (decision 15): an answer is a tale
// told while the other side's holds, which ends that one at once. A tale already told can't be told again in the battle.
public sealed partial class BattleCore
{
    /// <summary>The key item that lets the player tell the tales they have learned, by name.</summary>
    public const string BookOfTalesItem = TaleTable.BookItem;

    /// <summary>The tales the player may tell (<see cref="CoreSetup.Tales"/>): null with no Book of Tales.</summary>
    public IReadOnlyList<string>? PlayerTales { get; }

    private readonly TaleTable? talesGiven;

    /// <summary>The tales' rules this battle reads: the setup's, or the game's.</summary>
    public TaleTable TaleTable => talesGiven ?? TaleTable.Default;

    /// <summary>Whoever has told a tale this battle, whoever has answered one, and whoever has chosen to this turn (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> taleTold = new(), taleAnswered = new(), taleChosen = new();

    /// <summary>Every tale told in the battle, by either side: none is told twice.</summary>
    private readonly HashSet<string> talesHeard = new();

    /// <summary>Each Pokémon in a Tale Form and what it was before: put back as the tale ends, or with the battle.</summary>
    private readonly Dictionary<Pokemon, (string? Form, string? Ability)> beforeTale = new();

    /// <summary>The tales a place's trainer may tell: a trainer's own, the player's with the Book; null for none.</summary>
    private IReadOnlyList<string>? TalesOf(Battler b) => b.Trainer != null ? b.Trainer.Tales : b.IsPlayerSide ? PlayerTales : null;

    /// <summary>The tale told over the field now and its rule, or null.</summary>
    public TaleRule? TaleInForce => Field.Tale?.Rule;

    /// <summary>
    /// A tale told holds the day (the Zmeu's <c>noNight</c>, plan 46 · CP12): whatever the battle reads of the night reads
    /// day while it is told. The Dusk Ball is the one reader today; a night form of Carpathia's species asks it too.
    /// </summary>
    public bool TaleHoldsTheDay => TaleInForce?.NoNight == true;

    // ---------------------------------------------------------------- what may be chosen

    /// <summary>
    /// Why the trainer of a place can't tell <paramref name="tale"/> with its move this turn; null when it can. Under rules
    /// without the Tales, under a run with no later mechanics, with no Book or the tale not learned, once the tale has
    /// been told in the battle, while one of the side's own tales is told, or once the trainer has told one (with none of
    /// the other side's told) or answered one (with one of theirs told).
    /// </summary>
    public string? WhyNotTale(Battler b, string? tale)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantTellTale");
        if (!Rules.Tales || LaterMechanicsBarred || tale == null || mine.Pokemon is not { IsEgg: false }) return no;
        if (TalesOf(mine) is not { } learned || !learned.Contains(tale) || TaleTable.Get(tale) == null) return no;
        if (talesHeard.Contains(tale)) return Text.Of("battle.taleAlreadyTold", ("tale", TaleTable.NameOf(tale)));
        if (Field.Tale is { } told)
        {
            if (told.Teller == mine.Side) return no;
            if (taleAnswered.Contains(MegaKey(mine))) return Text.Of("battle.taleOnlyOnce");
        }
        else if (taleTold.Contains(MegaKey(mine))) return Text.Of("battle.taleOnlyOnce");
        return null;
    }

    /// <summary>Whether the trainer of a place may tell this tale with the move it chooses now (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanTellTale(Battler b, string tale) => WhyNotTale(b, tale) == null && !taleChosen.Contains(MegaKey(At(b.Place)));

    /// <summary>The tales the trainer of a place could tell now, in the order learned (what the move menu's TALE button turns through).</summary>
    public IReadOnlyList<string> TalesToTell(Battler b) =>
        TalesOf(At(b.Place)) is { } learned ? learned.Where(t => CanTellTale(b, t)).ToList() : Array.Empty<string>();

    /// <summary>A choice that tells a tale: with a move, and where <see cref="WhyNotTale"/> lets it.</summary>
    private string? WhyNotTaleChoice(BattleChoice choice, Battler b) =>
        choice.Kind != ChoiceKind.Fight ? Text.Of("battle.cantTellTale") : WhyNotTale(b, choice.Tale);

    /// <summary>What a chooser inside may not have: a tale the rules don't let it tell.</summary>
    private BattleChoice AsTheTaleRulesLet(Battler b, BattleChoice choice) =>
        choice.Tale != null && (WhyNotTaleChoice(choice, b) != null || !CanTellTale(b, choice.Tale)) ? choice with { Tale = null } : choice;

    /// <summary>Two tales of one trainer in one answer are refused.</summary>
    private void RefuseTwoTales(IReadOnlyList<BattleChoice> choices)
    {
        if (choices.Where(c => c.Tale != null).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.taleOnlyOnce"));
    }

    /// <summary>Notes the tales an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseTale(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen.Where(c => c.Tale != null))
            if (!taleChosen.Add(MegaKey(At(choice.Who)))) throw new ArgumentException(Text.Of("battle.taleOnlyOnce"));
    }

    // ---------------------------------------------------------------- the telling

    /// <summary>
    /// The turn's tales, after its Terastallizations, the fastest first: every trainer whose Pokémon's move came with
    /// one. A slower trainer's tale told on the same turn as a faster foe's is an answer to it. True when anyone told one,
    /// so that the moves still to come are put in order again (a Tale Form may be faster).
    /// </summary>
    private bool TellTales(IReadOnlyList<Act> acts)
    {
        taleChosen.Clear();
        var those = acts.Where(a => a.Choice is { Kind: ChoiceKind.Fight, Tale: not null } && a.User.IsActive && a.User.Pokemon == a.Actor).ToList();
        if (those.Count == 0) return false;
        bool any = false;
        foreach (var b in BySpeed().Where(b => those.Any(a => a.User == b)))
        {
            if (Result != BattleResult.None) break;
            string tale = those.First(a => a.User == b).Choice.Tale!;
            if (!b.IsActive || WhyNotTale(b, tale) != null) continue;
            Tell(b, tale);
            any = true;
        }
        return any;
    }

    /// <summary>Who tells a tale, as its line names them: a trainer's title, or the player's name.</summary>
    private string TellerName(Battler b) => b.Trainer != null ? b.Trainer.FullTitle : playerName;

    /// <summary>
    /// A tale told: an answer ends the other side's first (its forms put back, its weather gone), then the line, the
    /// tale's own line, its weather, and the bound Pokémon of the teller's side on the field wake into their Tale Forms.
    /// </summary>
    private void Tell(Battler b, string id)
    {
        var rule = TaleTable.Get(id)!;
        talesHeard.Add(id);
        int turns = TaleTable.Turns;
        if (Field.Tale is { } old)
        {
            taleAnswered.Add(MegaKey(b));
            EndTale(said: false);
            Say(Text.Of("battle.taleAnswered", ("trainer", TellerName(b)), ("tale", TaleTable.NameOf(id)), ("ended", TaleTable.NameOf(old.Id))))
                .With(new TaleAnswered(b.Side, id, old.Id, turns));
        }
        else
        {
            taleTold.Add(MegaKey(b));
            Say(Text.Of("battle.taleTold", ("trainer", TellerName(b)), ("tale", TaleTable.NameOf(id)))).With(new TaleTold(b.Side, id, turns));
        }
        Say(Text.Of($"basme.{id}.told"));
        bool brought = rule.Weather is { } weather && SetWeather(weather, turns, Starts(weather));
        Field.Tale = new TaleState(id, b.Side, turns, rule, brought);
        foreach (var mine in AllBattlers.Where(x => x.Side == b.Side && x.IsActive)) WakeToTheTale(mine);
    }

    /// <summary>
    /// A Pokémon bound to the tale told by its own side wakes into its Tale Form, unless it has taken another's shape or
    /// stands in a Mega, primal or Ultra form: as the tale is told, and as it comes in while it is told.
    /// </summary>
    private void WakeToTheTale(Battler b)
    {
        if (Field.Tale is not { } told || told.Teller != b.Side) return;
        var rule = told.Rule;
        if (b.Pokemon is not { } p || p.IsEgg || b.Volatile.Transformed || beforeTale.ContainsKey(p)) return;
        if (p.FormData?.Kind is FormKind.Mega or FormKind.Primal or FormKind.MegaDynamax || p.Form == UltraForm || b.Volatile.Dynamaxed) return;
        if (rule.FormFor(p.Species.Name) is not { } form || p.Species.Form(form) == null || p.Form == form) return;
        beforeTale[p] = (p.Form, p.AbilityName);
        TakeTaleForm(b, form);
        Say(Text.Of("battle.taleWoke", ("pokemon", b.Name), ("tale", TaleTable.NameOf(told.Id)))).With(new TaleFormChanged(b.Place, form, Woke: true));
    }

    /// <summary>A Tale Form taken or left: its ability, stats and types (as <see cref="TakeForm"/>, kept apart from the Megas, which last the battle out).</summary>
    private void TakeTaleForm(Battler b, string? form)
    {
        var p = b.Pokemon!;
        p.ChangeForm(form);
        b.Volatile.Types = null;
        b.Volatile.AddedType = null;
        if (b.Volatile.Original is { } o)
        {
            if (o.AbilityKept) o.AbilityName = p.AbilityName;
            if (o.StatsKept) (o.Attack, o.Defense, o.SpAttack, o.SpDefense, o.Speed) = (p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed);
        }
    }

    /// <summary>
    /// The tale over the field ends: whoever woke to it goes back (a line for whoever stands on the field, the event alone
    /// for one on the bench), the weather it brought goes if it still holds, and the field is free of it. Said (a tale whose
    /// turns ran out) with its own line; an answered one is told of by the answer's.
    /// </summary>
    private void EndTale(bool said)
    {
        if (Field.Tale is not { } told) return;
        var rule = told.Rule;
        Field.Tale = null;
        if (said) Say(Text.Of("battle.taleEnded", ("tale", TaleTable.NameOf(told.Id)))).With(new TaleEnded(told.Id));
        foreach (var (pokemon, (form, ability)) in beforeTale.ToList())
        {
            var b = AllBattlers.FirstOrDefault(x => x.Pokemon == pokemon && x.IsActive);
            if (b != null)
            {
                TakeTaleForm(b, form);
                pokemon.AbilityName = ability;
                Say(Text.Of("battle.taleSlept", ("pokemon", b.Name))).With(new TaleFormChanged(b.Place, form, Woke: false));
            }
            else
            {
                pokemon.ChangeForm(form);
                pokemon.AbilityName = ability;
            }
        }
        beforeTale.Clear();
        // The weather it brought goes with it (an answer that brings its own takes its place next)
        if (told.BroughtWeather && rule.Weather is { } weather && Field.Weather == Rules.Sky(weather) && !Field.WeatherLasts && !Field.WeatherIsStrong)
        {
            var gone = Field.Weather;
            Field.Weather = BattleWeather.None;
            Field.WeatherTurns = 0;
            Say(Stops(gone)).With(new WeatherChanged(BattleWeather.None));
        }
    }

    /// <summary>The tale runs down at the turn's end, after the terrain (five turn ends from the one it was told in).</summary>
    private void TaleRunsDown()
    {
        if (Field.Tale is not { } told) return;
        if (told.Turns <= 1) EndTale(said: true);
        else Field.Tale = told with { Turns = told.Turns - 1 };
    }

    /// <summary>Every tale ends with the battle, with nothing said: the forms are given back.</summary>
    private void EndTalesWithTheBattle()
    {
        foreach (var (pokemon, (form, ability)) in beforeTale)
        {
            pokemon.ChangeForm(form);
            pokemon.AbilityName = ability;
        }
        beforeTale.Clear();
        Field.Tale = null;
    }

    // ---------------------------------------------------------------- where the rule bites

    /// <summary>What the tale over a field does to a move's power, in hundredths (100: nothing), for whoever uses it.</summary>
    internal static int TaleBoostPercent(FieldState? field, PokemonType type, MoveFlags flags) =>
        field?.Tale?.Rule.Boost is { } boost && boost.Favours(type, flags) ? boost.Percent : 100;

    /// <summary>A move's chance to hit under the tale over the field (the Vârcolac's eclipse).</summary>
    private int TaleAccuracy(int rate) => TaleInForce is { Accuracy: not 100 } rule ? rate * rule.Accuracy / 100 : rate;
}
