using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Remix (plan 13 · V15, V16; <c>docs/mechanics/rulings.md</c>, "Remix"): its files held to the data and to its design
/// rules, its two points of use (a trainer's copy, a map's <see cref="Map.Tables"/>) on and off, the balance of its
/// leaders and the coverage of the 458 species so far, written to <c>docs/mechanics/remix-coverage.md</c>.
/// </summary>
public class RemixTests
{
    private static Remix R => Remix.Sinnoh;

    /// <summary>The batches in the story's order (plan 13: V15 Route 201, V16 to V19 plan 01's regions).</summary>
    public static readonly string[] Batches = { "V15", "V16", "V17", "V18", "V19" };

    /// <summary>What each batch covers, for the coverage table.</summary>
    public static readonly Dictionary<string, string> BatchNames = new()
    {
        ["V15"] = "the choice and Route 201", ["V16"] = "the south-west", ["V17"] = "the centre",
        ["V18"] = "the east and the sea", ["V19"] = "the north and the League"
    };

    /// <summary>
    /// The areas plan 01 · M5 opened that have tables of their own (the south-west): V16 writes every one, and Route 201
    /// is V15's. A later batch adds its own list beside this one.
    /// </summary>
    public static readonly string[] SouthWest =
    {
        "route_201", "lake_verity", "lake_verity_low_water", "twinleaf_town", "route_202", "route_203", "oreburgh_gate_1f",
        "oreburgh_gate_b1f", "oreburgh_mine_b1f", "oreburgh_mine_b2f", "ravaged_path", "route_204_south", "route_204_north",
        "route_205_south", "route_205_north", "valley_windworks_outside", "fuego_ironworks_outside"
    };

    /// <summary>The trainers V16 must write (plan 13 · V16: the story's battles of the south-west, and those who stand in the way first).</summary>
    public static readonly string[] SouthWestTeams =
    {
        "rival_route_203_piplup", "rival_route_203_turtwig", "rival_route_203_chimchar", "leader_roark", "commander_mars_valley_windworks",
        "galactic_grunt_jubilife_city_1", "galactic_grunt_jubilife_city_2", "galactic_grunt_floaroma_meadow_1", "galactic_grunt_floaroma_meadow_2",
        "galactic_grunt_valley_windworks_1", "galactic_grunt_valley_windworks_2", "galactic_grunt_valley_windworks_3"
    };

    /// <summary>The areas plan 01 · M6 opened that have tables (the centre, V17); every room of the Solaceon Ruins besides.</summary>
    public static readonly string[] Centre =
    {
        "eterna_city", "eterna_forest", "route_206", "route_207", "route_208", "route_209", "route_210_south", "route_211_west",
        "mt_coronet_1f_south", "wayward_cave_1f", "wayward_cave_b1f"
    };

    /// <summary>The story's battles of the centre (plan 13 · V17).</summary>
    public static readonly string[] CentreTeams =
    {
        "leader_gardenia", "leader_fantina", "commander_jupiter_team_galactic_eterna_building", "rival_route_209_piplup", "rival_route_209_turtwig",
        "rival_route_209_chimchar", "galactic_grunt_team_galactic_eterna_building_1f_1", "galactic_grunt_team_galactic_eterna_building_3f"
    };

    /// <summary>The areas plan 01 · M7 opened that have tables (the east and the sea, V18), the Great Marsh's six among them.</summary>
    public static readonly string[] East =
    {
        "route_210_north", "celestic_town", "route_211_east", "route_212_north", "route_212_south", "trophy_garden", "pastoria_city",
        "great_marsh_1", "great_marsh_2", "great_marsh_3", "great_marsh_4", "great_marsh_5", "great_marsh_6", "route_213", "route_214",
        "ruin_maniac_cave_short", "valor_lakefront", "route_215", "lake_valor", "route_218", "canalave_city", "iron_island", "iron_island_1f",
        "iron_island_b1f_left_room", "iron_island_b1f_right_room", "iron_island_b2f_left_room", "iron_island_b2f_right_room", "iron_island_b3f",
        "route_219", "route_220", "route_221"
    };

    /// <summary>The story's battles of the east and the sea (plan 13 · V18), the lakes' chapter's among them.</summary>
    public static readonly string[] EastTeams =
    {
        "leader_maylene", "leader_wake", "leader_byron", "galactic_boss_cyrus_celestic_town_ruins", "commander_saturn_valor_cavern", "commander_mars_lake_verity",
        "rival_pastoria_city_piplup", "rival_pastoria_city_turtwig", "rival_pastoria_city_chimchar", "rival_canalave_city_piplup", "rival_canalave_city_turtwig",
        "rival_canalave_city_chimchar", "galactic_grunt_veilstone_city_1", "galactic_grunt_iron_island_1", "galactic_grunt_lake_valor_1", "galactic_grunt_lake_verity_1"
    };

