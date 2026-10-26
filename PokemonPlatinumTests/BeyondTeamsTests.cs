using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Leagues' teams beyond (plan 03 · D15, design T; docs/plans/D15-species-of-ours-in-the-world.md): 26 teams of
/// species of our own, one for each Elite Four and Champion of the five built regions, each the rematch team it
/// replaces slot for slot, found by <see cref="BattleCore.BeyondTeamOf"/> and ready at the region's Hall of Fame with a
/// Pokémon of the team at the batch's lowest evolution level (question 10's default).
/// </summary>
public class BeyondTeamsTests
{
    /// <summary>Each team beyond, the team it is made from and the region of its batch.</summary>
    public static readonly (string Beyond, string From, string Region)[] Teams =
    {
        ("elite_four_aaron_beyond", "elite_four_aaron_rematch", RegionDatabase.Sinnoh),
        ("elite_four_bertha_beyond", "elite_four_bertha_rematch", RegionDatabase.Sinnoh),
        ("elite_four_flint_beyond", "elite_four_flint_rematch", RegionDatabase.Sinnoh),
        ("elite_four_lucian_beyond", "elite_four_lucian_rematch", RegionDatabase.Sinnoh),
        ("champion_cynthia_beyond", "champion_cynthia_rematch", RegionDatabase.Sinnoh),
        ("kanto_elite_four_lorelei_beyond", "kanto_elite_four_lorelei_2", RegionDatabase.Kanto),
        ("kanto_elite_four_bruno_beyond", "kanto_elite_four_bruno_2", RegionDatabase.Kanto),
        ("kanto_elite_four_agatha_beyond", "kanto_elite_four_agatha_2", RegionDatabase.Kanto),
        ("kanto_elite_four_lance_beyond", "kanto_elite_four_lance_2", RegionDatabase.Kanto),
        ("kanto_champion_beyond", "kanto_champion_rematch_squirtle", RegionDatabase.Kanto),
        ("johto_elite_four_will_will_beyond", "johto_elite_four_will_will_2", RegionDatabase.Johto),
        ("johto_elite_four_koga_koga_beyond", "johto_elite_four_koga_koga_2", RegionDatabase.Johto),
        ("johto_elite_four_bruno_bruno_beyond", "johto_elite_four_bruno_bruno_2", RegionDatabase.Johto),
        ("johto_elite_four_karen_karen_beyond", "johto_elite_four_karen_karen_2", RegionDatabase.Johto),
        ("johto_champion_lance_beyond", "johto_champion_lance_2", RegionDatabase.Johto),
        ("hoenn_sidney_beyond", "hoenn_sidney_rematch", RegionDatabase.Hoenn),
        ("hoenn_phoebe_beyond", "hoenn_phoebe_rematch", RegionDatabase.Hoenn),
        ("hoenn_glacia_beyond", "hoenn_glacia_rematch", RegionDatabase.Hoenn),
        ("hoenn_drake_beyond", "hoenn_drake_rematch", RegionDatabase.Hoenn),
        ("hoenn_wallace_beyond", "hoenn_wallace_rematch", RegionDatabase.Hoenn),
        ("hoenn_steven_beyond", "hoenn_steven", RegionDatabase.Hoenn),
        ("unova_elite_four_shauntal_beyond", "unova_elite_four_shauntal_4", RegionDatabase.Unova),
        ("unova_elite_four_grimsley_beyond", "unova_elite_four_grimsley_4", RegionDatabase.Unova),
        ("unova_elite_four_marshal_beyond", "unova_elite_four_marshal_4", RegionDatabase.Unova),
        ("unova_elite_four_caitlin_beyond", "unova_elite_four_caitlin_4", RegionDatabase.Unova),
        ("unova_champion_iris_beyond", "unova_champion_iris_4", RegionDatabase.Unova),
    };

    [Fact]
    public void ThereAreTwentySixTeamsOfOneHundredAndFortyFourPokemonOfOurs()
    {
        var beyond = TrainerDatabase.Ours.Where(t => t.Id.EndsWith(BattleCore.BeyondSuffix, StringComparison.Ordinal)).ToList();
        Assert.Equal(Teams.Select(t => t.Beyond).OrderBy(i => i, StringComparer.Ordinal), beyond.Select(t => t.Id).OrderBy(i => i, StringComparer.Ordinal));
        Assert.Equal(144, beyond.Sum(t => t.Party.Count));
        // None of them is Platinum's, and the table the original's trainers go through stays its own
        Assert.DoesNotContain(TrainerDatabase.All, t => t.Id.EndsWith(BattleCore.BeyondSuffix, StringComparison.Ordinal));
    }

