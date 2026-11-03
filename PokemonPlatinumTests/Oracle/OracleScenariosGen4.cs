using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Oracle;

/// <summary>
/// The scenarios of the oracle of generation 4 (plan 24 · X5), drawn from a <see cref="BattleRandom"/> of a fixed
/// seed, whose numbers are Platinum's and can't change with .NET: Platinum's 493 species as Platinum has them (its own
/// types and stats: the data's species as loaded, never <c>Under</c> the modern rules), with Platinum's forms that
/// change a battle (Rotom's appliances, Giratina's Origin Forme, Shaymin's Sky Forme, Deoxys's formes, Wormadam's
/// cloaks), at any level with any IVs, effort within 510, a nature, one of Platinum's 123 abilities the species has and
/// the engine runs (or none), one of Platinum's held items whose effect the engine runs (or none), stat stages, a
/// status now and then; a damaging move of Platinum's 467 of one hit, run fully, whose power the battle's own course
/// doesn't decide (Fury Cutter, Rollout and the counters stay with the scenario tests); sun, rain, a sandstorm or hail;
/// Reflect or Light Screen; critical or not; a single battle or a double with a spread move.
/// <para>
/// What the core decides before the formula is reached is left out, as the oracle of generation 9 leaves it
/// (<see cref="OracleScenarios.StoppedBeforeTheFormula"/>): an ability that takes a move in never meets one.
/// </para>
/// </summary>
internal static class OracleScenariosGen4
{
    public const int Count = 3000;
    public const uint Seed = 4;

    public static string ScenariosPath => Path.Combine(OracleScenarios.Folder, "scenarios-gen4.json");
    public static string AnswersPath => Path.Combine(OracleScenarios.Folder, "damage-gen4.json");
    public static string KnownPath => Path.Combine(OracleScenarios.Folder, "known-gen4.json");

    /// <summary>The weathers as the calculator of generation 4 names them, with ours.</summary>
    public static readonly (string Name, BattleWeather Weather)[] Weathers =
    {
        ("Sun", BattleWeather.Sun), ("Rain", BattleWeather.Rain), ("Sand", BattleWeather.Sandstorm), ("Hail", BattleWeather.Hail)
    };

    /// <summary>Platinum's forms that change a battle, by Showdown's names, which the data keeps.</summary>
    public static readonly HashSet<string> Forms = new(StringComparer.Ordinal)
    {
        "Rotom-Heat", "Rotom-Wash", "Rotom-Frost", "Rotom-Fan", "Rotom-Mow", "Giratina-Origin", "Shaymin-Sky",
        "Deoxys-Attack", "Deoxys-Defense", "Deoxys-Speed", "Wormadam-Sandy", "Wormadam-Trash"
    };

