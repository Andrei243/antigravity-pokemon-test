using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's picnics (plan 34 · PD7, decision 4 at its recommendation; <c>docs/mechanics/rulings.md</c>, "Tera Raids and
/// picnics"): the sandwiches are a second section of <c>recipes.json</c> on the camp's one pot, made at a Paldean picnic with
/// no crafting kit; a sandwich's Type Power weighs its type's rows up in the open map's draws and its Encounter Power brings
/// more out, and both wear off with play; its Sparkling Power adds shiny rolls for its type; and the picnic basket leaves the
/// team's couple's Eggs, more under the Egg Power. A Paldean deli sells the pantry's goods.
/// </summary>
public class PaldeaPicnicTests
{
    private const int T = OpenClock.TicksPerSecond;

    private static KitchenIngredient Material(string id) => new(id, true);
    private static KitchenIngredient Item(string name) => new(name, false);

    [Fact]
    public void TheSandwichesAreASecondSectionOnTheSamePotHeldToItsRules()
    {
        var table = Meals.Sandwiches;
        Assert.Empty(Meals.Problems(table));
        Assert.NotSame(Meals.Table, table);
        Assert.Equal(4, table.Grades.Count);
        Assert.All(table.Grades, g => Assert.Equal(1800, g.Seconds));
        // Each power a sandwich starts is read, and Paldea's two new kinds are among them
        Assert.All(table.Dishes.Where(d => d.Power != null), d => Assert.Contains(d.Power!.Value, PowerTable.Read));
        Assert.Contains(table.Dishes, d => d.Power == PowerKind.Egg);
        Assert.Contains(table.Dishes, d => d.Power == PowerKind.Sparkling);
        // What a pot of the picnic's makes, by the sandwiches' table and not the camp's
        Assert.Equal("tomatoCheeseSandwich", Meals.DishFor(new[] { Material("tomatoes"), Material("cheese") }, table).Id);
        Assert.Equal("berryJamSandwich", Meals.DishFor(new[] { Material("butter"), Item("Oran Berry") }, table).Id);
        Assert.Equal("starlightSandwich", Meals.DishFor(new[] { Material("cheese"), Material("tomatoes"), Item("Oran Berry"), Item("Honey") }, table).Id);
        Assert.Equal("picnicSandwich", Meals.DishFor(new[] { Material("rice") }, table).Id);
        Assert.Equal("beanHotpot", Meals.DishFor(new[] { Material("beans"), Material("onions"), Material("potatoes") }).Id);
        // A power of a type is granted only with its type
        var powers = new PowerState();
        Assert.False(powers.Grant(PowerKind.Sparkling, 1, 60));
        Assert.True(powers.Grant(PowerKind.Sparkling, 1, 60, PokemonType.Water));
        Assert.True(PowerTable.TakesType(PowerKind.Sparkling) && PowerTable.TakesType(PowerKind.TypeEncounter) && !PowerTable.TakesType(PowerKind.Egg));
    }

    [Fact]
    public void ASandwichServedAtThePicnicStartsItsPowerForHalfAnHourOfPlay()
    {
        var party = new Party();
        party.Add(Mon("Sprigatito", 10, "Scratch"));
        var powers = new PowerState();
        var table = Meals.Sandwiches;
        var dish = Meals.DishFor(new[] { Material("butter"), Item("Rawst Berry") }, table);
        var served = Meals.Serve(party, dish, DishGrade.Superb, powers, new CampVisit(), Meals.TypeFor(new[] { Item("Rawst Berry") }), table);
        Assert.NotNull(served.Power);
        Assert.Equal(PowerKind.TypeEncounter, served.Power!.Kind);
        Assert.Equal(3, served.Power.Level);
        Assert.Equal(1800f, powers.SecondsLeft(PowerKind.TypeEncounter));
        Assert.Equal(ItemDatabase.Get("Rawst Berry")!.NaturalGiftType, powers.TypeOf(PowerKind.TypeEncounter));
        // The camp's screen in Paldea cooks from the sandwiches' table
        var kitchen = new CampKitchen(new MaterialPouch(), new Inventory(), powers, TimingAssist.Off, 7, Meals.Sandwiches);
        Assert.True(kitchen.Sandwiches);
        Assert.Same(Meals.Sandwiches, kitchen.Recipes);
        Assert.False(new CampKitchen(new MaterialPouch(), new Inventory(), powers, TimingAssist.Off, 7).Sandwiches);
    }

