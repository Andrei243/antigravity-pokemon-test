using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 20 · J13, part d: Route 40's way to the Battle Frontier. The Frontier is Sinnoh's, a warp never crosses regions,
/// so Route 40's guide crosses into it (<c>travel to</c>, the crossing every region uses) and a guide on its grounds
/// crosses back; the records are the save's one set, whichever door the player came by (the user's decision of
/// 2026-10-25; docs/mechanics/rulings.md, "Route 40's way to the Battle Frontier").
/// </summary>
public class JohtoPartDTests
{
    private const string Gate = "route_40_battle_frontier_gatehouse";

    private static void Run(string name, string place, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, new NPC { Name = "Guide" });
        runner.RunToEnd();
    }

    private static Party Team(params string[] species)
    {
        var party = new Party();
        foreach (string s in species) party.Add(new Pokemon(PokemonDatabase.Get(s)!, 60));
        return party;
    }

    [Fact]
    public void RouteFortysGuideTakesNobodyBeforeJohtosHallOfFame()
    {
        var host = new HeadlessScriptHost(new StoryState(), Team("Typhlosion"), new Inventory()) { Region = RegionDatabase.Johto, PlayerTile = (5, 4) };
        Run("Guide", Gate, host);
        Assert.Empty(host.Crossings);
        Assert.Empty(host.Asked);
        Assert.Equal(RegionDatabase.Johto, host.Region);
        Assert.Equal((5, 4), host.PlayerTile);
    }

    [Fact]
    public void TheGuideCrossesIntoTheBattleFrontierAndTheJohtoGuideBringsThePlayerBack()
    {
        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        // Each region's place to come back to, as a Pokémon Center of each left it
        story.SetVar(SpawnLocations.VariableOf(RegionDatabase.Johto), 3);
        story.SetVar(SpawnLocations.VariableOf(RegionDatabase.Sinnoh), 7);
        var party = Team("Typhlosion", "Ampharos", "Gyarados");
        var team = party.Members.ToList();
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Johto, PlayerTile = (5, 4) };
        var records = host.Frontier;
        records.GivePoints(10);

        // A no leaves the player in the gate
        host.Answers.Enqueue(1);
        Run("Guide", Gate, host);
        Assert.Empty(host.Crossings);
        Assert.Equal(RegionDatabase.Johto, host.Region);

        // A yes walks the road: a crossing into Sinnoh, onto the grounds where the gate from the Fight Area lets out,
        // with the whole team, each Pokémon given the region it left
        Run("Guide", Gate, host);
        Assert.Equal(new[] { RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Contains(host.Log, l => l == $"travel to {RegionDatabase.Sinnoh} BattleFrontier 47 51");
        Assert.Equal(RegionDatabase.Sinnoh, host.Region);
        Assert.Equal((47, 51), host.PlayerTile);
        Assert.Equal(Direction.Up, host.PlayerFacing);
        Assert.Equal(team, party.Members);
        Assert.All(party.Members, p => Assert.Equal(RegionDatabase.Johto, p.Region));

        // A round won on the Sinnoh side counts in the same records
        host.Frontier.GivePoints(5);

        // And back along the road from the Johto guide on the grounds
        Run("JohtoGuide", "battle_frontier", host);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Johto }, host.Crossings);
        Assert.Equal(RegionDatabase.Johto, host.Region);
        Assert.Equal((5, 4), host.PlayerTile);
        Assert.Equal(Direction.Down, host.PlayerFacing);
        Assert.Equal(team, party.Members);
        Assert.All(party.Members, p => Assert.Equal(RegionDatabase.Johto, p.Region));
        Assert.Same(records, host.Frontier);
        Assert.Equal(15, host.Frontier.BattlePoints);
        // Nobody's place to come back to moved: there is no Pokémon Center on either road
        Assert.Equal(3, story.Var(SpawnLocations.VariableOf(RegionDatabase.Johto)));
        Assert.Equal(7, story.Var(SpawnLocations.VariableOf(RegionDatabase.Sinnoh)));
    }

    [Fact]
    public void TheTwoDoorsAreTheFrontiersOwnAndTheSaveKeepsOneSetOfRecords()
    {
        // Both guides' ways lead onto open ground of their maps, each in its own region
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap("BattleFrontier")!.Id);
        Assert.Equal(RegionDatabase.Johto, RegionDatabase.RegionOfMap("Route40_FrontierGate")!.Id);
        var crossings = ScriptLibrary.Default.All
            .SelectMany(s => s.Everything().Select(i => (s.File, i)))
            .Where(x => x.i.Op == Op.Travel && x.i.Name == "to")
            .Select(x => (x.File, x.i.Other, x.i.X, x.i.Y)).ToList();
        Assert.Contains((Gate, "BattleFrontier", 47, 51), crossings);
        Assert.Contains(("battle_frontier", "Route40_FrontierGate", 5, 4), crossings);

        // The Johto guide on the grounds is there only for whoever has entered Johto's Hall of Fame
        var overlay = World.LoadAll().Single(w => w.Index.Region == "Sinnoh").Overlay("battle_frontier");
        var guide = overlay!.Npcs!.Single(n => n.Id == "battle_frontier.JohtoGuide");
        Assert.Equal("JohtoHallOfFame", guide.ShownBy);
        Assert.Equal((46, 51), (guide.X, guide.Y));

        // One set of records in the save, whatever region the save was made in
        Assert.Single(typeof(SaveData).GetProperties(), p => p.PropertyType == typeof(FrontierRecords));
    }
}
