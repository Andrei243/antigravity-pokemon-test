using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto's own leftovers (plan 20): the Lake of Rage's fishing guru and his Magikarp record, the TM10 of the house beside
/// him, the Red Scale and Mr. Pokémon's Exp. Share, Mahogany's souvenir shop selling Rage Candy Bars once the Radio
/// Tower is freed, and the radio's Team Rocket broadcast and Mahogany signal.
/// </summary>
public class JohtoLeftoversTests
{
    private static HeadlessScriptHost Run(string name, string area, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, area)!, new PokemonPlatinumEngine.Overworld.NPC { Name = "Someone" });
        runner.RunToEnd();
        return host;
    }

    private static Pokemon Mon(string species, int level = 20) => new(PokemonDatabase.Get(species)!, level);

    private static int Count(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    [Fact]
    public void TheNewItemsAreInTheData()
    {
        var scale = ItemDatabase.Get("Red Scale")!;
        Assert.Equal(ItemPocket.KeyItems, scale.Pocket);
        var bar = ItemDatabase.Get("Rage Candy Bar")!;
        Assert.Equal(ItemPocket.Medicine, bar.Pocket);
        Assert.Equal(300, bar.Price);
        Assert.Contains("Rage Candy Bar", MartDatabase.Specialties["mahogany_souvenirs"]);
    }

    [Fact]
    public void TheFishingGuruKeepsTheLakesMagikarpRecordAndPaysAnElixirForIt()
    {
        const string area = "lake_of_rage_fishing_guru_house";
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Pikachu"));
        Run("Guru", area, host);
        Assert.Equal(0, Count(host, "Elixir"));

        var magikarp = Mon("Magikarp");
        host.Party.Add(magikarp);
        host.PokemonChoice = 1;
        Run("Guru", area, host);
        Assert.Equal(1, Count(host, "Elixir"));
        Assert.NotEqual(0, host.Story.Var("VAR_JOHTO_MAGIKARP_SIZE_RECORD"));
        // The same Magikarp again is no new record
        Run("Guru", area, host);
        Assert.Equal(1, Count(host, "Elixir"));
        // He only measures Magikarp
        host.PokemonChoice = 0;
        host.Transcript.Clear();
        Run("Guru", area, host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("no Magikarp"));
        // Kanto's fishing brother's record is his own
        Assert.Equal(0, host.Story.Var("VAR_MAGIKARP_SIZE_RECORD"));
    }

    [Fact]
    public void TheManByTheLakeGivesTM10Once()
    {
        const string area = "lake_of_rage_hidden_power_house";
        var host = new HeadlessScriptHost();
        Run("Man", area, host);
        Assert.Equal(1, Count(host, "TM10"));
        Run("Man", area, host);
        Assert.Equal(1, Count(host, "TM10"));
    }

    [Fact]
    public void TheRedGyaradosLeavesTheRedScaleAndMrPokemonTradesAnExpShareForIt()
    {
        // (The red Gyarados's own scene puts the scale in the bag: JohtoStoryTests' fourth chapter holds it)
        var host = new HeadlessScriptHost();
        host.Bag.AddItem(ItemDatabase.Get("Red Scale")!);

        // Refused, he keeps asking; agreed, the scale is his and the Exp. Share the player's, once
        host.Story.Set("FLAG_GOT_MYSTERY_EGG");
        host.Answers.Enqueue(1);
        Run("MrPokemon", "route_30_mr_pokemon_house", host);
        Assert.Equal(1, Count(host, "Red Scale"));
        Assert.Equal(0, Count(host, "Exp. Share"));
        Run("MrPokemon", "route_30_mr_pokemon_house", host);
        Assert.Equal(0, Count(host, "Red Scale"));
        Assert.Equal(1, Count(host, "Exp. Share"));
        Run("MrPokemon", "route_30_mr_pokemon_house", host);
        Assert.Equal(1, Count(host, "Exp. Share"));
    }

    [Fact]
    public void MahoganysSaleswomanSellsRageCandyBars()
    {
        var host = new HeadlessScriptHost();
        Run("Saleswoman", "mahogany_souvenir_shop", host);
        Assert.Contains(host.Log, l => l == "open Shop mahogany_souvenirs");
    }

    [Fact]
    public void TheRadioCarriesTeamRocketsBroadcastAndMahoganysSignalWhileTheyLast()
    {
        var story = new StoryState();
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("goldenrod", false, story));
        story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 1);
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("goldenrod", false, story));
        for (int v = 2; v <= 4; v++)
        {
            story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", v);
            Assert.Equal(RadioSignal.Rocket, RadioDial.SignalAt("new_bark", false, story));
            Assert.Equal(RadioSignal.NoSignal, RadioDial.SignalAt("union_cave_1f", true, story));
        }
        story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 5);
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("goldenrod", false, story));
        foreach (var spot in RadioDial.SpotsOf(RadioSignal.Rocket))
            Assert.Equal(RadioStation.TeamRocket, RadioDial.StationOf(spot.Slot, 9));
        Assert.NotEmpty(RadioShows.Of(RadioStation.TeamRocket));

        // The hideout's signal: in Mahogany, Route 43 and the Lake of Rage, from the red Gyarados met until Ariana is beaten
        var mahogany = new StoryState();
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("mahogany", false, mahogany));
        mahogany.Set("FLAG_HIDE_LAKE_OF_RAGE_RED_GYARADOS");
        Assert.Equal(RadioSignal.Mahogany, RadioDial.SignalAt("mahogany", false, mahogany));
        Assert.Equal(RadioSignal.Mahogany, RadioDial.SignalAt("lake_of_rage", false, mahogany));
        Assert.Equal(RadioSignal.Mahogany, RadioDial.SignalAt("route_43", false, mahogany));
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("blackthorn", false, mahogany));
        foreach (var spot in RadioDial.SpotsOf(RadioSignal.Mahogany))
            Assert.Equal(RadioStation.MahoganySignal, RadioDial.StationOf(spot.Slot, 9));
        Assert.NotEmpty(RadioShows.Of(RadioStation.MahoganySignal));
        mahogany.Set("FLAG_HIDE_MAHOGANY_SHOP_SHADY_SALESMAN");
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("mahogany", false, mahogany));
    }
}
