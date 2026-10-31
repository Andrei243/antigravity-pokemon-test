using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Terastallization (plan 06 · R23; docs/mechanics/rulings.md, "Terastallization"), the last of the four later
// mechanics, on R20's pattern: a flag on the move's choice (BattleChoice.Terastallize), refused by WhyNot where it
// can't be; the change carried out at its moment of the turn (after the Mega Evolutions and the Dynamaxes), with its
// line and an event of its own (Terastallized); the trainer AI's use of it (TrainerAi.WithTheLaterMechanics); all of
// it behind the Ruleset (Ruleset.Terastallization), so a battle of Platinum's rules never comes here. The rules are
// Pokémon Showdown's at the pinned commit: sim/battle-actions.ts (canTerastallize, terastallize, the power of 60 in
// getDamage and the same-type bonus in modifyDamage, which DamageCalculator.Tera.cs holds), sim/pokemon.ts (getTypes:
// a Terastallized Pokémon is its Tera Type alone, a Stellar one its own types; runEffectiveness for the Stellar
// type), sim/battle-queue.ts (terastallize, order 106, after runDynamax's 105), data/moves.ts (Tera Blast, Tera
// Starstorm) and data/abilities.ts (Tera Shift, Tera Shell, Teraform Zero, Embody Aspect). A Terastallization lasts
// through switches to the battle's end: what it changed is kept on the Pokémon (Pokemon.Terastallized), not on its
// place, and taken off as the battle ends.
public sealed partial class BattleCore
{
    /// <summary>The item in the bag that lets the player's Pokémon Terastallize, by name.</summary>
    public const string TeraOrbItem = "Tera Orb";

    /// <summary>The player has a Tera Orb (<see cref="CoreSetup.TeraOrb"/>).</summary>
    public bool TeraOrb { get; }

    /// <summary>Whoever has Terastallized this battle, and whoever has chosen to this turn: a trainer, or the player for their own (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> teraUsed = new(), teraChosen = new();

    /// <summary>Every Pokémon that Terastallized in the battle: their Tera Types are taken off as it ends.</summary>
    private readonly List<Pokemon> terastallized = new();

    /// <summary>The use of a move whose Stellar boost was last decided (Showdown's <c>move.stellarBoosted</c> is the use's own).</summary>
    private MoveUse? stellarUse;

    /// <summary>Ogerpon's masks and the stat its Embody Aspect raises as it Terastallizes (Showdown's four <c>embodyaspect…</c>).</summary>
    private static StatType EmbodiedStat(string? form) => form switch
    {
        "Ogerpon-Wellspring-Mask" => StatType.SpDefense,
        "Ogerpon-Hearthflame-Mask" => StatType.Attack,
        "Ogerpon-Cornerstone-Mask" => StatType.Defense,
        _ => StatType.Speed
    };

    private const string EmbodyAspect = "Embody Aspect";

    /// <summary>The trainer of the place has a Tera Orb: the player's own (<see cref="TeraOrb"/>), a trainer's or a partner's.</summary>
    private bool HasTeraOrb(Battler b) => b.Trainer != null ? b.Trainer.TeraOrb : b.IsPlayerSide && TeraOrb;

    /// <summary>The Pokémon in a place has Terastallized (and into what), for the screen and the tests.</summary>
    public static TeraType? TeraOf(Battler b) => b.Pokemon?.Terastallized;

    // ---------------------------------------------------------------- what may be chosen