    /// <summary>Platinum's species, as Platinum has them.</summary>
    private static readonly List<PokemonSpecies> Species =
        PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= 493).OrderBy(s => s.DexNumber).ToList();

    private static readonly Dictionary<string, PokemonSpecies> SpeciesByName = Species.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AbilitiesRun = AbilityEffectTable.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>One of Platinum's 123 abilities: the later ones a species' list may name have no part in generation 4.</summary>
    private static bool IsPlatinums(string ability) => AbilityDatabase.IdOf(ability) is >= 1 and <= 123;

    /// <summary>The moves a scenario may use: Platinum's 467, as Platinum has them.</summary>
    public static readonly List<MoveData> Moves = MoveDatabase.GetAll()
        .Where(m => m.Kind == MoveKind.Standard && m.Support == MoveEffectSupport.Full && m.Id is >= 1 and <= 467)
        .Where(m => m.Category != MoveCategory.Status && m.Power > 0 && (m.Effect == null || OracleScenarios.PlainEffects.Contains(m.Effect)))
        .OrderBy(m => m.Id).ToList();

    // Platinum's held items whose effect the engine runs, but the berries that halve a hit as it lands (the core's, not the formula's)
    private static readonly List<ItemData> Items = ItemDatabase.GetAll()
        .Where(i => DataImporter.Coverage.IsPlatinums(i) && HeldItemEffects.IsHoldable(i) && !i.HoldEffect!.StartsWith("Weaken", StringComparison.Ordinal))
        .OrderBy(i => i.Id).ToList();

    public static MoveData MoveOf(string name) => MoveDatabase.Get(name);

    public static PokemonSpecies SpeciesOf(OracleMon mon) =>
        SpeciesByName.TryGetValue(mon.Form == null ? mon.Species : PokemonDatabase.SpeciesOfForm(mon.Form)!.Name, out var s) ? s
            : throw new InvalidOperationException($"No species for {mon.Species}");

    /// <summary>Every scenario, drawn afresh.</summary>
    public static List<OracleScenario> Draw()
    {
        var dice = new BattleRandom(Seed);
        var list = new List<OracleScenario>(Count);
        for (int id = 1; id <= Count; id++) list.Add(DrawOne(id, dice));
        return list;
    }

    private static OracleScenario DrawOne(int id, BattleRandom dice)
    {
        bool doubles = dice.Next(4) == 0;
        var attacker = DrawMon(dice, attacking: true);
        var defender = DrawMon(dice, attacking: false);

        string? weather = dice.Next(3) == 0 ? Weathers[dice.Next(Weathers.Length)].Name : null;
        var field = new OracleField(weather, null, dice.Next(8) == 0, dice.Next(8) == 0, false);

        MoveData move;
        int tries = 0;
        do move = Moves[dice.Next(Moves.Count)];
        while (OracleScenarios.StoppedBeforeTheFormula(move, defender, weather, null) && ++tries < 50);
        if (OracleScenarios.StoppedBeforeTheFormula(move, defender, weather, null)) defender = defender with { Ability = null };

        bool spread = doubles && move.Target is MoveTarget.AllFoes or MoveTarget.AllOthers;
        bool crit = dice.Next(8) == 0;
        return new OracleScenario(id, doubles, spread, attacker, defender, move.Name, crit, field);
    }

    private static OracleMon DrawMon(BattleRandom dice, bool attacking)
    {
        var species = Species[dice.Next(Species.Count)];

        string gender = species.GenderRatio switch
        {
            < 0 => "N",
            0 => "M",
            >= 8 => "F",
            _ => dice.Next(8) < species.GenderRatio ? "F" : "M"
        };
        var forms = (species.Forms ?? new()).Where(f => Forms.Contains(f.Name)).ToList();
        string? form = forms.Count > 0 && dice.Next(2) == 0 ? forms[dice.Next(forms.Count)].Name : null;

        int level = dice.Next(3) switch { 0 => 50, 1 => 100, _ => dice.Next(5, 101) };
        var ivs = new int[6];
        bool perfect = dice.Next(2) == 0;
        for (int i = 0; i < 6; i++) ivs[i] = perfect ? 31 : dice.Next(32);
        var evs = new int[6];
        int left = 510;
        for (int k = 0; k < 6; k++)
        {
            int stat = (k + dice.Next(6)) % 6;
            if (evs[stat] != 0) continue;
            int give = dice.Next(Math.Min(252, left) + 1);
            evs[stat] = give;
            left -= give;
        }
        var nature = (Nature)dice.Next(25);

        var abilities = (form != null ? species.Form(form)!.Abilities : null) ?? species.Abilities;
        var run = abilities.Where(a => IsPlatinums(a) && AbilitiesRun.Contains(a) && !OracleScenarios.ActOnEntry.Contains(a)).ToList();
        string? ability = run.Count == 0 ? null : run[dice.Next(run.Count)];

        string? item = dice.Next(2) == 0 ? Items[dice.Next(Items.Count)].Name : null;

        var boosts = new int[4];
        if (dice.Next(3) == 0) for (int i = 0; i < 4; i++) boosts[i] = dice.Next(13) - 6;

        string status = "";
        if (attacking && dice.Next(8) == 0) status = "brn";
        else if (dice.Next(12) == 0) status = OracleScenarios.Statuses[dice.Next(OracleScenarios.Statuses.Length)];

        int hp = 0;
        if (attacking && dice.Next(6) == 0) hp = 1 + dice.Next(1000);

        return new OracleMon(form ?? species.Name, form, level, gender, nature.ToString(), ability, item, ivs, evs, boosts, status, hp);
    }

    /// <summary>The scenarios file, as <see cref="OracleScenarios.Write"/> writes generation 9's, with its own header.</summary>
    public static string Write(IReadOnlyList<OracleScenario> scenarios) =>
        OracleScenarios.Write(scenarios).Replace(
            "\"about\": \"plan 24 X6: the scenarios OracleScenarios draws (seed " + OracleScenarios.Seed + ")",
            "\"about\": \"plan 24 X5: the scenarios OracleScenariosGen4 draws (seed " + Seed + ") under Platinum's rules");
}