    // ------------------------------------------------------------------ the powers on the open map's draws

    private static OpenPos On(OpenMap map, double x, double z) =>
        OpenPos.FromMetres(x, map.Ground.Footing(x, z, 20, -20, walkableOnly: true)!.Value.Y, z);

    /// <summary>What a field over the lab's south meadow brings out in so many rounds, by species, and the most stood there at once.</summary>
    private static (Dictionary<string, int> Seen, int Most) Draws(PowerState powers, int rounds = 10)
    {
        var map = OpenWorldPeopleTests.LabWithPeople();
        var zone = map.Zones.Single(z => z.Name == "south_meadow");
        var field = new OpenField(map, On(map, 24, 6), OpenYaw.North);
        field.Wild.Now = TimeOfDay.Day;
        var seen = new Dictionary<string, int>();
        var known = new HashSet<int>();
        int most = 0;
        for (int round = 0; round < rounds; round++)
        {
            for (int i = 0; i < 8 * T; i++)
            {
                // As the engine does each frame: the powers in force on the spawner
                field.Wild.Favoured = OpenWildField.FavouredBy(powers);
                field.Wild.MoreStanding = OpenWildField.MoreStandingBy(powers);
                field.Player.Step(field.Map, OpenInput.None, field.Grants);
                field.StepTheRest();
                foreach (var w in field.Wild.All)
                    if (w.Zone == zone && known.Add(w.Id)) seen[w.Species] = seen.GetValueOrDefault(w.Species) + 1;
                most = Math.Max(most, field.Wild.All.Count(w => w.Zone == zone));
            }
            // Away and back, so the zone brings out afresh
            field.Player.Place(On(map, -60, -60), OpenYaw.East);
            field.Player.Step(field.Map, OpenInput.None, field.Grants);
            field.StepTheRest();
            field.Player.Place(On(map, 24, 6), OpenYaw.North);
        }
        return (seen, most);
    }

    [Fact]
    public void ASandwichsTypePowerChangesTheDrawsOfItsAreaAndWearsOff()
    {
        var none = Draws(new PowerState());
        int Share(Dictionary<string, int> seen) => 100 * seen.GetValueOrDefault("Starly") / Math.Max(1, seen.Values.Sum());

        // Flying favoured at its strongest: the meadow's Starly (Normal and Flying) far outweighs its Bidoof
        var flying = new PowerState();
        Assert.True(flying.Grant(PowerKind.TypeEncounter, 3, 1800, PokemonType.Flying));
        Assert.Equal((PokemonType.Flying, PowerTable.Strength(PowerKind.TypeEncounter, 3)), OpenWildField.FavouredBy(flying));
        var favoured = Draws(flying);
        Assert.True(Share(favoured.Seen) > Share(none.Seen), $"Starly {Share(favoured.Seen)}% favoured against {Share(none.Seen)}% plain");
        Assert.True(Share(favoured.Seen) >= 80, $"Starly {Share(favoured.Seen)}%");
        var map = OpenWorldPeopleTests.LabWithPeople();
        var starly = map.Zones.Single(z => z.Name == "south_meadow").Slots.Single(s => s.Species == "Starly");
        var wild = new OpenWildField { Favoured = OpenWildField.FavouredBy(flying) };
        Assert.Equal(starly.Weight * (1 + PowerTable.Strength(PowerKind.TypeEncounter, 3)), wild.WeightOf(starly));

        // Worn off with play: nothing favoured, and the draws are those of a field with no power, to the Pokémon
        flying.Tick(1801f);
        Assert.Null(OpenWildField.FavouredBy(flying));
        var after = Draws(flying);
        Assert.Equal(none.Seen, after.Seen);
    }

