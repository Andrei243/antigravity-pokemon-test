using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto's leftovers (plan 20, part p): Buena's Password and the Blue Card, the phone's gifts, the Gym Leaders' numbers,
/// rematches at the Fighting Dojo and street meetings, the Kimono Girl under the city, Mahogany's Rage Candy seller,
/// Route 47's waterfall, Eusine's trail, the dome's owner, Celadon's Pokémon prizes and the Embedded Tower.
/// </summary>
public partial class JohtoStoryTests
{
    private string KeyOf(Game game, string name) => game.Map.NPCs.First(n => n.Name == name).Key!;

    [Fact]
    public void BuenasPasswordIsOneWordOfThreeANightAndAllItsWordsAreTheDatas()
    {
        for (int d = 0; d < 400; d++)
        {
            var day = new DateTime(2026, 1, 1).AddDays(d);
            var three = BuenasPassword.Choices(day);
            Assert.Equal(3, three.Distinct().Count());
            Assert.Equal(BuenasPassword.WordOf(day), three[BuenasPassword.RightPlace(day)]);
            Assert.Equal(three, BuenasPassword.Choices(day));
        }
        for (int i = 0; i < BuenasPassword.Count; i++)
            Assert.True(BuenasPassword.IsMove(i) ? MoveDatabase.Get(BuenasPassword.NameOf(i)) != null : PokemonDatabase.Get(BuenasPassword.NameOf(i)) != null,
                BuenasPassword.NameOf(i));
        Assert.True(BuenasPassword.OnTheAir(TimeOfDay.Night));
        Assert.False(BuenasPassword.OnTheAir(TimeOfDay.Day));
    }

