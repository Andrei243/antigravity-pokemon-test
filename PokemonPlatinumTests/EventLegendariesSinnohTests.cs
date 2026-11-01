using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 29 · LS11a (docs/plans/decisions/LS11-proposals-event-legendaries.md, the user's answers of 2026-11-01): the
/// four event Pokémon of Platinum's Sinnoh each met by a way of our own after the Hall of Fame, and Phione bred from the
/// fourth. Darkrai by the Member Card, the Harbor Inn's night and Newmoon Island's wood; Shaymin by Oak's Letter, the
/// thanks at Route 224's white rock, the Seabreak Path and Flower Paradise, with the Gracidea after; Arceus by the Azure
/// Flute, the stair of light and the Hall of Origin; the Manaphy Egg from a Pokémon Ranger on Route 230.
/// </summary>
public class EventLegendariesSinnohTests
{
    private const string MemberCard = "FLAG_RECEIVED_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_MEMBER_CARD";
    private const string Woken = "FLAG_WOKE_UP_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY";

    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    /// <summary>A Champion with the National Pokédex, home again, as the post-game's tests have them.</summary>
    private static OpeningTests.Game Champion(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 58));
        game.Story.SetBadges("Sinnoh", 0xFF);
        game.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 6);
        game.Story.Set(StoryState.PokedexFlag);
        game.Facing = Direction.Up;
        game.Arrive("PokemonLeagueHallOfFame", 8, 15);
        Assert.True(game.Story.Has("FLAG_GAME_COMPLETED"));
        game.NationalDex = true;
        return game;
    }

    private static string Words(HeadlessScriptHost host) => string.Join(" ", host.Transcript.Select(l => l.Text));

    /// <summary>Speaks to someone from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing, string? place = null)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        Assert.False(game.Map.IsSolid(game.Tile.X, game.Tile.Y), $"nobody can stand at {game.Tile} to speak to {key}");
        return game.Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name)!, npc);
    }

    private static HeadlessScriptHost StepOnto(OpeningTests.Game game, int x, int y)
    {
        game.Tile = (x, y);
        var trigger = FieldScripts.TriggerAt(game.Map, x, y, game.Story) ?? throw new InvalidOperationException($"no trigger fires at {x}, {y}");
        return game.Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name)!);
    }

    // ------------------------------------------------------------------ Darkrai

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Caught)]
    [InlineData(BattleOutcome.Fled)]
    public void DarkraiIsMetByTheHarborInnsDream(BattleOutcome outcome)
    {
        var game = Champion(outcome);
        // The inn is shut until the boy gives his father's card, and the boy gives it only once he is well (his
        // nightmare, CanalaveCitySailorEldritchHouse.txt, played through: StarkMountainAndTheLegendaryPokemonTests)
        Assert.True(game.Maps["Sinnoh"].IsSolid(58, 713));
        Assert.Contains(Scripts.Find("LittleBoy", "CanalaveCitySailorEldritchHouse")!.Code,
            i => i.Op == Op.If && i.Condition!.Name == Woken);
        game.Story.Set(Woken);
        game.Arrive("CanalaveCitySailorEldritchHouse", 4, 8);
        TalkFrom(game, "little_boy", Direction.Up);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Member Card")!));
        Assert.True(game.Story.Has(MemberCard));
        TalkFrom(game, "little_boy", Direction.Up);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Member Card")!));

        // Its door opens, onto the inn's mat, where the innkeeper stands
        game.Arrive("Sinnoh", 58, 714);
        Assert.False(game.Map.IsSolid(58, 713));
        var door = game.Map.GetWarpAt(58, 713)!;
        Assert.Equal(("CanalaveHarborInn", 4, 8), (door.TargetMap, door.TargetX, door.TargetY));
        game.Arrive("CanalaveHarborInn", 4, 8);
        // A night there is the dream that sets the player down on Newmoon Island, its sailor away
        game.Answers.Enqueue(0);
        var night = TalkFrom(game, "innkeeper", Direction.Up);
        Assert.Contains("warp Sinnoh 152 277", night.Log);
        Assert.Equal("newmoon_island", game.Map.AreaAt(152, 277)!.Key);
        Assert.Null(game.Present("sailor", "newmoon_island"));

        // Into the wood: Darkrai stands in the hollow, met after its beat at Platinum's level
        game.Arrive("NewmoonIslandForest", 16, 20);
        var darkrai = game.Present("darkrai")!;
        Assert.Equal((16, 13, "Darkrai"), (darkrai.GridX, darkrai.GridY, darkrai.Species));
        var battle = TalkFrom(game, "darkrai", Direction.Up);
        Assert.Contains("cry Darkrai", battle.Log);
        Assert.Contains($"wildbattle Darkrai 50 {outcome}", battle.Log);
        Assert.Equal(outcome != BattleOutcome.Fled, game.Story.Has("FLAG_NEWMOON_ISLAND_DARKRAI_GONE"));
        game.Arrive("NewmoonIslandForest", 16, 20);
        Assert.Equal(outcome == BattleOutcome.Fled, game.Present("darkrai") != null);

        // Out of the wood the island's sailor is back on the pier to sail home, and Canalave's sails there from now on
        game.Arrive("Sinnoh", 137, 269);
        Assert.NotNull(game.Present("sailor", "newmoon_island"));
        Assert.Contains(Scripts.Find("Ferry", "canalave_city")!.Everything(), i => i.Op == Op.Warp && (i.X, i.Y) == (152, 277));
    }

    [Fact]
    public void WithoutTheCardTheInnHasNoRoom()
    {
        var game = Champion();
        game.Arrive("CanalaveHarborInn", 4, 8);
        var turned = TalkFrom(game, "innkeeper", Direction.Up);
        Assert.Contains("members", Words(turned));
        Assert.DoesNotContain(turned.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------ Shaymin

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Caught)]
    public void ShayminIsMetByOaksLetterAndTheThanksAtTheWhiteRock(BattleOutcome outcome)
    {
        var game = Champion(outcome);
        game.Story.Unset("FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK");
        game.Story.Set("FLAG_RECEIVED_ETERNA_CITY_SOUTH_HOUSE_UPGRADE");
        // Before the letter the rock is a rock
        game.Arrive("Sinnoh", 905, 495);
        Assert.Null(game.Present("prof_oak", "route_224"));
        Assert.Contains("unmarked", Words(game.Play(Scripts.Find("WhiteRock", "route_224")!)));

        // Oak gives the letter in Eterna City, and sets off for the rock
        game.Arrive("EternaCitySouthHouse", 4, 8);
        game.Talk("prof_oak");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Oak's Letter")!));
        Assert.Equal(1, game.Story.Var("VAR_SHAYMIN_EVENT_STATE"));

        // Route 224: Marley walks up, the professor waits at the rock
        game.Arrive("Sinnoh", 905, 495);
        Assert.NotNull(game.Present("prof_oak", "route_224"));
        Assert.Contains(StepOnto(game, 903, 497).Transcript, l => l.Speaker == "Marley");
        Assert.Equal(2, game.Story.Var("VAR_ROUTE_224_STATE"));

        // The thanks written: the Seabreak Path opens, Shaymin shows itself and runs off up it
        game.Tile = (908, 492);
        // (the engine lays the path's chunks as the variable is set behind the scene's fade, before Shaymin runs up it:
        // GameEngine.RefreshPresence; the headless host lays none, so the swap is laid here first)
        var sinnoh = game.Maps["Sinnoh"];
        sinnoh.ApplySwappedPlaces(name => name == "VAR_HIDDEN_LOCATION_SEABREAK_PATH" ? 4136 : 0);
        Assert.All(Enumerable.Range(476, 15), y => Assert.True(sinnoh.IsWalkable(911, y), $"911, {y}"));
        var rock = game.Play(Scripts.Find("WhiteRock", "route_224")!);
        Assert.True(game.Story.Has("FLAG_WROTE_ON_ROUTE_224_TABLET"));
        Assert.Equal(4136, game.Story.Var("VAR_HIDDEN_LOCATION_SEABREAK_PATH"));
        Assert.Contains("cry Shaymin", rock.Log);
        Assert.Equal(2, game.Story.Var("VAR_SHAYMIN_EVENT_STATE"));

        // Flower Paradise: Shaymin waits among the flowers, met at Platinum's level, and is gone once caught or beaten
        game.Arrive("Sinnoh", 911, 206);
        var shaymin = game.Present("shaymin", "flower_paradise")!;
        Assert.Equal((911, 203), (shaymin.GridX, shaymin.GridY));
        var battle = TalkFrom(game, "shaymin", Direction.Up, "flower_paradise");
        Assert.Contains($"wildbattle Shaymin 30 {outcome}", battle.Log);
        game.Arrive("Sinnoh", 911, 206);
        Assert.Null(game.Present("shaymin", "flower_paradise"));
        // and the professor and Marley have gone from the rock
        game.Arrive("Sinnoh", 905, 495);
        Assert.Null(game.Present("prof_oak", "route_224"));

        // Floaroma's flower shop gives the Gracidea to whoever brings a Shaymin
        if (outcome != BattleOutcome.Caught) game.Party.Add(new Pokemon(PokemonDatabase.Get("Shaymin")!, 30));
        game.Play(Scripts.Find("Sprayduck", "FlowerShop")!);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Gracidea")!));
        game.Play(Scripts.Find("Sprayduck", "FlowerShop")!);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Gracidea")!));
    }

    // ------------------------------------------------------------------ Arceus

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Caught)]
    public void ArceusIsMetUpTheStairOfLight(BattleOutcome outcome)
    {
        var game = Champion(outcome);
        game.Arrive("CelesticTownNorthHouse", 4, 8);
        var elder = game.Map.Everyone.First(n => n.Key == "elder");
        // Not until all three it made are caught
        game.Story.Set("FLAG_CAUGHT_DIALGA");
        game.Story.Set("FLAG_CAUGHT_PALKIA");
        game.Play(Scripts.Find("Elder", "CelesticTownNorthHouse")!, elder);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Azure Flute")!));
        game.Story.Set("FLAG_CAUGHT_GIRATINA");
        game.Play(Scripts.Find("Elder", "CelesticTownNorthHouse")!, elder);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Azure Flute")!));
        Assert.Equal(1, game.Story.Var("VAR_HALL_OF_ORIGIN_STATE"));

        // Each summit's top step asks for the flute; played, its stair leads to the Hall of Origin
        foreach (string summit in new[] { "SpearPillar", "SpearPillarDialga", "SpearPillarPalkia" })
            Assert.Equal("AzureFlute", FieldScripts.TriggerAt(game.Maps[summit], 31, 52, game.Story)!.Script);
        game.Arrive("SpearPillar", 31, 52);
        game.Answers.Enqueue(0);
        var flute = StepOnto(game, 31, 52);
        Assert.Contains("warp HallOfOrigin 31 52", flute.Log);
        Assert.Equal("HallOfOrigin", game.Map.Name);
        var arceus = game.Present("arceus")!;
        Assert.Equal((31, 27, "Arceus"), (arceus.GridX, arceus.GridY, arceus.Species));

        // The wide step across the hall is its meeting, at Platinum's level
        var battle = StepOnto(game, 31, 35);
        Assert.Contains("cry Arceus", battle.Log);
        Assert.Contains($"wildbattle Arceus 80 {outcome}", battle.Log);
        Assert.Equal(2, game.Story.Var("VAR_HALL_OF_ORIGIN_STATE"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 31, 35, game.Story));

        // The next Hall of Fame brings back one that wasn't caught
        game.Play(Scripts.Find("Victory", "PokemonLeagueHallOfFame")!);
        Assert.Equal(outcome == BattleOutcome.Caught ? 2 : 1, game.Story.Var("VAR_HALL_OF_ORIGIN_STATE"));
        game.Arrive("HallOfOrigin", 31, 52);
        Assert.Equal(outcome != BattleOutcome.Caught, game.Present("arceus") != null);
    }

    [Fact]
    public void TheHallOfOriginLeadsBackIntoTheMountain()
    {
        var game = Champion();
        var way = Assert.Single(game.Maps["HallOfOrigin"].Warps);
        Assert.Equal("MtCoronet6F", way.TargetMap);
        Assert.Equal("Hall of Origin", game.Maps["HallOfOrigin"].DisplayNameAt(31, 52));
    }

    // ------------------------------------------------------------------ Manaphy and Phione

    [Fact]
    public void AnEggOfManaphyIsHandedOverOnRoute230()
    {
        var game = Champion();
        game.Arrive("Sinnoh", 679, 433);
        Assert.Null(game.Present("ranger", "route_230"));
        // Mr. Backlot's book read, she is on the shore
        game.Story.Set("FLAG_SKIP_POKEMON_MANSION_OFFICE_BOOK_SEEN_MANAPHY_CHECK");
        game.Arrive("Sinnoh", 679, 433);
        Assert.NotNull(game.Present("ranger", "route_230"));
        game.Answers.Enqueue(0);
        var egg = TalkFrom(game, "ranger", Direction.Right, "route_230");
        var got = game.Party.Members[^1];
        Assert.True(got.IsEgg);
        Assert.Equal("Manaphy", got.Species.Name);
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_230_MANAPHY_EGG"));
        game.Arrive("Sinnoh", 679, 433);
        Assert.Null(game.Present("ranger", "route_230"));
        // and a Manaphy bred gives a Phione's Egg (Breeding.EggSpecies, as Platinum's)
        Assert.True(SpeciesSources.Sinnoh.Has("Phione"));
    }
}
