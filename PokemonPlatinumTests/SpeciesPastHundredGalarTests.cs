using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 8 (Galar and Hisui: docs/plans/D14-proposals-galar.md, all 42 designs approved by the user on
/// 2026-10-26, every question at its default but 7 and 8, whose designs wait for D14-proposals-galar-supplement.md).
/// The batch's own band (question 1: within Galar's climb, 226 to 240, the legendary sets at 244) and a test to each
/// condition new to the table: three critical hits in one battle (Sirfetch'd's), damage taken under the Stone Arch
/// (Runerigus's), twenty uses of a move (Wyrdeer's), recoil (Basculegion's), a spin holding a sweet (Alcremie's), and
/// dusk. The rules every design is held to are <see cref="SpeciesPastHundredTests"/>'.
/// </summary>
public class SpeciesPastHundredGalarTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static string? Find(Pokemon p, EvolutionTrigger trigger, EvolutionContext? context = null) =>
        Evolution.Find(p, trigger, context ?? new EvolutionContext())?.TargetSpecies;

    private static string? LevelUp(Pokemon p, EvolutionContext? context = null) => Find(p, EvolutionTrigger.LevelUp, context);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    [Fact]
    public void GalarsAndHisuisSpeciesEvolveWithinGalarsClimb()
    {
        var designs = SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root));
        var galar = designs.Where(d => Species(d.From).Generation == 8 && d.From != "Urshifu").ToList(); // Urshifu's is the supplement's
        Assert.Equal(42, galar.Count);
        Assert.All(galar, d => Assert.InRange(Species(d.Name).DexNumber, 2820, 2905));

        // The legendary sets (Regieleki and Regidrago, Glastrier and Spectrier) and Enamorus at 244, after the Champion
        var legends = galar.Where(d => Species(d.From).Legendary || Species(d.From).Mythical).Select(d => d.Name).ToArray();
        Assert.Equal(new[] { "Regifulgur", "Regidraconis", "Hoarsteed", "Shadesteed", "Enamorata" }, legends);
        Assert.All(galar.Where(d => legends.Contains(d.Name)), d => Assert.Equal(244, d.Evolution.Level));
        Assert.All(galar.Where(d => !legends.Contains(d.Name)), d => Assert.InRange(d.Evolution.Level, 226, 240));

        // Questions 5 and 3: an ability that works for one species alone is replaced; the females' forms are not
        // species of ours
        Assert.Equal(new[] { "Cud Chew" }, Species("Gannetgulp").Abilities);
        Assert.Equal(new[] { "Slush Rush" }, Species("Floeguin").Abilities);
        Assert.Equal(new[] { "Cheek Pouch" }, Species("Gorgehog").Abilities);
        Assert.All(new[] { "Majordomee", "Shoalwraith", "Amplipunk", "Urngeist", "Patisserene" }, name => Assert.Null(Species(name).Forms));

        // Left out, as the document says: the cover legendaries, Eternatus, Zarude and Calyrex (Urshifu's design is
        // the supplement's)
        Assert.All(new[] { "Zacian", "Zamazenta", "Eternatus", "Zarude", "Calyrex" },
            name => Assert.DoesNotContain(Species(name).Evolutions ?? new(), e => PokemonDatabase.Get(e.TargetSpecies)?.IsOurs == true));
    }

    [Fact]
    public void SirfetchdBecomesPaladrakeAfterThreeCriticalHitsInOneBattle()
    {
        // Its forebear's own way again (CriticalHits), past level 234: three critical hits landed in a battle, here
        // on a foe that resists them all, and the battle over (not a level-up)
        var sirfetchd = Mon("Sirfetch'd", 234, "Tackle");
        var battle = Battle(sirfetchd, Mon("Aggron", 260, "Harden"), Steady().Force(RollKind.Critical, 0), Ruleset.Modern);
        for (int i = 0; i < 3; i++) Turn(battle);
        Assert.False(battle.IsBattleOver);
        Assert.Equal(3, Evolution.Progress(sirfetchd, Evolution.CriticalHitsKey));
        Assert.Null(LevelUp(sirfetchd));
        Assert.Equal("Paladrake", Find(sirfetchd, EvolutionTrigger.BattleEnd));

        // Two are not enough; the next battle counts from nothing; at 233 three are not enough
        var two = Mon("Sirfetch'd", 234);
        Evolution.CountCriticalHit(two);
        Evolution.CountCriticalHit(two);
        Assert.Null(Find(two, EvolutionTrigger.BattleEnd));
        Evolution.CountCriticalHit(two);
        Evolution.BeginBattle(two);
        Assert.Null(Find(two, EvolutionTrigger.BattleEnd));
        var early = Mon("Sirfetch'd", 233);
        for (int i = 0; i < 3; i++) Evolution.CountCriticalHit(early);
        Assert.Null(Find(early, EvolutionTrigger.BattleEnd));
    }

    [Fact]
    public void RunerigusBecomesRunedolmenUnderTheStoneArchAfterTakingDamage()
    {
        // LevelAfterDamage under the Stone Arch: 49 HP taken without fainting, then a level-up there
        var arch = new EvolutionContext { Sites = new[] { "Stone Arch" } };
        var runerigus = Mon("Runerigus", 234);
        Evolution.CountDamageTaken(runerigus, 48);
        Assert.Null(LevelUp(runerigus, arch));
        Evolution.CountDamageTaken(runerigus, 5);
        Assert.Equal(49, Evolution.Progress(runerigus, Evolution.DamageKey));
        Assert.Null(LevelUp(runerigus));
        Assert.Null(LevelUp(runerigus, new EvolutionContext { Sites = new[] { "Moss Rock" } }));
        Assert.Equal("Runedolmen", LevelUp(runerigus, arch));

        // Fainting loses the count; below its level the arch does nothing
        Evolution.CountFaint(runerigus);
        Assert.Null(LevelUp(runerigus, arch));
        var early = Mon("Runerigus", 233);
        Evolution.CountDamageTaken(early, 49);
        Assert.Null(LevelUp(early, arch));
    }

    [Fact]
    public void WyrdeerBecomesWyrdstagAfterTwentyPsyshieldBashes()
    {
        // LevelAfterMoveUses, Stantler's own way again: twenty uses of Psyshield Bash, another move counting nothing
        var wyrdeer = Mon("Wyrdeer", 238, "Psyshield Bash");
        for (int i = 0; i < 19; i++) Evolution.CountMoveUse(wyrdeer, "Psyshield Bash");
        Evolution.CountMoveUse(wyrdeer, "Tackle");
        Assert.Null(LevelUp(wyrdeer));
        Evolution.CountMoveUse(wyrdeer, "Psyshield Bash");
        Assert.Equal("Wyrdstag", LevelUp(wyrdeer));

        var early = Mon("Wyrdeer", 237, "Psyshield Bash");
        for (int i = 0; i < 25; i++) Evolution.CountMoveUse(early, "Psyshield Bash");
        Assert.Equal(20, Evolution.Progress(early, Evolution.MoveKey("Psyshield Bash")));
        Assert.Null(LevelUp(early));

        // Counted by a battle's own uses, as Stantler's are
        var fighter = Mon("Wyrdeer", 238, "Psyshield Bash");
        var battle = Battle(fighter, Mon("Blissey", 260, "Harden"), Steady(), Ruleset.Modern);
        Turn(battle);
        Assert.Equal(1, Evolution.Progress(fighter, Evolution.MoveKey("Psyshield Bash")));
    }

    [Fact]
    public void BasculegionBecomesShoalwraithAfterItsRecoilAndHerFemaleToo()
    {
        // LevelAfterRecoil, its forebear's own way again: 294 HP lost to its own recoil without fainting
        var basculegion = Mon("Basculegion", 238);
        Evolution.CountRecoil(basculegion, 293);
        Assert.Null(LevelUp(basculegion));
        Evolution.CountRecoil(basculegion, 1);
        Assert.Equal("Shoalwraith", LevelUp(basculegion));
        Evolution.CountFaint(basculegion);
        Assert.Null(LevelUp(basculegion));

        // Question 3: the female, a form of her own, evolves by the species' row into the one species of ours
        var female = Mon("Basculegion", 238);
        female.ChangeForm("Basculegion-Female");
        Evolution.CountRecoil(female, 294);
        var evolution = Evolution.Find(female, EvolutionTrigger.LevelUp, new EvolutionContext())!;
        Assert.Equal("Shoalwraith", evolution.TargetSpecies);
        Evolution.Evolve(female, evolution, new EvolutionContext());
        Assert.Equal(("Shoalwraith", (string?)null), (female.Species.Name, female.Form));

        var indeedee = Mon("Indeedee", 236);
        indeedee.ChangeForm("Indeedee-Female");
        indeedee.Friendship = 255;
        Assert.Equal("Majordomee", LevelUp(indeedee, new EvolutionContext { IsNight = false }));
        Assert.Null(LevelUp(indeedee, new EvolutionContext { IsNight = true }));
    }

    [Fact]
    public void AlcremieBecomesPatissereneSpunRoundHoldingALoveSweet()
    {
        // SpinHoldingItem, Milcery's own way again, past level 232: a spin, not a level-up, and the sweet used up
        var alcremie = Mon("Alcremie", 232);
        alcremie.HeldItem = ItemDatabase.Get("Love Sweet");
        Assert.Null(LevelUp(alcremie));
        var spun = new EvolutionContext { Spin = new SpinMade(true, 1f) };
        var evolution = Evolution.Find(alcremie, EvolutionTrigger.Spin, spun)!;
        Assert.Equal(("Patisserene", (string?)null), (evolution.TargetSpecies, evolution.TargetForm));
        Evolution.Evolve(alcremie, evolution, spun);
        Assert.Equal("Patisserene", alcremie.Species.Name);
        Assert.Null(alcremie.HeldItem);

        // Another sweet, or the level before, is not enough; an Alcremie of another cream spins the same (question 4)
        var star = Mon("Alcremie", 232);
        star.HeldItem = ItemDatabase.Get("Star Sweet");
        Assert.Null(Find(star, EvolutionTrigger.Spin, spun));
        var early = Mon("Alcremie", 231);
        early.HeldItem = ItemDatabase.Get("Love Sweet");
        Assert.Null(Find(early, EvolutionTrigger.Spin, spun));
        var berry = Mon("Alcremie", 232);
        berry.ChangeForm("Alcremie-Vanilla-Cream-Berry-Sweet");
        berry.HeldItem = ItemDatabase.Get("Love Sweet");
        Assert.Equal("Patisserene", Find(berry, EvolutionTrigger.Spin, spun));
    }

    [Fact]
    public void StonjournerBecomesHenjournerAtDusk()
    {
        // LevelDusk, the table's first: Platinum's evening, and not the day or the night
        var stonjourner = Mon("Stonjourner", 235);
        Assert.Null(LevelUp(stonjourner, new EvolutionContext { IsNight = false }));
        Assert.Null(LevelUp(stonjourner, new EvolutionContext { IsNight = true }));
        Assert.Equal("Henjourner", LevelUp(stonjourner, new EvolutionContext { IsDusk = true }));
        Assert.Null(LevelUp(Mon("Stonjourner", 234), new EvolutionContext { IsDusk = true }));
    }

    [Fact]
    public void TheSetsGoFurtherBesideTheirKingTheirMakerAndTheLand()
    {
        // Regieleki and Regidrago beside Regigigas, Glastrier and Spectrier beside Calyrex, Enamorus beside Landorus
        // (question 2; the user said yes to Enamorata though Unova builds no forces of nature); another of the set, or
        // Tornadus or Thundurus, is not enough
        foreach (var (who, other, maker, into) in new[]
        {
            ("Regieleki", "Regidrago", "Regigigas", "Regifulgur"), ("Regidrago", "Regieleki", "Regigigas", "Regidraconis"),
            ("Glastrier", "Spectrier", "Calyrex", "Hoarsteed"), ("Spectrier", "Glastrier", "Calyrex", "Shadesteed"),
            ("Enamorus", "Tornadus", "Landorus", "Enamorata")
        })
        {
            var p = Mon(who, 244);
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(other, 70), Mon("Thundurus", 70)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(maker, 70)) }));
            Assert.Null(LevelUp(Mon(who, 243), new EvolutionContext { Party = PartyOf(Mon(maker, 70)) }));
            Assert.Equal(660, Species(into).BaseHP + Species(into).BaseAttack + Species(into).BaseDefense
                + Species(into).BaseSpAttack + Species(into).BaseSpDefense + Species(into).BaseSpeed);
            Assert.False(FrontierRules.MayEnter(Mon(into, 244)));
        }

        // A troop beside a troop: Falinks with another Falinks
        var falinks = Mon("Falinks", 233);
        Assert.Null(LevelUp(falinks, new EvolutionContext { Party = PartyOf(falinks) }));
        Assert.Equal("Phalanxion", LevelUp(falinks, new EvolutionContext { Party = PartyOf(falinks, Mon("Falinks", 5)) }));
        Assert.True(FrontierRules.MayEnter(Mon("Phalanxion", 233)));
    }
}