    /// <summary>The areas plan 01 · M8 opened that have tables and the story reaches before the Hall of Fame (the north and the League, V19).</summary>
    public static readonly string[] North =
    {
        "route_216", "route_217", "acuity_lakefront", "lake_acuity", "mt_coronet_1f_north_room_1", "mt_coronet_1f_north_room_2", "mt_coronet_1f_tunnel_room",
        "mt_coronet_2f", "mt_coronet_3f", "mt_coronet_4f_rooms_1_and_2", "mt_coronet_4f_room_3", "mt_coronet_5f", "mt_coronet_6f", "mt_coronet_b1f",
        "mt_coronet_outside_north", "mt_coronet_outside_south", "route_222", "sunyshore_city", "route_223", "victory_road_1f", "victory_road_2f",
        "victory_road_b1f", "pokemon_league", "sendoff_spring"
    };

    /// <summary>The story's battles of the north and the League (plan 13 · V19).</summary>
    public static readonly string[] NorthTeams =
    {
        "leader_candice", "leader_volkner", "commander_saturn_galactic_hq", "galactic_boss_cyrus_galactic_hq", "commander_mars_spear_pillar",
        "commander_jupiter_spear_pillar", "rival_spear_pillar_piplup", "rival_spear_pillar_turtwig", "rival_spear_pillar_chimchar",
        "galactic_boss_cyrus_distortion_world", "rival_pokemon_league_piplup", "rival_pokemon_league_turtwig", "rival_pokemon_league_chimchar",
        "elite_four_aaron", "elite_four_bertha", "elite_four_flint", "elite_four_lucian", "champion_cynthia",
        "galactic_grunt_galactic_hq_1f", "galactic_grunt_mt_coronet_3f_1", "galactic_grunt_spear_pillar_1", "galactic_grunt_spear_pillar_2"
    };

    /// <summary>The pacing targets' levels each leader's, the Elite Four's and the Champion's ace stays at (plan 02; plan 13, decision 8).</summary>
    public static readonly Dictionary<string, int> Aces = new()
    {
        ["leader_roark"] = 14, ["leader_gardenia"] = 22, ["leader_fantina"] = 26, ["leader_maylene"] = 32, ["leader_wake"] = 37,
        ["leader_byron"] = 41, ["leader_candice"] = 44, ["leader_volkner"] = 50, ["elite_four_aaron"] = 53, ["elite_four_bertha"] = 55,
        ["elite_four_flint"] = 57, ["elite_four_lucian"] = 59, ["champion_cynthia"] = 62
    };

    /// <summary>The 458 species of Generations 1 to 4 that are neither legendary nor mythical (plan 13, the Remix design).</summary>
    public static IReadOnlyList<PokemonSpecies> TheSet { get; } = PokemonDatabase.GetAll()
        .Where(s => s.DexNumber is >= 1 and <= 493 && !s.Legendary && !s.Mythical).OrderBy(s => s.DexNumber).ToList();

    private static readonly HashSet<string> SetNames = TheSet.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

    // ------------------------------------------------------------------ the files

    [Fact]
    public void TheSetIsTheFourHundredAndFiftyEight() => Assert.Equal(458, TheSet.Count);

    [Fact]
    public void EveryTrainerIdIsOneOfTheTablesAndEveryAreaKeyOneOfTheWorlds()
    {
        foreach (var team in R.Trainers.Trainers)
            Assert.True(TrainerDatabase.All.Any(t => t.Id == team.Id), $"{team.Id} is no trainer of trainers.json.");
        Assert.Equal(R.Trainers.Trainers.Count, R.TeamIds.Distinct().Count());

        var habitats = GameDataFiles.Load<WorldHabitatsFile>("world/sinnoh/" + WorldHabitatsFile.FileName).Areas.Select(a => a.Key).ToHashSet();
        foreach (var key in R.Wild.Areas.Keys) Assert.True(habitats.Contains(key), $"{key} is no area of habitats.json.");
        Assert.All(R.Trainers.Trainers, t => Assert.Contains(t.Batch, Batches));
        Assert.All(R.Wild.Areas.Values, a => Assert.Contains(a.Batch, Batches));
    }