    [Fact]
    public void BuenaGivesTheBlueCardTakesTonightsPasswordOnceANightAndTheCardsPointsBuyPrizes()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        game.Arrive("Goldenrod_RadioTower_2F", 18, 14);
        // By day: the card, and no show
        game.Talk("d23r0102_gswoman2_2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Blue Card")!));
        Assert.Empty(game.Talk("d23r0102_gswoman2_2").Asked);

        // On the air, the right word is a point, once a night
        game.Time = TimeOfDay.Night;
        int right = BuenasPassword.RightPlace(GameClock.Today);
        game.Answers.Enqueue(right);
        game.Talk("d23r0102_gswoman2_2");
        Assert.Equal(1, game.Story.Var(BuenasPassword.PointsVar));
        Assert.True(game.Story.Has(BuenasPassword.AnsweredFlag));
        Assert.Empty(game.Talk("d23r0102_gswoman2_2").Asked);

        // A wrong word is no point, and spends the night all the same
        game.Story.Unset(BuenasPassword.AnsweredFlag);
        game.Answers.Enqueue((right + 1) % 3);
        game.Talk("d23r0102_gswoman2_2");
        Assert.Equal(1, game.Story.Var(BuenasPassword.PointsVar));
        Assert.True(game.Story.Has(BuenasPassword.AnsweredFlag));

        // The woman beside her trades points for prizes, and refuses too few
        game.Answers.Enqueue(2);
        game.Talk("d23r0102_gswoman1");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        game.Story.SetVar(BuenasPassword.PointsVar, 5);
        game.Answers.Enqueue(2);
        game.Talk("d23r0102_gswoman1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        Assert.Equal(2, game.Story.Var(BuenasPassword.PointsVar));
    }

    [Fact]
    public void ACallerWithAGiftRingsAboutItAndHandsItOverOnTheirRoute()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        game.Story.Defeat("johto_youngster_joey");
        Pokegear.Register(game.Story, "joey");
        // A call to talk (the game sets VAR_PHONE_CALL 1) says he has something: one in three, the host's die at 0
        game.Story.SetVar(Pokegear.CallVar, Pokegear.CallTalk);
        game.Play(Scripts.Find("Joey", GameEngine.PhoneScripts)!);
        Assert.True(game.Story.Has("FLAG_JOHTO_PHONE_GIFT_JOEY"));
        // On Route 30 he hands it over, and the gift may come again
        game.Arrive("Johto", 553, 333);
        game.Talk(KeyOf(game, "Joey"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HP Up")!));
        Assert.False(game.Story.Has("FLAG_JOHTO_PHONE_GIFT_JOEY"));

        // Every caller of the book with a gift registers in a script of their place, and has a call of their own
        foreach (var contact in PhoneBook.All.Where(c => c.Gift != null))
        {
            Assert.NotNull(Scripts.Find(contact.Script, GameEngine.PhoneScripts));
            Assert.Contains(Scripts.All, s => s.Code.Any(i => i.Op == Op.AddContact && i.Name == contact.Id));
        }
    }

    [Fact]
    public void AGymLeaderGivesTheirNumberToJohtosChampionAndWaitsAtTheDojoForTheRematchTheyAskFor()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        // Every Leader of the book gives their number in a script, and asks with a script of their own
        foreach (var leader in PhoneBook.All.Where(c => c.Type == 12))
        {
            Assert.Contains(Scripts.All, s => s.Code.Any(i => i.Op == Op.AddContact && i.Name == leader.Id));
            Assert.NotEqual(Pokegear.NobodysCall, Scripts.Find(leader.Script, GameEngine.PhoneScripts)!.Name);
        }

        // Whitney's Gym, for Johto's Champion
        game.Story.Set("JohtoHallOfFame");
        game.Story.GiveBadge(RegionDatabase.Johto, "plain");
        game.Story.Defeat("johto_leader_whitney");
        game.Arrive("Goldenrod_Gym", 13, 5);
        game.Answers.Enqueue(0);
        game.Talk("t25gym0101_gsleader3");
        Assert.True(game.Story.Has(Pokegear.ContactFlag("whitney")));

        // Her day and hour: a call asks, and she waits at the dojo
        var whitney = PhoneBook.Get("whitney")!;
        var saturday = new DateTime(2026, 11, 7);
        Assert.Equal(Pokegear.CallRematch, PhoneRematch.Call(whitney, game.Story, saturday, TimeOfDay.Day, incoming: true));
        game.Arrive("Saffron_FightingDojo", 7, 12);
        Assert.NotNull(game.Present("t11r0101_gsleader3"));
        Assert.Null(game.Present("t11r0101_gsleader5"));
        // Fought as her second team, and gone
        var host = game.Talk("t11r0101_gsleader3");
        Assert.Contains(host.Log, l => l.Contains("battle") && l.Contains("johto_leader_whitney_2"));
        Assert.False(game.Story.Has(PhoneRematch.WaitingFlag("whitney")));
        game.Arrive("Saffron_FightingDojo", 7, 12);
        Assert.Null(game.Present("t11r0101_gsleader3"));
    }

    [Fact]
    public void LeadersMeetInTheStreetsOnTheirDaysOnceTheirNumbersAreRegistered()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Weekday = DayOfWeek.Wednesday;
        game.Arrive("Diglett_Cave", 36, 5);
        Assert.Null(game.Present("d01r0101_gsleader14"));
        Pokegear.Register(game.Story, "brock");
        game.Weekday = DayOfWeek.Tuesday;
        game.Arrive("Diglett_Cave", 36, 5);
        Assert.Null(game.Present("d01r0101_gsleader14"));
        game.Weekday = DayOfWeek.Wednesday;
        game.Arrive("Diglett_Cave", 36, 5);
        Assert.NotNull(game.Present("d01r0101_gsleader14"));
        Assert.NotEmpty(game.Talk("d01r0101_gsleader14").Transcript);
    }

    [Fact]
    public void TheBasementKeySendsAKimonoGirlUnderTheCityWhoGoesHomeOnceSpokenTo()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        game.Arrive("Goldenrod_Underground_B1F", 19, 32);
        Assert.Null(game.Present("d37r0102_dancer"));
        game.Story.SetVar("VAR_UNK_40E7", 2);
        game.Story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 5);
        game.Arrive("Goldenrod_Underground_B1F", 19, 32);
        Assert.NotNull(game.Present("d37r0102_dancer"));
        game.Talk("d37r0102_dancer");
        Assert.Equal(3, game.Story.Var("VAR_UNK_40E7"));
        game.Arrive("Goldenrod_Underground_B1F", 19, 32);
        Assert.Null(game.Present("d37r0102_dancer"));
    }

    [Fact]
    public void TheEmbeddedTowerOpensRoomByRoomWithMrPokemonsOrbs()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 70));
        // Nothing for a player without the sixteen Badges
        game.Arrive("Route30_MrPokemonsHouse", 5, 5);
        string mr = KeyOf(game, "Mr. Pokémon");
        game.Talk(mr);
        Assert.False(game.Story.Has("FLAG_JOHTO_GOT_ORBS"));
        game.Story.Set("JohtoHallOfFame");
        foreach (string badge in new[] { "boulder", "cascade", "thunder", "rainbow", "soul", "marsh", "volcano", "earth" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        game.Talk(mr);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Red Orb")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Blue Orb")!));

        // Route 47's hole leads to Groudon's room, then Kyogre's, then Rayquaza's
        game.Arrive("Johto", 87, 390);
        Assert.Equal("EmbeddedTower_GroudonRoom", game.Map.GetWarpAt(87, 389)!.TargetMap);
        game.Arrive("EmbeddedTower_GroudonRoom", 16, 28);
        Assert.NotNull(game.Present("d52r0101_tsure_poke_static_groudon"));
        game.Talk("d52r0101_tsure_poke_static_groudon");
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_GROUDON"));
        game.Arrive("Johto", 87, 390);
        Assert.Equal("EmbeddedTower_KyogreRoom", game.Map.GetWarpAt(87, 389)!.TargetMap);
        game.Arrive("EmbeddedTower_KyogreRoom", 16, 28);
        game.Talk("d52r0102_tsure_poke_static_kyogre");
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_KYOGRE"));
        game.Arrive("Johto", 87, 390);
        Assert.Equal("EmbeddedTower_RayquazaRoom", game.Map.GetWarpAt(87, 389)!.TargetMap);
        // Rayquaza waits for the Jade Orb, which Mr. Pokémon gives now
        game.Arrive("EmbeddedTower_RayquazaRoom", 16, 28);
        Assert.Null(game.Present("d52r0103_tsure_poke_static_rayquaza"));
        game.Arrive("Route30_MrPokemonsHouse", 5, 5);
        game.Talk(mr);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Jade Orb")!));
        game.Arrive("EmbeddedTower_RayquazaRoom", 16, 28);
        Assert.NotNull(game.Present("d52r0103_tsure_poke_static_rayquaza"));
        game.Talk("d52r0103_tsure_poke_static_rayquaza");
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_RAYQUAZA"));
    }

    [Fact]
    public void ChuckAndCrasherWakeTrainOnRoute47OnceTheKantoBadgesAreWon()
    {
        var game = new Game();
        game.Arrive("Johto", 85, 375);
        Assert.Null(game.Present("r47_gsleader5"));
        game.Story.Set("JohtoHallOfFame");
        foreach (string badge in new[] { "boulder", "cascade", "thunder", "rainbow", "soul", "marsh", "volcano", "earth" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        game.Arrive("Johto", 85, 375);
        Assert.NotNull(game.Present("r47_gsleader5"));
        Assert.NotNull(game.Present("r47_leader3"));
    }

    [Fact]
    public void TheDomesOwnerGreetsAFirstVisitOnce()
    {
        var game = new Game();
        game.Arrive("PokeathlonDome", 42, 45);
        Assert.True(game.Story.Has("FLAG_UNK_225"));
        Assert.NotEmpty(game.Last.Transcript);
        game.Arrive("PokeathlonDome", 42, 45);
        Assert.Empty(game.Last.Transcript);
    }

    [Fact]
    public void MahoganysRageCandySellerSellsABarAndLeavesTheWayEastOpen()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        game.Arrive("Johto", 540, 176);
        var seller = game.Present("t28_gsmiddleman1");
        Assert.NotNull(seller);
        // The street's two other rows east of him are open
        Assert.True(game.Map.IsWalkable(540, 176) && game.Map.IsWalkable(540, 177));
        game.Answers.Enqueue(0);
        game.Talk("t28_gsmiddleman1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Rage Candy Bar")!));
    }

    [Fact]
    public void TheCaptainsGranddaughterIsFoundTwiceAndBroughtBackForAMetalCoat()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        game.Arrive("SSAqua_CaptainRoom", 9, 6);
        Assert.Null(game.Present("p01r0302_gsbabygirl1"));
        game.Talk("p01r0302_captain");
        game.Arrive("SSAqua_1F_NorthwestRooms", 8, 9);
        Assert.NotNull(game.Present("p01r0306_gsbabygirl1_3"));
        game.Talk("p01r0306_gsbabygirl1_3");
        Assert.Equal(1, game.Story.Var("VAR_JOHTO_SS_AQUA_GIRL"));
        game.Arrive("SSAqua_B1F", 2, 13);
        Assert.NotNull(game.Present("p01r0307_gsbabygirl1"));
        game.Talk("p01r0307_gsbabygirl1");
        game.Arrive("SSAqua_CaptainRoom", 9, 6);
        Assert.NotNull(game.Present("p01r0302_gsbabygirl1"));
        game.Talk("p01r0302_captain");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Metal Coat")!));
        Assert.Equal(3, game.Story.Var("VAR_JOHTO_SS_AQUA_GIRL"));
    }
}
