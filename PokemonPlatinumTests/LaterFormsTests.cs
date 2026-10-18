using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The later forms and evolutions (plan 06 · R29): the key items that change a form outside a battle (the fusions, the
/// Reveal Glass, the Prison Bottle, the Zygarde Cube, the Rotom Catalog), the forms a held item or a move gives (the
/// three Origin Formes' items, Genesect's drives, Silvally's memories, Keldeo's Secret Sword), Platinum's meteorites
/// for Deoxys, and the forms an evolution chooses (Toxtricity, Maushold, Dudunsparce, Alcremie). One rule to a test;
/// docs/mechanics/rulings.md, "Forms changed outside a battle", has the rulings.
/// </summary>
public class LaterFormsTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new ArgumentException(name);

    private static Party Team(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static string[] MovesOf(Pokemon p) => p.Moves.Select(m => m.Name).ToArray();

    // ------------------------------------------------------------------ the data

    [Fact]
    public void TheFormChangingKeyItemsAreInTheBagsKeyItems()
    {
        foreach (var name in new[] { "DNA Splicers", "Reveal Glass", "Prison Bottle", "Zygarde Cube", "N-Solarizer", "N-Lunarizer", "Rotom Catalog", "Reins of Unity" })
        {
            var item = Item(name);
            Assert.Equal(ItemPocket.KeyItems, item.Pocket);
            Assert.True(FormChanges.Handles(item), name);
            Assert.True(BagScreen.CanUse(item), name);
        }
        Assert.Equal("DialgaBoost", Item("Adamant Crystal").HoldEffect);
        Assert.Equal("PalkiaBoost", Item("Lustrous Globe").HoldEffect);
        Assert.Equal("GiratinaBoost", Item("Griseous Core").HoldEffect);
    }

    // ------------------------------------------------------------------ the fusions

    [Fact]
    public void TheDnaSplicersFuseKyuremWithZekromAndTurnItsTwoMoves()
    {
        var kyurem = Mon("Kyurem", 60, "Scary Face", "Glaciate", "Dragon Pulse");
        var zekrom = Mon("Zekrom", 60, "Bolt Strike");
        var party = Team(kyurem, zekrom);

        var result = FormChanges.Apply(Item("DNA Splicers"), kyurem, party);
        Assert.True(result.Applied);
        Assert.Equal("Kyurem-Black", kyurem.Form);
        Assert.Same(zekrom, kyurem.FusedWith);
        Assert.DoesNotContain(zekrom, party.Members);
        Assert.Equal(new[] { "Fusion Bolt", "Freeze Shock", "Dragon Pulse" }, MovesOf(kyurem));

        // Used again it splits them: Zekrom back beside it, the moves as they were
        Assert.True(FormChanges.Apply(Item("DNA Splicers"), kyurem, party).Applied);
        Assert.Null(kyurem.Form);
        Assert.Null(kyurem.FusedWith);
        Assert.Equal(new[] { kyurem, zekrom }, party.Members);
        Assert.Equal(new[] { "Scary Face", "Glaciate", "Dragon Pulse" }, MovesOf(kyurem));
    }

    [Fact]
    public void ReshiramMakesAWhiteKyurem()
    {
        var kyurem = Mon("Kyurem", 60, "Scary Face", "Glaciate");
        FormChanges.Apply(Item("DNA Splicers"), kyurem, Team(kyurem, Mon("Reshiram", 60)));
        Assert.Equal("Kyurem-White", kyurem.Form);
        Assert.Equal(new[] { "Fusion Flare", "Ice Burn" }, MovesOf(kyurem));
    }

    [Fact]
    public void AFusionWantsItsPartnerInTheTeamAndASplitWantsRoom()
    {
        var kyurem = Mon("Kyurem", 60);
        Assert.False(FormChanges.WouldHelp(Item("DNA Splicers"), kyurem, Team(kyurem, Mon("Pikachu", 20))));
        Assert.False(FormChanges.Apply(Item("DNA Splicers"), kyurem, Team(kyurem)).Applied);
        // The wrong item does nothing for it
        Assert.False(FormChanges.WouldHelp(Item("N-Solarizer"), kyurem, Team(kyurem, Mon("Zekrom", 60))));

        var party = Team(kyurem, Mon("Zekrom", 60));
        FormChanges.Apply(Item("DNA Splicers"), kyurem, party);
        while (!party.IsFull) party.Add(Mon("Bidoof", 5));
        Assert.False(FormChanges.WouldHelp(Item("DNA Splicers"), kyurem, party));
        Assert.Equal("Kyurem-Black", kyurem.Form);
    }

    [Fact]
    public void TheNSolarizerTeachesSunsteelStrikeAndASplitForgetsIt()
    {
        var necrozma = Mon("Necrozma", 60, "Photon Geyser");
        var party = Team(necrozma, Mon("Solgaleo", 60));
        FormChanges.Apply(Item("N-Solarizer"), necrozma, party);
        Assert.Equal("Necrozma-Dusk", necrozma.Form);
        Assert.Equal(new[] { "Photon Geyser", "Sunsteel Strike" }, MovesOf(necrozma));
        Assert.True(necrozma.HasType(PokemonType.Steel));

        FormChanges.Apply(Item("N-Solarizer"), necrozma, party);
        Assert.Equal(new[] { "Photon Geyser" }, MovesOf(necrozma));
    }

    [Fact]
    public void AFusionsMoveThatDoesntFitIsAskedAbout()
    {
        var necrozma = Mon("Necrozma", 60, "Photon Geyser", "Psycho Cut", "Night Slash", "Rock Blast");
        var result = FormChanges.Apply(Item("N-Lunarizer"), necrozma, Team(necrozma, Mon("Lunala", 60)));
        Assert.Equal("Necrozma-Dawn", necrozma.Form);
        Assert.Equal(new[] { "Moongeist Beam" }, result.Wanted);
    }

    [Fact]
    public void ASplitThatWouldLeaveNoMoveLeavesConfusion()
    {
        var calyrex = Mon("Calyrex", 80, "Growth");
        var party = Team(calyrex, Mon("Spectrier", 75));
        FormChanges.Apply(Item("Reins of Unity"), calyrex, party);
        Assert.Equal("Calyrex-Shadow", calyrex.Form);
        Assert.True(calyrex.HasType(PokemonType.Ghost));
        calyrex.Moves.RemoveAt(0); // only Astral Barrage left
        FormChanges.Apply(Item("Reins of Unity"), calyrex, party);
        Assert.Equal(new[] { FormChanges.Fallback }, MovesOf(calyrex));
    }

    [Fact]
    public void AFusedPartnerIsSavedInsideIt()
    {
        var calyrex = Mon("Calyrex", 80, "Growth");
        var glastrier = Mon("Glastrier", 75, "Icicle Crash");
        glastrier.Nickname = "Frost";
        FormChanges.Apply(Item("Reins of Unity"), calyrex, Team(calyrex, glastrier));

        var back = SavedPokemonData.FromPokemon(calyrex).ToPokemon(Ruleset.Modern);
        Assert.Equal("Calyrex-Ice", back.Form);
        Assert.NotNull(back.FusedWith);
        Assert.Equal("Glastrier", back.FusedWith!.Species.Name);
        Assert.Equal("Frost", back.FusedWith.Nickname);
        Assert.Contains("Glacial Lance", MovesOf(back));
        // One fused with nobody writes nothing of it
        Assert.Null(SavedPokemonData.FromPokemon(Mon("Calyrex", 80)).FusedWith);
    }

    // ------------------------------------------------------------------ the other key items

    [Fact]
    public void TheRevealGlassTurnsLandorusBetweenItsFormes()
    {
        var landorus = Mon("Landorus", 60);
        landorus.AbilityName = landorus.Abilities[0];
        Assert.True(FormChanges.Apply(Item("Reveal Glass"), landorus, Team(landorus)).Applied);
        Assert.Equal("Landorus-Therian", landorus.Form);
        Assert.Equal("Intimidate", landorus.AbilityName);
        FormChanges.Apply(Item("Reveal Glass"), landorus, Team(landorus));
        Assert.Null(landorus.Form);
        Assert.False(FormChanges.WouldHelp(Item("Reveal Glass"), Mon("Pikachu", 10), null));
    }

    [Fact]
    public void ThePrisonBottleUnbindsHoopaAndTurnsHyperspaceHole()
    {
        var hoopa = Mon("Hoopa", 60, "Hyperspace Hole", "Psychic");
        FormChanges.Apply(Item("Prison Bottle"), hoopa, Team(hoopa));
        Assert.Equal("Hoopa-Unbound", hoopa.Form);
        Assert.Equal(new[] { "Hyperspace Fury", "Psychic" }, MovesOf(hoopa));
        FormChanges.Apply(Item("Prison Bottle"), hoopa, Team(hoopa));
        Assert.Null(hoopa.Form);
        Assert.Equal(new[] { "Hyperspace Hole", "Psychic" }, MovesOf(hoopa));
    }

    [Fact]
    public void TheZygardeCubeTurnsZygardeBetweenTenAndFiftyKeepingItsKindOfAbility()
    {
        var zygarde = Mon("Zygarde", 60);
        FormChanges.Apply(Item("Zygarde Cube"), zygarde, null);
        Assert.Equal("Zygarde-10", zygarde.Form);
        FormChanges.Apply(Item("Zygarde Cube"), zygarde, null);
        Assert.Null(zygarde.Form);

        var cells = Mon("Zygarde", 60);
        cells.ChangeForm("Zygarde-10-Power-Construct");
        FormChanges.Apply(Item("Zygarde Cube"), cells, null);
        Assert.Equal("Zygarde-50-Power-Construct", cells.Form);
        Assert.Equal("Power Construct", cells.AbilityName);
    }

    [Fact]
    public void TheRotomCatalogTurnsItsPagesAndItsAppliancesMove()
    {
        var rotom = Mon("Rotom", 30, "Thunder Shock", "Astonish");
        var catalog = Item("Rotom Catalog");
        FormChanges.Apply(catalog, rotom, null);
        Assert.Equal("Rotom-Heat", rotom.Form);
        Assert.Equal(new[] { "Thunder Shock", "Astonish", "Overheat" }, MovesOf(rotom));
        FormChanges.Apply(catalog, rotom, null);
        Assert.Equal("Rotom-Wash", rotom.Form);
        // The new appliance's move takes the last one's place
        Assert.Equal(new[] { "Thunder Shock", "Astonish", "Hydro Pump" }, MovesOf(rotom));
        foreach (var _ in Enumerable.Range(0, 4)) FormChanges.Apply(catalog, rotom, null);
        Assert.Null(rotom.Form);
        Assert.Equal(new[] { "Thunder Shock", "Astonish" }, MovesOf(rotom));

        // Its appliance's move as its only one, it is left Thunder Shock
        var alone = Mon("Rotom", 30, "Leaf Storm");
        alone.ChangeForm("Rotom-Mow");
        FormChanges.Apply(catalog, alone, null);
        Assert.Equal(new[] { FormChanges.RotomFallback }, MovesOf(alone));
    }

    // ------------------------------------------------------------------ forms by a held item and by a move

    [Fact]
    public void LegendsArceussThreeHoldTheirPokemonInTheOriginFormes()
    {
        foreach (var (species, item) in new[] { ("Dialga", "Adamant Crystal"), ("Palkia", "Lustrous Globe"), ("Giratina", "Griseous Core") })
        {
            var p = Mon(species, 70);
            p.HeldItem = Item(item);
            Assert.True(FormRules.ByHeldItem(p), species);
            Assert.Equal(species + "-Origin", p.Form);
            p.HeldItem = null;
            Assert.True(FormRules.ByHeldItem(p));
            Assert.Null(p.Form);
        }
        // The orb Platinum's Dialga holds powers it without changing it
        var dialga = Mon("Dialga", 70);
        dialga.HeldItem = Item("Adamant Orb");
        Assert.False(FormRules.ByHeldItem(dialga));
    }

    [Fact]
    public void GenesectAndSilvallyLookLikeWhatTheyHold()
    {
        var genesect = Mon("Genesect", 60);
        genesect.HeldItem = Item("Burn Drive");
        Assert.True(FormRules.ByHeldItem(genesect));
        Assert.Equal("Genesect-Burn", genesect.Form);

        var silvally = Mon("Silvally", 60);
        silvally.AbilityName = "RKS System";
        silvally.HeldItem = Item("Water Memory");
        Assert.True(FormRules.ByHeldItem(silvally));
        Assert.Equal("Silvally-Water", silvally.Form);
        Assert.True(silvally.HasType(PokemonType.Water));
    }

    [Fact]
    public void KeldeoIsResoluteWhileItKnowsSecretSword()
    {
        var keldeo = Mon("Keldeo", 60, "Aqua Jet");
        MoveTeaching.Learn(keldeo, "Secret Sword");
        Assert.Equal("Keldeo-Resolute", keldeo.Form);
        MoveTeaching.Learn(Fill(keldeo), "Surf", forget: keldeo.Moves.FindIndex(m => m.Name == "Secret Sword"));
        Assert.Null(keldeo.Form);

        static Pokemon Fill(Pokemon p)
        {
            while (p.Moves.Count < 4) p.TryLearn(new[] { "Bubble", "Leer", "Double Kick" }[p.Moves.Count - 2]);
            return p;
        }
    }

    // ------------------------------------------------------------------ Veilstone City's meteorites (Platinum's own)

    [Fact]
    public void AMeteoritePutsEveryDeoxysOfTheTeamInItsForme()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Deoxys", 50));
        host.Party.Add(Mon("Deoxys", 50));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("veilstone_city.MeteoriteAttack")!);
        runner.RunToEnd();
        Assert.All(host.Party.Members, p => Assert.Equal("Deoxys-Attack", p.Form));

        runner.Start(ScriptLibrary.Default.Find("veilstone_city.MeteoriteNormal")!);
        runner.RunToEnd();
        Assert.All(host.Party.Members, p => Assert.Null(p.Form));
    }

    [Fact]
    public void WithNoDeoxysAMeteoriteIsOnlyAStone()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", 10));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("veilstone_city.MeteoriteSpeed")!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, t => t.Text.Contains("fell from the stars"));
        Assert.Null(host.Party.Members[0].Form);
    }

    // ------------------------------------------------------------------ the forms an evolution chooses

    private static EvolutionContext Day => new();

    [Fact]
    public void ToxtricitysFormIsItsNatures()
    {
        var toxel = new Pokemon(PokemonDatabase.Get("Toxel")!, 30, Gender.Male, Nature.Timid, false);
        Assert.Equal("Toxtricity-Low-Key", Evolution.Find(toxel, EvolutionTrigger.LevelUp, Day)?.TargetForm);
        var amped = new Pokemon(PokemonDatabase.Get("Toxel")!, 30, Gender.Male, Nature.Adamant, false);
        Assert.Null(Evolution.Find(amped, EvolutionTrigger.LevelUp, Day)?.TargetForm);
        // A mint doesn't change what it becomes
        toxel.MintNature = Nature.Adamant;
        Assert.Equal("Toxtricity-Low-Key", Evolution.Find(toxel, EvolutionTrigger.LevelUp, Day)?.TargetForm);
    }

    [Fact]
    public void OneMausholdInAHundredIsAFamilyOfThree()
    {
        var tandemaus = Mon("Tandemaus", 25);
        tandemaus.Personality = 1200;
        Assert.Equal("Maushold-Family-Of-Three", Evolution.Find(tandemaus, EvolutionTrigger.LevelUp, Day)?.TargetForm);
        tandemaus.Personality = 1201;
        Assert.Null(Evolution.Find(tandemaus, EvolutionTrigger.LevelUp, Day)?.TargetForm);

        var dunsparce = Mon("Dunsparce", 40, "Hyper Drill");
        dunsparce.Personality = 300;
        Assert.Equal("Dudunsparce-Three-Segment", Evolution.Find(dunsparce, EvolutionTrigger.LevelUp, Day)?.TargetForm);
    }

    [Theory]
    [InlineData(true, 2f, false, false, "Vanilla-Cream")]
    [InlineData(false, 2f, false, false, "Ruby-Cream")]
    [InlineData(true, 2f, true, false, "Matcha-Cream")]
    [InlineData(false, 6f, true, false, "Mint-Cream")]
    [InlineData(true, 6f, true, false, "Lemon-Cream")]
    [InlineData(false, 2f, true, false, "Salted-Cream")]
    [InlineData(false, 6f, false, false, "Ruby-Swirl")]
    [InlineData(true, 6f, false, false, "Caramel-Swirl")]
    [InlineData(false, 11f, false, true, "Rainbow-Swirl")]
    [InlineData(false, 6f, false, true, "Ruby-Swirl")]
    public void AlcremiesCreamIsTheSpinsWayLengthAndHour(bool clockwise, float seconds, bool night, bool dusk, string cream)
    {
        Assert.Equal(cream, Evolution.AlcremieCream(clockwise, seconds, night, dusk));
    }

    [Fact]
    public void AlcremieWearsItsSweetOnItsCream()
    {
        var milcery = Mon("Milcery", 10);
        milcery.HeldItem = Item("Star Sweet");
        var context = new EvolutionContext { Spin = new SpinMade(false, 7f), IsNight = true };
        Assert.Equal("Alcremie-Mint-Cream-Star-Sweet", Evolution.Find(milcery, EvolutionTrigger.Spin, context)?.TargetForm);
        // A quick clockwise spin by day with the Strawberry Sweet is the species' own
        milcery.HeldItem = Item("Strawberry Sweet");
        Assert.Null(Evolution.Find(milcery, EvolutionTrigger.Spin, new EvolutionContext { Spin = new SpinMade(true, 1f) })?.TargetForm);
    }

    [Fact]
    public void ASpinIsToldOnceItIsOverWithItsWayAndLength()
    {
        const float dt = 1f / 60f;
        var spin = new SpinTracker();
        // Anticlockwise, a quarter turn a second, for eight quarter turns
        var around = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        bool told = false;
        for (int i = 0; i <= 8; i++)
            for (int f = 0; f < 60; f++) told |= spin.Update(around[i % 4], dt);
        Assert.False(told);
        for (int f = 0; f < 80; f++) told |= spin.Update(Direction.Up, dt);
        Assert.True(told);
        Assert.False(spin.Last!.Value.Clockwise);
        // From the first quarter turn to the eighth
        Assert.InRange(spin.Last.Value.Seconds, 6.9f, 7.1f);
    }
}
