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

/// <summary>One Pokémon of a scenario, as both our engine and the calculator are told of it.</summary>
/// <param name="Species">The species' name, or the form's where it is in one (Showdown's spelling, which the data's forms keep).</param>
/// <param name="Form">The form, or null for the species' own.</param>
/// <param name="Gender">M, F or N.</param>
/// <param name="Ability">An ability the engine runs, or null for none (the calculator's "No Ability").</param>
/// <param name="Ivs">HP, Attack, Defense, Sp. Atk, Sp. Def, Speed.</param>
/// <param name="Evs">The same, 510 in all at most and 252 to a stat.</param>
/// <param name="Boosts">The stages of Attack, Defense, Sp. Atk and Sp. Def.</param>
/// <param name="Status">"", brn, par, psn, tox, slp or frz, as the calculator names them.</param>
/// <param name="Hp">Its HP now, or 0 for all of it.</param>
internal sealed record OracleMon(string Species, string? Form, int Level, string Gender, string Nature, string? Ability, string? Item,
    int[] Ivs, int[] Evs, int[] Boosts, string Status, int Hp);

/// <summary>The field of a scenario: the weather and terrain as the calculator names them, and the defender's side's screens.</summary>
internal sealed record OracleField(string? Weather, string? Terrain, bool Reflect, bool LightScreen, bool AuroraVeil);

/// <summary>
/// One hit to be worked out by both (plan 24 · X6): who hits whom with what, critical or not, on what field, in a single
/// battle or a double (where a move that hits both foes, or everyone else, is spread).
/// </summary>
internal sealed record OracleScenario(int Id, bool Doubles, bool Spread, OracleMon Attacker, OracleMon Defender, string Move, bool Crit, OracleField Field);

/// <summary>
/// The scenarios of the oracle of generation 9 (plan 24 · X6), drawn from a <see cref="BattleRandom"/> of a fixed seed,
/// whose numbers are Platinum's and can't change with .NET: all 1025 species and their forms that are kept outside
/// battle (regional and alternate), built <c>Under(Ruleset.Modern)</c>, at any level with any IVs, effort within 510,
/// a nature, an ability of theirs the engine runs, a held item of Platinum's whose effect the engine runs (or none),
/// stat stages, a status now and then; a damaging move of one hit that the engine runs fully and whose power the
/// battle's own course doesn't decide; the weathers, terrains and screens of the modern rules; critical or not; a
/// single battle or a double. No Mega Evolution, Z-Move, Dynamax or Terastallization: plan 06 · R20 to R23 add theirs.
/// <para>
/// What the core decides before the formula is reached is left out, since the oracle compares the formula alone:
/// an ability that takes a move in (Levitate, Water Absorb, Volt Absorb, Motor Drive, Flash Fire, Dry Skin, Wonder
/// Guard, Soundproof) never meets a move it would take, and the strong weathers never meet a move they put out.
/// </para>
/// </summary>
internal static class OracleScenarios
{
    public const int Count = 3000;
    public const uint Seed = 9;

    public static string Folder => Path.Combine(Repository.Root, "PokemonPlatinumTests", "Oracle");
    public static string ScenariosPath => Path.Combine(Folder, "scenarios-gen9.json");
    public static string AnswersPath => Path.Combine(Folder, "damage-gen9.json");
    public static string KnownPath => Path.Combine(Folder, "known-gen9.json");

    /// <summary>The effects of a move whose one hit the formula works out alone, with its own power, in a battle that has only begun.</summary>
    public static readonly HashSet<string> PlainEffects = new(StringComparer.Ordinal)
    {
        "RechargeAfter", "BindHit", "ContinueAndConfuseSelf", "SwitchHit", "CrashOnMiss", "Blizzard", "HalveDefense",
        "StealHeldItem", "LeaveWith1Hp", "IncreasePrizeMoney", "ChargeTurnHighCrit", "Fly", "Dig", "Dive", "Bounce",
        "ShadowForce", "ChargeTurnDefUp", "ChargeTurnHighCritFlinch", "SkipChargeTurnInSun", "Thunder", "RaiseAtkWhenHit",
        "TriAttack", "DamageWhileAsleep", "Whirlpool", "Uproar",
        "HitFly", "SecretPower", "RemoveProtect", "Chatter", "EatBerry", "HitBeforeSwitch", "DoubleDamageFlyOrBounce",
        "DoubleDamageDive", "DoubleDamageDig", "FlinchDoubleDamageFlyOrBounce", "FlinchMinimizeDoubleHit",
        "HitFirstIfTargetAttacking", "RemoveHazardsAndBinding"
    };

