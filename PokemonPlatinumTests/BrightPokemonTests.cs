using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX3: what is seen in the grass. Bright Pokémon by the count of their species (read from the personality, never
/// drawn), the catch streak and the chain of hooks with their steps (drawn only at a step), and glints of hidden items within
/// three tiles.
/// </summary>
public class BrightPokemonTests
{
    private static Pokemon Bidoof(uint personality)
    {
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 20, new Random(3)) { Personality = personality };
        p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = 5;
        return p;
    }

    [Fact]
    public void TheBandsGoByTheCountAndNothingIsBrightWithNoneBeatenOrCaught()
    {
        Assert.Equal(new BrightBand(0, 0, 0), BrightPokemon.BandOf(0));
        Assert.Equal(1, BrightPokemon.BandOf(1).PerfectIvs);
        Assert.Equal(2, BrightPokemon.BandOf(20).PerfectIvs);
        Assert.Equal(3, BrightPokemon.BandOf(50).PerfectIvs);
        Assert.Equal(0, BrightPokemon.BandOf(49).ShinyRolls);
        Assert.Equal(1, BrightPokemon.BandOf(50).ShinyRolls);
        Assert.Equal(5, BrightPokemon.BandOf(500).ShinyRolls);
        for (uint p = 0; p < 2000; p++) Assert.False(BrightPokemon.IsBright(p, BrightPokemon.BandOf(0)));
        // Over many personalities the share bright is about the band's chance
        int bright = 0;
        for (uint p = 0; p < 20000; p++) if (BrightPokemon.IsBright(p * 2654435761u, BrightPokemon.BandOf(50))) bright++;
        Assert.InRange(bright, 20000 * 3 / 100, 20000 * 5 / 100);
    }

    [Fact]
    public void TheSamePersonalityIsTheSameAnswerAndABrightOneHasItsPerfectIvsAndAnEggMove()
    {
        var band = BrightPokemon.BandOf(50);
        uint personality = Enumerable.Range(0, 100000).Select(i => (uint)i * 2654435761u).First(p => BrightPokemon.IsBright(p, band));
        Assert.True(BrightPokemon.IsBright(personality, band));
        Assert.Equal(BrightPokemon.PerfectStats(personality, 3), BrightPokemon.PerfectStats(personality, 3));
        Assert.Equal(3, BrightPokemon.PerfectStats(personality, 3).Distinct().Count());

        var p = Bidoof(personality);
        string? egg = BrightPokemon.EggMoveFor(p);
        Assert.True(BrightPokemon.Apply(p, band));
        Assert.True(p.Bright);
        int perfect = new[] { p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed }.Count(v => v == 31);
        Assert.Equal(3, perfect);
        if (egg != null) Assert.Contains(p.Moves, m => m.Data.Name == egg);
        Assert.True(p.Moves.Count <= 4);

        // One that isn't bright is left as it was
        uint plain = Enumerable.Range(0, 1000).Select(i => (uint)i * 2654435761u).First(x => !BrightPokemon.IsBright(x, band));
        var q = Bidoof(plain);
        var moves = q.Moves.Select(m => m.Data.Name).ToList();
        Assert.False(BrightPokemon.Apply(q, band));
        Assert.False(q.Bright);
        Assert.Equal(5, q.IvAttack);
        Assert.Equal(moves, q.Moves.Select(m => m.Data.Name));
    }

    [Fact]
    public void TheCountIsTheResearchsBeatenAndCaught()
    {
        var dex = new Pokedex();
        var bidoof = PokemonDatabase.Get("Bidoof")!;
        Assert.Equal(0, BrightPokemon.CountOf(dex, bidoof));
        dex.Research.Tell(bidoof, ResearchKind.Defeated, n: 30);
        dex.Research.Tell(bidoof, ResearchKind.Caught, n: 20);
        Assert.Equal(50, BrightPokemon.CountOf(dex, bidoof));
    }

    [Fact]
    public void TheStreakIsCountedAndBrokenByAnotherCatchOrByItsSpeciesFleeing()
    {
        var streak = new CatchStreak();
        Assert.True(streak.IsEmpty);
        for (int i = 0; i < 11; i++) streak.Caught("Bidoof");
        Assert.Equal(11, streak.StreakFor("Bidoof"));
        Assert.Equal(0, streak.StreakFor("Starly"));
        streak.Fled("Starly");
        Assert.Equal(11, streak.Count);
        streak.Caught("Starly");
        Assert.Equal("Starly", streak.Species);
        Assert.Equal(1, streak.Count);
        streak.Fled("Starly");
        Assert.True(streak.IsEmpty);
        // Let's Go's steps
        Assert.Equal(0, CatchStreak.PerfectIvs(10));
        Assert.Equal(2, CatchStreak.PerfectIvs(11));
        Assert.Equal(3, CatchStreak.PerfectIvs(21));
        Assert.Equal(4, CatchStreak.PerfectIvs(31));
        Assert.Equal(3, CatchStreak.ShinyRolls(11));
        Assert.Equal(11, CatchStreak.ShinyRolls(40));
    }

    [Fact]
    public void AStreakOfElevenGivesTheTwelfthItsPerfectIvsAndNothingIsDrawnBelowIt()
    {
        var below = Bidoof(0x1234);
        var rng = new Random(9);
        int next = new Random(9).Next();
        CatchStreak.Bonus(below, 10, rng);
        Assert.Equal(next, rng.Next());
        Assert.Equal(0, new[] { below.IvHP, below.IvAttack, below.IvDefense, below.IvSpAttack, below.IvSpDefense, below.IvSpeed }.Count(v => v == 31));

        var twelfth = Bidoof(0x1234);
        CatchStreak.Bonus(twelfth, 11, new Random(9));
        Assert.Equal(2, new[] { twelfth.IvHP, twelfth.IvAttack, twelfth.IvDefense, twelfth.IvSpAttack, twelfth.IvSpDefense, twelfth.IvSpeed }.Count(v => v == 31));
    }

    [Fact]
    public void AChainOfHooksIsCountedAtItsSpotAndBrokenByEachOfItsBreaks()
    {
        var chain = new CatchStreak();
        Assert.Equal(0, CatchStreak.HookRolls(0));
        for (int i = 0; i < 5; i++) chain.Hooked("Magikarp", "Sinnoh", 10, 12);
        Assert.Equal(5, chain.ChainFor("Magikarp", "Sinnoh", 10, 12));
        Assert.Equal(10, CatchStreak.HookRolls(5));
        Assert.Equal(40, CatchStreak.HookRolls(99));
        // Another spot, another species: nothing, and a hook there begins again
        Assert.Equal(0, chain.ChainFor("Magikarp", "Sinnoh", 11, 12));
        Assert.Equal(0, chain.ChainFor("Goldeen", "Sinnoh", 10, 12));
        chain.Hooked("Goldeen", "Sinnoh", 10, 12);
        Assert.Equal(1, chain.Hooks);
        chain.Hooked("Goldeen", "Sinnoh", 10, 13);
        Assert.Equal(1, chain.Hooks);
        chain.BreakChain();
        Assert.Equal(0, chain.Hooks);
        Assert.True(chain.IsEmpty);
    }

    [Fact]
    public void TheStreakAndTheChainAreSavedAndLeftOutWhenEmpty()
    {
        var empty = JsonSerializer.Serialize(new SaveData());
        Assert.DoesNotContain("\"CatchStreak\"", empty);
        var streak = new CatchStreak { Species = "Bidoof", Count = 12, HookSpecies = "Magikarp", HookMap = "Sinnoh", HookX = 3, HookY = 4, Hooks = 7 };
        var back = JsonSerializer.Deserialize<CatchStreak>(JsonSerializer.Serialize(streak))!;
        Assert.Equal(12, back.Count);
        Assert.Equal("Magikarp", back.HookSpecies);
        Assert.Equal(7, back.Hooks);
    }

    [Fact]
    public void AHiddenItemGlintsOnlyWithinReachAndOnlyUntilItIsFound()
    {
        var map = new Map(20, 20);
        map.HiddenItems[(5, 5)] = new HiddenItem("Potion", 1, "FLAG_HIDDEN_A");
        map.HiddenItems[(12, 5)] = new HiddenItem("Ether", 1, "FLAG_HIDDEN_B");
        var story = new StoryState();
        Assert.Equal(new[] { (5, 5) }, Glints.Near(map, 7, 6, story));
        Assert.Empty(Glints.Near(map, 0, 0, story));
        Assert.Equal(new[] { (12, 5) }, Glints.Near(map, 9, 5, story));
        // Both within reach of a tile between them, the nearer first
        map.HiddenItems[(10, 5)] = new HiddenItem("Ether", 1, "FLAG_HIDDEN_C");
        Assert.Equal(new[] { (10, 5), (5, 5) }, Glints.Near(map, 8, 5, story));
        story.Set("FLAG_HIDDEN_A");
        Assert.DoesNotContain((5, 5), Glints.Near(map, 7, 6, story));
        // A beat every two seconds of the field's clock
        Assert.True(Glints.Beats(1.9, 2.1));
        Assert.False(Glints.Beats(2.1, 3.9));
    }

    [Fact]
    public void TheGlintsRowFollowsTheRulesUnlessChosen()
    {
        Assert.Equal(RulesDefault.Rules, new GameSettings().ItemGlints);
        Assert.True(FieldHelp.On(RulesDefault.Rules, RulesPreset.Modern));
        Assert.False(FieldHelp.On(RulesDefault.Rules, RulesPreset.Platinum));
        Assert.False(FieldHelp.On(RulesDefault.Off, RulesPreset.Modern));
        Assert.Equal(PokemonPlatinumEngine.UI.OptionCategory.Field, PokemonPlatinumEngine.UI.OptionsScreen.CategoryOf(PokemonPlatinumEngine.UI.OptionRow.ItemGlints));
    }
}

