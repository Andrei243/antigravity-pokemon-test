using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF2: the cards of the people beaten. Who gets a card and once, the album's order, every Leader, Elite Four
/// member and Champion of the game's tables made into a card, the Trainer Card's page driven with no window, and a card
/// won by a battle and kept by a save. In the collection that runs alone, since the game's test changes the clocks.
/// </summary>
[Collection("Shared clocks and dice")]
public class CardAlbumTests
{
    [Fact]
    public void ALeadersFirstWinGivesTheCardOnceAndARematchNone()
    {
        var album = new CardAlbum();
        Assert.Null(album.ToSave());
        var card = album.Give("leader_roark", "Roark", "roark");
        Assert.NotNull(card);
        Assert.Equal(("Sinnoh", CardRank.Leader, "Roark"), CardAlbum.PersonOf(card!.Id));
        // Again, their rematch's team, none
        Assert.Null(album.Give("leader_roark", "Roark", "roark"));
        Assert.Null(album.Give("leader_roark_rematch", "Roark", "roark"));
        Assert.Single(album.Cards);
        // Nobody else gives one: another class, a team leader who is no Gym Leader, a friend's team, no team at all
        Assert.Null(album.Give("youngster_tristan", "Tristan", "youngster"));
        Assert.Null(album.Give("hoenn_archie", "Archie", "archie"));
        Assert.Null(album.Give("friend_0123", "Roark", "roark"));
        Assert.Null(album.Give("", "Roark", "roark"));
        Assert.Single(album.Cards);
    }

    [Fact]
    public void KantosAndHoennsChampionsEachGiveTheirsAndAPersonOfTwoRanksHasTwo()
    {
        var album = new CardAlbum();
        // Kanto's Champion is the rival, whichever starter his team answers and whatever he was named
        Assert.NotNull(album.Give("kanto_champion_first_squirtle", "Blue", "kantorival"));
        Assert.Null(album.Give("kanto_champion_rematch_bulbasaur", "Blue", "kantorival"));
        Assert.Null(album.Give("kanto_champion_megamax_charmander", "Blue", "kantorival"));
        Assert.NotNull(album.Give("hoenn_wallace", "Wallace", "wallace"));
        Assert.Null(album.Give("hoenn_wallace_rematch", "Wallace", "wallace"));
        // Koga is a Leader in Kanto and one of Johto's Four: two cards
        Assert.NotNull(album.Give("kanto_leader_koga", "Koga", "koga"));
        Assert.NotNull(album.Give("johto_elite_four_koga_koga", "Koga", "koga"));
        Assert.Equal(4, album.Cards.Count);
    }

    [Fact]
    public void BlackTwosClassesNameThePersonAndGiveTheirRank()
    {
        Assert.Equal(CardRank.Leader, CardAlbum.RankOf("Leader Elesa"));
        Assert.Equal(CardRank.EliteFour, CardAlbum.RankOf("Elite Four Shauntal"));
        Assert.Equal(CardRank.Champion, CardAlbum.RankOf("Champion"));
        Assert.Null(CardAlbum.RankOf("Aqua Leader"));
        Assert.Null(CardAlbum.RankOf("Magma Leader"));
        Assert.Null(CardAlbum.RankOf("Rival"));
        Assert.Equal("Unova", CardAlbum.RegionOf("unova_leader_elesa_rematch"));
        Assert.Equal("Hoenn", CardAlbum.RegionOf("hoenn_sidney_rematch"));
        Assert.Equal("Sinnoh", CardAlbum.RegionOf("champion_cynthia_beyond"));
    }

    [Fact]
    public void TheAlbumIsListedByRegionAndThenByWhenTheyWereBeaten()
    {
        var album = new CardAlbum();
        foreach (string id in new[] { "johto_leader_falkner_falkner", "leader_gardenia", "kanto_leader_brock", "leader_roark" })
            Assert.NotNull(album.Give(id, "", ""));
        Assert.Equal(new[] { "leader_gardenia", "leader_roark", "kanto_leader_brock", "johto_leader_falkner_falkner" },
            album.Shown().Select(c => c.Id));
        // A save keeps the order they were won in, and a card whose team the data lacks is kept and not shown
        var saved = album.ToSave()!;
        saved.Add(new AlbumCard { Id = "a_packs_leader_since_taken_away" });
        var back = new CardAlbum();
        back.Load(saved);
        Assert.Equal(saved.Select(c => c.Id), back.Cards.Select(c => c.Id));
        Assert.Equal(4, back.Shown().Count);
        back.Load(null);
        Assert.Empty(back.Cards);
    }