    [Fact]
    public void EveryTeamIsLegal()
    {
        foreach (var team in R.Trainers.Trainers)
        {
            Assert.InRange(team.Party.Count, 1, Party.MaxSize);
            Assert.False(string.IsNullOrWhiteSpace(team.Note), $"{team.Id} says nothing of what it is built round.");
            if (team.Ai != null) Assert.NotEqual(AiFlags.None, AiFlagNames.Parse(team.Ai));
            foreach (string item in team.Items ?? new()) Assert.NotNull(ItemDatabase.Get(item));
            foreach (var m in team.Party)
            {
                string who = $"{team.Id}'s {m.Species}";
                var species = PokemonDatabase.Get(m.Species);
                Assert.True(species != null, $"{who}: no such species.");
                Assert.True(SetNames.Contains(m.Species), $"{who} is outside the 458.");
                if (m.Form != null) Assert.True(species!.Form(m.Form) != null, $"{who}: no form {m.Form}.");
                Assert.InRange(m.Level, 1, 100);
                // No species before its stage: a Pokémon of this level could be it
                Assert.True(DifficultyRules.StageAt(m.Species, m.Level) == m.Species, $"{who} at {m.Level} is past what its level allows.");
                Assert.InRange(m.Moves.Count, 1, 4);
                Assert.Equal(m.Moves.Count, m.Moves.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                var learnable = Legality.Learnable(species!);
                foreach (string move in m.Moves)
                {
                    Assert.True(MoveDatabase.Has(move), $"{who}: no move {move}.");
                    Assert.True(learnable.Contains(move), $"{who} can't know {move}.");
                }
                if (m.Ability != null) Assert.True(species!.Abilities.Contains(m.Ability) || species.HiddenAbility == m.Ability, $"{who} can't have {m.Ability}.");
                if (m.Item != null) Assert.True(ItemDatabase.Get(m.Item) is { HoldEffect.Length: > 0 }, $"{who}: {m.Item} is nothing to hold.");
                Assert.True(Enum.IsDefined(m.Nature));
                if (m.Ivs != null) { Assert.Equal(6, m.Ivs.Length); Assert.All(m.Ivs, iv => Assert.InRange(iv, 0, 31)); }
                if (m.Evs != null) { Assert.Equal(6, m.Evs.Length); Assert.All(m.Evs, ev => Assert.InRange(ev, 0, 252)); Assert.True(m.Evs.Sum() <= 510); }
            }
        }
    }

    [Fact]
    public void ATeamIsNeverHigherThanTheTables()
    {
        // Remix is harder by species, moves, natures, items and full teams, not by levels (plan 13, decision 8): a
        // leader's ace stays at the pacing level
        foreach (var team in R.Trainers.Trainers)
        {
            var own = TrainerDatabase.Get(team.Id)!;
            Assert.True(team.Party.Max(m => m.Level) <= own.Party.Max(m => m.Level),
                $"{team.Id}'s best is level {team.Party.Max(m => m.Level)}, the table's {own.Party.Max(m => m.Level)}.");
            // A trainer who battles two at a time brings two
            if (own.DoubleBattle) Assert.True(team.Party.Count >= 2, $"{team.Id} battles two at a time.");
        }
        foreach (var (id, ace) in Aces) Assert.Equal(ace, R.TeamOf(id)!.Party.Max(m => m.Level));
    }

    [Fact]
    public void ARivalsBattleHasATeamForEachStarterOrNone()
    {
        var starters = new[] { "piplup", "turtwig", "chimchar" };
        var rivals = R.TeamIds.Where(id => starters.Any(s => id.EndsWith("_" + s, StringComparison.Ordinal)))
            .Select(id => id[..id.LastIndexOf('_')]).Distinct();
        foreach (string battle in rivals)
            foreach (string s in starters)
                Assert.True(R.TeamOf($"{battle}_{s}") != null, $"{battle} has no team for {s}.");
        // Each is the starter the rival took against the player's
        Assert.Equal("Turtwig", R.TeamOf("rival_route_201_piplup")!.Party.Last().Species);
        Assert.Equal("Chimchar", R.TeamOf("rival_route_201_turtwig")!.Party.Last().Species);
        Assert.Equal("Piplup", R.TeamOf("rival_route_201_chimchar")!.Party.Last().Species);
    }

    [Fact]
    public void EveryTableHasPlatinumsSlotsAndLevels()
    {
        foreach (var (key, area) in R.Wild.Areas)
        {
            var own = GameDataFiles.Load<WorldAreaFile>($"world/sinnoh/areas/{key}.json");
            void Same(string what, List<AreaEncounter>? mine, List<AreaEncounter>? theirs, int slots)
            {
                Assert.True((mine?.Count ?? 0) == (theirs?.Count ?? 0), $"{key}'s {what}: Remix has {mine?.Count ?? 0} slots, the area {theirs?.Count ?? 0}.");
                if (mine == null || mine.Count == 0) return;
                Assert.Equal(slots, mine.Count);
                for (int i = 0; i < mine.Count; i++)
                {
                    // The area's own levels, slot by slot (decision 8), and no rate of Remix's own
                    Assert.Equal(theirs![i].Level, mine[i].Level);
                    Assert.Equal(theirs[i].MaxLevel, mine[i].MaxLevel);
                    Assert.Null(mine[i].Weight);
                    var e = mine[i];
                    Assert.True(SetNames.Contains(e.Species), $"{key}'s {what} slot {i}: {e.Species} is outside the 458.");
                    Assert.True(DifficultyRules.StageAt(e.Species, e.Level) == e.Species, $"{key}'s {what} slot {i}: {e.Species} at {e.Level} is past what its level allows.");
                }
            }
            Same("land", area.Land, own.Land, WorldAreaFile.LandSlotWeights.Length);
            Same("water", area.Water, own.Water, WorldAreaFile.WaterSlotWeights.Length);
            Same("old rod", area.OldRod, own.OldRod, WorldAreaFile.WaterSlotWeights.Length);
            Same("good rod", area.GoodRod, own.GoodRod, WorldAreaFile.RodSlotWeights.Length);
            Same("super rod", area.SuperRod, own.SuperRod, WorldAreaFile.RodSlotWeights.Length);
        }
    }

    [Fact]
    public void TheSouthWestIsWrittenWhole()
    {
        foreach (string key in SouthWest) Assert.True(R.HasArea(key), $"{key} has no Remix tables.");
        Assert.Equal("V15", R.Wild.Areas["route_201"].Batch);
        foreach (string id in SouthWestTeams.Concat(new[] { "rival_route_201_piplup", "rival_route_201_turtwig", "rival_route_201_chimchar" }))
            Assert.True(R.TeamOf(id) != null, $"{id} has no Remix team.");
        // Roark's team has more than the table's, and his ace stays the pacing level's 14
        Assert.True(R.TeamOf("leader_roark")!.Party.Count > TrainerDatabase.Get("leader_roark")!.Party.Count);
    }

    [Theory]
    [InlineData("V17")]
    [InlineData("V18")]
    [InlineData("V19")]
    public void ALaterBatchIsWrittenWhole(string batch)
    {
        var (areas, teams) = batch switch { "V17" => (Centre, CentreTeams), "V18" => (East, EastTeams), _ => (North, NorthTeams) };
        foreach (string key in areas)
        {
            Assert.True(R.HasArea(key), $"{key} has no Remix tables.");
            Assert.Equal(batch, R.Wild.Areas[key].Batch);
        }
        foreach (string id in teams)
        {
            Assert.True(R.TeamOf(id) != null, $"{id} has no Remix team.");
            Assert.Equal(batch, R.Trainers.Trainers.First(t => t.Id == id).Batch);
        }
        // Every room of the Solaceon Ruins, the centre's Unown, has its tables
        if (batch == "V17")
            foreach (string key in RuinsRooms) Assert.True(R.HasArea(key), $"{key} has no Remix tables.");
        // A leader's, the Elite Four's and the Champion's teams have more than the table's, or a full six
        foreach (string id in teams.Where(Aces.ContainsKey))
        {
            int mine = R.TeamOf(id)!.Party.Count, theirs = TrainerDatabase.Get(id)!.Party.Count;
            Assert.True(mine > theirs || mine == Party.MaxSize, $"{id} has {mine}, the table {theirs}.");
        }
    }

    /// <summary>Every room of the Solaceon Ruins with a table of its own (V17 writes them all, each Unown's).</summary>
    public static IEnumerable<string> RuinsRooms =>
        GameDataFiles.Load<WorldHabitatsFile>("world/sinnoh/" + WorldHabitatsFile.FileName).Areas.Select(a => a.Key)
            .Where(k => k.StartsWith("solaceon_ruins", StringComparison.Ordinal) && File.Exists(Path.Combine(GameDataFiles.Directory, "world", "sinnoh", "areas", k + ".json"))
                && GameDataFiles.Load<WorldAreaFile>($"world/sinnoh/areas/{k}.json").Land is { Count: > 0 });

    [Fact]
    public void ASafariBattleUnderRemixMeetsOnlyRemixsSpecies()
    {
        var (map, area) = AreaMap("great_marsh_1");
        map.Tables = Remix.For(RunRules.Normal with { Remix = true });
        var designed = R.Wild.Areas["great_marsh_1"].Land!.Select(e => e.Species).ToHashSet();
        // A Safari Game's moment names the day's marsh species, which a Remix table never takes in
        var (table, rate) = map.WildAt(5, 5, moment: new EncounterMoment { MarshDaily = 12345 });
        Assert.Equal(area.LandRate, rate);
        Assert.Equal(12, table.Count);
        Assert.All(table, e => Assert.Contains(e.SpeciesName, designed));
        var met = new HashSet<string>();
        var dice = new Random(11);
        for (int i = 0; i < 3000; i++) met.Add(WildEncounterRules.Meet(table, false, null, dice)!.SpeciesName);
        Assert.Subset(designed, met);
        Assert.Contains("Carnivine", met);
    }

    // ------------------------------------------------------------------ the points of use

    [Fact]
    public void WithRemixOffATrainerAndAMapAreTheirOwn()
    {
        var trainer = Trainer("leader_roark");
        Assert.Same(trainer, Remix.Prepare(trainer, RunRules.Normal));
        Assert.Same(trainer, Remix.Prepare(trainer, null));
        Assert.Same(trainer, Remix.Prepare(trainer, RunRules.Hardcore));
        Assert.Null(Remix.For(RunRules.Normal));
        Assert.Same(trainer, DifficultyRules.Prepare(trainer, RunRules.Normal));

        var (map, area) = AreaMap("route_201");
        Assert.Null(map.Tables);
        Assert.Same(area.WildEncounters, map.WildAt(5, 5).Table);
    }

    [Fact]
    public void WithRemixOnRoute201MeetsOnlyRemixsSpecies()
    {
        var (map, area) = AreaMap("route_201");
        map.Tables = Remix.For(RunRules.Normal with { Remix = true });
        var designed = R.Wild.Areas["route_201"].Land!.Select(e => e.Species).ToHashSet();
        var (table, rate) = map.WildAt(5, 5, moment: new EncounterMoment());
        Assert.Equal(area.LandRate, rate);
        Assert.Equal(12, table.Count);
        Assert.Equal(WorldAreaFile.LandSlotWeights, table.Select(e => e.Weight));
        Assert.All(table, e => Assert.Contains(e.SpeciesName, designed));
        // Met many times over, through the same rules a step uses: never one of Platinum's own here
        var met = new HashSet<string>();
        var dice = new Random(7);
        for (int i = 0; i < 3000; i++) met.Add(WildEncounterRules.Meet(table, false, null, dice)!.SpeciesName);
        Assert.Subset(designed, met);
        Assert.DoesNotContain("Bidoof", met);
        // The water's table of an area Remix writes with none is empty, so nothing is met surfing there
        Assert.Empty(map.WildAt(5, 5, water: true).Table);
    }

    [Fact]
    public void ATrainerUnderRemixFightsRemixsTeamOnACopy()
    {
        var run = RunRules.Normal with { Remix = true };
        var roark = Trainer("leader_roark");
        var copy = Remix.Prepare(roark, run);
        Assert.NotSame(roark, copy);
        Assert.Equal(new[] { "Geodude", "Onix", "Cranidos" }, roark.Party.Members.Select(p => p.Species.Name));
        Assert.Equal(R.TeamOf("leader_roark")!.Party.Select(m => m.Species), copy.Party.Members.Select(p => p.Species.Name));
        Assert.Equal(roark.Ai, copy.Ai);
        Assert.Equal(roark.Items, copy.Items);
        var cranidos = copy.Party.Members.Last();
        Assert.Equal(Nature.Adamant, cranidos.Nature);
        Assert.Equal("Mold Breaker", cranidos.AbilityName);
        Assert.Equal("Oran Berry", cranidos.HeldItem?.Name);
        Assert.Equal(new[] { "Headbutt", "Pursuit", "Rock Tomb", "Leer" }, cranidos.Moves.Select(m => m.Name));
        Assert.False(cranidos.IsShiny);
        Assert.Equal(Remix.DefaultIv, cranidos.IvSpeed);
        // A trainer Remix writes no team for is handed back as it is
        var other = Trainer(TrainerDatabase.All.First(t => R.TeamOf(t.Id) == null && t.Party.Count > 0).Id);
        Assert.Same(other, Remix.Prepare(other, run));
        // And the difficulty is laid on what Remix wrote, not on the table's
        var hard = DifficultyRules.Prepare(copy, run with { Difficulty = Difficulty.Challenge });
        Assert.Equal(copy.Party.Members[0].Species.Name, hard.Party.Members[0].Species.Name);
    }

    [Fact]
    public void ATeamIsBuiltTheSameTwice()
    {
        foreach (var team in R.Trainers.Trainers)
        {
            var a = R.BuildTeam(team);
            var b = R.BuildTeam(team);
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Personality, b[i].Personality);
                Assert.Equal(a[i].Gender, b[i].Gender);
                Assert.Equal(a[i].MaxHP, b[i].MaxHP);
                Assert.Equal(a[i].Speed, b[i].Speed);
                Assert.Equal(a[i].Moves.Select(m => m.Name), b[i].Moves.Select(m => m.Name));
            }
        }
    }

    [Fact]
    public void TheRunPanelHasARemixRow()
    {
        var panel = new RunPanel();
        panel.Open();
        for (int guard = 0; guard < 40 && panel.SelectedRow != RunRow.Remix; guard++) panel.Move(1);
        Assert.Equal(RunRow.Remix, panel.SelectedRow);
        Assert.Equal("shared.off", panel.ValueKey(RunRow.Remix));
        panel.Change(1);
        Assert.True(panel.Rules.Remix);
        Assert.Equal("shared.on", panel.ValueKey(RunRow.Remix));
        Assert.False(panel.Rules.IsNormal);
        Assert.Equal("custom", panel.Rules.PresetKey);
        panel.Change(-1);
        Assert.True(panel.Rules.IsNormal);
        Assert.True((RunRules.Normal with { Remix = true }).Has("remix"));
        Assert.False(RunRules.Normal.Has("remix"));
    }

    [Fact]
    public void ARunWithoutRemixIsSavedAsBefore()
    {
        var run = RunRules.NuzlockePreset;
        string text = GameDataFiles.Serialize(run);
        Assert.DoesNotContain("remix", text, StringComparison.OrdinalIgnoreCase);
        var remix = run with { Remix = true };
        string written = GameDataFiles.Serialize(remix);
        Assert.Contains("\"remix\": true", written);
        Assert.Equal(remix, GameDataFiles.Deserialize<RunRules>(written));
        Assert.Equal(run, GameDataFiles.Deserialize<RunRules>(text));
    }

    // ------------------------------------------------------------------ coverage

    /// <summary>
    /// The species met in a batch's areas before the Hall of Fame: every table but the Super Rod's, which Platinum
    /// gives only once the Hall of Fame is entered.
    /// </summary>
    public static IEnumerable<string> MetIn(string batch) => R.Wild.Areas.Values.Where(a => a.Batch == batch)
        .SelectMany(a => new[] { a.Land, a.Water, a.OldRod, a.GoodRod }.Where(t => t != null).SelectMany(t => t!))
        .Select(e => e.Species);

    /// <summary>Everything a set of species evolves into, by any of the data's ways (a trade's too), within the 458.</summary>
    public static HashSet<string> WithEvolutions(IEnumerable<string> met)
    {
        var all = new HashSet<string>(met, StringComparer.Ordinal);
        var queue = new Queue<string>(all);
        while (queue.Count > 0)
            foreach (var e in PokemonDatabase.Get(queue.Dequeue())?.Evolutions ?? new())
                if (SetNames.Contains(e.TargetSpecies) && all.Add(e.TargetSpecies)) queue.Enqueue(e.TargetSpecies);
        return all;
    }

    /// <summary>
    /// The completeness test, in the floor's place since V19: each of the 458 is in a Remix table of an area the story
    /// reaches before the Hall of Fame (the Super Rod's are the post-game's and don't count), or evolves from one that is,
    /// by any of the data's ways, a trade's too (plan 03, decision 4).
    /// </summary>
    [Fact]
    public void EverySpeciesCanBeMet()
    {
        var met = Batches.SelectMany(MetIn).ToHashSet();
        var reached = WithEvolutions(met);
        var missing = TheSet.Where(s => !reached.Contains(s.Name)).Select(s => s.Name).ToList();
        Assert.True(missing.Count == 0, $"Remix never meets {string.Join(", ", missing)} before the Hall of Fame.");
        Assert.True(WithEvolutions(MetIn("V15")).Count >= 12);
        Assert.All(reached, s => Assert.Contains(s, SetNames));
        // Every table counted is of an area a batch names, none of the post-game's
        var named = SouthWest.Concat(Centre).Concat(East).Concat(North).Concat(RuinsRooms).ToHashSet();
        foreach (string key in R.Wild.Areas.Keys) Assert.True(named.Contains(key), $"{key} is in no batch's list.");
    }

    [Fact]
    public void TheCoverageTableIsWhatTheFilesGive()
    {
        string path = Path.Combine(Repository.Root, "docs", "mechanics", "remix-coverage.md");
        string text = CoverageTable();
        string? old = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
        if (old == text) return;
        if (PokemonPlatinumTests.Golden.GoldenFile.Accepting || Environment.GetEnvironmentVariable("REMIX") == "write")
        {
            File.WriteAllText(path, text);
            return;
        }
        Assert.Fail($"{path} is not what Remix's files give today: run with GOLDEN=accept (or REMIX=write) and read the diff.");
    }

    /// <summary>The coverage table: each batch's areas and teams, the species it meets first, and what is left for the batches to come.</summary>
    public static string CoverageTable()
    {
        var md = new StringBuilder();
        md.Append("# Remix: the species met so far\n\n");
        md.Append("Written by `RemixTests.TheCoverageTableIsWhatTheFilesGive` from `PokemonPlatinumEngine/Data/remix/sinnoh/` ");
        md.Append("(plan 13 · V15 to V19; `docs/mechanics/rulings.md`, \"Remix\"); run `GOLDEN=accept dotnet test PokemonPlatinumTests --filter Remix` to write it again. ");
        md.Append("A species counts as met where a table of a batch's areas has it before the Hall of Fame (the grass, a cave's floor, the water, the Old and the Good Rods; ");
        md.Append("the Super Rod is the post-game's), and as reached once it is met or evolves from one met, by any of the data's ways. ");
        md.Append($"The goal is all {TheSet.Count} species of Generations 1 to 4 that are neither legendary nor mythical.\n\n");
        md.Append("| Batch | What | Areas | Teams | Met first here | Met so far | Reached so far |\n|---|---|---:|---:|---:|---:|---:|\n");
        var met = new HashSet<string>();
        foreach (string batch in Batches)
        {
            int areas = R.Wild.Areas.Values.Count(a => a.Batch == batch);
            int teams = R.Trainers.Trainers.Count(t => t.Batch == batch);
            int before = met.Count;
            met.UnionWith(MetIn(batch));
            md.Append($"| {batch} | {BatchNames[batch]} | {areas} | {teams} | {met.Count - before} | {met.Count} | {WithEvolutions(met).Count} |\n");
        }
        var reached = WithEvolutions(met);
        var left = TheSet.Where(s => !reached.Contains(s.Name)).ToList();
        md.Append($"\n## Left for the batches to come ({left.Count})\n\n");
        foreach (var generation in left.GroupBy(s => s.DexNumber switch { <= 151 => 1, <= 251 => 2, <= 386 => 3, _ => 4 }))
            md.Append($"- Generation {generation.Key} ({generation.Count()}): {string.Join(", ", generation.Select(s => s.Name))}\n");
        md.Append("\n## Met in each batch\n\n");
        foreach (string batch in Batches)
        {
            var mine = MetIn(batch).Distinct().OrderBy(s => PokemonDatabase.Get(s)!.DexNumber).ToList();
            if (mine.Count == 0) continue;
            md.Append($"- {batch} ({mine.Count}): {string.Join(", ", mine)}\n");
        }
        return md.ToString();
    }

    // ------------------------------------------------------------------ helpers

    private static Trainer Trainer(string id)
    {
        var record = TrainerDatabase.Get(id)!;
        var trainer = new Trainer { Id = record.Id, Name = record.Name, TrainerClass = record.Class };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    /// <summary>A map of its own whose one area is an area of Sinnoh with Platinum's table, as the tests of the dual slot make one.</summary>
    private static (Map Map, MapArea Area) AreaMap(string key)
    {
        var own = GameDataFiles.Load<WorldAreaFile>($"world/sinnoh/areas/{key}.json");
        var area = new MapArea { Key = key, Open = true, LandRate = own.LandRate ?? 0 };
        for (int i = 0; i < own.Land!.Count; i++)
            area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = own.Land[i].Species, MinLevel = own.Land[i].Level, MaxLevel = own.Land[i].Level, Weight = WorldAreaFile.LandSlotWeights[i] });
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        map.SetArea(0, 0, area);
        return (map, area);
    }
}

