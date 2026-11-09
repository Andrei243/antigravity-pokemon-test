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