    /// <summary>Every record of every table of trainers whose class is a Leader's, an Elite Four's or a Champion's.</summary>
    private static List<TrainerRecord> TheLeagues() =>
        TrainerDatabase.All.Concat(TrainerDatabase.Ours).Concat(TrainerDatabase.Regions.SelectMany(TrainerDatabase.OfRegion))
            .Where(t => (t.Class.StartsWith("Leader", StringComparison.Ordinal) || t.Class.StartsWith("Elite Four", StringComparison.Ordinal) || t.Class == "Champion"))
            .ToList();

    [Fact]
    public void EveryLeaderEliteFourMemberAndChampionOfEveryRegionCanBeMadeIntoACard()
    {
        var paths = SavePaths.Current;
        try
        {
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            var leagues = TheLeagues();
            foreach (var record in leagues)
            {
                Assert.True(CardAlbum.PersonOf(record.Id) != null, record.Id);
                game.CardAlbum.Give(record.Id, record.Name, "ace_trainer");
            }
            // One card to a person, every person with one
            var people = leagues.Select(r => CardAlbum.PersonOf(r.Id)!.Value).Distinct().ToList();
            Assert.Equal(people.Count, game.CardAlbum.Cards.Count);
            foreach (var person in people) Assert.True(game.CardAlbum.Has(person), person.ToString());
            // Every built League is among them
            foreach (string region in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova", "Kalos", "Galar", "Paldea" })
                Assert.Contains(people, p => p.Region == region && p.Rank == CardRank.Leader);

            var shown = game.AlbumShown;
            Assert.Equal(people.Count, shown.Count);
            foreach (var entry in shown)
            {
                Assert.False(string.IsNullOrEmpty(entry.Name), entry.ToString());
                Assert.DoesNotContain("{", entry.Name);
                Assert.DoesNotContain("{", entry.Line);
                Assert.DoesNotContain("cardAlbum.", entry.Rank);
                Assert.DoesNotContain("dexText.", entry.Region, StringComparison.OrdinalIgnoreCase);
                Assert.NotEmpty(entry.Team);
                Assert.All(entry.Team, m => Assert.False(string.IsNullOrEmpty(m.Model)));
            }
            // A band where the region has them, the plain one where it has none; a line where trainer-lines has one
            Assert.Equal(SceneTransition.BandOf("Leader", "Roark").Base, shown.Single(e => e.Name == "Roark").Band.Base);
            Assert.Equal(SceneTransition.PlainBand.Base, shown.Single(e => e.Name == "Milo").Band.Base);
            Assert.NotEqual("", shown.Single(e => e.Name == "Roark").Line);
        }
        finally
        {
            InputManager.Feed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void TheCardsPageMovesOpensAndPutsBackWithNoWindow()
    {
        var card = new TrainerCardScreen { IsActive = true };
        card.Open(regionPages: 2);
        Assert.False(card.GoTo(CardPage.Cards));
        card.Open(regionPages: 2, cards: 14);
        Assert.True(card.GoTo(CardPage.Cards));
        Assert.Equal((CardPage.Cards, -1), (card.Page, card.RegionShown));
        Assert.Equal((0, 0, 2), (card.CardCursor, card.Sheet, card.Sheets));
        Assert.True(card.MoveCard(1, 0));
        Assert.True(card.MoveCard(0, 1));
        Assert.True(card.MoveCard(0, 1));
        Assert.Equal(7, card.CardCursor);
        // A sheet runs on into the next, and the short last row takes its end
        Assert.True(card.MoveCard(0, 1));
        Assert.Equal((10, 1), (card.CardCursor, card.Sheet));
        Assert.True(card.MoveCard(0, 1));
        Assert.Equal(13, card.CardCursor);
        Assert.False(card.MoveCard(0, 1));
        Assert.False(card.MoveCard(1, 0));
        // The pointer: the sheet's own cells, and a click anywhere puts an opened card back
        var cells = ModernUi.AlbumCells(1920, 5);
        var middle = new Vector2(cells[2].X + cells[2].Width / 2, cells[2].Y + cells[2].Height / 2);
        Assert.Equal(11, card.HitTest(middle));
        card.PointAt(11);
        Assert.Equal(11, card.CardCursor);
        Assert.Null(card.HitTest(new Vector2(10, 10)));
        card.PressConfirm();
        Assert.True(card.CardOpened);
        Assert.Equal(PokemonPlatinumEngine.UI.Kit.UiPointer.Anywhere, card.HitTest(middle));
        Assert.False(card.MoveCard(0, -1));
        Assert.True(card.MoveCard(1, 0));
        Assert.Equal(12, card.CardCursor);
        // B puts the card back, and only then puts the Trainer Card away
        card.PressCancel();
        Assert.True(card.IsActive);
        Assert.False(card.CardOpened);
        // Left on the first card turns back to the page before
        while (card.MoveCard(0, -1)) { }
        while (card.CardCursor > 0) card.MoveCard(-1, 0);
        Assert.True(card.MoveCard(-1, 0));
        Assert.Equal((CardPage.Region, 1), (card.Page, card.RegionShown));
        // Off the cards' page the pointer finds nothing and A turns the card over as ever
        Assert.Null(card.HitTest(middle));
        card.PressConfirm();
        Assert.Equal(CardPage.Front, card.Page);
        card.PressCancel();
        Assert.False(card.IsActive);
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(1f / 60f);
    }

    private static void Win(GameEngine engine, GameEngine.Driver game, string team)
    {
        var someone = game.Map.Everyone.First(n => n.IsTrainer && n.TrainerData != null);
        var own = someone.TrainerData!;
        var trainer = new Trainer { Id = team };
        TrainerDatabase.Fill(trainer, TrainerDatabase.Get(team)!);
        someone.TrainerData = trainer;
        game.StartTrainerBattle(someone);
        for (int f = 0; f < 60 * 120 && game.State != GameState.Battle; f++) Run(engine, ActionFrame.None);
        Assert.Equal(GameState.Battle, game.State);
        var battle = game.Battle!;
        for (int f = 0; f < 60 * 600 && !battle.IsBattleOver; f++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                // The foe is left a single HP, so that any hit ends it
                battle.EnemyPokemon.CurrentHP = 1;
                battle.SelectMove(battle.PlayerPokemon.Moves.FindIndex(m => m.Data.Name == "Surf"));
            }
            Run(engine, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        for (int f = 0; f < 60 * 120 && game.State != GameState.Overworld; f++)
            Run(engine, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        Assert.Equal(GameState.Overworld, game.State);
        someone.TrainerData = own;
    }

    [Fact]
    public void RoarksCardIsGivenByHisBattleAndKeptByASaveAndTheKeysOpenIt()
    {
        string folder = Path.Combine(Path.GetTempPath(), "card-album-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(7);
            var engine = new GameEngine(new GameSettings { Muted = true, BattleStyle = BattleStyle.Set });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            var lead = new Pokemon(PokemonDatabase.Get("Kyogre")!, 100);
            lead.Moves.Clear();
            lead.Moves.Add(MoveDatabase.Create("Surf"));
            game.Party.Add(lead);
            game.State = GameState.Overworld;
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Right);

            // A game with no card has no page of them and keeps nothing
            Assert.Empty(game.CardNow.Album);
            Assert.Null(game.BuildSave().CardAlbum);

            Win(engine, game, "leader_roark");
            var card = Assert.Single(game.CardAlbum.Cards);
            Assert.Equal(("leader_roark", "Roark"), (card.Id, card.Name));
            Assert.NotEqual("", card.Look);
            Win(engine, game, "leader_roark_rematch");
            Assert.Single(game.CardAlbum.Cards);
            // A trainer who is no Leader gives none
            Win(engine, game, "youngster_tristan");
            Assert.Single(game.CardAlbum.Cards);

            var read = SaveManager.Read(SaveManager.Write(game.BuildSave()))!;
            game.CardAlbum.Load(null);
            game.ApplySave(read);
            Assert.Equal((card.Id, card.Name, card.Look), (game.CardAlbum.Cards[0].Id, game.CardAlbum.Cards[0].Name, game.CardAlbum.Cards[0].Look));

            // The card's page, by the keys: the team is the data's, nothing asks the graphics card for a bust
            game.State = GameState.Overworld;
            game.OpenTrainerCard();
            Assert.True(game.TrainerCard.HasCardsPage);
            for (int i = 0; i < 12 && game.TrainerCard.Page != CardPage.Cards; i++) Run(engine, ActionFrame.Press(GameAction.Right));
            Assert.Equal(CardPage.Cards, game.TrainerCard.Page);
            Assert.Single(game.AlbumBustsInView);
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Assert.True(game.TrainerCard.CardOpened);
            // The sheet's busts are kept under the opened card's large one
            Assert.Equal(new[] { false, true }, game.AlbumBustsInView.Select(b => b.Large));
            Assert.Equal(0, game.AlbumBustsRendered);
            var entry = Assert.Single(game.CardNow.Album);
            Assert.Equal(TrainerDatabase.Get("leader_roark")!.Party.Select(m => m.Species), entry.Team.Select(m => m.Model));
            Run(engine, ActionFrame.Press(GameAction.Cancel));
            Assert.Equal((GameState.TrainerCard, false), (game.State, game.TrainerCard.CardOpened));
            Run(engine, ActionFrame.Press(GameAction.Cancel));
            Assert.Equal(GameState.Overworld, game.State);

            // A new game begins with none
            engine.StartNewGame();
            Assert.Empty(game.CardAlbum.Cards);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
