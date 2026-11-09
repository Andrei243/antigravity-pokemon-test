using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 23, Alola's last wave ("Alola finished"): the plane on to Galar from Hau'oli City's Pokémon Center, played as a
/// player plays it.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaFinishTests
{
    private static Pokemon Mon(string species, int level = 60) => new(PokemonDatabase.Get(species)!, level);

    [Fact]
    public void AChampionOfAlolaFliesToGalarFromHauolisPokemonCenterWithTheWholeTeam()
    {
        var center = MapDatabase.Get("HauoliCity_PokemonCenter_1F");
        var attendant = center.Everyone.Single(n => n.IsTransportAttendant);
        var host = new HeadlessScriptHost { Map = center, PlayerTile = (13, 11), MapNamed = MapDatabase.Get, Region = RegionDatabase.Alola };
        foreach (var species in new[] { "Decidueye", "Lycanroc", "Toucannon" }) host.Party.Add(Mon(species));
        var team = host.Party.Members.ToList();

        // Before Alola's Hall of Fame the attendant has no way on
        var before = new ScriptRunner(ScriptLibrary.Default, host);
        before.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, attendant);
        before.RunToEnd();
        Assert.Empty(host.Crossings);

        // After it, Galar is the way on, by plane, and the whole team goes
        host.Story.CompleteRegion(RegionDatabase.Alola);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, attendant);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(new[] { RegionDatabase.Galar }, host.Crossings);
        Assert.Equal(RegionDatabase.Galar, host.Region);
        Assert.True(host.Story.HasEntered(RegionDatabase.Galar));
        Assert.Equal(team, host.Party.Members);
    }

    private static HeadlessScriptHost Champion(string map, params string[] team)
    {
        var host = new HeadlessScriptHost { Map = MapDatabase.Get(map), MapNamed = MapDatabase.Get, Region = RegionDatabase.Alola };
        host.Story.Set("AlolaHallOfFame");
        foreach (string species in team.Length > 0 ? team : new[] { "Decidueye" }) host.Party.Add(Mon(species));
        return host;
    }

    private static void Run(HeadlessScriptHost host, string place, string script, NPC? self = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, self);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void TheUltraWarpRideIsOfferedOnceNecrozmaIsMetAndEachLightLeadsToItsWorld()
    {
        var host = Champion("PoniAltar");
        host.Story.SetVar("VAR_ALOLA_ULTRA", 3);
        // Before Necrozma the wormhole offers the Ultra Beasts' worlds alone
        host.Answers.Enqueue(5);
        Run(host, "poni_altar", "Wormhole");
        Assert.DoesNotContain(host.Asked, a => a.Answer == "Ride the warp");

        var worlds = new[] { "UltraWildsWater", "UltraWildsField", "UltraWildsCliff", "UltraWildsCave" };
        for (int light = 0; light < 4; light++)
        {
            host.Story.SetVar("VAR_ALOLA_ULTRA", 4);
            host.Map = MapDatabase.Get("PoniAltar");
            host.Answers.Enqueue(1);
            host.Answers.Enqueue(light);
            Run(host, "poni_altar", "Wormhole");
            Assert.Equal(worlds[light], host.Map!.Name);
        }
    }

    [Theory]
    [InlineData("ultra_wilds_water", "UltraWildsWater", 9, "Entei,Raikou,Reshiram,Zekrom")]
    [InlineData("ultra_wilds_field", "UltraWildsField", 10, "Decidueye")]
    [InlineData("ultra_wilds_cliff", "UltraWildsCliff", 10, "Kyogre,Groudon,Tornadus,Thundurus")]
    [InlineData("ultra_wilds_cave", "UltraWildsCave", 8, "Regirock,Regice,Registeel,Dialga,Palkia")]
    public void EachWorldShowsALegendNotYetCaughtAndItsCallersOnlyWithTheirKin(string place, string map, int count, string kin)
    {
        var legends = MapDatabase.Get(map).Everyone.Where(n => n.Id.StartsWith("warp_")).ToList();
        Assert.Equal(count, legends.Count);
        Assert.All(legends, n => Assert.Equal((32, 16), (n.GridX, n.GridY)));
        var seen = new System.Collections.Generic.HashSet<string>();
        var caught = new System.Collections.Generic.HashSet<string>();
        // Every die's face shows somebody while nobody is caught; with all caught but one, every face finds that one
        for (int round = 0; round < count; round++)
        {
            var host = Champion(map, kin.Split(","));
            foreach (string c in caught) host.Story.Set(c);
            host.Rolls.Enqueue(round);
            Run(host, place, "OnEnter");
            var shown = legends.Where(n => host.Story.Has(n.ShownBy!) && !host.Story.Has(n.HiddenBy!)).ToList();
            Assert.Single(shown);
            seen.Add(shown[0].Name);
            // Its meeting comes after its beat, at level 60, and a catch keeps it from the dais for good
            host.Fight = _ => BattleOutcome.Caught;
            Run(host, place, shown[0].Script!, shown[0]);
            Assert.Contains(host.Log, l => l.StartsWith($"wildbattle {shown[0].Name} 60"));
            Assert.True(host.Story.Has(shown[0].HiddenBy!));
            caught.Add(shown[0].HiddenBy!);
        }
        // With every one caught the world is empty, and says so
        var empty = Champion(map);
        foreach (string c in caught) empty.Story.Set(c);
        Run(empty, place, "OnEnter");
        Assert.DoesNotContain(legends, n => empty.Story.Has(n.ShownBy!));
        Assert.Single(empty.Transcript);
    }

    [Fact]
    public void ACallerOfTheWarpWaitsForItsKinInTheTeam()
    {
        // Suicune is the blue light's first; with no Entei and Raikou in the team the die passes it by
        var host = Champion("UltraWildsWater");
        host.Rolls.Enqueue(0);
        Run(host, "ultra_wilds_water", "OnEnter");
        Assert.False(host.Story.Has("FLAG_MAP_LOCAL_WARP_SUICUNE"));
        Assert.True(host.Story.Has("FLAG_MAP_LOCAL_WARP_LUGIA"));
    }

    [Fact]
    public void AlolasMythicalsAreMetAfterTheHallOfFameAndLeaveWhatTheyOwe()
    {
        // Marshadow: a shadow in the cemetery at night, once the little girl has gone after her friend
        var cemetery = Champion("Melemele");
        cemetery.Story.SetVar("VAR_ALOLA_CEMETERY_GIRL", 3);
        cemetery.TimeOfDay = PokemonPlatinumEngine.Core.TimeOfDay.Night;
        Run(cemetery, "hauoli_cemetery", "OnEnter");
        Assert.True(cemetery.Story.Has("FLAG_ALOLA_MARSHADOW_NEAR"));
        var shadow = MapDatabase.Get("Melemele").Everyone.Single(n => n.Id == "marshadow");
        Run(cemetery, "hauoli_cemetery", "Marshadow", shadow);
        Assert.Contains(cemetery.Log, l => l.StartsWith("wildbattle Marshadow 60"));
        Assert.Equal(1, cemetery.Bag.GetQuantity(ItemDatabase.Get("Marshadium Z")!));

        // Meltan: the old Megamart's, which leaves the candy Melmetal's evolution asks for
        var mart = Champion("ThriftyMegamart");
        var meltan = mart.Map!.Everyone.Single(n => n.Id == "meltan");
        Run(mart, "ThriftyMegamart", "Meltan", meltan);
        Assert.Equal(400, mart.Bag.GetQuantity(ItemDatabase.Get("Meltan Candy")!));

        // Magearna and Zeraora each in their rooms, by flags their places keep
        var observatory = Champion("HokulaniObservatory");
        Assert.Equal("FLAG_ALOLA_TOTEM_VIKAVOLT_DONE", observatory.Map!.Everyone.Single(n => n.Id == "magearna").ShownBy);
        Run(observatory, "HokulaniObservatory", "Magearna", observatory.Map.Everyone.Single(n => n.Id == "magearna"));
        Assert.Contains(observatory.Log, l => l.StartsWith("wildbattle Magearna 60"));
        var plant = Champion("BlushMountain_PowerPlant");
        Assert.Equal("FLAG_ALOLA_ZERAORA_NEAR", plant.Map!.Everyone.Single(n => n.Id == "zeraora").ShownBy);
        Run(plant, "BlushMountain_PowerPlant", "Zeraora", plant.Map.Everyone.Single(n => n.Id == "zeraora"));
        Assert.Contains(plant.Log, l => l.StartsWith("wildbattle Zeraora 60"));
    }

    [Fact]
    public void ATrainerBeatenInTheTreeIsScoutedAndCanBeChosenToPartnerTheMultiRoom()
    {
        var records = new PokemonPlatinumEngine.Models.Frontier.FrontierRecords();
        var party = new Party();
        foreach (var species in new[] { "Decidueye", "Lycanroc", "Toucannon", "Vikavolt" }) party.Add(Mon(species));
        int n = 7;
        int Next16() => (n = n * 1103 + 12345) & 0xFFFF;

        // A single room's win scouts the trainer beaten; a host's doesn't
        var single = PokemonPlatinumEngine.Models.Facilities.StreakChallenge.Begin(records,
            PokemonPlatinumEngine.Models.Facilities.BattleTree.Room("single", false), new[] { 0, 1, 2 }, party, Next16);
        var beaten = single.Opponent(records);
        single.Won(records, trophyHeld: false);
        var scouted = PokemonPlatinumEngine.Models.Facilities.BattleTree.MultiRecord(records).Scouted!;
        Assert.Single(scouted);
        Assert.Equal(beaten.ShownName, scouted[0].Name);
        Assert.Equal(3, scouted[0].Sets.Count);

        // The tree's own partner until one is chosen; then the scouted trainer with the first two of their team
        var multiRoom = PokemonPlatinumEngine.Models.Facilities.BattleTree.Room("multi", false);
        var own = PokemonPlatinumEngine.Models.Facilities.StreakChallenge.Begin(records, multiRoom, new[] { 0, 1 }, party, Next16).Partner!;
        PokemonPlatinumEngine.Models.Facilities.BattleTree.MultiRecord(records).Partner = 1;
        var chosen = PokemonPlatinumEngine.Models.Facilities.StreakChallenge.Begin(records, multiRoom, new[] { 0, 1 }, party, Next16).Partner!;
        Assert.Equal(beaten.ShownName, chosen.ShownName);
        Assert.Equal(2, chosen.Pokemon.Count);
        Assert.Equal(beaten.Pokemon.Take(2).Select(p => p.Species.Name), chosen.Pokemon.Select(p => p.Species.Name));
        Assert.All(chosen.Pokemon, p => Assert.Equal(50, p.Level));
        Assert.NotEqual(own.ShownName + own.Pokemon[0].Species.Name, chosen.ShownName + chosen.Pokemon[0].Species.Name);
    }

    [Fact]
    public void TheTreesDeskOffersTheScoutedPartnersMostRecentFirst()
    {
        var host = Champion("PoniGauntlet_BattleTree", "Decidueye", "Lycanroc");
        var record = PokemonPlatinumEngine.Models.Facilities.BattleTree.MultiRecord(host.Frontier);
        record.Scouted = new()
        {
            new() { Id = "a", Class = "Ace Trainer", Name = "Mele" },
            new() { Id = "b", Class = "Veteran", Name = "Koa" },
        };
        // Enter, Multi, choose a partner: yes; Koa (the most recent): no; Mele: yes; then back out at the choice of Pokémon
        foreach (int answer in new[] { 0, 2, 0, 1, 0 }) host.Answers.Enqueue(answer);
        host.Decide = (_, answers) => answers.Count - 1;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Desk", "PoniGauntlet_BattleTree")!, host.Map!.Everyone.Single(n => n.Id == "desk"));
        runner.RunToEnd();
        Assert.Contains(host.Asked, a => a.Question.Contains("Veteran Koa", System.StringComparison.Ordinal));
        Assert.Contains(host.Asked, a => a.Question.Contains("Ace Trainer Mele", System.StringComparison.Ordinal));
        Assert.Equal(1, record.Partner);
    }

    private static readonly string[] Islands = { "original-melemele", "original-akala", "original-ulaula", "original-poni" };

    [Fact]
    public void AlolaHasItsFourIslandPokedexesBesideItsOwnEachWithItsDiplomaAndWords()
    {
        var alola = RegionDatabase.Get(RegionDatabase.Alola)!;
        Assert.Equal(new[] { "original-alola" }.Concat(Islands), alola.Dexes);
        Assert.Equal("original-alola", alola.Dex);
        var dex = new Pokedex();
        dex.Enter(alola);
        foreach (string id in Islands)
        {
            var entries = Pokedex.Entries(id);
            Assert.InRange(entries.Count, 100, 130);
            Assert.Equal(1, entries[0].Number);
            Assert.Same(alola, Pokedex.RegionOf(id));
            Assert.Contains(id, dex.Dexes);
            string key = Pokedex.KeyOf(id);
            foreach (string path in new[] { "mode", "modeName", "diploma" })
                Assert.NotEqual($"pokedex.{path}.{key}", PokemonPlatinumEngine.Core.Text.Of($"pokedex.{path}.{key}"));
            Assert.NotEqual($"pokedex.diploma.line.{key}", PokemonPlatinumEngine.Core.Text.Of($"pokedex.diploma.line.{key}", ("count", 120)));
            // Every species of the island seen gives that island's diploma
            foreach (var e in entries) dex.RegisterSeen(e.Species.DexNumber);
            Assert.True(dex.IsComplete(id));
            Assert.Contains(id, dex.AwardDiplomas());
        }
    }
}
