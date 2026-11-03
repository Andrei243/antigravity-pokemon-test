using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Oracle;

/// <summary>
/// The oracle of generation 4 (plan 24 · X5): Platinum's rules' damage formula against Pokémon Showdown's calculator of
/// generation 4, scenario by scenario, as <see cref="OracleTests"/> holds the modern rules to generation 9. The
/// scenarios are drawn by <see cref="OracleScenariosGen4"/> and committed with the calculator's answers
/// (<c>node oracle.mjs --gen 4</c> in <c>tools/CalcOracle</c>, run by the workflow <c>oracle.yml</c>), so no test runs
/// node. Each scenario is built as two <see cref="Battler"/>s under <see cref="Ruleset.Platinum"/> and
/// <see cref="DamageCalculator.Calculate"/> asked for each of the sixteen rolls with the critical hit fixed; the
/// calculator's first roll, its weakest, is our 15. A difference fails unless <c>known-gen4.json</c> names it, with
/// who owes it and its row in <c>docs/mechanics/rulings.md</c>, "Oracle disagreements". The decompilation decides
/// every disagreement, never the calculator, which treats Diamond, Pearl, Platinum, HeartGold and SoulSilver as one.
/// </summary>
public class OracleGen4Tests
{
    private static bool Writing => string.Equals(Environment.GetEnvironmentVariable(OracleTests.Switch), "write", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The committed scenarios can all be built. <c>ORACLE=write</c> draws them afresh; then <c>node oracle.mjs --gen 4</c>
    /// answers them, and both are committed.
    /// </summary>
    [Fact]
    public void TheCommittedScenariosCanAllBeBuilt()
    {
        if (Writing) File.WriteAllText(OracleScenariosGen4.ScenariosPath, OracleScenariosGen4.Write(OracleScenariosGen4.Draw()));
        var scenarios = OracleScenarios.Read(File.ReadAllText(OracleScenariosGen4.ScenariosPath));
        Assert.Equal(OracleScenariosGen4.Count, scenarios.Count);
        foreach (var s in scenarios)
        {
            var (attacker, defender) = Build(s);
            Assert.NotNull(attacker.Pokemon);
            Assert.NotNull(defender.Pokemon);
            Assert.True(OracleScenariosGen4.MoveOf(s.Move).Category != MoveCategory.Status, $"scenario {s.Id}'s {s.Move} no longer hits");
            Assert.True(OracleScenariosGen4.SpeciesOf(s.Attacker).DexNumber <= 493 && OracleScenariosGen4.SpeciesOf(s.Defender).DexNumber <= 493);
        }
    }

    [Fact]
    public void TheScenariosAreDrawnTheSameEveryTime() =>
        Assert.Equal(OracleScenariosGen4.Write(OracleScenariosGen4.Draw()), OracleScenariosGen4.Write(OracleScenariosGen4.Draw()));

    [Fact]
    public void TheCommittedScenariosAreTheOnesDrawn()
    {
        // Unlike generation 9's, Platinum's data is Platinum's and does not move: the scenarios drawn today are the file
        Assert.Equal(File.ReadAllText(OracleScenariosGen4.ScenariosPath).Replace("\r\n", "\n"), OracleScenariosGen4.Write(OracleScenariosGen4.Draw()));
    }

    [Fact]
    public void TheScenarioReaderReadsAFileOfThree()
    {
        var three = OracleScenariosGen4.Draw().Take(3).ToList();
        var read = OracleScenarios.Read(OracleScenariosGen4.Write(three));
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
    public void TheAnswersAreTheCalculatorsToTheseScenarios()
    {
        string scenarios = File.ReadAllText(OracleScenariosGen4.ScenariosPath);
        var answers = OracleScenarios.ReadAnswers(File.ReadAllText(OracleScenariosGen4.AnswersPath));
        Assert.True(answers.Scenarios == OracleScenarios.Hash(scenarios),
            "damage-gen4.json answers other scenarios: run `npm ci` and `node oracle.mjs --gen 4` in tools/CalcOracle and commit the answers");
        Assert.Contains("generation 4", answers.Calculator);
        var ids = OracleScenarios.Read(scenarios).Select(s => s.Id).ToList();
        Assert.Equal(ids, answers.Answers.Select(a => a.Id).ToList());
        Assert.All(answers.Answers, a => Assert.True(a.Error != null || a.Damage is { Length: 16 }, $"scenario {a.Id} has no sixteen rolls"));
    }

    [Fact]
    public void PlatinumsDamageIsGeneration4s()
    {
        var scenarios = OracleScenarios.Read(File.ReadAllText(OracleScenariosGen4.ScenariosPath));
        var answers = OracleScenarios.ReadAnswers(File.ReadAllText(OracleScenariosGen4.AnswersPath)).Answers.ToDictionary(a => a.Id);
        var known = OracleTests.KnownList.Read(File.ReadAllText(OracleScenariosGen4.KnownPath));
        var excused = known.ToDictionary(k => k.Name, _ => 0);
        var unexplained = new List<string>();

        foreach (var s in scenarios)
        {
            var found = Compare(s, answers[s.Id]);
            if (found == null) continue;
            var by = known.FirstOrDefault(k => k.Excuses(s, found, OracleScenariosGen4.MoveOf));
            if (by != null) excused[by.Name]++;
            else unexplained.Add(found.Line);
        }

        string received = Path.Combine(OracleScenarios.Folder, "disagreements-gen4.received.txt");
        bool asked = string.Equals(Environment.GetEnvironmentVariable(OracleTests.Switch), "report", StringComparison.OrdinalIgnoreCase);
        if (asked || unexplained.Count > 0 || excused.Values.Any(n => n == 0))
        {
            var text = new StringBuilder();
            text.Append($"{scenarios.Count} scenarios; {excused.Values.Sum()} disagreements known, {unexplained.Count} not\n\nknown:\n");
            foreach (var (name, n) in excused) text.Append($"  {n,5}  {name}\n");
            text.Append("\nnot known:\n");
            foreach (var line in unexplained) text.Append("  ").Append(line).Append('\n');
            File.WriteAllText(received, text.ToString());
        }
        else if (File.Exists(received)) File.Delete(received);

        Assert.True(unexplained.Count == 0, $"{unexplained.Count} scenarios disagree with generation 4 and known-gen4.json names none of them " +
            $"(all of them in {received}):\n" + string.Join("\n", unexplained.Take(25)));
        var stale = excused.Where(e => e.Value == 0).Select(e => e.Key).ToList();
        Assert.True(stale.Count == 0, "known-gen4.json names differences no scenario shows any more; take them off: " + string.Join(", ", stale));
    }

    /// <summary>
    /// What the oracle found first (41 of its first 47 disagreements): Platinum applies a dual type's two multipliers in the
    /// order of its chart (<c>sTypeMatchupMultipliers</c>, walked from the top by <c>BattleSystem_ApplyTypeChart</c>),
    /// each rounded down, not in the order of the target's own types. A Ground move on Cradily (Rock and Grass) is halved
    /// for the Grass first and doubled for the Rock after, so every roll comes out even; and Ice against Fire is the
    /// chart's last line of Ice, after Flying.
    /// </summary>
    [Fact]
    public void ADualTypesMultipliersComeInTheOrderOfTheOriginalsChart()
    {
        Assert.True(DamageCalculator.ChartOrder(PokemonType.Ground, PokemonType.Grass) < DamageCalculator.ChartOrder(PokemonType.Ground, PokemonType.Rock));
        Assert.True(DamageCalculator.ChartOrder(PokemonType.Fighting, PokemonType.Bug) < DamageCalculator.ChartOrder(PokemonType.Fighting, PokemonType.Rock));
        Assert.True(DamageCalculator.ChartOrder(PokemonType.Ice, PokemonType.Flying) < DamageCalculator.ChartOrder(PokemonType.Ice, PokemonType.Fire));

        // Scenario 1316: L50 Venusaur's Earthquake on a L50 Cradily, no STAB, a neutral hit made of a half and a double
        var s = OracleScenarios.Read(File.ReadAllText(OracleScenariosGen4.ScenariosPath)).Single(x => x.Id == 1316);
        Assert.Equal("Earthquake", s.Move);
        var (attacker, defender) = Build(s);
        var move = new Move(OracleScenariosGen4.MoveOf(s.Move));
        for (int roll = 0; roll < 16; roll++)
        {
            var rng = new BattleRandom(1).Force(RollKind.Critical, 1).Force(RollKind.Damage, roll);
            int damage = DamageCalculator.Calculate(attacker, defender, move, rng, spread: s.Spread, rules: Ruleset.Platinum).Damage;
            Assert.True(damage % 2 == 0, $"roll {roll} came to {damage}: halved after it was doubled");
        }
    }

    /// <summary>Null when we agree with the calculator; otherwise what differs, on one line.</summary>
    internal static OracleTests.Difference? Compare(OracleScenario s, OracleScenarios.Answer answer)
    {
        if (answer.Error != null) return new OracleTests.Difference("refused", null, null, $"#{s.Id} the calculator refused: {answer.Error} | {OracleTests.Describe(s)}");

        Battler attacker, defender;
        try { (attacker, defender) = Build(s); }
        catch (Exception e) { return new OracleTests.Difference("unbuilt", null, null, $"#{s.Id} we can't build it: {e.Message} | {OracleTests.Describe(s)}"); }

        int[] ours = StatsOf(attacker.Pokemon!), theirs = answer.Attacker!;
        if (!ours.SequenceEqual(theirs)) return new OracleTests.Difference("stats", ours, theirs, $"#{s.Id} attacker's stats ours {string.Join("/", ours)} theirs {string.Join("/", theirs)} | {OracleTests.Describe(s)}");
        ours = StatsOf(defender.Pokemon!);
        theirs = answer.Defender!;
        if (!ours.SequenceEqual(theirs)) return new OracleTests.Difference("stats", ours, theirs, $"#{s.Id} defender's stats ours {string.Join("/", ours)} theirs {string.Join("/", theirs)} | {OracleTests.Describe(s)}");

        // The move as the core hands it to the formula: Normal, whatever it was, for a user with Normalize
        var data = OracleScenariosGen4.MoveOf(s.Move);
        if (s.Attacker.Ability == "Normalize" && data.Type != PokemonType.Normal) data = data.OfType(PokemonType.Normal);
        var move = new Move(data);
        var rolls = new int[16];
        bool? crit = null;
        for (int roll = 0; roll < 16; roll++)
        {
            var rng = new BattleRandom(1).Force(RollKind.Critical, s.Crit ? 0 : 1).Force(RollKind.Damage, 15 - roll);
            var result = DamageCalculator.Calculate(attacker, defender, move, rng, spread: s.Spread, rules: Ruleset.Platinum);
            rolls[roll] = result.Damage;
            crit = result.IsCritical;
        }
        var calc = answer.Damage!;
        if (rolls.SequenceEqual(calc)) return null;
        if (calc.All(d => d == 0) && OracleScenarios.StoppedBeforeTheFormula(data, s.Defender, s.Field.Weather, null)) return null;
        int points = rolls.Zip(calc, (a, b) => Math.Abs(a - b)).Max();
        return new OracleTests.Difference("damage", rolls, calc,
            $"#{s.Id} by {points} ({100.0 * points / Math.Max(1, calc.Max()):0.#}%) ours {Range(rolls)} theirs {Range(calc)}{(crit != s.Crit && rolls.Any(r => r > 0) ? " (our hit's critical: " + crit + ")" : "")} | {OracleTests.Describe(s)}");
    }

    private static string Range(int[] rolls) => rolls.Distinct().Count() == 1 ? $"{rolls[0]}" : $"{rolls[0]}-{rolls[15]} [{string.Join(",", rolls)}]";

    private static int[] StatsOf(Pokemon p) => new[] { p.MaxHP, p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed };

    /// <summary>The two battlers of a scenario on a field of their own, as Platinum's rules have them.</summary>
    internal static (Battler Attacker, Battler Defender) Build(OracleScenario s)
    {
        var field = new FieldState();
        var attacker = new Battler(BattleSide.Player, 0) { Pokemon = Make(s.Attacker), Field = field };
        var defender = new Battler(BattleSide.Enemy, 0) { Pokemon = Make(s.Defender), Field = field };
        var both = new[] { attacker, defender };

        if (s.Field.Weather is { } weather) field.Weather = OracleScenariosGen4.Weathers.First(w => w.Name == weather).Weather;
        var side = field.Side(BattleSide.Enemy);
        if (s.Field.Reflect) side.ReflectTurns = 5;
        if (s.Field.LightScreen) side.LightScreenTurns = 5;

        field.WeatherIgnored = () => both.Any(b => b.IsActive && BattleEffects.Of(b).Any(e => e.IgnoresWeather));
        // Two to a side in a double battle, so that a screen is shared as it is there
        field.Standing = _ => s.Doubles ? 2 : 1;
        field.Battlers = () => both;
        return (attacker, defender);
    }

    private static Pokemon Make(OracleMon m)
    {
        var species = OracleScenariosGen4.SpeciesOf(m);
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
}