/// <summary>
/// Remix's balance check (plan 13 · V15): a reference team, plan 02's pacing party made of what Remix's tables offer by
/// then at the leader's cap, fights the leader's Remix team over twenty seeds with Platinum's AI on both sides (the
/// reference side thinking as the leader does, <see cref="CoreSetup.PlayerTrainer"/>), and its share of wins is held in a
/// band of our own.
/// </summary>
public static class RemixBalance
{
    public const int Seeds = 20;

    /// <summary>How many of <see cref="Seeds"/> battles the reference team wins against a trainer's Remix team.</summary>
    public static int Wins(string trainerId, IEnumerable<(string Species, int Level)> reference)
    {
        var record = TrainerDatabase.Get(trainerId)!;
        int wins = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var foe = new Trainer { Id = record.Id, Name = record.Name, TrainerClass = record.Class };
            TrainerDatabase.Fill(foe, record);
            foe = Remix.Sinnoh.ForTrainer(foe);
            var party = new Party();
            int place = 0;
            foreach (var (species, level) in reference)
            {
                var p = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(100 + place++)) { IsShiny = false };
                p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = 15;
                p.RecalculateStats();
                p.CurrentHP = p.MaxHP;
                party.Add(p);
            }
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { foe }, Random = new BattleRandom((uint)seed), Rules = Ruleset.Platinum,
                PlayerController = TrainerAi.Instance,
                PlayerTrainer = new Trainer { Name = "Reference", TrainerClass = "Ace Trainer", Ai = foe.Ai }
            });
            core.Start();
            if (core.Result == BattleResult.PlayerVictory) wins++;
        }
        return wins;
    }
}