    [Fact]
    public void ASandwichsEncounterPowerBringsMoreOutAndWearsOff()
    {
        var plain = Draws(new PowerState(), rounds: 4);
        var map = OpenWorldPeopleTests.LabWithPeople();
        int target = OpenWildField.TargetOf(map.Zones.Single(z => z.Name == "south_meadow"));
        Assert.Equal(target, plain.Most);
        var more = new PowerState();
        Assert.True(more.Grant(PowerKind.Encounter, 3, 1800));
        Assert.Equal(3, OpenWildField.MoreStandingBy(more));
        Assert.True(Draws(more, rounds: 4).Most > target);
        more.Tick(1800f);
        Assert.Equal(0, OpenWildField.MoreStandingBy(more));
        Assert.Equal(plain.Most, Draws(more, rounds: 4).Most);
    }

    // ------------------------------------------------------------------ the picnic basket

    [Fact]
    public void ThePicnicBasketLeavesTheCouplesEggsAndMoreUnderTheEggPower()
    {
        var party = new Party();
        var male = Mon("Lechonk", 20);
        male.Gender = Gender.Male;
        var female = Mon("Lechonk", 20);
        female.Gender = Gender.Female;
        var fuecoco = Mon("Fuecoco", 20);
        fuecoco.Gender = Gender.Male;
        party.Add(male);
        party.Add(female);
        party.Add(fuecoco);
        // (the first couple in the team's order: the Lechonk pair, before the Fuecoco and the female)
        Assert.Equal((male, female), PicnicBasket.Couple(party));
        Assert.Equal(50, PicnicBasket.ChanceOf(Compatibility.Medium, 0));
        Assert.Equal(85, PicnicBasket.ChanceOf(Compatibility.Medium, 35));
        Assert.Equal(100, PicnicBasket.ChanceOf(Compatibility.High, 50));
        Assert.Equal(0, PicnicBasket.ChanceOf(Compatibility.None, 50));

        // Under a minute leaves nothing; ten minutes some Eggs, of the couple's species, the same for the same picnic
        Assert.Empty(PicnicBasket.Find(party, 59f, 0, 1234, 4321, Ruleset.Modern));
        var eggs = PicnicBasket.Find(party, 600f, 0, 1234, 4321, Ruleset.Modern);
        Assert.NotEmpty(eggs);
        Assert.InRange(eggs.Count, 1, PicnicBasket.Most);
        Assert.All(eggs, e => Assert.True(e.IsEgg));
        Assert.All(eggs, e => Assert.Equal("Lechonk", e.Species.Name));
        Assert.Equal(eggs.Select(e => e.Personality), PicnicBasket.Find(party, 600f, 0, 1234, 4321, Ruleset.Modern).Select(e => e.Personality));
        // The Egg Power leaves at least as many over many picnics, and never more than the basket holds
        int plain = 0, powered = 0;
        for (uint seed = 0; seed < 40; seed++)
        {
            plain += PicnicBasket.Find(party, 600f, 0, seed, 4321, Ruleset.Modern).Count;
            powered += PicnicBasket.Find(party, 600f, PowerTable.Strength(PowerKind.Egg, 3), seed, 4321, Ruleset.Modern).Count;
        }
        Assert.True(powered > plain, $"{powered} Eggs with the power against {plain} without");
        Assert.True(PicnicBasket.Find(party, 3600f, 100, 9, 4321, Ruleset.Modern).Count <= PicnicBasket.Most);
        // A team with no couple leaves nothing
        var lone = new Party();
        lone.Add(fuecoco);
        lone.Add(male);
        Assert.Null(PicnicBasket.Couple(lone));
        Assert.Empty(PicnicBasket.Find(lone, 3600f, 100, 9, 4321, Ruleset.Modern));
    }

    // ------------------------------------------------------------------ the deli

    [Fact]
    public void MesagozasDeliOpensThePantryWithNoKitNeeded()
    {
        var story = new StoryState();
        var party = new Party();
        party.Add(Mon("Sprigatito", 10, "Scratch"));
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Paldea };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("MesagozaDeli", "mesagoza")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Log, l => l.Contains("Crafting") && l.Contains("pantry"));
    }
}
