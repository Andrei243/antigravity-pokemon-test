using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4b-1, the ladder's data (docs/plans/W4b-levelling-the-world.md, design C, approved 2026-10-26): each
/// region's pairs rise, its first route stands at or above the Champion before it less 5, its post-game's strongest is
/// met at min(1.15 × the Champion, the next Champion − 2, 260), and its caps by Badge are its trainers' files' own,
/// mapped where the region is crossed into.
/// </summary>
public class LevelLadderTests
{
    /// <summary>A built region crossed into, its Leaders in the order their Badges are listed, its Champion's ids.</summary>
    private sealed record Rung(string Region, string[] Leaders, string[] Champion, int ChampionOwn, int ChampionMet, int PostGameOwn, int PostGameMet, int[] CapsMet);

    private static readonly Rung[] Ladder =
    {
        new(RegionDatabase.Kanto,
            new[] { "kanto_leader_brock", "kanto_leader_misty", "kanto_leader_lt_surge", "kanto_leader_erika", "kanto_leader_koga", "kanto_leader_sabrina", "kanto_leader_blaine", "kanto_leader_giovanni" },
            new[] { "kanto_champion_first_squirtle", "kanto_champion_first_bulbasaur", "kanto_champion_first_charmander" },
            63, 104, 75, 120, new[] { 70, 75, 77, 81, 90, 90, 93, 95, 104 }),
        new(RegionDatabase.Johto,
            new[] { "johto_leader_falkner_falkner", "johto_leader_bugsy_bugsy", "johto_leader_whitney", "johto_leader_morty_morty", "johto_leader_chuck_chuck", "johto_leader_jasmine_jasmine", "johto_leader_pryce_pryce", "johto_leader_clair_clair" },
            new[] { "johto_champion_lance" },
            50, 136, 88, 156, new[] { 111, 114, 115, 119, 123, 125, 126, 130, 136 }),
        new(RegionDatabase.Hoenn,
            new[] { "hoenn_roxanne_1", "hoenn_brawly_1", "hoenn_wattson_1", "hoenn_flannery_1", "hoenn_norman_1", "hoenn_winona_1", "hoenn_tate_and_liza_1", "hoenn_juan_1" },
            new[] { "hoenn_wallace" },
            58, 162, 78, 183, new[] { 142, 144, 146, 149, 149, 150, 155, 156, 162 }),
        new(RegionDatabase.Unova,
            new[] { "unova_leader_cheren", "unova_leader_roxie", "unova_leader_burgh", "unova_leader_elesa", "unova_leader_clay", "unova_leader_skyla", "unova_leader_drayden", "unova_leader_marlon" },
            new[] { "unova_champion_iris" },
            59, 185, 80, 203, new[] { 166, 168, 171, 173, 175, 177, 181, 182, 185 })
    };

    /// <summary>Cynthia's strongest, where the climb starts; and Kalos's Champion as the W4 study placed it, until plan 23 builds Kalos.</summary>
    private const int SinnohChampion = 62, KalosChampion = 205;

    private static Region RegionOf(string id) => RegionDatabase.Get(id) ?? throw new ArgumentException(id);

    private static int Best(string region, string id) =>
        TrainerDatabase.OfRegion(RegionOf(region).Folder).FirstOrDefault(t => t.Id == id)?.Party.Max(m => m.Level)
        ?? throw new ArgumentException($"{region} has no trainer {id}");

    [Fact]
    public void EveryRegionsPairsRiseOnBothSides()
    {
        foreach (var rung in Ladder)
        {
            var points = RegionOf(rung.Region).Levels!.Points;
            Assert.Equal(3, points.Count);
            for (int i = 1; i < points.Count; i++)
            {
                Assert.True(points[i].From > points[i - 1].From, $"{rung.Region}'s own levels rise");
                Assert.True(points[i].To > points[i - 1].To, $"{rung.Region}'s met levels rise");
            }
            // Every level of its own game, 1 to 100, is met no lower than the one under it, within the cap
            var levels = RegionOf(rung.Region).Levels!;
            for (int l = 2; l <= 100; l++)
            {
                Assert.True(levels.Map(l) >= levels.Map(l - 1), $"{rung.Region} at {l}");
                Assert.InRange(levels.Map(l), 1, Ruleset.HighestLevel);
            }
        }
    }