    [Fact]
    public void EachTeamIsItsRematchTeamSlotForSlotInSpeciesOfItsRegionsBatch()
    {
        foreach (string region in Teams.Select(t => t.Region).Distinct())
        {
            var used = new HashSet<string>();
            foreach (var (id, from, _) in Teams.Where(t => t.Region == region))
            {
                var team = TrainerDatabase.Get(id)!;
                var before = TrainerDatabase.Get(from)!;
                Assert.Equal((before.Name, before.Class, before.Party.Count, before.PrizeMoney), (team.Name, team.Class, team.Party.Count, team.PrizeMoney));
                Assert.Equal(before.Ai, team.Ai);
                Assert.Equal(before.Items, team.Items);
                foreach (var (mine, theirs) in team.Party.Zip(before.Party))
                {
                    var species = PokemonDatabase.Get(mine.Species)!;
                    Assert.True(OursSlots.Placeable(species), $"{id}: {mine.Species} is no species of ours that may be met");
                    Assert.Equal(region, OursSlots.RegionOf(species));
                    Assert.True(used.Add(mine.Species), $"{mine.Species} is in two of {region}'s teams");
                    // Never under its own evolution, never past the highest level
                    Assert.InRange(mine.Level, OursSlots.EvolutionLevel(species), Ruleset.HighestLevel);
                    Assert.Equal((theirs.Personality, theirs.IvScale, theirs.Item), (mine.Personality, mine.IvScale, mine.Item));
                    Assert.Null(mine.Moves);
                }
                Assert.Equal(region, BattleCore.RegionOfBeyond(team));
            }
        }
    }

    // The levels: the rematch team's as the region maps them (W4b-1's post-game pair), lifted where the region's batch
    // would stand above it so that the Champion's best is the batch's highest evolution and four (Sinnoh +46, Kanto +14)
    [Theory]
    [InlineData("champion_cynthia_beyond", 124)]
    [InlineData("kanto_champion_beyond", 134)]
    [InlineData("johto_champion_lance_beyond", 149)]
    [InlineData("hoenn_wallace_beyond", 170)]
    [InlineData("hoenn_steven_beyond", 183)]
    [InlineData("unova_champion_iris_beyond", 202)]
    public void AChampionsBestStandsAtTheTopOfTheBand(string id, int best) =>
        Assert.Equal(best, TrainerDatabase.Get(id)!.Party.Max(p => p.Level));

    [Fact]
    public void EveryTeamIsBuiltWithMovesItsLevelsTaught()
    {
        foreach (var (id, _, _) in Teams)
            foreach (var p in TrainerDatabase.PartyOf(TrainerDatabase.Get(id)!).Members)
            {
                Assert.InRange(p.Moves.Count, 1, 4);
                Assert.False(p.IsShiny);
            }
    }

    // Every League id the scripts battle with names its team beyond
    [Theory]
    [InlineData("elite_four_aaron", "elite_four_aaron_beyond")]
    [InlineData("elite_four_aaron_rematch", "elite_four_aaron_beyond")]
    [InlineData("elite_four_bertha_rematch", "elite_four_bertha_beyond")]
    [InlineData("elite_four_flint_rematch", "elite_four_flint_beyond")]
    [InlineData("elite_four_lucian_rematch", "elite_four_lucian_beyond")]
    [InlineData("champion_cynthia_rematch", "champion_cynthia_beyond")]
    [InlineData("kanto_elite_four_lorelei_2", "kanto_elite_four_lorelei_beyond")]
    [InlineData("kanto_elite_four_bruno_2", "kanto_elite_four_bruno_beyond")]
    [InlineData("kanto_elite_four_agatha_2", "kanto_elite_four_agatha_beyond")]
    [InlineData("kanto_elite_four_lance_2", "kanto_elite_four_lance_beyond")]
    [InlineData("kanto_champion_rematch_bulbasaur", "kanto_champion_beyond")]
    [InlineData("kanto_champion_rematch_charmander", "kanto_champion_beyond")]
    [InlineData("kanto_champion_rematch_squirtle", "kanto_champion_beyond")]
    [InlineData("kanto_champion_first_squirtle", "kanto_champion_beyond")]
    [InlineData("johto_elite_four_will_will", "johto_elite_four_will_will_beyond")]
    [InlineData("johto_elite_four_koga_koga", "johto_elite_four_koga_koga_beyond")]
    [InlineData("johto_elite_four_bruno_bruno", "johto_elite_four_bruno_bruno_beyond")]
    [InlineData("johto_elite_four_karen_karen", "johto_elite_four_karen_karen_beyond")]
    [InlineData("johto_champion_lance", "johto_champion_lance_beyond")]
    [InlineData("hoenn_sidney_rematch", "hoenn_sidney_beyond")]
    [InlineData("hoenn_phoebe_rematch", "hoenn_phoebe_beyond")]
    [InlineData("hoenn_glacia_rematch", "hoenn_glacia_beyond")]
    [InlineData("hoenn_drake_rematch", "hoenn_drake_beyond")]
    [InlineData("hoenn_wallace_rematch", "hoenn_wallace_beyond")]
    [InlineData("hoenn_steven", "hoenn_steven_beyond")]
    [InlineData("unova_elite_four_shauntal_2", "unova_elite_four_shauntal_beyond")]
    [InlineData("unova_elite_four_grimsley_2", "unova_elite_four_grimsley_beyond")]
    [InlineData("unova_elite_four_marshal_2", "unova_elite_four_marshal_beyond")]
    [InlineData("unova_elite_four_caitlin_2", "unova_elite_four_caitlin_beyond")]
    [InlineData("unova_champion_iris_2", "unova_champion_iris_beyond")]
    public void EveryLeagueIdTheScriptsBattleWithNamesItsTeamBeyond(string id, string beyond)
    {
        Assert.Equal(beyond, BattleCore.BeyondIdOf(id));
        Assert.NotNull(TrainerDatabase.Get(beyond));
    }

