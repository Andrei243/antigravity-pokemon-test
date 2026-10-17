using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Oracle;

/// <summary>
/// The oracle of generation 9 (plan 24 · X6): the modern rules' damage formula against Pokémon Showdown's calculator,
/// scenario by scenario. The scenarios are drawn by <see cref="OracleScenarios"/> and committed with the calculator's
/// answers (<c>tools/CalcOracle</c>, run by the workflow <c>oracle.yml</c>), so no test runs node. Each scenario is
/// built here as two <see cref="Battler"/>s on a field of their own under <see cref="Ruleset.Modern"/>, and
/// <see cref="DamageCalculator.Calculate"/> is asked for each of the sixteen rolls with the critical hit fixed; the
/// calculator's first roll, its weakest, is our 15. A difference fails unless <c>known-gen9.json</c> names it, with
/// the session of plan 06 that owes it and its row in <c>docs/mechanics/rulings.md</c>, "Oracle disagreements".
/// </summary>
public class OracleTests
{
    /// <summary>The environment variable that rewrites the scenarios file from what <see cref="OracleScenarios"/> draws: <c>ORACLE=write</c>.</summary>
    public const string Switch = "ORACLE";

    private static bool Writing => string.Equals(Environment.GetEnvironmentVariable(Switch), "write", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The committed scenarios are the oracle's, and they are what the comparison reads: a session that changes the
    /// data (a move's support, a species' forms) changes what <see cref="OracleScenarios.Draw"/> would draw, and is
    /// not stopped for it, since drawing afresh needs the calculator again. It is stopped only if a scenario can no
    /// longer be built. <c>ORACLE=write</c> draws them afresh; then <c>node oracle.mjs</c> answers them, and both are committed.
    /// </summary>
    [Fact]
    public void TheCommittedScenariosCanAllBeBuilt()
    {
        if (Writing) File.WriteAllText(OracleScenarios.ScenariosPath, OracleScenarios.Write(OracleScenarios.Draw()));
        var scenarios = OracleScenarios.Read(File.ReadAllText(OracleScenarios.ScenariosPath));
        Assert.Equal(OracleScenarios.Count, scenarios.Count);
        foreach (var s in scenarios)
        {
            var (attacker, defender) = Build(s);
            Assert.NotNull(attacker.Pokemon);
            Assert.NotNull(defender.Pokemon);
            Assert.True(OracleScenarios.MoveOf(s.Move).Category != MoveCategory.Status, $"scenario {s.Id}'s {s.Move} no longer hits");
        }
    }

    [Fact]
    public void TheScenariosAreDrawnTheSameEveryTime() =>
        Assert.Equal(OracleScenarios.Write(OracleScenarios.Draw()), OracleScenarios.Write(OracleScenarios.Draw()));

    [Fact]
    public void TheAnswersAreTheCalculatorsToTheseScenarios()
    {
        string scenarios = File.ReadAllText(OracleScenarios.ScenariosPath);
        var answers = OracleScenarios.ReadAnswers(File.ReadAllText(OracleScenarios.AnswersPath));
        Assert.True(answers.Scenarios == OracleScenarios.Hash(scenarios),
            "damage-gen9.json answers other scenarios: run `npm ci` and `node oracle.mjs` in tools/CalcOracle and commit the answers");
        var ids = OracleScenarios.Read(scenarios).Select(s => s.Id).ToList();
        Assert.Equal(ids, answers.Answers.Select(a => a.Id).ToList());
        Assert.All(answers.Answers, a => Assert.True(a.Error != null || a.Damage is { Length: 16 }, $"scenario {a.Id} has no sixteen rolls"));
    }

    [Fact]
    public void TheScenarioReaderReadsAFileOfThree()
    {
        var three = OracleScenarios.Draw().Take(3).ToList();
        var read = OracleScenarios.Read(OracleScenarios.Write(three));
        Assert.Equal(3, read.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(three[i].Id, read[i].Id);
            Assert.Equal(three[i].Move, read[i].Move);
            Assert.Equal(three[i].Attacker.Ivs, read[i].Attacker.Ivs);
            Assert.Equal(three[i].Defender.Evs, read[i].Defender.Evs);
            Assert.Equal(three[i].Field, read[i].Field);
        }
    }

    [Fact]
    public void TheModernRulesAreGeneration9s()
    {
        var scenarios = OracleScenarios.Read(File.ReadAllText(OracleScenarios.ScenariosPath));
        var answers = OracleScenarios.ReadAnswers(File.ReadAllText(OracleScenarios.AnswersPath)).Answers.ToDictionary(a => a.Id);
        var known = KnownList.Read(File.ReadAllText(OracleScenarios.KnownPath));
        var excused = known.ToDictionary(k => k.Name, _ => 0);
        var unexplained = new List<string>();

        foreach (var s in scenarios)
        {
            var answer = answers[s.Id];
            var found = Compare(s, answer);
            if (found == null) continue;
            var by = known.FirstOrDefault(k => k.Excuses(s, found));
            if (by != null) excused[by.Name]++;
            else unexplained.Add(found.Line);
        }

        string report = Report(scenarios.Count, excused, unexplained);
        string received = Path.Combine(OracleScenarios.Folder, "disagreements.received.txt");
        bool asked = string.Equals(Environment.GetEnvironmentVariable(Switch), "report", StringComparison.OrdinalIgnoreCase);
        if (asked || unexplained.Count > 0 || excused.Values.Any(n => n == 0)) File.WriteAllText(received, report);
        else if (File.Exists(received)) File.Delete(received);

        Assert.True(unexplained.Count == 0, $"{unexplained.Count} scenarios disagree with generation 9 and known-gen9.json names none of them " +
            $"(all of them in {received}):\n" + string.Join("\n", unexplained.Take(25)));
        var stale = excused.Where(e => e.Value == 0).Select(e => e.Key).ToList();
        Assert.True(stale.Count == 0, "known-gen9.json names differences no scenario shows any more; take them off: " + string.Join(", ", stale));
    }

    private static string Report(int count, Dictionary<string, int> excused, List<string> unexplained)
    {
        var text = new StringBuilder();
        text.Append($"{count} scenarios; {excused.Values.Sum()} disagreements known, {unexplained.Count} not\n\nknown:\n");
        foreach (var (name, n) in excused) text.Append($"  {n,5}  {name}\n");
        text.Append("\nnot known:\n");
        foreach (var line in unexplained) text.Append("  ").Append(line).Append('\n');
        return text.ToString();
    }

    // ------------------------------------------------------------------ one scenario

    /// <summary>How a scenario came out against the calculator's answer.</summary>
    internal sealed record Difference(string Kind, int[]? Ours, int[]? Theirs, string Line)
    {
        /// <summary>The most any roll differs by, in points.</summary>
        public int Points => Ours == null || Theirs == null ? int.MaxValue : Ours.Zip(Theirs, (a, b) => Math.Abs(a - b)).Max();

        /// <summary>The most any roll differs by, in hundredths of the calculator's strongest roll.</summary>
        public double Percent => Ours == null || Theirs == null ? double.MaxValue : 100.0 * Points / Math.Max(1, Theirs.Max());
    }

    /// <summary>Null when we agree with the calculator; otherwise what differs, on one line.</summary>
    internal static Difference? Compare(OracleScenario s, OracleScenarios.Answer answer)
    {
        if (answer.Error != null) return new Difference("refused", null, null, $"#{s.Id} the calculator refused: {answer.Error} | {Describe(s)}");

        Battler attacker, defender;
        try { (attacker, defender) = Build(s); }
        catch (Exception e) { return new Difference("unbuilt", null, null, $"#{s.Id} we can't build it: {e.Message} | {Describe(s)}"); }

        int[] ours = StatsOf(attacker.Pokemon!), theirs = answer.Attacker!;
        if (!ours.SequenceEqual(theirs)) return new Difference("stats", ours, theirs, $"#{s.Id} attacker's stats ours {string.Join("/", ours)} theirs {string.Join("/", theirs)} | {Describe(s)}");
        ours = StatsOf(defender.Pokemon!);
        theirs = answer.Defender!;
        if (!ours.SequenceEqual(theirs)) return new Difference("stats", ours, theirs, $"#{s.Id} defender's stats ours {string.Join("/", ours)} theirs {string.Join("/", theirs)} | {Describe(s)}");

        var move = new Move(OracleScenarios.MoveOf(s.Move));
        var rolls = new int[16];
        bool? crit = null;
        for (int roll = 0; roll < 16; roll++)
        {
            var rng = new BattleRandom(1).Force(RollKind.Critical, s.Crit ? 0 : 1).Force(RollKind.Damage, 15 - roll);
            var result = DamageCalculator.Calculate(attacker, defender, move, rng, spread: s.Spread, rules: Ruleset.Modern);
            rolls[roll] = result.Damage;
            crit = result.IsCritical;
        }
        var calc = answer.Damage!;
        if (rolls.SequenceEqual(calc)) return null;
        int points = rolls.Zip(calc, (a, b) => Math.Abs(a - b)).Max();
        return new Difference("damage", rolls, calc,
            $"#{s.Id} by {points} ({100.0 * points / Math.Max(1, calc.Max()):0.#}%) ours {Range(rolls)} theirs {Range(calc)}{(crit != s.Crit && rolls.Any(r => r > 0) ? " (our hit's critical: " + crit + ")" : "")} | {Describe(s)}");
    }

    private static string Range(int[] rolls) => rolls.Distinct().Count() == 1 ? $"{rolls[0]}" : $"{rolls[0]}-{rolls[15]} [{string.Join(",", rolls)}]";

    internal static string Describe(OracleScenario s)
    {
        string Mon(OracleMon m) => $"L{m.Level} {m.Species} {m.Gender} {m.Nature} " +
            $"{m.Ability ?? "(no ability)"}{(m.Item == null ? "" : " @" + m.Item)}" +
            $"{(m.Status == "" ? "" : " " + m.Status)}{(m.Hp > 0 ? $" hp{m.Hp}/1000" : "")}" +
            $"{(m.Boosts.Any(b => b != 0) ? " boosts " + string.Join("/", m.Boosts) : "")}";
        var f = s.Field;
        var field = new List<string>();
        if (f.Weather != null) field.Add(f.Weather);
        if (f.Terrain != null) field.Add(f.Terrain + " Terrain");
        if (f.Reflect) field.Add("Reflect");
        if (f.LightScreen) field.Add("Light Screen");
        if (f.AuroraVeil) field.Add("Aurora Veil");
        if (s.Doubles) field.Add(s.Spread ? "doubles, spread" : "doubles");
        return $"{Mon(s.Attacker)} uses {s.Move}{(s.Crit ? " (crit)" : "")} on {Mon(s.Defender)}{(field.Count > 0 ? " | " + string.Join(", ", field) : "")}";
    }

    private static int[] StatsOf(Pokemon p) => new[] { p.MaxHP, p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed };

    /// <summary>The two battlers of a scenario on a field of their own, as the modern rules have them.</summary>
    internal static (Battler Attacker, Battler Defender) Build(OracleScenario s)
    {
        var field = new FieldState();
        var attacker = new Battler(BattleSide.Player, 0) { Pokemon = Make(s.Attacker), Field = field };
        var defender = new Battler(BattleSide.Enemy, 0) { Pokemon = Make(s.Defender), Field = field };
        var both = new[] { attacker, defender };

        if (s.Field.Weather is { } weather) field.Weather = OracleScenarios.Weathers.First(w => w.Name == weather).Weather;
        if (s.Field.Terrain is { } terrain)
        {
            field.Terrain = OracleScenarios.Terrains.First(t => t.Name == terrain).Terrain;
            field.TerrainTurns = 5;
        }
        var side = field.Side(BattleSide.Enemy);
        if (s.Field.Reflect) side.ReflectTurns = 5;
        if (s.Field.LightScreen) side.LightScreenTurns = 5;
        if (s.Field.AuroraVeil) side.AuroraVeilTurns = 5;

        field.WeatherIgnored = () => both.Any(b => b.IsActive && BattleEffects.Of(b).Any(e => e.IgnoresWeather));
        // Two to a side in a double battle, so that a screen is shared as it is there
        field.Standing = _ => s.Doubles ? 2 : 1;
        field.Battlers = () => both;
        return (attacker, defender);
    }

    private static Pokemon Make(OracleMon m)
    {
        var species = OracleScenarios.SpeciesOf(m);
        var p = new Pokemon(species, m.Level, 0u, new BattleRandom(1));
        p.Gender = m.Gender switch { "M" => Gender.Male, "F" => Gender.Female, _ => Gender.Genderless };
        p.ChangeForm(m.Form);
        p.AbilityName = m.Ability;
        p.Nature = Enum.Parse<Nature>(m.Nature);
        p.HeldItem = m.Item == null ? null : ItemDatabase.Get(m.Item) ?? throw new InvalidOperationException($"no item {m.Item}");
        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed) = (m.Ivs[0], m.Ivs[1], m.Ivs[2], m.Ivs[3], m.Ivs[4], m.Ivs[5]);
        (p.EvHP, p.EvAttack, p.EvDefense, p.EvSpAttack, p.EvSpDefense, p.EvSpeed) = (m.Evs[0], m.Evs[1], m.Evs[2], m.Evs[3], m.Evs[4], m.Evs[5]);
        p.RecalculateStats();
        p.CurrentHP = m.Hp > 0 ? Math.Max(1, p.MaxHP * m.Hp / 1000) : p.MaxHP;
        p.Status = m.Status switch
        {
            "brn" => StatusCondition.Burn, "par" => StatusCondition.Paralyze, "psn" => StatusCondition.Poison,
            "tox" => StatusCondition.Toxic, "slp" => StatusCondition.Sleep, "frz" => StatusCondition.Freeze, _ => StatusCondition.None
        };
        p.ResetStatStages();
        p.StatStages[StatType.Attack] = m.Boosts[0];
        p.StatStages[StatType.Defense] = m.Boosts[1];
        p.StatStages[StatType.SpAttack] = m.Boosts[2];
        p.StatStages[StatType.SpDefense] = m.Boosts[3];
        return p;
    }

    // ------------------------------------------------------------------ the known list

    /// <summary>
    /// One difference known and owed (<c>known-gen9.json</c>): a name, the session of plan 06 that owes it, why, and
    /// what a scenario must have for the difference to be this one. Every condition named must hold; a list is any
    /// of its values. <c>within</c> excuses only a difference of at most that many points on every roll.
    /// </summary>
    internal sealed record KnownList(string Name, string Owner, string Reason, Dictionary<string, JsonElement>? When, int? Within, double? WithinPercent, string? Kind)
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

        private sealed record File(List<KnownList> Known);

        public static List<KnownList> Read(string text) => JsonSerializer.Deserialize<File>(text, Json)!.Known;

        public bool Excuses(OracleScenario s, Difference d)
        {
            if (Kind != null && Kind != d.Kind) return false;
            // Within so many points, or so many hundredths of the hit, on every roll (either will do where both are given)
            if (Within != null || WithinPercent != null)
            {
                bool near = (Within is { } points && d.Points <= points) || (WithinPercent is { } share && d.Percent <= share);
                if (!near) return false;
            }
            foreach (var (key, value) in When ?? new())
                if (!Holds(key, value, s)) return false;
            return true;
        }

        private static bool Holds(string key, JsonElement value, OracleScenario s)
        {
            var move = OracleScenarios.MoveOf(s.Move);
            string?[] seen = key switch
            {
                "ability" => new[] { s.Attacker.Ability, s.Defender.Ability },
                "attackerAbility" => new[] { s.Attacker.Ability },
                "defenderAbility" => new[] { s.Defender.Ability },
                "item" => new[] { s.Attacker.Item, s.Defender.Item },
                "attackerItem" => new[] { s.Attacker.Item },
                "defenderItem" => new[] { s.Defender.Item },
                "species" => new[] { s.Attacker.Species, s.Defender.Species },
                "attackerSpecies" => new[] { s.Attacker.Species },
                "defenderSpecies" => new[] { s.Defender.Species },
                "move" => new[] { s.Move },
                "moveType" => new[] { move.Type.ToString() },
                "moveEffect" => new[] { move.Effect },
                "category" => new[] { move.Category.ToString() },
                "weather" => new[] { s.Field.Weather },
                "terrain" => new[] { s.Field.Terrain },
                "attackerStatus" => new[] { s.Attacker.Status },
                "crit" => new[] { s.Crit ? "true" : "false" },
                "doubles" => new[] { s.Doubles ? "true" : "false" },
                "spread" => new[] { s.Spread ? "true" : "false" },
                "screen" => new[] { s.Field.Reflect ? "Reflect" : null, s.Field.LightScreen ? "Light Screen" : null, s.Field.AuroraVeil ? "Aurora Veil" : null },
                _ => throw new InvalidOperationException($"known-gen9.json asks after {key}, which the oracle doesn't know")
            };
            var wanted = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(v => v.ToString()).ToList() : new List<string> { value.ToString() };
            return seen.Any(v => v != null && wanted.Contains(v, StringComparer.Ordinal));
        }
    }
}
