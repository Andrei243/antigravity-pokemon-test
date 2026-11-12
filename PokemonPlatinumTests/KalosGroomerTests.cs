using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Lumiose's groomer and Furfrou's trims (plan 23, "Finishing Kalos"; rulings, "Lumiose's groomer and Furfrou's trims"):
/// a trim given for 500, lasting five days on the team and growing out at nought or in a box, kept in the save.
/// </summary>
public class KalosGroomerTests
{
    private static Pokemon Furfrou() => new(PokemonDatabase.Get("Furfrou")!, 30);

    [Fact]
    public void EveryTrimIsAFormOfTheData()
    {
        var species = PokemonDatabase.Get("Furfrou")!;
        foreach (var (word, form) in FormRules.Trims) Assert.True(species.Form(form) != null, $"{word}: {form} is no form of Furfrou");
        Assert.Equal(9, FormRules.Trims.Length);
    }

    [Fact]
    public void ATrimLastsFiveDaysOnTheTeamAndGrowsOut()
    {
        var p = Furfrou();
        Assert.Equal(1, FormRules.Trim(p, "kabuki"));
        Assert.Equal("Furfrou-Kabuki", p.Form);
        Assert.Equal(FormRules.TrimLasts, p.TrimDays);
        FormRules.TrimDaysPass(new[] { p }, 4);
        Assert.Equal("Furfrou-Kabuki", p.Form);
        Assert.Equal(1, p.TrimDays);
        // The same trim again begins its days anew
        Assert.Equal(2, FormRules.Trim(p, "kabuki"));
        Assert.Equal(FormRules.TrimLasts, p.TrimDays);
        FormRules.TrimDaysPass(new[] { p }, 7);
        Assert.Null(p.Form);
        Assert.Equal(0, p.TrimDays);
        // No Furfrou, no trim
        Assert.Equal(0, FormRules.Trim(new Pokemon(PokemonDatabase.Get("Bunnelby")!, 10), "heart"));
        // In a box it grows out at once, its days with it
        FormRules.Trim(p, "heart");
        Assert.True(FormRules.TrimGrowsOut(p));
        Assert.Equal(0, p.TrimDays);
    }

    [Fact]
    public void ATrimsDaysAreSavedAndLeftOutUntrimmed()
    {
        var p = Furfrou();
        Assert.DoesNotContain("TrimDays", SaveManager.Write(new SaveData { Party = new() { SavedPokemonData.FromPokemon(p) } }));
        FormRules.Trim(p, "star");
        FormRules.TrimDaysPass(new[] { p }, 2);
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal(("Furfrou-Star", 3), (back.Form, back.TrimDays));
        var copy = Furfrou();
        copy.CopyStateFrom(p);
        Assert.Equal(3, copy.TrimDays);
    }

    [Fact]
    public void TheGroomerTrimsTheFurfrouChosenForFiveHundred()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Chespin")!, 20));
        var furfrou = Furfrou();
        party.Add(furfrou);
        var salon = MapDatabase.Fresh()("LumioseCity_GroomingSalon");
        var host = new HeadlessScriptHost(new StoryState(), party, new Inventory())
        {
            Region = RegionDatabase.Kalos, Map = salon, Money = 1000, PokemonChoice = 1
        };
        // Yes, the Furfrou, then "Other trims" and Pharaoh
        foreach (int a in new[] { 0, 4, 4 }) host.Answers.Enqueue(a);
        var library = ScriptLibrary.Default;
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Groomer", "LumioseCity_GroomingSalon") ?? throw new Xunit.Sdk.XunitException("no Groomer"),
            salon.NPCs.First(n => n.Name == "Groomer"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(("Furfrou-Pharaoh", FormRules.TrimLasts, 500), (furfrou.Form, furfrou.TrimDays, host.Money));
        Assert.Null(party.Members[0].Form);
    }
}
