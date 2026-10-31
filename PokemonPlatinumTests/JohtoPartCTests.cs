using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>Plan 20 ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â· J13, part c: Johto's swarms, the Move Relearner and the Dragon's Den after the League.</summary>
public class JohtoPartCTests
{
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == "Johto"));
    private static readonly Lazy<List<Map>> Maps = new(() => Johto.Value.BuildMaps().ToList());

    private static MapArea AreaOf(string key) => Maps.Value.SelectMany(m => m.Areas).First(a => a.Key == key);

    private static HeadlessScriptHost Run(string name, string area, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, area)!, new NPC { Name = "Someone" });
        runner.RunToEnd();
        return host;
    }

    private static Pokemon Mon(string species, int level = 20) => new(PokemonDatabase.Get(species)!, level);

    [Fact]
    public void JohtosSwarmsAreHeartGoldsTwentyPlacesEachMetInItsWay()
    {
        var places = Swarms.Places(RegionDatabase.Johto);
        Assert.Equal(20, places.Count);
        Assert.All(places, p => Assert.NotEmpty(p.Species));
        // sSwarmMapLUT's order, and the way of each (0 land, 1 surf, 2 fish)
        Assert.Equal(("johto_route_1", null), (places[0].Area, places[0].Way));
        Assert.Equal(("route_32", "fish"), (places[6].Area, places[6].Way));
        Assert.Equal(("route_27", "surf"), (places[8].Area, places[8].Way));
        Assert.Equal(("violet", "fish"), (places[19].Area, places[19].Way));
        // Sinnoh's stay its own
        Assert.Equal(22, Swarms.Places(RegionDatabase.Sinnoh).Count);
        // The day's number picks one (Roamers_GetRand modulo SWARM_MAP_COUNT), once swarms have begun
        var state = new SpecialEncounters { SwarmDaily = 26 };
        Assert.Null(Swarms.PlaceToday(state, RegionDatabase.Johto));
        state.SwarmsOn = true;
        Assert.Equal("route_32", Swarms.PlaceToday(state, RegionDatabase.Johto)!.Area);

        // Each open place has its species in the way the table keeps: the area's file and the table agree
        foreach (var place in places)
        {
            if (!Johto.Value.Index.Areas.Contains(place.Area)) continue;
            var area = AreaOf(place.Area);
            string? species = place.Way switch { "surf" => area.SurfSwarm, "fish" => area.FishSwarm, _ => area.SwarmSlots.FirstOrDefault() };
            Assert.Equal(place.Species, species);
        }
    }

    [Fact]
    public void ASwarmTakesTheGrassTheWaterOrTheRodsSlotsOfItsPlace()
    {
        // Route 35's Yanma: the grass's slots 0 and 1
        var grass = EncounterSlots.Grass(AreaOf("route_35"), new EncounterMoment { SwarmArea = "route_35", SwarmWay = SwarmWay.Grass });
        Assert.Equal(new[] { "Yanma", "Yanma" }, grass.Take(2).Select(e => e.SpeciesName));
        // ... and only that way: a surf swarm leaves the grass alone
        Assert.NotEqual("Yanma", EncounterSlots.Grass(AreaOf("route_35"), new EncounterMoment { SwarmArea = "route_35", SwarmWay = SwarmWay.Surf })[0].SpeciesName);

        // Route 27's Luvdisc: the water's first slot
        var route27 = AreaOf("route_27");
        var surf = EncounterSlots.Surf(route27, route27.WaterEncounters, new EncounterMoment { SwarmArea = "route_27", SwarmWay = SwarmWay.Surf });
        Assert.Equal("Luvdisc", surf[0].SpeciesName);
        Assert.Equal(route27.WaterEncounters.Select(e => e.SpeciesName).Skip(1), surf.Select(e => e.SpeciesName).Skip(1));
        Assert.Same(route27.WaterEncounters, EncounterSlots.Surf(route27, route27.WaterEncounters, new EncounterMoment { SwarmArea = "route_34", SwarmWay = SwarmWay.Surf }));

        // Route 32's Qwilfish: the old rod's third slot, the good rod's first, third and fourth, all five of the super rod's
        var route32 = AreaOf("route_32");
        var moment = new EncounterMoment { SwarmArea = "route_32", SwarmWay = SwarmWay.Fish };
        var old = EncounterSlots.Rod(route32, FishingRod.Old, route32.RodEncounters[(int)FishingRod.Old], moment);
        Assert.Equal(new[] { 2 }, Enumerable.Range(0, old.Count).Where(i => old[i].SpeciesName == "Qwilfish"));
        var good = EncounterSlots.Rod(route32, FishingRod.Good, route32.RodEncounters[(int)FishingRod.Good], moment);
        Assert.All(new[] { 0, 2, 3 }, i => Assert.Equal("Qwilfish", good[i].SpeciesName));
        Assert.Equal(route32.RodEncounters[(int)FishingRod.Good][1].SpeciesName, good[1].SpeciesName);
        var super = EncounterSlots.Rod(route32, FishingRod.Super, route32.RodEncounters[(int)FishingRod.Super], moment);
        Assert.All(super, e => Assert.Equal("Qwilfish", e.SpeciesName));
    }

    [Fact]
    public void TheDaysSwarmIsToldInTheRegionsOwnPlacesAndOnProfessorOaksRadio()
    {
        var host = new HeadlessScriptHost();
        host.Encounters.SwarmDaily = 10;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var script = ScriptParser.Parse("test", "script A\n  say \"{swarm} on {swarmplace}\"\n  end\n")[0];
        runner.Start(script);
        runner.RunToEnd();
        // The headless host is Sinnoh's: the 11th of its 22 (10 % 22)
        Assert.Contains(Swarms.PlaceOf(10, RegionDatabase.Sinnoh)!.Name, host.Transcript.Last().Text);
        Assert.NotEqual(Swarms.PlaceOf(10, RegionDatabase.Johto)!.Name, Swarms.PlaceOf(10, RegionDatabase.Sinnoh)!.Name);

        string line = RadioShows.SwarmLine(new Random(1), "Yanma", "Route 35");
        Assert.Contains("Yanma", line);
        Assert.Contains("Route 35", line);
        Assert.DoesNotContain("{", line);
    }

    [Fact]
    public void TheMoveRelearnerOfBlackthornTeachesAMoveForAHeartScaleAndAsksWhichToForgetWhenFull()
    {
        const string area = "blackthorn_move_tutor_house";
        Assert.Contains(Johto.Value.Overlay(area)!.People!, p => p.Value.Script == "Relearner");

        // No Heart Scale, no question
        var poor = Run("Relearner", area, new HeadlessScriptHost());
        Assert.Empty(poor.Asked);

        // A PokÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â©mon with room: the move picked is learned and the scale goes
        var pupil = Mon("Pidgey", 30);
        pupil.Moves.Clear();
        pupil.Moves.Add(MoveDatabase.Create("Tackle"));
        var slipped = Relearning.MovesFor(pupil);
        Assert.NotEmpty(slipped);
        var host = new HeadlessScriptHost();
        host.Bag.AddItem(ItemDatabase.Get("Heart Scale")!, 2);
        host.Party.Add(pupil);
        host.PokemonChoice = 0;
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Run("Relearner", area, host);
        Assert.True(pupil.Knows(slipped[0]));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!));

        // One that knows four is asked which move is to go
        var full = Mon("Pidgey", 30);
        full.Moves.Clear();
        foreach (string m in new[] { "Tackle", "Sand Attack", "Gust", "Quick Attack" }) full.Moves.Add(MoveDatabase.Create(m));
        var left = Relearning.MovesFor(full);
        Assert.NotEmpty(left);
        var crowded = new HeadlessScriptHost();
        crowded.Bag.AddItem(ItemDatabase.Get("Heart Scale")!, 1);
        crowded.Party.Add(full);
        crowded.PokemonChoice = 0;
        foreach (int answer in new[] { 0, 0, 0 }) crowded.Answers.Enqueue(answer);
        Run("Relearner", area, crowded);
        Assert.True(full.Knows(left[0]));
        Assert.False(full.Knows("Tackle"), string.Join(", ", full.Moves.Select(m => m.Name)) + " | " + string.Join(", ", crowded.Transcript.Select(l => l.Text)) + " | " + string.Join(", ", left));
        Assert.Equal(0, crowded.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!));

        // An Egg and a PokÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â©mon with nothing to remember keep the scale
        var egg = Mon("Pidgey", 30);
        egg.IsEgg = true;
        var eggHost = new HeadlessScriptHost();
        eggHost.Bag.AddItem(ItemDatabase.Get("Heart Scale")!, 1);
        eggHost.Party.Add(egg);
        eggHost.PokemonChoice = 0;
        eggHost.Answers.Enqueue(0);
        Run("Relearner", area, eggHost);
        Assert.Equal(1, eggHost.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!));
    }
}