    /// <summary>
    /// Why the Pokémon in a place can't Terastallize with its move this turn; null when it can (Showdown's
    /// <c>canTerastallize</c>, and our one-mechanic-a-Pokémon rule, decision 06 · 3). Under rules without it, with no
    /// Tera Orb, once its trainer has Terastallized, holding a Mega Stone it could Mega Evolve with or a Z-Crystal, in
    /// a Mega, primal or Ultra form, or Dynamaxed.
    /// </summary>
    public string? WhyNotTera(Battler b)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantTerastallize", ("pokemon", mine.Name));
        if (!Rules.Terastallization || LaterMechanicsBarred || mine.Pokemon is not { } p || p.IsEgg || !HasTeraOrb(mine) || p.Terastallized != null) return no;
        if (teraUsed.Contains(MegaKey(mine))) return Text.Of("battle.teraOnlyOnce");
        if (p.HeldItem?.ZCrystal != null || MegaFormOf(p) != null || p.FormData?.Kind is FormKind.Mega or FormKind.Primal or FormKind.MegaDynamax || p.Form == UltraForm) return no;
        if (mine.Volatile.Dynamaxed) return no;
        return null;
    }

    /// <summary>Whether the Pokémon in a place may Terastallize with the move it chooses now (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanTerastallize(Battler b) => WhyNotTera(b) == null && !teraChosen.Contains(MegaKey(At(b.Place)));

    /// <summary>A choice that Terastallizes: with a move, alone of the later mechanics, and where <see cref="WhyNotTera"/> lets it.</summary>
    private string? WhyNotTeraChoice(BattleChoice choice, Battler b)
    {
        if (choice.Kind != ChoiceKind.Fight || choice.Mega || choice.ZMove || choice.UltraBurst || choice.Dynamax)
            return Text.Of("battle.cantTerastallize", ("pokemon", b.Name));
        return WhyNotTera(b);
    }

    /// <summary>What a chooser inside may not have: a Terastallization the rules don't let it make.</summary>
    private BattleChoice AsTheTeraRulesLet(Battler b, BattleChoice choice) =>
        choice.Terastallize && (WhyNotTeraChoice(choice, b) != null || !CanTerastallize(b)) ? choice with { Terastallize = false } : choice;

    /// <summary>Two Terastallizations of one trainer in one answer are refused.</summary>
    private void RefuseTwoTera(IReadOnlyList<BattleChoice> choices)
    {
        if (choices.Where(c => c.Terastallize).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.teraOnlyOnce"));
    }

    /// <summary>Notes the Terastallizations an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseTera(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen.Where(c => c.Terastallize))
            if (!teraChosen.Add(MegaKey(At(choice.Who)))) throw new ArgumentException(Text.Of("battle.teraOnlyOnce"));
    }

    // ---------------------------------------------------------------- the Terastallization

    /// <summary>
    /// The turn's Terastallizations, after its Mega Evolutions and Dynamaxes (Showdown's <c>terastallize</c>, order
    /// 106), the fastest first: every Pokémon still on the field whose move came with one. True when anyone did, so
    /// that the moves still to come are put in order again (an Embody Aspect raises a Speed).
    /// </summary>
    private bool Terastallizations(IReadOnlyList<Act> acts)
    {
        teraChosen.Clear();
        var those = acts.Where(a => a.Choice is { Kind: ChoiceKind.Fight, Terastallize: true } && a.User.IsActive && a.User.Pokemon == a.Actor).ToList();
        if (those.Count == 0) return false;
        bool any = false;
        foreach (var b in BySpeed().Where(b => those.Any(a => a.User == b)))
        {
            if (Result != BattleResult.None) break;
            if (!b.IsActive || WhyNotTera(b) != null) continue;
            Terastallize(b);
            any = true;
        }
        return any;
    }

    /// <summary>
    /// Showdown's <c>terastallize</c>: the line, the Tera Type taken (whatever a move had added to its types goes), the
    /// Terastallization spent for the trainer; an Ogerpon's Embody Aspect raises a stat by its mask, and a Terapagos in
    /// its Terastal Form takes its Stellar Form, whose Teraform Zero clears the weather and the terrain.
    /// </summary>
    private void Terastallize(Battler b)
    {
        var p = b.Pokemon!;
        var tera = p.TeraType;
        teraUsed.Add(MegaKey(b));
        p.Terastallized = tera;
        p.StellarBoosted = new HashSet<string>();
        p.StellarThisUse = false;
        terastallized.Add(p);
        b.Volatile.AddedType = null;
        string type = tera.IsStellar ? Text.Of("battle.stellarType") : Words.Type(tera.Type);
        string? form = null;
        if (p.Species.Name == "Terapagos" && p.Form == "Terapagos-Terastal" && !b.Volatile.Transformed && p.Species.Form("Terapagos-Stellar") != null)
        {
            TakeForm(b, "Terapagos-Stellar");
            form = p.Form;
        }
        Say(Text.Of("battle.terastallized", ("pokemon", b.Name), ("type", type))).With(new Terastallized(b.Place, tera.Name, form, p.MaxHP, p.CurrentHP));
        if (p.Species.Name == "Ogerpon" && !b.Volatile.Transformed) Embody(b);
        if (form != null && b.Ability?.Name == "Teraform Zero") TeraformZero(b);
    }

    /// <summary>Ogerpon's mask shines as it Terastallizes: its ability is Embody Aspect for the battle, and raises a stat by the mask.</summary>
    private void Embody(Battler b)
    {
        var p = b.Pokemon!;
        beforeMega.TryAdd(p, (p.Form, p.AbilityName));
        p.AbilityName = EmbodyAspect;
        if (b.Volatile.Original is { AbilityKept: true } o) o.AbilityName = EmbodyAspect;
        ChangeStat(b, EmbodiedStat(p.Form), 1, b, false, By.Ability);
    }

    /// <summary>Teraform Zero (Showdown's <c>onAfterTerastallization</c>): the weather and the terrain end, whatever brought them.</summary>
    private void TeraformZero(Battler b)
    {
        if (Field.Weather == BattleWeather.None && Field.Terrain == null) return;
        Say(Text.Of("battle.teraformZero", ("pokemon", b.Name), ("ability", AbilityOf(b))));
        if (Field.Weather != BattleWeather.None)
        {
            var gone = Field.Weather;
            Field.Weather = BattleWeather.None;
            Field.WeatherLasts = false;
            Field.WeatherTurns = 0;
            Field.StrongWeatherBy = null;
            Say(Stops(gone)).With(new WeatherChanged(BattleWeather.None));
        }
        if (Field.Terrain is { } terrain) EndTerrain(terrain);
    }

    /// <summary>
    /// What Terastallization does as a Pokémon comes in (from <see cref="ShapeOnEntry"/>, once a stay): a Terapagos
    /// with Tera Shift takes its Terastal Form for the battle (Showdown's <c>terashift</c> <c>onSwitchIn</c>, its HP
    /// raised with its most), and a Terastallized Ogerpon's Embody Aspect raises its stat again.
    /// </summary>
    private void TeraOnEntry(Battler b)
    {
        var p = b.Pokemon!;
        if (Is(b, "Terapagos", "Tera Shift") && p.Form == null && p.Species.Form("Terapagos-Terastal") != null)
        {
            TakeForm(b, "Terapagos-Terastal");
            Say(Text.Of("battle.transformed", ("pokemon", b.Name))).With(new Reshaped(b.Place));
        }
        else if (p.Terastallized != null && p.Species.Name == "Ogerpon" && b.Ability?.Name == EmbodyAspect && !b.Volatile.Transformed)
            ChangeStat(b, EmbodiedStat(p.Form), 1, b, false, By.Ability);
    }

    /// <summary>Every Terastallization ends with the battle: the Pokémon is its own types again.</summary>
    private void EndTerastallizations()
    {
        foreach (var p in terastallized)
        {
            p.Terastallized = null;
            p.StellarBoosted = null;
            p.StellarThisUse = false;
        }
        terastallized.Clear();
    }

    // ---------------------------------------------------------------- a hit

    /// <summary>
    /// The power a hit is worked out with, given what the move's effect made of it (null: the data's), as a
    /// Terastallized user has it (Showdown's <c>getDamage</c>): a move of its Tera Type (for a Stellar one, of a type
    /// its boost is still for) weaker than 60 hits as 60, unless it has priority, strikes several times or is of a
    /// power its effect decides from nothing (its data's 0, or 150). Decides first whether the use has the Stellar
    /// boost, once a use (Showdown's <c>move.stellarBoosted</c>).
    /// </summary>
    private int? TeraPower(MoveUse use, int? power)
    {
        if (use.User.Pokemon is not { Terastallized: { } tera } p) return power;
        var data = use.Data;
        if (tera.IsStellar && stellarUse != use)
        {
            stellarUse = use;
            string key = data.Stellar ? "Stellar" : data.Type.ToString();
            bool boosted = !p.StellarBoosted!.Contains(key);
            p.StellarThisUse = boosted;
            if (boosted && p.Form != "Terapagos-Stellar") p.StellarBoosted.Add(key);
        }
        bool ofItsType = tera.IsStellar ? p.StellarThisUse : !data.Stellar && use.User.HasType(data.Type);
        int now = power ?? data.Power;
        var own = MoveDatabase.Get(use.Own?.Name ?? data.Name) ?? data;
        bool decided = own.Power is 0 or 150 && (use.Effect.BasePower != null || use.Effect.Power != null || use.Effect.Deals != null);
        if (!ofItsType || now <= 0 || now >= 60 || own.Priority > 0 || use.Effect.Strikes != null || decided) return power;
        return 60;
    }

    // ---------------------------------------------------------------- Tera Blast and Tera Starstorm

    /// <summary>A Pokémon's Attack is the higher of the two at their stages (Showdown's <c>getStat(stat, false, true)</c>): Tera Blast and Tera Starstorm hit as physical moves.</summary>
    private static bool AttackIsHigher(Pokemon p) =>
        Formulas.Staged(p.Attack, p.StatStages.GetValueOrDefault(StatType.Attack)) > Formulas.Staged(p.SpAttack, p.StatStages.GetValueOrDefault(StatType.SpAttack));

    private static readonly Dictionary<string, MoveEffect> TeraEffects = new()
    {
        // Tera Blast (Showdown's terablast): a Terastallized user's is of its Tera Type, whatever Normalize says, and
        // physical where its Attack is the higher; a Stellar one's is of the Stellar type at 100 power, and lowers its
        // user's Attack and Sp. Atk once it has hit
        ["TeraBlast"] = new()
        {
            Start = (b, use) =>
            {
                if (use.User.Pokemon is not { Terastallized: { } tera } p) return true;
                var own = use.Own ?? use.Data;
                var data = tera.IsStellar ? own.AsStellar() : own.OfType(tera.Type);
                if (AttackIsHigher(p)) data = data.OfCategory(MoveCategory.Physical);
                use.Move = new Move(data, use.Move.CurrentPP);
                return true;
            },
            BasePower = (b, use, t) => use.Data.Stellar ? 100 : use.Data.Power,
            Then = (b, use) =>
            {
                if (!use.Data.Stellar) return;
                b.ChangeStat(use.User, StatType.Attack, -1, use.User, false, By.Move);
                b.ChangeStat(use.User, StatType.SpAttack, -1, use.User, false, By.Move);
            }
        },
        // Tera Starstorm (Showdown's terastarstorm): a Terapagos in its Stellar Form makes it of the Stellar type and
        // aims it at every foe, physical where its Attack is the higher; anyone else's is a Normal move at one target
        ["TeraStarstorm"] = new()
        {
            Aim = (user, target) => user.Pokemon?.Form == "Terapagos-Stellar" && !user.Volatile.Transformed ? MoveTarget.AllFoes : MoveTarget.Selected,
            Start = (b, use) =>
            {
                if (use.User.Pokemon is not { Form: "Terapagos-Stellar" } p || use.User.Volatile.Transformed) return true;
                var data = (use.Own ?? use.Data).AsStellar();
                if (p.Terastallized != null && AttackIsHigher(p)) data = data.OfCategory(MoveCategory.Physical);
                use.Move = new Move(data, use.Move.CurrentPP);
                return true;
            }
        }
    };
}