    [Fact]
    public void EachRegionsFirstRouteIsAtTheChampionBeforeIt()
    {
        int before = SinnohChampion;
        foreach (var rung in Ladder)
        {
            var levels = RegionOf(rung.Region).Levels!;
            Assert.True(levels.Map(2) >= before - 5, $"{rung.Region}'s first route {levels.Map(2)} against the Champion before it, {before}");
            Assert.Equal(rung.ChampionMet, levels.Map(rung.ChampionOwn));
            before = rung.ChampionMet;
        }
    }

    [Fact]
    public void EachPostGamesStrongestIsMetAtItsRuleAndUnderTheNextChampion()
    {
        for (int i = 0; i < Ladder.Length; i++)
        {
            var rung = Ladder[i];
            var region = RegionOf(rung.Region);
            // The post-game's own level is the strongest trainer the region's file has
            Assert.Equal(rung.PostGameOwn, TrainerDatabase.OfRegion(region.Folder).Max(t => t.Party.Count == 0 ? 0 : t.Party.Max(m => m.Level)));
            int next = i + 1 < Ladder.Length ? Ladder[i + 1].ChampionMet : KalosChampion;
            int rule = Math.Min(Math.Min((int)Math.Round(1.15 * rung.ChampionMet, MidpointRounding.AwayFromZero), next - 2), Ruleset.HighestLevel);
            Assert.Equal(rule, rung.PostGameMet);
            var last = region.Levels!.Points[^1];
            Assert.Equal((rung.PostGameOwn, rung.PostGameMet), (last.From, last.To));
            Assert.True(rung.PostGameMet < next, $"{rung.Region}'s post-game {rung.PostGameMet} under the next Champion {next}");
            Assert.True(rung.PostGameMet > rung.ChampionMet);
        }
    }

    [Fact]
    public void EveryCapTableIsItsLeadersBestInRisingOrderThenItsChampions()
    {
        foreach (var rung in Ladder)
        {
            var own = LevelCaps.TableOf(rung.Region);
            Assert.NotNull(own);
            // The Leaders' best in rising order (Johto's Chuck, Jasmine and Pryce come in more than one order), then the Champion's
            var expected = rung.Leaders.Select(id => Best(rung.Region, id)).OrderBy(l => l)
                .Append(rung.Champion.Max(id => Best(rung.Region, id))).ToArray();
            Assert.Equal(expected, own);
            // Mapped where the region is crossed into
            Assert.Equal(rung.CapsMet, LevelCaps.TableOf(rung.Region, crossed: true));
            Assert.Equal(own!.Select(RegionOf(rung.Region).Levels!.Map), LevelCaps.TableOf(rung.Region, crossed: true)!);
        }
        // Sinnoh's is read from its file too, and has no mapping to cross by
        Assert.Equal(new[] { 14, 22, 26, 32, 37, 41, 44, 50, 62 }, LevelCaps.TableOf(RegionDatabase.Sinnoh));
        Assert.Equal(LevelCaps.TableOf(RegionDatabase.Sinnoh), LevelCaps.TableOf(RegionDatabase.Sinnoh, crossed: true));
        // A region with no table yet has none either way
        Assert.Null(LevelCaps.TableOf(RegionDatabase.Kalos));
        Assert.Null(LevelCaps.TableOf(RegionDatabase.Kalos, crossed: true));
    }

    [Fact]
    public void ACappedRunInACrossedRegionIsHeldAtTheMappedCap()
    {
        var capped = new RunRules { LevelCap = true };
        var story = new StoryState();
        Assert.Equal(14, LevelCaps.For(capped, RegionDatabase.Kanto, story));
        Assert.Equal(70, LevelCaps.For(capped, RegionDatabase.Kanto, story, crossed: true));
        story.GiveBadge(RegionDatabase.Kanto, RegionOf(RegionDatabase.Kanto).BadgeNames[0]);
        Assert.Equal(75, LevelCaps.For(capped, RegionDatabase.Kanto, story, crossed: true));
        // Under a Normal run nothing is capped, crossed or not
        Assert.Null(LevelCaps.For(RunRules.Normal, RegionDatabase.Kanto, story, crossed: true));
        // And the region's Hall of Fame ends it
        story.CompleteRegion(RegionDatabase.Kanto);
        Assert.Null(LevelCaps.For(capped, RegionDatabase.Kanto, story, crossed: true));
    }
}
