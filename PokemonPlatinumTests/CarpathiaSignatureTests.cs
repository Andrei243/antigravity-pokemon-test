using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's signature moves and abilities (plan 46 · CP10; docs/mechanics/rulings.md, "Carpathia's signature moves and
/// abilities"): the seven abilities the starters' last stages and the four legendaries carry in place of CP3's stand-ins, and
/// the seven moves they learn, each by the proposal's sentence. One rule to a test, under the modern rules, nothing left to
/// chance (<see cref="CoreScenario.Calm"/>).
/// </summary>
public class CarpathiaSignatureTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None) =>
        Wild(mine, foe, rolls, Modern, sky);

    private static int Stage(Pokemon p, StatType stat) => p.StatStages.GetValueOrDefault(stat);

    private static Pokemon Snorlax() => Mon("Snorlax", 100);

    /// <summary>The tales told in a log, whatever line carries them.</summary>
    private static List<TaleTold> Told(IEnumerable<BattleEvent> log) => log.OfType<Said>().SelectMany(s => s.Shows).OfType<TaleTold>().ToList();

    // ------------------------------------------------------------------ the data

    [Fact]
    public void TheSevenAbilitiesAndMovesAreInTheDataWithAnEffectEach()
    {
        foreach (var ability in new[] { "Forest Hush", "Anvil", "Echo Pouch", "Noonday", "Eclipse", "Three Heads", "War Cry" })
        {
            var a = AbilityDatabase.Get(ability);
            Assert.NotNull(a);
            Assert.True(a!.IsImplemented, ability);
        }
        string[] moves = { "Codru's Ambush", "Smith's Hammer", "Delta Horn", "Sun Seizure", "Moon Howl", "Hailstorm Rider", "Banner Howl" };
        for (int i = 0; i < moves.Length; i++)
        {
            var m = MoveDatabase.Get(moves[i]);
            Assert.NotNull(m);
            Assert.Equal(0, m!.Generation);
            Assert.Equal(3101 + i, m.Id);
        }
        Assert.Equal(PokemonType.Sound, MoveDatabase.Get("Delta Horn")!.Type);
        Assert.Equal(PokemonType.Sound, MoveDatabase.Get("Moon Howl")!.Type);
        Assert.Equal(PokemonType.Sound, MoveDatabase.Get("Banner Howl")!.Type);
    }

    [Fact]
    public void TheSpeciesCarryThemInPlaceOfTheStandIns()
    {
        foreach (var line in new[] { "Lynkit", "Firlynx", "Codrulynx" }) Assert.Equal("Forest Hush", PokemonDatabase.Get(line)!.HiddenAbility);
        foreach (var line in new[] { "Salamint", "Furnander", "Fierander" }) Assert.Equal("Anvil", PokemonDatabase.Get(line)!.HiddenAbility);
        foreach (var line in new[] { "Pelicub", "Pelireed", "Buciumbill" }) Assert.Equal("Echo Pouch", PokemonDatabase.Get(line)!.HiddenAbility);
        Assert.Equal(new[] { "Noonday" }, PokemonDatabase.Get("Solzmeu")!.Abilities);
        Assert.Equal(new[] { "Eclipse" }, PokemonDatabase.Get("Lunacolac")!.Abilities);
        Assert.Equal(new[] { "Three Heads" }, PokemonDatabase.Get("Furtunaur")!.Abilities);
        Assert.Equal(new[] { "War Cry" }, PokemonDatabase.Get("Stindrac")!.Abilities);
        var signatures = new Dictionary<string, string>
        {
            ["Codrulynx"] = "Codru's Ambush", ["Fierander"] = "Smith's Hammer", ["Buciumbill"] = "Delta Horn", ["Solzmeu"] = "Sun Seizure",
            ["Lunacolac"] = "Moon Howl", ["Furtunaur"] = "Hailstorm Rider", ["Stindrac"] = "Banner Howl"
        };
        foreach (var (species, move) in signatures)
            Assert.Contains(PokemonDatabase.Get(species)!.Learnset, l => l.MoveName == move);
    }

    // ------------------------------------------------------------------ Forest Hush

    [Fact]
    public void ForestHushStopsSoundMovesAtFullHpAndLetsThemInOnceItIsHurt()
    {
        var mine = With(Mon("Codrulynx", 50), "Forest Hush");
        var core = Duel(mine, With(Mon("Snorlax", 50, "Hyper Voice"), "Run Away"));
        int full = mine.MaxHP;
        var said = Turn(core, 0, 0);
        Assert.Equal(full, mine.CurrentHP);
        Assert.Contains(said, l => l.Contains("blocks Hyper Voice"));

        mine.CurrentHP = full - 1;
        Turn(core, 0, 0);
        Assert.True(mine.CurrentHP < full - 1);
    }

    // ------------------------------------------------------------------ Anvil

    [Fact]
    public void AnvilLetsAContactMoveThatMissedDealAQuarter()
    {
        int Lost(string ability, int accuracyRoll, out List<string> said)
        {
            var foe = Snorlax();
            var core = Duel(With(Mon("Fierander", 50, "Iron Tail"), ability), foe, Calm().Force(RollKind.Accuracy, accuracyRoll));
            int hp = foe.CurrentHP;
            said = Turn(core, 0, 0);
            return hp - foe.CurrentHP;
        }
        int hit = Lost("Run Away", 0, out _);
        int missed = Lost("Run Away", 99, out _);
        int glanced = Lost("Anvil", 99, out var said);
        Assert.True(hit > 4);
        Assert.Equal(0, missed);
        Assert.Equal(hit / 4, glanced);
        Assert.Contains(said, l => l.Contains("glanced a blow"));
    }

    [Fact]
    public void AnvilIsNothingToAMoveWithoutContactOrOneThatLands()
    {
        var foe = Snorlax();
        var core = Duel(With(Mon("Fierander", 50, "Fire Blast"), "Anvil"), foe, Calm().Force(RollKind.Accuracy, 99));
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        Assert.Equal(hp, foe.CurrentHP);
    }

    // ------------------------------------------------------------------ Echo Pouch

    [Fact]
    public void EchoPouchStrikesASoundMoveAgainTheTurnAfterAtHalfPower()
    {
        var foe = Snorlax();
        var core = Duel(With(Idling(Mon("Buciumbill", 50, "Hyper Voice")), "Echo Pouch"), foe);
        int start = foe.CurrentHP;
        Turn(core, 0, 0);
        int first = start - foe.CurrentHP;
        Assert.True(first > 4);

        int before = foe.CurrentHP;
        var said = Turn(core, 1, 0);
        int echo = before - foe.CurrentHP;
        Assert.InRange(echo, first / 2 - 2, first / 2 + 2);
        Assert.Contains(said, l => l.Contains("The echo of Hyper Voice"));

        // and it is the once: the turn after that nothing comes
        before = foe.CurrentHP;
        Turn(core, 1, 0);
        Assert.Equal(before, foe.CurrentHP);
    }

    [Fact]
    public void EchoPouchLosesItsEchoWhenItsPokemonLeaves()
    {
        var foe = Snorlax();
        var core = Wild(With(Idling(Mon("Buciumbill", 50, "Hyper Voice")), "Echo Pouch"), foe, Calm(), Modern, BattleWeather.None, BattleTerrain.Plain, Mon("Snorlax", 50));
        Turn(core, 0, 0);
        int before = foe.CurrentHP;
        SwitchTo(core, 1);
        Assert.Equal(before, foe.CurrentHP);
    }

    // ------------------------------------------------------------------ Noonday

    [Fact]
    public void NoondayBringsTheHarshSunThatNoOtherWeatherReplacesAndLeavesWithIt()
    {
        var mine = With(Mon("Solzmeu", 50), "Noonday");
        var bench = Mon("Snorlax", 50);
        var core = Wild(mine, Idling(Mon("Snorlax", 50, "Rain Dance")), Calm(), Modern, BattleWeather.None, BattleTerrain.Plain, bench);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        Turn(core, 0, 0);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        SwitchTo(core, 1, 1);
        Assert.Equal(BattleWeather.None, core.Field.Weather);
    }

    // ------------------------------------------------------------------ Eclipse

    [Fact]
    public void EclipseTellsTheVarcolacsTaleForFiveTurnsAndItEnds()
    {
        var core = Duel(With(Mon("Lunacolac", 50), "Eclipse"), Mon("Snorlax", 50));
        var told = Told(core.TakeLog()).Single();
        Assert.Equal("varcolac", told.Tale);
        Assert.Equal(BattleSide.Player, told.Teller);
        Assert.Equal(5, told.Turns);
        Assert.Equal(80, core.TaleInForce!.Accuracy);
        Assert.Equal(130, core.TaleInForce.Boost!.Percent);
        for (int i = 0; i < 5; i++) Turn(core, 0, 0);
        Assert.Null(core.Field.Tale);
    }

    [Fact]
    public void EclipseDarkensDarkMovesAndDoesNothingUnderPlatinumsRulesOrWhileATaleHoldsTheField()
    {
        // Dark moves are stronger under the tale
        int Took(string ability)
        {
            var foe = Snorlax();
            var core = Duel(With(Mon("Lunacolac", 50, "Crunch"), ability), foe);
            int hp = foe.CurrentHP;
            Turn(core, 0, 0);
            return hp - foe.CurrentHP;
        }
        Assert.InRange(Took("Eclipse") / (double)Took("Run Away"), 1.26, 1.34);
        // Platinum's rules have no Tales
        var platinum = Wild(With(Mon("Lunacolac", 50), "Eclipse"), Mon("Snorlax", 50), Calm(), Ruleset.Platinum);
        Assert.Null(platinum.Field.Tale);
        // Two Eclipses: the second finds a tale told already
        var two = Duel(With(Mon("Lunacolac", 50), "Eclipse"), With(Mon("Lunacolac", 50), "Eclipse"));
        Assert.Single(Told(two.TakeLog()));
    }

    // ------------------------------------------------------------------ Three Heads

    [Fact]
    public void ThreeHeadsStrikesThreeTimesAtFortyPercent()
    {
        int Took(string ability, out List<string> said)
        {
            var foe = Snorlax();
            var core = Duel(With(Mon("Furtunaur", 100, "Dragon Pulse"), ability), foe);
            int hp = foe.CurrentHP;
            said = Turn(core, 0, 0);
            return hp - foe.CurrentHP;
        }
        int three = Took("Three Heads", out var said);
        int one = Took("Run Away", out _);
        Assert.Contains("Hit 3 times!", said);
        Assert.InRange(three / (double)one, 1.14, 1.26);
    }

    [Fact]
    public void ThreeHeadsLeavesAStatusMoveAndASpreadMoveAlone()
    {
        var core = Doubles(new[] { With(Mon("Furtunaur", 50, "Hyper Voice"), "Three Heads"), Mon("Snorlax", 50) }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) }, null, Modern);
        var said = DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.DoesNotContain("Hit 3 times!", said);
    }

    // ------------------------------------------------------------------ War Cry

    [Fact]
    public void WarCryRaisesTheTeamsAttacksAtEachTurnsEndThreeTimesAtMost()
    {
        var stindrac = With(Mon("Stindrac", 50), "War Cry");
        var ally = Mon("Snorlax", 50);
        var core = Doubles(new[] { stindrac, ally }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) }, null, Modern);
        var expected = new[] { 1, 2, 3, 3 };
        foreach (int stage in expected)
        {
            DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
            Assert.Equal(stage, Stage(stindrac, StatType.Attack));
            Assert.Equal(stage, Stage(stindrac, StatType.SpAttack));
            Assert.Equal(stage, Stage(ally, StatType.Attack));
            Assert.Equal(stage, Stage(ally, StatType.SpAttack));
        }
    }

    // ------------------------------------------------------------------ the moves

    [Fact]
    public void CodrusAmbushStrikesFirstAndOnlyOnTheTurnItComesIn()
    {
        var foe = Snorlax();
        var core = Duel(Mon("Codrulynx", 50, "Codru's Ambush"), foe);
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        Assert.True(foe.CurrentHP < hp);
        hp = foe.CurrentHP;
        var said = Turn(core, 0, 0);
        Assert.Equal(hp, foe.CurrentHP);
        Assert.Contains("But it failed!", said);
        Assert.Equal(2, MoveDatabase.Get("Codru's Ambush")!.Priority);
    }

    [Fact]
    public void SmithsHammerRaisesTheUsersDefenseWhenItLands()
    {
        var mine = Mon("Fierander", 50, "Smith's Hammer");
        var core = Duel(mine, Snorlax());
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(mine, StatType.Defense));
    }

    [Fact]
    public void DeltaHornHitsBothFoesAndMayLowerTheirSpecialDefense()
    {
        var a = Mon("Snorlax", 80);
        var b = Mon("Snorlax", 80);
        var core = Doubles(new[] { Mon("Buciumbill", 50, "Delta Horn"), Mon("Snorlax", 50) }, new[] { a, b }, Calm().Force(RollKind.SideEffect, 0), Modern);
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.True(a.CurrentHP < a.MaxHP);
        Assert.True(b.CurrentHP < b.MaxHP);
        Assert.Equal(-1, Stage(a, StatType.SpDefense));
        Assert.Equal(-1, Stage(b, StatType.SpDefense));
    }

    [Fact]
    public void SunSeizureHoldsTheHarshSunFiveTurnsAgainstTheFoesWeather()
    {
        var core = Duel(With(Idling(Mon("Solzmeu", 20, "Sun Seizure")), "Run Away"), Mon("Snorlax", 100, "Rain Dance"));
        Turn(core, 0, 0);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        Assert.False(core.Field.WeatherLasts);
        Assert.True(core.Field.WeatherTurns is > 0 and <= 5);
        for (int i = 0; i < 6; i++)
        {
            if (core.Field.Weather == BattleWeather.None) break;
            Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
            Turn(core, 1, 0);
        }
        Assert.Equal(BattleWeather.None, core.Field.Weather);
    }

    [Fact]
    public void MoonHowlHitsEveryFoeAndRemovesTheWeather()
    {
        var a = Mon("Snorlax", 80);
        var b = Mon("Snorlax", 80);
        var core = Doubles(new[] { With(Mon("Lunacolac", 50, "Moon Howl"), "Run Away"), Mon("Snorlax", 50) }, new[] { a, b }, null, Modern, BattleWeather.Rain);
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.True(a.CurrentHP < a.MaxHP && b.CurrentHP < b.MaxHP);
        Assert.Equal(BattleWeather.None, core.Field.Weather);
    }

    [Fact]
    public void MoonHowlCannotBlowAwayAStrongWeatherAnAbilityHolds()
    {
        var core = Wild(With(Mon("Lunacolac", 50, "Moon Howl"), "Run Away"), With(Mon("Solzmeu", 80), "Noonday"), Calm(), Modern);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        Turn(core, 0, 0);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
    }

    [Fact]
    public void HailstormRiderBringsRainOrTheWeatherOfTheTaleOverTheField()
    {
        var core = Duel(With(Mon("Furtunaur", 50, "Hailstorm Rider"), "Run Away"), Snorlax());
        Turn(core, 0, 0);
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);

        var cold = Duel(With(Mon("Furtunaur", 50, "Hailstorm Rider"), "Run Away"), Snorlax());
        cold.Field.Tale = new TaleState("dochia", BattleSide.Player, 5, TaleTable.Default.Get("dochia")!);
        Turn(cold, 0, 0);
        Assert.Equal(Modern.Sky(BattleWeather.Hail), cold.Field.Weather);
    }

    [Fact]
    public void BannerHowlRaisesTheAttackAndSpecialAttackOfTheUserAndItsAllies()
    {
        var user = With(Mon("Stindrac", 50, "Banner Howl"), "Run Away");
        var ally = Mon("Snorlax", 50);
        var core = Doubles(new[] { user, ally }, new[] { Mon("Snorlax", 80), Mon("Snorlax", 80) }, null, Modern);
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        foreach (var p in new[] { user, ally })
        {
            Assert.Equal(1, Stage(p, StatType.Attack));
            Assert.Equal(1, Stage(p, StatType.SpAttack));
        }
    }
}