    /// <summary>The weathers as the calculator names them, with ours.</summary>
    public static readonly (string Name, BattleWeather Weather)[] Weathers =
    {
        ("Sun", BattleWeather.Sun), ("Rain", BattleWeather.Rain), ("Sand", BattleWeather.Sandstorm), ("Snow", BattleWeather.Snow),
        ("Harsh Sunshine", BattleWeather.HarshSun), ("Heavy Rain", BattleWeather.HeavyRain), ("Strong Winds", BattleWeather.StrongWinds)
    };

    /// <summary>The terrains as the calculator names them, with ours.</summary>
    public static readonly (string Name, FieldCondition Terrain)[] Terrains =
    {
        ("Electric", FieldCondition.ElectricTerrain), ("Grassy", FieldCondition.GrassyTerrain),
        ("Psychic", FieldCondition.PsychicTerrain), ("Misty", FieldCondition.MistyTerrain)
    };

    public static readonly string[] Statuses = { "brn", "par", "psn", "tox", "slp", "frz" };

    /// <summary>
    /// Abilities whose part in a hit is played as their holder comes in (the core's <c>SwitchInChecks</c>), which the
    /// calculator plays into every hit: Download's stage, Forecast's and Multitype's types. A scenario is a hit in a
    /// battle that has only begun, with the stages it is given, so they are left out.
    /// </summary>
    public static readonly HashSet<string> ActOnEntry = new(StringComparer.Ordinal) { "Download", "Forecast", "Multitype" };

    /// <summary>The forms of Let's Go's partners, which generation 9 (the calculator's) doesn't have.</summary>
    public static readonly HashSet<string> FormsNotInGeneration9 = new(StringComparer.Ordinal) { "Eevee-Starter", "Pikachu-Starter" };

    private static readonly List<PokemonSpecies> Species =
        PokemonDatabase.GetAll().Where(s => s.DexNumber >= 1).OrderBy(s => s.DexNumber).Select(s => s.Under(Ruleset.Modern)).ToList();

    private static readonly Dictionary<string, PokemonSpecies> SpeciesByName = Species.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AbilitiesRun = AbilityEffectTable.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Let's Go's partner moves, which the data has and generation 9 (the calculator's) doesn't.</summary>
    public static readonly HashSet<string> NotInGeneration9 = new(StringComparer.Ordinal)
    {
        "Zippy Zap", "Splishy Splash", "Floaty Fall", "Bouncy Bubble", "Buzzy Buzz", "Sizzly Slide", "Glitzy Glow",
        "Baddy Bad", "Sappy Seed", "Freezy Frost", "Sparkly Swirl"
    };

    /// <summary>The moves a scenario may use, as the modern rules have them.</summary>
    public static readonly List<MoveData> Moves = MoveDatabase.GetAll()
        // Generation 0 is ours (plan 06 · R31), which the calculator has never heard of
        .Where(m => m.Kind == MoveKind.Standard && m.Support == MoveEffectSupport.Full && !NotInGeneration9.Contains(m.Name) && m.Generation > 0)
        .Select(m => m.Under(Ruleset.Modern))
        .Where(m => m.Category != MoveCategory.Status && m.Power > 0 && (m.Effect == null || PlainEffects.Contains(m.Effect)))
        .OrderBy(m => m.Id).ToList();

    // Platinum's held items whose effect the engine runs, but the berries that halve a hit as it lands (the core's, not the formula's)
    private static readonly List<ItemData> Items = ItemDatabase.GetAll()
        .Where(i => DataImporter.Coverage.IsPlatinums(i) && HeldItemEffects.IsHoldable(i) && !i.HoldEffect!.StartsWith("Weaken", StringComparison.Ordinal))
        .OrderBy(i => i.Id).ToList();

    /// <summary>A scenario's move as the modern rules have it today, whether or not it would still be drawn.</summary>
    public static MoveData MoveOf(string name) => MoveDatabase.Get(name).Under(Ruleset.Modern);

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
        string? terrain = dice.Next(5) == 0 ? Terrains[dice.Next(Terrains.Length)].Name : null;
        var field = new OracleField(weather, terrain, dice.Next(8) == 0, dice.Next(8) == 0, dice.Next(12) == 0);

        MoveData move;
        int tries = 0;
        do move = Moves[dice.Next(Moves.Count)];
        while (StoppedBeforeTheFormula(move, defender, weather, terrain) && ++tries < 50);
        if (StoppedBeforeTheFormula(move, defender, weather, terrain)) defender = defender with { Ability = null };

        bool spread = doubles && move.Target is MoveTarget.AllFoes or MoveTarget.AllOthers;
        bool crit = dice.Next(8) == 0;
        return new OracleScenario(id, doubles, spread, attacker, defender, move.Name, crit, field);
    }

