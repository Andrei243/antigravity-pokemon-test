using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Every finished region's Leaders and League fight again after its Hall of Fame (plan 41 · EG5; docs/mechanics/rulings.md,
/// "The Leaders' rematches in every region"): each Leader, Elite Four member and Champion of Sinnoh, Kanto, Johto,
/// Hoenn and Unova has a team that a script, the phone or Match Call fights once the region's Hall of Fame is entered,
/// met as every rematch is (<see cref="TrainerDatabase.IsRematchTeam"/>), the exceptions named with why. A region built
/// later fails until its session adds its rows or is named as owing them.
/// </summary>
public class RematchParityTests
{
    /// <summary>A first team of a Leader, an Elite Four member or a Champion and the team it battles with once the region is done.</summary>
    public static readonly (string Region, string First, string Again)[] Table =
    {
        // Sinnoh: the Battleground's four a day (plan 02 · S15c), the Elite Four's and Cynthia's rematch teams
        (RegionDatabase.Sinnoh, "leader_roark", "leader_roark_rematch"),
        (RegionDatabase.Sinnoh, "leader_gardenia", "leader_gardenia_rematch"),
        (RegionDatabase.Sinnoh, "leader_maylene", "leader_maylene_rematch"),
        (RegionDatabase.Sinnoh, "leader_wake", "leader_wake_rematch"),
        (RegionDatabase.Sinnoh, "leader_fantina", "leader_fantina_rematch"),
        (RegionDatabase.Sinnoh, "leader_byron", "leader_byron_rematch"),
        (RegionDatabase.Sinnoh, "leader_candice", "leader_candice_rematch"),
        (RegionDatabase.Sinnoh, "leader_volkner", "leader_volkner_rematch"),
        (RegionDatabase.Sinnoh, "elite_four_aaron", "elite_four_aaron_rematch"),
        (RegionDatabase.Sinnoh, "elite_four_bertha", "elite_four_bertha_rematch"),
        (RegionDatabase.Sinnoh, "elite_four_flint", "elite_four_flint_rematch"),
        (RegionDatabase.Sinnoh, "elite_four_lucian", "elite_four_lucian_rematch"),
        (RegionDatabase.Sinnoh, "champion_cynthia", "champion_cynthia_rematch"),
        // Kanto: FireRed's Gyms once a day (EG5), the Four and the Champion after Celio's scene
        (RegionDatabase.Kanto, "kanto_leader_brock", "kanto_leader_brock_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_misty", "kanto_leader_misty_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_lt_surge", "kanto_leader_lt_surge_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_erika", "kanto_leader_erika_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_koga", "kanto_leader_koga_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_sabrina", "kanto_leader_sabrina_rematch"),
        (RegionDatabase.Kanto, "kanto_leader_blaine", "kanto_leader_blaine_rematch"),
        (RegionDatabase.Kanto, "kanto_elite_four_lorelei", "kanto_elite_four_lorelei_2"),
        (RegionDatabase.Kanto, "kanto_elite_four_bruno", "kanto_elite_four_bruno_2"),
        (RegionDatabase.Kanto, "kanto_elite_four_agatha", "kanto_elite_four_agatha_2"),
        (RegionDatabase.Kanto, "kanto_elite_four_lance", "kanto_elite_four_lance_2"),
        (RegionDatabase.Kanto, "kanto_champion_first_squirtle", "kanto_champion_rematch_squirtle"),
        (RegionDatabase.Kanto, "kanto_champion_first_bulbasaur", "kanto_champion_rematch_bulbasaur"),
        (RegionDatabase.Kanto, "kanto_champion_first_charmander", "kanto_champion_rematch_charmander"),
        // Johto: the sixteen by phone (Saffron's dojo), the Four's and Lance's second round in their rooms (EG5)
        (RegionDatabase.Johto, "johto_leader_falkner_falkner", "johto_leader_falkner_falkner_2"),
        (RegionDatabase.Johto, "johto_leader_bugsy_bugsy", "johto_leader_bugsy_bugsy_2"),
        (RegionDatabase.Johto, "johto_leader_whitney", "johto_leader_whitney_2"),
        (RegionDatabase.Johto, "johto_leader_morty_morty", "johto_leader_morty_morty_2"),
        (RegionDatabase.Johto, "johto_leader_pryce_pryce", "johto_leader_pryce_pryce_2"),
        (RegionDatabase.Johto, "johto_leader_jasmine_jasmine", "johto_leader_jasmine_jasmine_2"),
        (RegionDatabase.Johto, "johto_leader_chuck_chuck", "johto_leader_chuck_chuck_2"),
        (RegionDatabase.Johto, "johto_leader_clair_clair", "johto_leader_clair_clair_2"),
        (RegionDatabase.Johto, "johto_leader_brock_brock", "johto_leader_brock_brock_2"),
        (RegionDatabase.Johto, "johto_leader_misty_misty", "johto_leader_misty_misty_2"),
        (RegionDatabase.Johto, "johto_leader_lt_surge_lt__surge", "johto_leader_lt_surge_lt__surge_2"),
        (RegionDatabase.Johto, "johto_leader_erika_erika", "johto_leader_erika_erika_2"),
        (RegionDatabase.Johto, "johto_leader_janine_janine", "johto_leader_janine_janine_2"),
        (RegionDatabase.Johto, "johto_leader_sabrina_sabrina", "johto_leader_sabrina_sabrina_2"),
        (RegionDatabase.Johto, "johto_leader_blaine_blaine", "johto_leader_blaine_blaine_2"),
        (RegionDatabase.Johto, "johto_leader_blue_blue", "johto_leader_blue_blue_2"),
        (RegionDatabase.Johto, "johto_elite_four_will_will", "johto_elite_four_will_will_2"),
        (RegionDatabase.Johto, "johto_elite_four_koga_koga", "johto_elite_four_koga_koga_2"),
        (RegionDatabase.Johto, "johto_elite_four_bruno_bruno", "johto_elite_four_bruno_bruno_2"),
        (RegionDatabase.Johto, "johto_elite_four_karen_karen", "johto_elite_four_karen_karen_2"),
        (RegionDatabase.Johto, "johto_champion_lance", "johto_champion_lance_2"),
        // Hoenn: Match Call's rematches (the Gym Leaders' four more teams each), the Four and Wallace on their day
        (RegionDatabase.Hoenn, "hoenn_roxanne_1", "hoenn_roxanne_2"),
        (RegionDatabase.Hoenn, "hoenn_brawly_1", "hoenn_brawly_2"),
        (RegionDatabase.Hoenn, "hoenn_wattson_1", "hoenn_wattson_2"),
        (RegionDatabase.Hoenn, "hoenn_flannery_1", "hoenn_flannery_2"),
        (RegionDatabase.Hoenn, "hoenn_norman_1", "hoenn_norman_2"),
        (RegionDatabase.Hoenn, "hoenn_winona_1", "hoenn_winona_2"),
        (RegionDatabase.Hoenn, "hoenn_tate_and_liza_1", "hoenn_tate_and_liza_2"),
        (RegionDatabase.Hoenn, "hoenn_juan_1", "hoenn_juan_2"),
        (RegionDatabase.Hoenn, "hoenn_sidney", "hoenn_sidney_rematch"),
        (RegionDatabase.Hoenn, "hoenn_phoebe", "hoenn_phoebe_rematch"),
        (RegionDatabase.Hoenn, "hoenn_glacia", "hoenn_glacia_rematch"),
        (RegionDatabase.Hoenn, "hoenn_drake", "hoenn_drake_rematch"),
        (RegionDatabase.Hoenn, "hoenn_wallace", "hoenn_wallace_rematch"),
        // Unova: Black 2's second teams, in the Gyms (EG5) and in the League's rooms
        (RegionDatabase.Unova, "unova_leader_cheren", "unova_leader_cheren_rematch"),
        (RegionDatabase.Unova, "unova_leader_roxie", "unova_leader_roxie_rematch"),
        (RegionDatabase.Unova, "unova_leader_burgh", "unova_leader_burgh_rematch"),
        (RegionDatabase.Unova, "unova_leader_elesa", "unova_leader_elesa_rematch"),
        (RegionDatabase.Unova, "unova_leader_clay", "unova_leader_clay_rematch"),
        (RegionDatabase.Unova, "unova_leader_skyla", "unova_leader_skyla_rematch"),
        (RegionDatabase.Unova, "unova_leader_drayden", "unova_leader_drayden_rematch"),
        (RegionDatabase.Unova, "unova_leader_marlon", "unova_leader_marlon_rematch"),
        (RegionDatabase.Unova, "unova_elite_four_shauntal", "unova_elite_four_shauntal_2"),
        (RegionDatabase.Unova, "unova_elite_four_grimsley", "unova_elite_four_grimsley_2"),
        (RegionDatabase.Unova, "unova_elite_four_marshal", "unova_elite_four_marshal_2"),
        (RegionDatabase.Unova, "unova_elite_four_caitlin", "unova_elite_four_caitlin_2"),
        (RegionDatabase.Unova, "unova_champion_iris", "unova_champion_iris_2"),
        // Kalos: the Leaders at the Battle Château by the weekday (plan 23, Kalos's last places; Korrina's tower battle
        // too), the Four and Diantha in their rooms once the League is challenged again (plan 23, Kalos finished), and
        // Malva's battle of Looker's episode by her rematch
        (RegionDatabase.Kalos, "kalos_viola_1", "kalos_viola_rematch"),
        (RegionDatabase.Kalos, "kalos_grant_1", "kalos_grant_rematch"),
        (RegionDatabase.Kalos, "kalos_korrina_1", "kalos_korrina_rematch"),
        (RegionDatabase.Kalos, "kalos_korrina_tower", "kalos_korrina_rematch"),
        (RegionDatabase.Kalos, "kalos_ramos_1", "kalos_ramos_rematch"),
        (RegionDatabase.Kalos, "kalos_clemont_1", "kalos_clemont_rematch"),
        (RegionDatabase.Kalos, "kalos_valerie_1", "kalos_valerie_rematch"),
        (RegionDatabase.Kalos, "kalos_olympia_1", "kalos_olympia_rematch"),
        (RegionDatabase.Kalos, "kalos_wulfric_1", "kalos_wulfric_rematch"),
        (RegionDatabase.Kalos, "kalos_malva_1", "kalos_malva_rematch"),
        (RegionDatabase.Kalos, "kalos_malva_looker", "kalos_malva_rematch"),
        (RegionDatabase.Kalos, "kalos_siebold_1", "kalos_siebold_rematch"),
        (RegionDatabase.Kalos, "kalos_wikstrom_1", "kalos_wikstrom_rematch"),
        (RegionDatabase.Kalos, "kalos_drasna_1", "kalos_drasna_rematch"),
        (RegionDatabase.Kalos, "kalos_diantha_1", "kalos_diantha_rematch"),
    };

