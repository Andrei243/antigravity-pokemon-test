using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// What part p of Johto left that needs nobody to see the game (plan 20, part q): Warden Baoba on Route 40, Blue at the
/// League's reception gate, Cameron and his photo album, the held-back Leaders' street meetings (Chuck in the Cliff Edge
/// Gate, Bugsy in the National Park, Lt. Surge on Route 39 and at Saffron's station), Eusine's second place on the cape,
/// Steven in Silph Co.'s lobby and Pewter's museum, and HeartGold's calls the story makes (Elm about the Egg, the bike
/// shop, Bill's boxes). The radio's Kanto station is <see cref="JohtoLeftoversTests"/>' neighbour below.
/// </summary>
public partial class JohtoStoryTests
{
    /// <summary>Whoever is placed stands where they can be spoken to: their tile isn't solid and a tile beside it is walked.</summary>
    private static void StandsInTheOpen(Game game, string key)
    {
        var npc = game.Map.NPCs.First(n => n.Key == key);
        Assert.False(game.Map.IsSolid(npc.GridX, npc.GridY), $"{key} stands on a solid tile");
        Assert.True(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => game.Map.IsWalkable(npc.GridX + d.Item1, npc.GridY + d.Item2)),
            $"{key} has no open tile beside them");
    }

    [Fact]
    public void BaobaWaitsOnRoute40UntilHisSafariZoneOpens()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 30));
        game.Arrive("Johto", 250, 272);
        Assert.NotNull(game.Present("route_40_baoba"));
        StandsInTheOpen(game, "route_40_baoba");
        var met = game.Talk("route_40_baoba");
        Assert.True(game.Story.Has("FLAG_JOHTO_MET_BAOBA"));
        Assert.Contains(met.Transcript, l => l.Speaker == "Baoba");
        // His call opens the zone (olivine.txt), and he is gone from the beach
        game.Story.SetVar("VAR_UNK_4057", 1);
        game.Arrive("Johto", 250, 272);
        Assert.Null(game.Present("route_40_baoba"));
    }

    [Fact]
    public void BlueWaitsAtTheReceptionGateFromHisGymsBattleUntilRedIsBeaten()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Route22_LeagueReceptionGate", 11, 20);
        Assert.Null(game.Present("reception_gate_blue"));
        game.Story.Set("JohtoHallOfFame");
        game.Story.Set("FLAG_JOHTO_DEFEATED_BLUE");
        foreach (string badge in new[] { "boulder", "cascade", "thunder", "rainbow", "soul", "marsh", "volcano", "earth" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        game.Arrive("Route22_LeagueReceptionGate", 11, 20);
        Assert.NotNull(game.Present("reception_gate_blue"));
        StandsInTheOpen(game, "reception_gate_blue");
        game.Talk("reception_gate_blue");
        Assert.True(game.Story.Has("FLAG_JOHTO_BLUE_TOLD_OF_RED"));
        game.Story.Set("FLAG_JOHTO_BEAT_RED");
        game.Arrive("Route22_LeagueReceptionGate", 11, 20);
        Assert.Null(game.Present("reception_gate_blue"));
    }

    /// <summary>The places Cameron stands (HeartGold's FLAG_HIDE_CAMERON objects of the places built).</summary>
    private static readonly string[] CameronsPlaces =
    {
        "pallet", "viridian", "pewter", "pewter_museum_of_science", "cerulean", "celadon", "celadon_department_store_2f",
        "fuchsia", "johto_route_1", "johto_route_14", "route_21", "johto_seafoam_islands_b1f", "route_48", "safari_zone_gate"
    };

    [Fact]
    public void CamerasFatherSendsHimOutAndEveryPhotoHeTakesGoesInTheAlbum()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        // Not out before his father has been met
        game.Arrive("Johto", 1031, 372);
        Assert.Null(game.Present("t01_gsmiddleman1"));
        game.Arrive("Cianwood_PhotoStudio", 4, 6);
        game.Talk("t24r0601_fishing_2");
        Assert.True(game.Story.Has("FLAG_JOHTO_MET_CAMERONS_FATHER"));
        Assert.DoesNotContain(game.Talk("t24r0601_fishing_2").Log, l => l.Contains("PhotoAlbum"));

        // At Pallet Town, a photograph of the player and the team's lead
        game.Arrive("Johto", 1031, 372);
        Assert.NotNull(game.Present("t01_gsmiddleman1"));
        game.Answers.Enqueue(0);
        game.Talk("t01_gsmiddleman1");
        var photo = Assert.Single(game.Photos.Photos);
        Assert.Equal("pallet", photo.Place);
        Assert.Equal("Typhlosion", photo.Pokemon);
        Assert.Equal(GameClock.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), photo.Day);
        // A no takes none
        game.Answers.Enqueue(1);
        game.Talk("t01_gsmiddleman1");
        Assert.Equal(1, game.Photos.Count);

        // The studio opens the album; a full album takes no more
        game.Arrive("Cianwood_PhotoStudio", 4, 6);
        game.Answers.Enqueue(0);
        Assert.Contains(game.Talk("t24r0601_fishing_2").Log, l => l.Contains("PhotoAlbum"));
        while (!game.Photos.IsFull) game.Photos.Add(new Photo { Place = "pallet" });
        game.Arrive("Johto", 1031, 372);
        game.Answers.Enqueue(0);
        game.Talk("t01_gsmiddleman1");
        Assert.Equal(PhotoAlbum.Capacity, game.Photos.Count);

        // Every Cameron of an overlay waits for his father's word and runs his script
        foreach (string place in CameronsPlaces)
        {
            var overlay = Johto.Value.Overlay(place)!;
            var (id, person) = Assert.Single(overlay.People!, p => p.Value.Script == "Cameron");
            Assert.Equal("FLAG_JOHTO_MET_CAMERONS_FATHER", person.ShownBy);
            Assert.DoesNotContain(id, overlay.HeldBack ?? new());
        }
    }

    [Fact]
    public void TheAlbumScreenTurnsItsPagesAndThrowsAPhotographAwayOnlyWhenAskedTwice()
    {
        var album = new PhotoAlbum();
        album.Add(new Photo { Place = "pallet", Pokemon = "Pikachu", Day = "2026-11-02" });
        album.Add(new Photo { Place = "cerulean", Day = "2026-11-03" });
        var screen = new PhotoAlbumScreen();
        screen.Open(album);
        Assert.Equal("pallet", screen.Shown!.Place);
        screen.Turn(1);
        Assert.Equal("cerulean", screen.Shown!.Place);
        screen.Turn(1);
        Assert.Equal(0, screen.Page);
        screen.Confirm();
        Assert.True(screen.AskingToRemove);
        screen.Turn(1);
        Assert.Equal(0, screen.Page);
        screen.Cancel();
        Assert.False(screen.AskingToRemove);
        Assert.Equal(2, album.Count);
        screen.Confirm();
        screen.Confirm();
        Assert.Equal("cerulean", Assert.Single(album.Photos).Place);
        screen.Cancel();
        Assert.False(screen.IsActive);

        // The save keeps it whole, and an empty album is no field of the save
        var saved = album.ToSave();
        var back = new PhotoAlbum();
        back.Load(saved);
        Assert.Equal("cerulean", back.Photos[0].Place);
        Assert.Null(new PhotoAlbum().ToSave());
    }

    [Theory]
    [InlineData("CliffEdgeGate", 35, 23, "d48r0101_gsleader5", "chuck", DayOfWeek.Saturday)]
    [InlineData("NationalPark", 58, 41, "d22r0101_gsleader2", "bugsy", DayOfWeek.Sunday)]
    [InlineData("Johto", 263, 190, "r39_gsleader9", "lt_surge", DayOfWeek.Monday)]
    [InlineData("Saffron_MagnetTrainStation", 19, 12, "t11r0601_gsleader9", "lt_surge", DayOfWeek.Wednesday)]
    public void HeldBackLeadersMeetOnTheirDaysOnceTheirNumbersAreRegistered(string map, int x, int y, string key, string contact, DayOfWeek day)
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Weekday = day;
        game.Arrive(map, x, y);
        Assert.Null(game.Present(key));
        Pokegear.Register(game.Story, contact);
        game.Weekday = day == DayOfWeek.Saturday ? DayOfWeek.Friday : DayOfWeek.Saturday;
        game.Arrive(map, x, y);
        Assert.Null(game.Present(key));
        game.Weekday = day;
        game.Arrive(map, x, y);
        Assert.NotNull(game.Present(key));
        StandsInTheOpen(game, key);
        Assert.NotEmpty(game.Talk(key).Transcript);
    }

    [Fact]
    public void ChuckIsAtTheCliffEdgeGateOnSaturdaysAndNotByTheWaterfall()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Story.Set("JohtoHallOfFame");
        foreach (string badge in new[] { "boulder", "cascade", "thunder", "rainbow", "soul", "marsh", "volcano", "earth" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        Pokegear.Register(game.Story, "chuck");
        game.Weekday = DayOfWeek.Saturday;
        game.Arrive("Johto", 100, 390);
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_47_CHUCK"));
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_47_CRASHER_WAKE"));
        game.Weekday = DayOfWeek.Friday;
        game.Arrive("Johto", 100, 390);
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_47_CHUCK"));
    }

    [Fact]
    public void EusineWaitsAtHisSecondPlaceOnTheCapeOnceSuicuneIsFoughtAndPartsWhenSpokenTo()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Johto", 1410, 44);
        Assert.Null(game.Present("r25_minaki_2"));
        game.Story.Set("FLAG_JOHTO_FOUGHT_SUICUNE");
        game.Arrive("Johto", 1410, 44);
        Assert.NotNull(game.Present("r25_minaki_2"));
        Assert.Null(game.Present("r25_minaki"));
        game.Talk("r25_minaki_2");
        Assert.True(game.Story.Has("FLAG_JOHTO_EUSINE_PARTED"));
        game.Arrive("Johto", 1410, 44);
        Assert.Null(game.Present("r25_minaki_2"));
    }

    [Theory]
    [InlineData(0, "Treecko")]
    [InlineData(1, "Torchic")]
    [InlineData(2, "Mudkip")]
    public void StevenGivesASixteenBadgeChampionOneOfHoennsFirstThreeAndGoesOnToPewtersMuseum(int choice, string species)
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Saffron_SilphCo", 8, 12);
        Assert.Null(game.Present("t11r0701_daigo"));
        game.Story.Set("JohtoHallOfFame");
        foreach (string badge in new[] { "boulder", "cascade", "thunder", "rainbow", "soul", "marsh", "volcano", "earth" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        game.Arrive("Saffron_SilphCo", 8, 12);
        Assert.NotNull(game.Present("t11r0701_daigo"));
        StandsInTheOpen(game, "t11r0701_daigo");
        game.Answers.Enqueue(choice);
        game.Talk("t11r0701_daigo");
        Assert.Contains(game.Party.Members, p => p.Species.Name == species && p.Level == 5);
        Assert.True(game.Story.Has("FLAG_JOHTO_GOT_HOENN_STARTER"));
        game.Arrive("Saffron_SilphCo", 8, 12);
        Assert.Null(game.Present("t11r0701_daigo"));
        game.Arrive("Pewter_Museum", 22, 9);
        Assert.NotNull(game.Present("t03r0101_daigo"));
        Assert.NotEmpty(game.Talk("t03r0101_daigo").Transcript);
    }

    [Fact]
    public void TheStoryRingsElmAboutTheEggTheBikeShopADayLaterAndBillForEachFullBox()
    {
        var story = new StoryState();
        var party = new Party();
        var boxes = new PcBoxes();
        party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 30));
        Assert.Null(StoryCalls.Ring(story, party, boxes));

        // Elm, once the Egg from his aide has hatched
        Pokegear.Register(story, "prof_elm");
        story.Set("FLAG_GOT_TOGEPI_EGG");
        Assert.Null(StoryCalls.Due(story, party, boxes));
        party.Add(new Pokemon(PokemonDatabase.Get("Togepi")!, 5));
        Assert.Equal(StoryCalls.ElmEgg, StoryCalls.Ring(story, party, boxes));
        Assert.Null(StoryCalls.Ring(story, party, boxes));

        // The bike shop, not on the day it lent the Bicycle, once the day after
        Pokegear.Register(story, "bike_shop");
        story.Set("FLAG_UNK_089");
        story.Set(StoryCalls.BikeLentTodayFlag);
        Assert.Null(StoryCalls.Due(story, party, boxes));
        story.Unset(StoryCalls.BikeLentTodayFlag);
        Assert.Equal(StoryCalls.BikeShop, StoryCalls.Ring(story, party, boxes));
        Assert.Null(StoryCalls.Ring(story, party, boxes));

        // Bill, each time another box fills
        Pokegear.Register(story, "bill");
        for (int i = 0; i < PcBoxes.BoxSize; i++) boxes.Store(new Pokemon(PokemonDatabase.Get("Rattata")!, 3));
        Assert.Equal(StoryCalls.BillBoxes, StoryCalls.Ring(story, party, boxes));
        Assert.Equal(1, story.Var(StoryCalls.BillBoxesVar));
        Assert.Null(StoryCalls.Ring(story, party, boxes));
        for (int i = 0; i < PcBoxes.BoxSize; i++) boxes.Store(new Pokemon(PokemonDatabase.Get("Rattata")!, 3));
        Assert.Equal(StoryCalls.BillBoxes, StoryCalls.Ring(story, party, boxes));

        // Each call is a script of phone.txt, and the shop and Bill give their numbers
        foreach (string script in new[] { StoryCalls.ElmEgg, StoryCalls.BikeShop, StoryCalls.BillBoxes, "BikeShop", "Bill" })
            Assert.NotNull(Scripts.Find(script, GameEngine.PhoneScripts));
        foreach (string id in new[] { "bike_shop", "bill" })
            Assert.Contains(Scripts.All, s => s.Code.Any(i => i.Op == Op.AddContact && i.Name == id));
    }

    [Fact]
    public void HeartGoldsKantoTakesKantosStationsWithTheExpnCard()
    {
        // The importer writes HeartGold's regionNo: Kanto's areas say so, Johto's don't
        var game = new Game();
        game.Arrive("Johto", 1325, 110);
        Assert.True(game.Map.Areas.First(a => a.Key == "cerulean").InKanto);
        Assert.False(game.Map.Areas.First(a => a.Key == "goldenrod").InKanto);

        var story = new StoryState();
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("cerulean", false, story, kanto: true, expnCard: false));
        Assert.Equal(RadioSignal.Kanto, RadioDial.SignalAt("cerulean", false, story, kanto: true, expnCard: true));
        Assert.Equal(RadioSignal.NoSignal, RadioDial.SignalAt("johto_rock_tunnel_1f", true, story, kanto: true, expnCard: true));
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("goldenrod", false, story, kanto: false, expnCard: true));

        // Kanto's dial: the music, Oak's talk, That Town, These People and the Poké Flute, whose song is the flute's
        var dial = new RadioDial { Signal = RadioSignal.Kanto };
        var stations = RadioDial.SpotsOf(RadioSignal.Kanto).Select(s => RadioDial.StationOf(s.Slot, 10)).ToList();
        Assert.Equal(new[] { RadioStation.PokemonMusic, RadioStation.PokemonTalk, RadioStation.ThatTownThesePeople, RadioStation.PokeFlute }, stations);
        Assert.Equal(RadioSong.PokeFlute, RadioDial.SongOf(RadioStation.PokeFlute, DayOfWeek.Monday, false));
        Assert.Equal("kanto/poke_flute", RadioDial.SongFile(RadioSong.PokeFlute));
        var flute = RadioDial.SpotsOf(RadioSignal.Kanto)[3];
        dial.Place(flute.X, flute.Y);
        Assert.Equal(4, dial.Tuned()!.Value.Spot.Slot);
    }}
