using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Training (plan 36; not part of <c>all</c>; <c>TRAINING_PART=skills</c> runs one part). TR1, <c>skills</c> (tr01, tr02):
    /// a Bidoof played through two drills by the autopilot (the throw to gold for its Attack, the dash to silver for its Speed),
    /// its SKILLS page with the effort the drills gave and then with its effort full. It prints each drill's score and grade, the
    /// points given and the stats before and after. Later parts (the hall, the stall, the keeper, the powers) add their own.
    /// </summary>
    public void TrainingMode()
    {
        string part = Environment.GetEnvironmentVariable("TRAINING_PART") ?? "";
        if (part == "" || part == "skills") TrainingSkills();
    }

    // A drill played by the autopilot from a seed, then its grade's effort given
    int DrillFor(Pokemon pokemon, StatType stat, int seed, float knack)
    {
        var drill = TrainingDrill.Create(stat, seed);
        var grade = drill.PlayItself(knack);
        int before = EffortRules.Get(pokemon, stat);
        int gain = EffortDrills.Award(pokemon, stat, grade);
        Console.WriteLine($"  {drill.Kind} for {stat}: seed {seed}, score {drill.Score}, {grade} -> +{gain} effort ({before} -> {EffortRules.Get(pokemon, stat)})");
        return gain;
    }

    void ShowSkills(Pokemon pokemon, string shot)
    {
        var screen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        screen.Open();
        screen.ShowSummary = true;
        while (screen.SummaryPage != PartyScreen.SkillsPage) screen.TurnSummaryPage(1);
        Frames(40); Shot(shot);
        screen.ShowSummary = false;
        game.State = GameState.Overworld;
    }

    void TrainingSkills()
    {
        Console.WriteLine($"training: rules in force {(Ruleset.Current == Ruleset.Modern ? "modern" : "platinum")}, a stat holds {Ruleset.Current.EvStatCap} effort at most, {EffortRules.MaxTotal} in all");
        party.Clear();
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50);
        party.Add(bidoof);
        game.Pokedex.RegisterSeen(bidoof.Species.DexNumber);
        game.Pokedex.RegisterCaught(bidoof.Species.DexNumber);
        Console.WriteLine($"  before: Attack {bidoof.Attack}, Speed {bidoof.Speed}, effort {EffortRules.Total(bidoof)}");

        // Two drills, as a player would play them: the throw well, the dash fairly
        int seed = 1;
        while (TrainingDrill.Create(StatType.Attack, seed).PlayItself(0.9f) != DrillGrade.Gold) seed++;
        DrillFor(bidoof, StatType.Attack, seed, 0.9f);
        seed = 1;
        while (TrainingDrill.Create(StatType.Speed, seed).PlayItself() != DrillGrade.Silver) seed++;
        DrillFor(bidoof, StatType.Speed, seed, TrainingDrill.DefaultKnack);
        Console.WriteLine($"  after: Attack {bidoof.Attack}, Speed {bidoof.Speed}, effort {EffortRules.Total(bidoof)} (HP {bidoof.EvHP}, Attack {bidoof.EvAttack}, Defense {bidoof.EvDefense}, Sp. Atk {bidoof.EvSpAttack}, Sp. Def {bidoof.EvSpDefense}, Speed {bidoof.EvSpeed})");
        ShowSkills(bidoof, "tr01_skills_effort");

        // Full: two stats at the most a stat holds and the total at 510, one at its limit shown in gold
        bidoof.EvHP = 0;
        bidoof.EvAttack = Ruleset.Current.EvStatCap;
        bidoof.EvDefense = 0;
        bidoof.EvSpAttack = 0;
        bidoof.EvSpDefense = 6;
        bidoof.EvSpeed = Ruleset.Current.EvStatCap;
        bidoof.ReckonStats();
        Console.WriteLine($"  nearly full: effort {EffortRules.Total(bidoof)}; refusals: {EffortDrills.WhyNot(bidoof, StatType.Attack)} / {EffortDrills.WhyNot(bidoof, StatType.Defense) ?? "none"}");
        bidoof.EvDefense = 510 - EffortRules.Total(bidoof);
        bidoof.ReckonStats();
        Console.WriteLine($"  at 510: effort {EffortRules.Total(bidoof)}; {EffortDrills.WhyNot(bidoof, StatType.SpAttack)}");
        ShowSkills(bidoof, "tr02_skills_effort_full");
    }
}
