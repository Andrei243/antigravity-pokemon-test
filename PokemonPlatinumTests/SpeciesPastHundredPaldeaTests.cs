using System;
using System.Linq;
using System.Text.Json;
using DataImporter;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 9 (Paldea: docs/plans/D14-proposals-paldea.md, approved by the user on 2026-10-26, all 39
/// designs; its questions 3, 4 and 6 answered "b", whose designs wait in D14-proposals-paldea-supplement.md). What is
/// new for this batch beside what <see cref="SpeciesPastHundredTests"/> holds for every batch: Paldea's climb (244 to
/// 256, the Treasures of Ruin at 258), the Treasures' ring, Tatsugiri's and Dondozo's pair, Gholdengo's second hoard of
/// coins, the forms that evolve into one look of ours, and the first numbers past 3000.
/// </summary>
public class SpeciesPastHundredPaldeaTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static int Total(PokemonSpecies s) => s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed;

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    private static readonly string[] Treasures = { "Tabletruin", "Swordruin", "Vesselruin", "Beadruin" };

    [Fact]
    public void PaldeasSpeciesEvolveWithinPaldeasClimb()
    {
        // Question 1's default: the climb wins wherever it starts past 160: 244 to 256 for the region's own, the
        // Treasures of Ruin at 258, Paldea's Champion's level, two under the cap of 260
        var designs = SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root)).Where(d => Species(d.From).Generation == 9).ToList();
        Assert.Equal(39, designs.Count);
        Assert.All(designs.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 244, 256));
        Assert.Equal(Treasures, designs.Where(d => Species(d.From).Legendary).Select(d => d.Name));
        Assert.All(designs.Where(d => Species(d.From).Legendary), d => Assert.Equal(258, d.Evolution.Level));
        Assert.All(designs, d => Assert.InRange(d.Evolution.Level, 1, Ruleset.Modern.LevelCap));
        Assert.Equal(256, designs.Single(d => d.Name == "Bullionaire").Evolution.Level);
    }

    [Fact]
    public void TheTreasuresOfRuinCloseARing()
    {
        // Question 2's default: each at 258 with the next in the party (the tablets with the sword, the sword with the
        // vessel, the vessel with the beads, the beads with the tablets), +90 to 660, banned from the Frontier
        foreach (var (who, next, before, into) in new[]
        {
            ("Wo-Chien", "Chien-Pao", "Chi-Yu", "Tabletruin"), ("Chien-Pao", "Ting-Lu", "Wo-Chien", "Swordruin"),
            ("Ting-Lu", "Chi-Yu", "Chien-Pao", "Vesselruin"), ("Chi-Yu", "Wo-Chien", "Ting-Lu", "Beadruin")
        })
        {
            var p = Mon(who, 258);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(before, 70), Mon(who, 70)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(next, 70)) }));
            Assert.Null(LevelUp(Mon(who, 257), new EvolutionContext { Party = PartyOf(Mon(next, 70)) }));
            Assert.Equal(660, Total(Species(into)));
            Assert.Equal(3, Species(into).CatchRate);
            Assert.True(FrontierRules.IsBannedOfOurs(Species(into)));
            Assert.False(FrontierRules.MayEnter(Mon(into, 258)));
        }
    }

    [Fact]
    public void TatsugiriAndDondozoEachGoFurtherBesideTheOther()
    {
        // The commander and its catfish, each with the other in the party (as batch 3's Latias and Latios); Mabosstiff
        // with a Maschiff
        foreach (var (who, partner, into, level) in new[]
        {
            ("Tatsugiri", "Dondozo", "Tatsushogun", 250), ("Dondozo", "Tatsugiri", "Grandozo", 250), ("Mabosstiff", "Maschiff", "Mastiffdon", 250)
        })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(who, level)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(partner, 5)) }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { Party = PartyOf(Mon(partner, 5)) }));
        }

        // Question 11's default: Commander as the rules have it, kept by Tatsushogun; nothing in the engine changed
        Assert.Equal(new[] { "Commander" }, Species("Tatsushogun").Abilities);
        Assert.Equal(new[] { "Unaware", "Oblivious" }, Species("Grandozo").Abilities);
    }

    [Fact]
    public void GholdengoNeedsASecondHoardOfNineHundredAndNinetyNineCoins()
    {
        // LevelWithItemsInBag with a level (Gimmighoul's own way, past 100): at 256 with 999 Gimmighoul Coins in the
        // bag, which are used up; 998 coins, or level 255, are not enough
        var coin = ItemDatabase.Get("Gimmighoul Coin")!;
        Assert.True(Evolution.IsGatheredForEvolution(coin));
        Assert.NotEqual(ItemPocket.KeyItems, coin.Pocket);

        var bag = new Inventory();
        bag.AddItem(coin, 998);
        var gholdengo = Mon("Gholdengo", 256);
        Assert.Null(LevelUp(gholdengo, new EvolutionContext()));
        Assert.Null(LevelUp(gholdengo, new EvolutionContext { Bag = bag }));
        bag.AddItem(coin, 3);
        Assert.Null(LevelUp(Mon("Gholdengo", 255), new EvolutionContext { Bag = bag }));

        var context = new EvolutionContext { Bag = bag };
        var found = Evolution.Find(gholdengo, EvolutionTrigger.LevelUp, context);
        Assert.Equal("Bullionaire", found?.TargetSpecies);
        Evolution.Evolve(gholdengo, found!, context);
        Assert.Equal("Bullionaire", gholdengo.Species.Name);
        Assert.Equal(2, bag.GetQuantity(coin));
        Assert.Equal("Good as Gold", gholdengo.Ability?.Name);

        // Gimmighoul still becomes Gholdengo by its own 999, at any level, and never skips a stage
        var gimmighoul = Mon("Gimmighoul", 256);
        var hoard = new Inventory();
        hoard.AddItem(coin, 999);
        Assert.Equal("Gholdengo", LevelUp(gimmighoul, new EvolutionContext { Bag = hoard }));

        // Not a legendary's: it may enter the Frontier
        Assert.True(FrontierRules.MayEnter(Mon("Bullionaire", 256)));
    }

    [Fact]
    public void ByDayByNightInTheRainAndByAStoneAgainPastTheirLevel()
    {
        // FriendshipDay past a level (Maushold, Cyclizar), the hour (Lokix, Toedscruel, Houndstone at night; Brambleghast
        // and Rabsca by day), Bellibolt in the rain
        foreach (var (who, into, level) in new[] { ("Maushold", "Squeakshire", 246), ("Cyclizar", "Velocidrake", 250) })
        {
            var p = Mon(who, level);
            p.Friendship = FriendshipRules.EvolveAt;
            Assert.Equal(into, LevelUp(p, new EvolutionContext { IsNight = false }));
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = true }));
            p.Friendship = FriendshipRules.EvolveAt - 1;
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = false }));
        }
        foreach (var (who, into, level, night) in new[]
        {
            ("Lokix", "Locustorm", 246, true), ("Toedscruel", "Myceliarch", 252, true), ("Houndstone", "Barrowhound", 248, true),
            ("Brambleghast", "Thornwraith", 248, false), ("Rabsca", "Khepristar", 246, false)
        })
        {
            Assert.Equal(into, LevelUp(Mon(who, level), new EvolutionContext { IsNight = night }));
            Assert.Null(LevelUp(Mon(who, level), new EvolutionContext { IsNight = !night }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { IsNight = night }));
        }
        Assert.Null(LevelUp(Mon("Bellibolt", 248), new EvolutionContext()));
        Assert.Equal("Thunderbelly", LevelUp(Mon("Bellibolt", 248), new EvolutionContext { IsRaining = true }));

        // The stone that made it, or the cup, used from the bag again past its level and not before
        foreach (var (who, item, into, level) in new[]
        {
            ("Scovillain", "Fire Stone", "Habanerogue", 248), ("Cetitan", "Ice Stone", "Cetiberg", 252), ("Sinistcha", "Masterpiece Teacup", "Chagamage", 250)
        })
        {
            var context = new EvolutionContext { Item = ItemDatabase.Get(item) };
            Assert.Null(Evolution.Find(Mon(who, level - 1), EvolutionTrigger.UseItem, context));
            Assert.Equal(into, Evolution.Find(Mon(who, level), EvolutionTrigger.UseItem, context)?.TargetSpecies);
            Assert.Null(LevelUp(Mon(who, 260), new EvolutionContext()));
        }
    }

    [Fact]
    public void EveryFormOfAForebearBecomesTheOneLookOfOurs()
    {
        // Question 5's default: ours have one look each, whatever form evolves (none of these forms is regional); the
        // female Oinkologne keeps her own look as a female Trufflord, with the species' one set of stats
        var female = new Pokemon(Species("Oinkologne"), 248, Gender.Female, Nature.Hardy, false);
        Assert.Equal("Oinkologne-Female", female.Form);
        female.Moves.Add(new Move(MoveDatabase.Get("Earth Power")));
        var found = Evolution.Find(female, EvolutionTrigger.LevelUp, new EvolutionContext());
        Assert.Equal("Trufflord", found?.TargetSpecies);
        Evolution.Evolve(female, found!, new EvolutionContext());
        Assert.Equal(("Trufflord", (string?)null, Gender.Female), (female.Species.Name, female.Form, female.Gender));
        Assert.True(Species("Trufflord").GenderDifferences);

        foreach (var (who, form, into, level, move) in new[]
        {
            ("Maushold", "Maushold-Family-Of-Three", "Squeakshire", 246, (string?)null),
            ("Dudunsparce", "Dudunsparce-Three-Segment", "Dunsprawl", 252, "Hyper Drill"),
            ("Tatsugiri", "Tatsugiri-Droopy", "Tatsushogun", 250, null),
            ("Tatsugiri", "Tatsugiri-Stretchy", "Tatsushogun", 250, null),
            ("Squawkabilly", "Squawkabilly-White-Plumage", "Bossquawk", 244, "Taunt")
        })
        {
            var p = Mon(who, level);
            p.ChangeForm(form);
            p.Friendship = 255;
            if (move != null) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            var context = new EvolutionContext { IsNight = false, Party = PartyOf(p, Mon("Dondozo", 5)) };
            var e = Evolution.Find(p, EvolutionTrigger.LevelUp, context);
            Assert.Equal(into, e?.TargetSpecies);
            Evolution.Evolve(p, e!, context);
            Assert.Null(p.Form);
        }

        var masterpiece = Mon("Sinistcha", 250);
        masterpiece.ChangeForm("Sinistcha-Masterpiece");
        Assert.Equal("Chagamage", Evolution.Find(masterpiece, EvolutionTrigger.UseItem, new EvolutionContext { Item = ItemDatabase.Get("Masterpiece Teacup") })?.TargetSpecies);
    }

    [Fact]
    public void TheFirstNumbersPastThreeThousandAreSpeciesAndMeetNoMove()
    {
        // Question 8's default: 2000 + the forebear's, so Gholdengo's is 3000, the Treasures' 3001 to 3004 and
        // Sinistcha's 3013; R31's moves numbered 3001 to 3072 are another table
        foreach (var (name, number) in new[] { ("Bullionaire", 3000), ("Tabletruin", 3001), ("Beadruin", 3004), ("Chagamage", 3013) })
        {
            Assert.Equal(number, Species(name).DexNumber);
            Assert.Same(Species(name), PokemonDatabase.GetByDex(number));
            Assert.True(Species(name).IsOurs);
        }
        Assert.Equal("Trufflord", PokemonDatabase.Ours().Where(s => s.DexNumber > 2900).OrderBy(s => s.DexNumber).First().Name);
        Assert.NotNull(MoveDatabase.GetAll().FirstOrDefault(m => m.Id == 3001));

        // Saved and read back, and found by the number where the name is gone
        foreach (var name in new[] { "Bullionaire", "Tabletruin", "Fortarach" })
        {
            var p = new Pokemon(Species(name), 256, new Random(5));
            var saved = SavedPokemonData.FromPokemon(p);
            Assert.Equal(Species(name).DexNumber, saved.DexNumber);
            var read = JsonSerializer.Deserialize<SavedPokemonData>(JsonSerializer.Serialize(saved, GameDataFiles.Json), GameDataFiles.Json)!.ToPokemon();
            Assert.Equal((name, 256, p.Personality), (read.Species.Name, read.Level, read.Personality));
            saved.SpeciesName = "Missingno";
            Assert.Equal(name, saved.ToPokemon().Species.Name);
        }
    }
}
