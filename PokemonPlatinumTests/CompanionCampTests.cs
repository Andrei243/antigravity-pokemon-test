using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F6 and F9 in the game: the start menu's CAMP and the camp played through the headless game, its clearing's
/// places; and the marks of an encounter on the summary, where the button makes one the title, and on the cards.
/// </summary>
[Collection("Shared clocks and dice")]
public class CompanionCampTests
{
    private const float Frame = 1f / 60f;

    // ------------------------------------------------------------------ F6 · the camp

    [Fact]
    public void TheStartMenuOffersCampAfterPokemonOnlyWhenAskedAndWithATeam()
    {
        var menu = new StartMenu { HasPokedex = true, HasPokemon = true, HasCamp = false };
        int without = menu.EntryCount;
        menu.HasCamp = true;
        Assert.Equal(without + 1, menu.EntryCount);
        menu.Open();
        menu.Move(1); // POKéDEX, then POKéMON
        menu.Move(1); // then CAMP
        Assert.Equal(StartMenuChoice.Camp, menu.Confirm());
        // No team, no camp, as POKéMON
        var empty = new StartMenu { HasPokedex = true, HasPokemon = false, HasCamp = true };
        Assert.Equal(new StartMenu { HasPokedex = true, HasPokemon = false }.EntryCount, empty.EntryCount);
    }

    private static (GameEngine Engine, GameEngine.Driver Game) NewGame(bool follower)
    {
        Dice.Seed(41);
        var engine = new GameEngine(new GameSettings { Muted = true, Follower = follower });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 12));
        game.State = GameState.Overworld;
        game.TrainersLookOnArrival = false;
        return (engine, game);
    }

    private static void Done()
    {
        InputManager.Feed(null);
        Dice.Seed(null);
        Ruleset.Use(RulesPreset.Platinum);
    }

    [Fact]
    public void TheCampIsOpenedFromTheStartMenuPettedAndLeftThroughTheGame()
    {
        try
        {
            var (off, offGame) = NewGame(follower: false);
            Assert.False(offGame.HasCamp);

            var (engine, game) = NewGame(follower: true);
            Assert.True(game.HasCamp);
            var lead = game.Party.Members[0];
            int before = lead.Friendship;
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.Equal(GameState.Camp, game.State);
            Assert.True(game.Camp.IsActive);
            Assert.Same(lead, game.Camp.Selected);

            Assert.True(game.Camp.Pet());
            Assert.True(lead.Friendship > before);
            Assert.Equal(0f, game.Camp.PettedFor(0));
            Assert.False(game.Camp.Pet());
            Assert.Contains(lead.DisplayName, game.Camp.Note);

            // The item button asks for a berry: the bag opens on the berries, the camp waiting under it
            game.Camp.AskToFeed();
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
            Assert.Equal(GameState.BagMenu, game.State);

            // Fed straight from the bag, as the bag's pick hands it over
            var berry = ItemDatabase.Get("Oran Berry")!;
            game.Bag.AddItem(berry, 1);
            Assert.True(game.Camp.Feed(berry, game.Bag));
            Assert.Equal(0, game.Bag.GetQuantity(berry));

            game.State = GameState.Camp;
            game.Camp.Cancel();
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
            Assert.Equal(GameState.Overworld, game.State);
        }
        finally { Done(); }
    }

    [Fact]
    public void AnEggIsNeitherPettedNorFedAndSaysSo()
    {
        var party = new Party();
        var egg = Of(Gender.Male, "Starly", 1);
        egg.IsEgg = true;
        party.Add(egg);
        var screen = new CampScreen();
        screen.Open(party, new CampCare());
        Assert.False(screen.Pet());
        Assert.Equal(Text.Of("camp.note.egg"), screen.Note);
        screen.AskToFeed();
        Assert.Equal(CampRequest.None, screen.TakeRequest());
    }

    [Fact]
    public void TheClearingStandsEveryoneApartAndInsideIt()
    {
        var clearing = ModernUi.CampClearing;
        foreach (int count in new[] { 1, 2, 3, 4, 5, 6 })
        {
            var places = Enumerable.Range(0, count).Select(i => ModernUi.CampPlace(clearing, i, count)).ToList();
            foreach (var p in places)
            {
                Assert.InRange(p.X, clearing.X + 100, clearing.X + clearing.Width - 100);
                Assert.InRange(p.Y, clearing.Y + clearing.Height * 0.42f, clearing.Y + clearing.Height);
            }
            for (int a = 0; a < count; a++)
                for (int b = a + 1; b < count; b++)
                    Assert.True(System.Numerics.Vector2.Distance(places[a], places[b]) >= 200, $"{count}: {a} and {b} stand too close");
            // The near row is lower on the screen than the far one
            if (count > CampScreen.Columns) Assert.True(places[CampScreen.Columns].Y > places[0].Y);
        }
    }

    [Fact]
    public void FriendshipShowsAsHeartsByItsBands()
    {
        Assert.Equal(1, ModernUi.FriendshipHearts(0));
        Assert.Equal(2, ModernUi.FriendshipHearts(70));
        Assert.Equal(3, ModernUi.FriendshipHearts(150));
        Assert.Equal(4, ModernUi.FriendshipHearts(Affection.Affectionate));
        Assert.Equal(5, ModernUi.FriendshipHearts(255));
    }

    // ------------------------------------------------------------------ F9 · the summary's marks and the title

    [Fact]
    public void TheMarksCursorGoesOnToTheMarksMetWithAndTheButtonMakesOneTheTitle()
    {
        var starly = Of(Gender.Male, "Starly", 10);
        var view = new SummaryView();
        view.Open();
        Assert.Equal(ModernUi.MarkCount, SummaryView.MarkPlaces(starly));
        Assert.True(view.EditMarks(starly));
        for (int i = 0; i < ModernUi.MarkCount; i++) view.MoveMark(1, SummaryView.MarkPlaces(starly));
        Assert.Equal(0, view.MarkCursor);

        starly.EncounterMarks.Add(nameof(Mark.SleepyTime));
        Assert.Equal(ModernUi.MarkCount + 1, SummaryView.MarkPlaces(starly));
        view.MoveMark(-1, SummaryView.MarkPlaces(starly));
        Assert.Equal(ModernUi.MarkCount, view.MarkCursor);
        Assert.Equal(Mark.SleepyTime, SummaryView.EncounterMarkAt(starly, view.MarkCursor));
        int marks = starly.Marks;
        view.ToggleMark(starly);
        Assert.Equal(nameof(Mark.SleepyTime), starly.Title);
        Assert.Equal(marks, starly.Marks);
        view.ToggleMark(starly);
        Assert.Null(starly.Title);
    }

    [Fact]
    public void ACardShowsTheTitlesMarkFirstAndNoneForAPokemonMetWithNone()
    {
        var p = Of(Gender.Male, "Starly", 10);
        Assert.Null(ModernUi.CardMarkOf(p));
        p.EncounterMarks.Add(nameof(Mark.Rare));
        p.EncounterMarks.Add(nameof(Mark.Rainy));
        Assert.Equal(Mark.Rare, ModernUi.CardMarkOf(p));
        EncounterMarks.ChooseTitle(p, Mark.Rainy);
        Assert.Equal(Mark.Rainy, ModernUi.CardMarkOf(p));
    }
}