    /// <summary>The first teams that have no rematch, and why.</summary>
    public static readonly Dictionary<string, string> NoRematch = new()
    {
        ["kanto_leader_giovanni"] = "FireRed leaves Viridian's Gym empty after the Earth Badge (FLAG_HIDE_VIRIDIAN_GIOVANNI); Giovanni battles again in plan 41's epilogue (EG9) as the Rocket leader",
    };

    /// <summary>Finished regions' trainers that the table need not name: a second Pokémon, not a Leader's or a League member's own team.</summary>
    private static bool IsFirstTeam(TrainerRecord t) =>
        (t.Class == "Leader" || t.Class == "Elite Four" || t.Class == "Champion" || t.Class.StartsWith("Leader ", StringComparison.Ordinal) || t.Class.StartsWith("Elite Four ", StringComparison.Ordinal))
        && !Regex.IsMatch(t.Id, "_[2-9]$") && !t.Id.Contains("_rematch", StringComparison.Ordinal) && !t.Id.Contains("_beyond", StringComparison.Ordinal)
        && !t.Id.Contains("_megamax", StringComparison.Ordinal) && !t.Id.Contains("_fight_area", StringComparison.Ordinal);

    /// <summary>Regions built after Unova that have a story, and the session that owes their rows.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        [RegionDatabase.Alola] = "plan 23 · Z7 (Alola's trials and League)",
        [RegionDatabase.Galar] = "plan 33 · GL4 to GL7 (Galar's Gym Challenge, Champion Cup and after the Hall of Fame)",
        // A side region with no Gyms and no League (plan 23 · Z17): its Nobles and the Pearl and Diamond Clans' wardens
        // are fought in plan 23 · Z19 and Z20, whose sessions decide whether they are fought again
        [RegionDatabase.Hisui] = "plan 23 · Z19 and Z20 (Hisui's Nobles and wardens)",
    };

    private static IEnumerable<TrainerRecord> Everyone() =>
        TrainerDatabase.All.Concat(TrainerDatabase.Ours).Concat(RegionDatabase.All.Skip(1).SelectMany(r => TrainerDatabase.OfRegion(r.Folder))).DistinctBy(t => t.Id);

    private static readonly Lazy<string> ScriptsAndRematchData = new(() =>
    {
        string data = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data");
        var parts = Directory.GetFiles(Path.Combine(data, "scripts"), "*.txt").Select(File.ReadAllText).ToList();
        parts.AddRange(Directory.GetFiles(Path.Combine(data, "world"), "rematches.json", SearchOption.AllDirectories).Select(File.ReadAllText));
        return string.Join("\n", parts);
    });

    /// <summary>Whether some script (a battle's <c>as</c>) or a region's rematch data names the team.</summary>
    private static bool SomethingFights(string team) => ScriptsAndRematchData.Value.Contains("\"" + team + "\"", StringComparison.Ordinal);

    [Fact]
    public void EveryLeaderAndLeagueMemberOfAFinishedRegionHasATeamThatIsFoughtAgain()
    {
        var problems = new List<string>();
        foreach (var (region, first, again) in Table)
        {
            if (TrainerDatabase.Get(first) == null) problems.Add($"{region}: {first} is no trainer");
            if (TrainerDatabase.Get(again) == null) { problems.Add($"{region}: {again} is no trainer"); continue; }
            if (!TrainerDatabase.IsRematchTeam(again)) problems.Add($"{region}: {again} is not met as a rematch (TrainerDatabase.IsRematchTeam)");
            if (!SomethingFights(again)) problems.Add($"{region}: no script, phone or Match Call fights {again}");
            if (RematchLevels.RegionOfTeam(again).Id != region) problems.Add($"{region}: {again} belongs to {RematchLevels.RegionOfTeam(again).Id}");
        }
        Assert.Empty(problems);
    }

    [Fact]
    public void NoFirstTeamOfAFinishedRegionIsLeftWithoutARowOrAReason()
    {
        var finished = Table.Select(r => r.Region).Distinct().ToHashSet();
        var named = Table.Select(r => r.First).Concat(NoRematch.Keys).ToHashSet();
        var left = Everyone().Where(IsFirstTeam).Where(t => finished.Contains(RematchLevels.RegionOfTeam(t.Id).Id)).Select(t => t.Id).Where(id => !named.Contains(id)).Order().ToList();
        Assert.Empty(left);
        foreach (string id in NoRematch.Keys) Assert.NotNull(TrainerDatabase.Get(id));
    }

    [Fact]
    public void ARegionBuiltLaterFailsUntilItsRowsAreAddedOrItsSessionIsNamed()
    {
        var finished = Table.Select(r => r.Region).Distinct().ToHashSet();
        foreach (var region in RegionDatabase.All.Where(r => r.HasStory))
        {
            if (finished.Contains(region.Id)) { Assert.False(Owed.ContainsKey(region.Id), $"{region.Id} has rows: take it off the owed list"); continue; }
            Assert.True(Owed.ContainsKey(region.Id), $"{region.Id} has a story and no rematch rows: add them to RematchParityTests.Table, or name the session that owes them in Owed");
        }
        foreach (string id in Owed.Keys) Assert.Contains(RegionDatabase.All, r => r.Id == id);
    }

    [Fact]
    public void TheSecondRoundsAreMetAtTheFurthestRegionsLevelsAsEveryRematchIs()
    {
        // The Leaders' and the League's teams of a region gone past take the furthest region's post-game levels (RematchLevels)
        var story = new StoryState();
        story.Set("UnovaHallOfFame");
        var order = RegionDatabase.All.Select(r => r.Id).ToList();
        int unova = order.IndexOf(RegionDatabase.Unova);
        foreach (var (region, _, again) in Table.Where(r => order.IndexOf(r.Region) < unova))
        {
            Assert.NotNull(RematchLevels.For(again, story, RegionDatabase.Sinnoh));
        }
        // ... and Unova's own, while Unova is the furthest, are met as they are, as are those of a region past it (Kalos's)
        foreach (var (_, _, again) in Table.Where(r => order.IndexOf(r.Region) >= unova))
            Assert.Null(RematchLevels.For(again, story, RegionDatabase.Sinnoh));
        // Kalos's are lifted once a region past Kalos is finished
        story.Set("AlolaHallOfFame");
        foreach (var (_, _, again) in Table.Where(r => r.Region == RegionDatabase.Kalos))
            Assert.NotNull(RematchLevels.For(again, story, RegionDatabase.Sinnoh));
    }

    [Fact]
    public void TheNewTeamsOfKantoKnowOnlyMovesTheirSpeciesCanLearn()
    {
        var bad = new List<string>();
        foreach (var record in TrainerDatabase.Ours.Where(t => (t.Id.StartsWith("kanto_leader_", StringComparison.Ordinal) || t.Id.StartsWith("unova_leader_", StringComparison.Ordinal)) && t.Id.EndsWith("_rematch", StringComparison.Ordinal)))
        {
            Assert.InRange(record.Party.Count, record.Id.StartsWith("unova_", StringComparison.Ordinal) ? 3 : 5, 6);
            foreach (var member in record.Party)
            {
                var species = PokemonDatabase.Get(member.Species);
                Assert.NotNull(species);
                var learnable = Legality.Learnable(species!);
                foreach (string move in member.Moves ?? new List<string>())
                {
                    Assert.NotNull(MoveDatabase.Get(move));
                    if (!record.Id.StartsWith("unova_", StringComparison.Ordinal) && !learnable.Contains(move)) bad.Add($"{record.Id}: {member.Species} can't learn {move}");
                }
                Assert.InRange((member.Moves ?? new List<string>()).Count, 3, 4);
                if (record.Id.StartsWith("unova_", StringComparison.Ordinal)) Assert.InRange(member.Level, 68, 70); else Assert.InRange(member.Level, 50, 65);
            }
        }
        Assert.Empty(bad);
    }
}