/// <summary>Plan 38 · EX3, the engine's part: a Pokémon in sight made with nothing in force draws as it always did.</summary>
[Collection("Shared clocks and dice")]
public class BrightPokemonGameTests
{
    [Fact]
    public void APokemonInSightIsMadeWithTheDrawsItAlwaysWasUntilACountOrAStreakIsAtAStep()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        var entry = new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 4, MaxLevel = 6, Weight = 20 };

        (uint Personality, int Level, int Next) Make(Func<WildEncounterEntry, Pokemon> make)
        {
            Dice.Seed(41);
            var rng = new Random(41);
            game.FieldRandom = rng;
            var made = make(entry);
            return (made.Personality, made.Level, rng.Next());
        }

        try
        {
        var plain = Make(e => engine.MakeWild(e, null));
        Assert.Equal(plain, Make(e => engine.MakeSeenWild(e, null)));
        // Twenty beaten: a band with no rolls, still no draw
        game.Pokedex.Research.Tell(PokemonDatabase.Get("Bidoof")!, ResearchKind.Defeated, n: 20);
        Assert.Equal(plain, Make(e => engine.MakeSeenWild(e, null)));
        // Fifty: a roll more
        game.Pokedex.Research.Tell(PokemonDatabase.Get("Bidoof")!, ResearchKind.Defeated, n: 30);
        Assert.NotEqual(plain.Next, Make(e => engine.MakeSeenWild(e, null)).Next);
        }
        finally
        {
            // A whole game puts the modern rules in force: Platinum's back for the tests after it
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