/// <summary>The balance of the south-west's leader under Remix (plan 13 · V16), and the engine's two points of use in a whole game.</summary>
[Collection("Shared clocks and dice")]
public class RemixGameTests
{
    /// <summary>
    /// Roark's reference team: a Monferno at his cap of 14, with a Machop and a Wooper met on Remix's Route 203 and
    /// Lake Verity, each a little under it, as plan 02's pacing party would stand at his door.
    /// </summary>
    public static readonly (string, int)[] RoarkReference = { ("Wooper", 12), ("Machop", 13), ("Monferno", 14) };

    /// <summary>Roark's band: of twenty battles the reference team wins at least this many and at most that.</summary>
    public const int RoarkLeast = 6, RoarkMost = 17;

    /// <summary>
    /// The later leaders', the Elite Four's and the Champion's reference teams and bands (plan 13 · V17 to V19): plan 02's
    /// pacing party at each door, made of what Remix's tables offer by then (the starter's line, and the centre's,
    /// the east's and the north's finds), its best at the leader's cap, and of twenty battles the share of wins it must
    /// fall in. The League's references are a whole team of six, as a player's is there.
    /// </summary>
    public static readonly Dictionary<string, ((string, int)[] Team, int Least, int Most)> References = new()
    {
        ["leader_gardenia"] = (new[] { ("Ponyta", 20), ("Staravia", 21), ("Monferno", 22) }, 4, 18),
        ["leader_fantina"] = (new[] { ("Kadabra", 24), ("Ponyta", 25), ("Luxio", 25), ("Monferno", 26) }, 4, 18),
        ["leader_maylene"] = (new[] { ("Kadabra", 30), ("Ponyta", 30), ("Staravia", 31), ("Monferno", 32) }, 4, 18),
        ["leader_wake"] = (new[] { ("Luxray", 35), ("Roserade", 35), ("Kadabra", 36), ("Staraptor", 36), ("Infernape", 37) }, 4, 18),
        ["leader_byron"] = (new[] { ("Machoke", 38), ("Quagsire", 39), ("Staraptor", 40), ("Infernape", 41) }, 4, 18),
        ["leader_candice"] = (new[] { ("Machoke", 41), ("Luxray", 42), ("Staraptor", 43), ("Empoleon", 44) }, 4, 18),
        ["leader_volkner"] = (new[] { ("Quagsire", 46), ("Machoke", 47), ("Staraptor", 48), ("Golem", 49), ("Infernape", 50) }, 4, 18),
        ["elite_four_aaron"] = (new[] { ("Golem", 51), ("Gyarados", 51), ("Luxray", 52), ("Staraptor", 52), ("Alakazam", 52), ("Infernape", 53) }, 4, 18),
        ["elite_four_bertha"] = (new[] { ("Golem", 53), ("Gyarados", 53), ("Luxray", 54), ("Staraptor", 54), ("Alakazam", 54), ("Infernape", 55) }, 4, 18),
        ["elite_four_flint"] = (new[] { ("Golem", 55), ("Gyarados", 55), ("Luxray", 56), ("Staraptor", 56), ("Alakazam", 56), ("Torterra", 57) }, 4, 18),
        ["elite_four_lucian"] = (new[] { ("Golem", 57), ("Gyarados", 57), ("Luxray", 58), ("Staraptor", 58), ("Alakazam", 58), ("Infernape", 59) }, 4, 18),
        ["champion_cynthia"] = (new[] { ("Golem", 60), ("Gyarados", 60), ("Luxray", 61), ("Staraptor", 61), ("Alakazam", 61), ("Infernape", 62) }, 4, 18),
    };

