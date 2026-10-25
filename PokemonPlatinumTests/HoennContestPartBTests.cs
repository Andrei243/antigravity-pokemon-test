using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The second half of plan 21 · H11: a Master Rank won by the same round twice, the Lilycove Museum's gallery (the
/// paintings, the curator and his Glass Ornament, the patrons who come), Slateport's chairman and his five scarves,
/// Mossdeep's black belt and the colour his eye finds, Feebas grown beautiful on Pokéblocks, and the Safari Zone's counter
/// asking for the Case.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennContestPartBTests
{
    private const string Lobby = "lilycove_city_contest_lobby";
    private const string Museum = "lilycove_city_lilycove_museum_2f";

    private static Pokemon Mon(string species = "Bidoof", Nature nature = Nature.Hardy) =>
        new(PokemonDatabase.Get(species)!, 20, Gender.Male, nature, false);

    private static HeadlessScriptHost Run(string name, string area, HeadlessScriptHost host, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, area)!, new NPC());
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    // ---------------------------------------------------------------- a Master Rank, the same twice

    private static (int Place, int Points, int[] Totals, int[] Standings) PlayMaster(int seed)
    {
        var player = new Pokemon(PokemonDatabase.Get("Milotic")!, 50) { Cool = 160, Beauty = 255, Cute = 160, Smart = 120, Tough = 120, Sheen = 255 };
        var stage = HoennContestRules.Stage(player, "Ann", ContestStat.Beauty, AppealRank.Master, afterTheGame: true, new Random(seed));
        var round = new AppealRound(stage, ContestStat.Beauty, AppealRank.Master, new Random(seed + 1));
        int moves = round.Appealers[round.PlayerIndex].Moves.Count;
        // The same four moves in turn: never one twice running, so nothing is lost to a repeat
        while (!round.IsOver)
        {
            round.ChooseMoves(round.AppealNumber % moves);
            for (int t = 0; t < AppealRound.Contestants; t++) round.PlayTurn();
        }
        return (round.FinalStandings[round.PlayerIndex], round.TotalPoints[round.PlayerIndex], round.TotalPoints.ToArray(), round.FinalStandings.ToArray());
    }

    [Fact]
    public void AMasterRankIsWonByTheSameRoundOnASecondRunAndTheWinGivesTheRibbonAndTheLuxuryBall()
    {
        // The first seed that the player wins: the contest is the seed's and the choices', so it is the same each time
        int seed = Enumerable.Range(1, 400).FirstOrDefault(s => PlayMaster(s).Place == 0);
        Assert.NotEqual(0, seed);
        var first = PlayMaster(seed);
        var second = PlayMaster(seed);
        Assert.Equal(first.Place, second.Place);
        Assert.Equal(first.Points, second.Points);
        Assert.Equal(first.Totals, second.Totals);
        Assert.Equal(first.Standings, second.Standings);
        Assert.Equal(0, second.Place);

        // A winner of the Master Rank that has the rank below is given the ribbon; won again, a Luxury Ball
        var winner = new Pokemon(PokemonDatabase.Get("Milotic")!, 50);
        foreach (var rank in new[] { AppealRank.Normal, AppealRank.Super, AppealRank.Hyper })
            winner.Ribbons.Add(HoennContestRules.RibbonOf(ContestStat.Beauty, rank));
        Assert.Equal(AppealEntry.Equal, HoennContestRules.Check(winner, ContestStat.Beauty, AppealRank.Master));
        var won = HoennContestRules.Conclude(ContestStat.Beauty, AppealRank.Master, winner, 0, second.Points);
        Assert.Equal("RIBBON_HOENN_BEAUTY_MASTER", won.Ribbon);
        Assert.False(won.GaveLuxuryBall);
        Assert.Equal(second.Points >= AppealRules.PaintingPoints, won.PaintingOffered);
        var again = HoennContestRules.Conclude(ContestStat.Beauty, AppealRank.Master, winner, 0, second.Points);
        Assert.Null(again.Ribbon);
        Assert.True(again.GaveLuxuryBall);
    }

    // ---------------------------------------------------------------- the gallery

    [Fact]
    public void AFrameIsEmptyUntilAPaintingHangsAndThenIsLookedAtInAScreenOfItsOwn()
    {
        var empty = Run("CoolPainting", Museum, new HeadlessScriptHost());
        Assert.Contains(empty.Transcript, l => l.Text.Contains("An empty frame") && l.Text.Contains("Coolness"));
        Assert.DoesNotContain(empty.Log, l => l.StartsWith("open ContestPainting"));

        var host = new HeadlessScriptHost();
        var p = Mon("Milotic");
        var outcome = HoennContestRules.Conclude(ContestStat.Cool, AppealRank.Master, p, 0, 830);
        host.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(outcome, p, "Ann"));
        Run("CoolPainting", Museum, host);
        Assert.Contains("open ContestPainting", host.Log);
        Assert.DoesNotContain(host.Transcript, l => l.Text.Contains("An empty frame"));
        // Another quality's frame is still empty
        Run("SmartPainting", Museum, host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Smartness"));
        Assert.Single(host.Log, l => l.StartsWith("open ContestPainting"));
    }

    [Fact]
    public void ThePaintingScreenShowsAPaintingAndEitherButtonPutsItAway()
    {
        var screen = new FashionCaseScreen();
        var painting = new ContestPainting { Species = "Milotic", Trainer = "Ann", Category = ContestStat.Beauty, Rank = AppealRank.Master, Points = 840 };
        screen.OpenPainting(painting);
        Assert.True(screen.IsActive);
        Assert.Same(painting, screen.Painting);
        // The arrows and the tab button do nothing to a picture
        screen.Move(1, 0);
        screen.MoveTab(1);
        Assert.Equal(0, screen.Tab);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Null(screen.Painting);
    }

    [Fact]
    public void TheCuratorThanksForEachPaintingAndGivesTheGlassOrnamentOnceAllFiveHang()
    {
        var host = new HeadlessScriptHost();
        Run("Curator", Museum, host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("fill these five frames"));
        Assert.Empty(host.Decorations);

        var qualities = Enum.GetValues<ContestStat>();
        for (int i = 0; i < 4; i++)
        {
            var p = Mon();
            var outcome = HoennContestRules.Conclude(qualities[i], AppealRank.Master, p, 0, 900);
            host.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(outcome, p, "Ann"));
        }
        Assert.Equal(4, host.HoennContests.MuseumCount);
        var four = Run("Curator", Museum, new HeadlessScriptHost(host.Story, host.Party, host.Bag) { });
        Assert.DoesNotContain(four.Transcript, l => l.Text.Contains("Glass Ornament"));

        // The fifth frame: the ornament goes into the decoration PC, once
        var fifth = new HeadlessScriptHost();
        foreach (var q in qualities)
        {
            var p = Mon();
            var outcome = HoennContestRules.Conclude(q, AppealRank.Master, p, 0, 900);
            fifth.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(outcome, p, "Ann"));
        }
        Run("Curator", Museum, fifth);
        Assert.True(fifth.Story.Has("FLAG_RECEIVED_GLASS_ORNAMENT"));
        Assert.Contains(fifth.Decorations, d => DecorationDatabase.Get(d.Kind)?.Name == "Glass Ornament");
        int held = fifth.Decorations.Count;
        Run("Curator", Museum, fifth);
        Assert.Equal(held, fifth.Decorations.Count);
        Assert.Contains(fifth.Transcript, l => l.Text.Contains("honour"));
    }

    [Fact]
    public void TheMuseumFillsWithPatronsAsPaintingsHangAndTouristsWithTheFifth()
    {
        string[] flags =
        {
            "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_1", "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_2", "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_3",
            "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_4", "FLAG_HIDE_LILYCOVE_MUSEUM_TOURISTS"
        };
        var host = new HeadlessScriptHost();
        foreach (string flag in flags) host.Story.Set(flag);
        var qualities = Enum.GetValues<ContestStat>();
        for (int i = 0; i < 5; i++)
        {
            var p = Mon();
            var outcome = HoennContestRules.Conclude(qualities[i], AppealRank.Master, p, 0, 900);
            host.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(outcome, p, "Ann"));
            Run("MuseumPatrons", Lobby, host);
            for (int f = 0; f < 5; f++) Assert.Equal(f > i, host.Story.Has(flags[f]));
        }
        // A quality hung again replaces its painting and brings nobody new
        var again = Mon();
        host.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(HoennContestRules.Conclude(ContestStat.Cool, AppealRank.Master, again, 0, 1000), again, "Bea"));
        Assert.Equal(5, host.HoennContests.MuseumCount);
        Assert.Equal("Bea", host.HoennContests.Museum[(int)ContestStat.Cool]!.Trainer);
    }

    [Fact]
    public void ThePaintersOfferHangsThePatronsToo()
    {
        var host = new HeadlessScriptHost { AppealPoints = 850, Map = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().Single(m => m.Name == "LilycoveCity_ContestLobby") };
        foreach (string flag in new[] { "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_1" }) host.Story.Set(flag);
        var p = Mon();
        foreach (var rank in new[] { AppealRank.Normal, AppealRank.Super, AppealRank.Hyper, AppealRank.Master })
            p.Ribbons.Add(HoennContestRules.RibbonOf(ContestStat.Tough, rank));
        host.Party.Add(p);
        host.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        // ENTER, the Master Rank, Tough; "enter anyway?" yes; "take the painting to the museum?" yes
        foreach (int a in new[] { 0, 3, 4, 0, 0 }) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("ContestReceptionist", Lobby)!, new NPC());
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(1, host.HoennContests.MuseumCount);
        Assert.False(host.Story.Has("FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_1"));
        Assert.True(host.Story.Has("FLAG_ENTERED_CONTEST"));
    }

    [Fact]
    public void ThePaintingsAreKeptInTheSaveAndComeBackAsTheyWere()
    {
        var records = new HoennContestRecords();
        var p = Mon("Milotic");
        records.HangInMuseum(HoennContestRules.PaintingOf(HoennContestRules.Conclude(ContestStat.Beauty, AppealRank.Master, p, 0, 870), p, "Ann"));
        var json = System.Text.Json.JsonSerializer.Serialize(records);
        var back = System.Text.Json.JsonSerializer.Deserialize<HoennContestRecords>(json)!;
        Assert.Equal(1, back.MuseumCount);
        var painting = back.Museum[(int)ContestStat.Beauty]!;
        Assert.Equal(("Milotic", "Ann", 870, AppealRank.Master), (painting.Species, painting.Trainer, painting.Points, painting.Rank));
    }

    // ---------------------------------------------------------------- Slateport's chairman

    [Fact]
    public void TheChairmanWantsToSeeThePokemonOfAContestantAndGivesAScarfForACondition()
    {
        const string area = "slateport_city_pokemon_fan_club";
        var host = new HeadlessScriptHost();
        var lead = Mon();
        lead.Cool = 210;
        host.Party.Add(lead);
        // Before a contest: he only wishes to see the Pokémon of one who has entered
        Run("Chairman", area, host);
        Assert.True(host.Story.Has("FLAG_MET_SLATEPORT_FANCLUB_CHAIRMAN"));
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Red Scarf")!));
        Run("Chairman", area, host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("That, I would like to see"));

        // After one: the Red Scarf for a Cool condition of 200, and not twice
        host.Story.Set(HoennContestRules.EnteredFlag);
        Run("Chairman", area, host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Red Scarf")!));
        Assert.True(host.Story.Has("FLAG_RECEIVED_RED_SCARF"));
        Run("Chairman", area, host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Red Scarf")!));
        Assert.Contains(host.Transcript, l => l.Text.Contains("more Pokéblocks"));

        // Under 200 in a quality, none for it; 200 in Beauty brings the Blue Scarf
        lead.Cool = 199;
        lead.Beauty = 200;
        Run("Chairman", area, host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Blue Scarf")!));
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Pink Scarf")!));

        // The first of the qualities that fits goes first: Cool, Beauty, Cute, Smart, Tough
        var others = new HeadlessScriptHost();
        var all = Mon();
        all.Cool = all.Beauty = all.Cute = all.Smart = all.Tough = 255;
        others.Party.Add(all);
        others.Story.Set(HoennContestRules.EnteredFlag);
        string[] scarves = { "Red Scarf", "Blue Scarf", "Pink Scarf", "Green Scarf", "Yellow Scarf" };
        foreach (string scarf in scarves) Run("Chairman", area, others);
        foreach (string scarf in scarves) Assert.Equal(1, others.Bag.GetQuantity(ItemDatabase.Get(scarf)!));
        // With all five he has nothing more to give
        Run("Chairman", area, others);
        Assert.Equal(2, others.Story.Var("VAR_SLATEPORT_FAN_CLUB_STATE"));
        Assert.Contains(others.Transcript, l => l.Text.Contains("nothing else to give"));
    }

    [Fact]
    public void AScarfAddsTwentyToItsQualityOnTheStage()
    {
        var p = Mon();
        p.Cool = 100;
        p.Beauty = 100;
        p.HeldItem = ItemDatabase.Get("Red Scarf");
        var onStage = HoennContestRules.PlayerAppealer(p, "Ann");
        Assert.Equal(120, onStage.Stats[(int)ContestStat.Cool]);
        Assert.Equal(100, onStage.Stats[(int)ContestStat.Beauty]);
    }

    // ---------------------------------------------------------------- Mossdeep's black belt

    [Theory]
    [InlineData(Nature.Adamant, "Red Pokéblock")]
    [InlineData(Nature.Modest, "Blue Pokéblock")]
    [InlineData(Nature.Timid, "Pink Pokéblock")]
    [InlineData(Nature.Calm, "Green Pokéblock")]
    [InlineData(Nature.Bold, "Yellow Pokéblock")]
    public void TheBlackBeltNamesTheColourOfPokeblockTheLeadsNatureLikes(Nature nature, string color)
    {
        Assert.Equal(color, Pokeblocks.NameOf(Pokeblocks.FavouriteOf(nature)));
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", nature));
        Run("BlackBelt", "mossdeep_city_house_1", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains(color + "s"));
    }

    [Fact]
    public void ANatureWithNoTasteLikesNoColourAndTheBlackBeltSaysSo()
    {
        foreach (var nature in new[] { Nature.Hardy, Nature.Docile, Nature.Serious, Nature.Bashful, Nature.Quirky })
            Assert.Equal(PokeblockColor.None, Pokeblocks.FavouriteOf(nature));
        // Every other nature likes exactly one flavour, and the colour is that flavour's
        foreach (var nature in Enum.GetValues<Nature>().Except(new[] { Nature.Hardy, Nature.Docile, Nature.Serious, Nature.Bashful, Nature.Quirky }))
            Assert.NotEqual(PokeblockColor.None, Pokeblocks.FavouriteOf(nature));
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", Nature.Serious));
        Run("BlackBelt", "mossdeep_city_house_1", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("like or dislike any Pokéblocks"));
    }

    // ---------------------------------------------------------------- Feebas

    [Fact]
    public void FeebasFedDryPokeblocksFromTheBlenderGrowsBeautifulEnoughToEvolve()
    {
        // A Modest Feebas likes dry; the blender's Chesto Berry (dry 10, smoothness 25) at speed makes a dry block
        var chesto = BlenderBerry.Of(ItemDatabase.Get("Chesto Berry")!);
        var block = Pokeblocks.Blend(new[] { chesto }, 33300, new Random(1));
        Assert.True(block.Dry > 0);
        var feebas = Mon("Feebas", Nature.Modest);
        var context = new EvolutionContext();
        int fed = 0;
        while (feebas.Beauty < 170)
        {
            Assert.True(Pokeblocks.WouldEat(feebas));
            Assert.Null(Evolution.Find(feebas, EvolutionTrigger.LevelUp, context));
            Pokeblocks.Feed(block, feebas);
            fed++;
        }
        Assert.InRange(fed, 2, 12);
        Assert.Equal("Milotic", Evolution.Find(feebas, EvolutionTrigger.LevelUp, context)!.TargetSpecies);
    }

    // ---------------------------------------------------------------- the Safari Zone's counter

    [Fact]
    public void TheSafariCounterAsksForThePokeblockCaseBeforeAnythingElse()
    {
        var hoenn = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn);
        var map = hoenn.BuildMaps().Single(m => m.Name == "Route121_SafariZoneEntrance");
        var without = new HeadlessScriptHost { Map = map, PlayerTile = (8, 4) };
        without.Money = 3000;
        Run("Counter", "route_121_safari_zone_entrance", without);
        Assert.Equal(3000, without.Money);
        Assert.Contains(without.Transcript, l => l.Text.Contains("Pokéblock Case"));
        Assert.False(without.Safari.Active);

        var with = new HeadlessScriptHost { Map = map, PlayerTile = (8, 4) };
        with.Money = 3000;
        with.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!);
        Run("Counter", "route_121_safari_zone_entrance", with);
        Assert.True(with.Safari.Active);
        Assert.Equal(3000 - SafariGame.Fee, with.Money);
    }

    // ---------------------------------------------------------------- the data

    [Fact]
    public void TheMuseumsFramesAndTheBlackBeltsAndChairmansScriptsAreWiredToTheirPeopleAndSigns()
    {
        var hoenn = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn);
        var maps = hoenn.BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        var gallery = maps["LilycoveCity_LilycoveMuseum_2F"];
        string[] frames = { "CoolPainting", "BeautyPainting", "CutePainting", "SmartPainting", "ToughPainting" };
        foreach (string frame in frames) Assert.Contains(frame, gallery.TileScripts.Values.Concat(gallery.SignScripts.Values));
        Assert.Contains(gallery.Everyone, n => n.Script == "Curator");
        Assert.Contains(maps["MossdeepCity_House1"].Everyone, n => n.Script == "BlackBelt");
        Assert.Contains(maps["SlateportCity_PokemonFanClub"].Everyone, n => n.Script == "Chairman");
    }
}