    /// <summary>
    /// The core keeps the move off before the formula is asked: an ability takes it in, a strong weather puts it out,
    /// Psychic Terrain keeps a move of priority off (whoever is on the ground: any, for the scenario's sake).
    /// </summary>
    public static bool StoppedBeforeTheFormula(MoveData move, OracleMon defender, string? weather, string? terrain)
    {
        if (weather == "Harsh Sunshine" && move.Type == PokemonType.Water) return true;
        if (weather == "Heavy Rain" && move.Type == PokemonType.Fire) return true;
        if (terrain == "Psychic" && move.Priority > 0) return true;
        return defender.Ability switch
        {
            "Levitate" => move.Type == PokemonType.Ground,
            "Water Absorb" or "Dry Skin" => move.Type == PokemonType.Water,
            "Volt Absorb" or "Motor Drive" => move.Type == PokemonType.Electric,
            "Flash Fire" => move.Type == PokemonType.Fire,
            "Soundproof" => (move.Flags & MoveFlags.Sound) != 0,
            // The later abilities that take a move in (plan 06 · R27)
            "Lightning Rod" => move.Type == PokemonType.Electric,
            "Storm Drain" => move.Type == PokemonType.Water,
            "Sap Sipper" => move.Type == PokemonType.Grass,
            "Earth Eater" or "Eelevate" => move.Type == PokemonType.Ground,
            "Well-Baked Body" => move.Type == PokemonType.Fire,
            "Bulletproof" => (move.Flags & MoveFlags.Ballistic) != 0,
            "Wind Rider" => LaterAbilities.IsWind(move),
            "Dazzling" or "Queenly Majesty" or "Armor Tail" => move.Priority > 0,
            "Wonder Guard" => true,
            _ => false
        };
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
        // A female of a species whose females are a form of their own is in it; anyone else may be in a form kept outside battle
        string? form = gender == "F" ? species.Form(species.Name + "-Female")?.Name : null;
        if (form == null && species.Form(species.Name + "-Female") == null)
        {
            var forms = (species.Forms ?? new()).Where(f => f.Kind is FormKind.Regional or FormKind.Alternate && !FormsNotInGeneration9.Contains(f.Name)).ToList();
            if (forms.Count > 0 && dice.Next(3) == 0) form = forms[dice.Next(forms.Count)].Name;
        }

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
        var run = abilities.Where(a => AbilitiesRun.Contains(a) && !ActOnEntry.Contains(a)).ToList();
        string? ability = run.Count == 0 ? null : run[dice.Next(run.Count)];

        string? item = dice.Next(2) == 0 ? Items[dice.Next(Items.Count)].Name : null;

        var boosts = new int[4];
        if (dice.Next(3) == 0) for (int i = 0; i < 4; i++) boosts[i] = dice.Next(13) - 6;

        string status = "";
        if (attacking && dice.Next(8) == 0) status = "brn";
        else if (dice.Next(12) == 0) status = Statuses[dice.Next(Statuses.Length)];

        int hp = 0;
        if (attacking && dice.Next(6) == 0) hp = 1 + dice.Next(1000);    // a share of its HP, in thousandths, made whole by the test and the oracle alike

        return new OracleMon(form ?? species.Name, form, level, gender, nature.ToString(), ability, item, ivs, evs, boosts, status, hp);
    }

    // ------------------------------------------------------------------ the files

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>The scenarios file: a header and a scenario to a line, so a diff reads a scenario at a time.</summary>
    public static string Write(IReadOnlyList<OracleScenario> scenarios)
    {
        var text = new StringBuilder();
        text.Append("{\n  \"about\": \"plan 24 X6: the scenarios OracleScenarios draws (seed ").Append(Seed)
            .Append("), which tools/CalcOracle hands to Pokemon Showdown's calculator. Written by ORACLE=write; don't edit by hand.\",\n");
        text.Append("  \"scenarios\": [\n");
        for (int i = 0; i < scenarios.Count; i++)
        {
            text.Append("    ").Append(JsonSerializer.Serialize(scenarios[i], Json));
            text.Append(i + 1 < scenarios.Count ? ",\n" : "\n");
        }
        text.Append("  ]\n}\n");
        return text.ToString();
    }

    private sealed record ScenarioFile(List<OracleScenario> Scenarios);

    /// <summary>Reads a scenarios file.</summary>
    public static List<OracleScenario> Read(string text) => JsonSerializer.Deserialize<ScenarioFile>(text, Json)!.Scenarios;

    /// <summary>The calculator's answer to one scenario: its sixteen rolls, weakest first, and both Pokémon's stats; or why it refused.</summary>
    public sealed record Answer(int Id, int[]? Damage, int[]? Attacker, int[]? Defender, string? Error, string[]? Notes);

    public sealed record AnswerFile(string Calculator, string Scenarios, List<Answer> Answers);

    public static AnswerFile ReadAnswers(string text) => JsonSerializer.Deserialize<AnswerFile>(text, Json)!;

    /// <summary>The hash the oracle writes of the scenarios it answered: SHA-256 of the file with its line ends made LF.</summary>
    public static string Hash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