    public static IEnumerable<object[]> Leaders => References.Keys.Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(Leaders))]
    public void ALaterLeaderIsWithinTheirBand(string id)
    {
        var (team, least, most) = References[id];
        int wins = RemixBalance.Wins(id, team);
        Assert.InRange(wins, least, most);
    }

    [Fact]
    public void RoarkIsWithinHisBand()
    {
        int wins = RemixBalance.Wins("leader_roark", RoarkReference);
        Assert.InRange(wins, RoarkLeast, RoarkMost);
    }

    [Fact]
    public void ARemixGameLaysItsTablesAndFightsItsTeams()
    {
        string folder = Path.Combine(Path.GetTempPath(), "remix-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame(null, PlayerLook.Boy, null, RunRules.Normal with { Remix = true });
            var sinnoh = MapDatabase.Get("Sinnoh");
            Assert.Same(Remix.Sinnoh, sinnoh.Tables);
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Monferno")!, 30));
            game.State = GameState.Overworld;
            game.TrainersLookOnArrival = false;
            game.Place(sinnoh, 116, 886, Direction.Down);

            var trainer = new Trainer { Id = "leader_roark", Name = "Roark", TrainerClass = "Leader" };
            TrainerDatabase.Fill(trainer, TrainerDatabase.Get("leader_roark")!);
            var person = new NPC { Key = "roark", Name = "Roark", IsTrainer = true, TrainerData = trainer, GridX = game.Player.GridX + 1, GridY = game.Player.GridY };
            game.StartTrainerBattle(person);
            for (int f = 0; f < 60 * 30 && game.State != GameState.Battle; f++) { InputManager.Feed(ActionFrame.None); engine.Update(1f / 60f); }
            Assert.Equal(GameState.Battle, game.State);
            Assert.Equal(Remix.Sinnoh.TeamOf("leader_roark")!.Party.Select(m => m.Species), game.Battle!.Trainers[0].Party.Members.Select(p => p.Species.Name));
            // The trainer of the map keeps the table's team
            Assert.Equal(3, trainer.Party.Count);

            // A game begun afterwards with Remix off takes the tables away again
            engine.StartNewGame(null, PlayerLook.Boy, null, RunRules.Normal);
            Assert.Null(sinnoh.Tables);
        }
        finally
        {
            foreach (string name in MapDatabase.MapNames)
                if (MapDatabase.TryGet(name) is { } map) map.Tables = null;
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            RunRules.Current = RunRules.Normal;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