    [Theory]
    [InlineData("champion_cynthia_megamax")]
    [InlineData("johto_champion_lance_megamax")]
    [InlineData("elite_four_aaron_beyond")]
    public void ATeamBeyondOrAMegaDynamaxTeamHasNoneBeyondIt(string id) => Assert.Null(BattleCore.BeyondIdOf(id));

    [Theory]
    [InlineData(RegionDatabase.Sinnoh, 101)]
    [InlineData(RegionDatabase.Kanto, 101)]
    [InlineData(RegionDatabase.Johto, 104)]
    [InlineData(RegionDatabase.Hoenn, 140)]
    [InlineData(RegionDatabase.Unova, 165)]
    public void ABatchIsReadyAtItsLowestEvolutionLevel(string region, int level) => Assert.Equal(level, BattleCore.ReadyLevel(region));

    [Theory]
    [InlineData("elite_four_aaron_rematch", RegionDatabase.Sinnoh)]
    [InlineData("kanto_champion_rematch_squirtle", RegionDatabase.Kanto)]
    [InlineData("johto_champion_lance", RegionDatabase.Johto)]
    [InlineData("hoenn_steven", RegionDatabase.Hoenn)]
    [InlineData("unova_elite_four_caitlin_2", RegionDatabase.Unova)]
    public void ATeamBeyondWaitsForTheRegionsHallOfFameAndAPokemonAtTheBatchsLevel(string id, string region)
    {
        int ready = BattleCore.ReadyLevel(region);
        var story = new StoryState();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, ready));
        // No Hall of Fame: the team asked for
        Assert.Null(BattleCore.BeyondTeamOf(id, story, party));
        // Another region's Hall of Fame is not this one's
        story.Set(RegionDatabase.Get(region == RegionDatabase.Sinnoh ? RegionDatabase.Kanto : RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        Assert.Null(BattleCore.BeyondTeamOf(id, story, party));
        story.Set(RegionDatabase.Get(region)!.StoryCompleteFlag);
        Assert.Equal(BattleCore.BeyondIdOf(id), BattleCore.BeyondTeamOf(id, story, party)?.Id);
        // A team that has not been past the batch's lowest level
        var weaker = new Party();
        weaker.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, ready - 1));
        Assert.Null(BattleCore.BeyondTeamOf(id, story, weaker));
        // An Egg counts for nothing
        var egg = new Pokemon(PokemonDatabase.Get("Garchomp")!, ready) { IsEgg = true };
        weaker.Add(egg);
        Assert.Null(BattleCore.BeyondTeamOf(id, story, weaker));
        // Nobody else has one
        Assert.Null(BattleCore.BeyondTeamOf("leader_roark", story, party));
    }

    [Theory]
    [InlineData(100, "elite_four_aaron_rematch")]
    [InlineData(101, "elite_four_aaron_beyond")]
    public void AaronBringsHisTeamBeyondToAChampionWhoseTeamHasBeenPastLevelOneHundred(int level, string fought)
    {
        var game = new OpeningTests.Game();
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, level));
        game.Story.SetBadges("Sinnoh", 0xFF);
        game.Story.Set("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN");
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        game.Arrive("PokemonLeagueNorthPokemonCenter", 11, 11);
        var into = game.Maps["PokemonLeagueElevatorToAaronRoom"].Warps.Single(w => w.TargetMap == "PokemonLeagueAaronRoom");
        game.Facing = Direction.Up;
        game.Arrive(into.TargetMap, into.TargetX, into.TargetY);
        Assert.Contains($"battle {fought} Won", game.Talk("aaron").Log);
    }
}
