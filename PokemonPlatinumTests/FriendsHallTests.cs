using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The friends' hall (plan 13 · V23; rulings, "The friends' hall"): the trainers of the files in <c>trainers/</c> stand in
/// Jubilife City's Pokémon Center upstairs, the same folder and day the same trainers on the same spots, each beaten
/// once a day, fought on copies at level 50 so the party, the bag and the money are what they were, and a refused file
/// stands nobody.
/// </summary>
[Collection("Shared clocks and dice")]
public class FriendsHallTests
{
    private static readonly DateTime Day = new(2026, 6, 1);

    private static BuiltTeam Weak(string name) => new()
    {
        Name = name,
        Members = { new BuiltPokemon { Species = "Magikarp", Level = 50, Nature = Nature.Hardy, Ability = "Swift Swim", Moves = { "Splash" } } }
    };

    private static TrainerFile File(string name, PlayerLook look = PlayerLook.Girl) => TrainerFile.Of(name, look, "Ace Trainer", 0, 1, Weak(name));

    private static string Folder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "friendshall-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    [Fact]
    public void TheSameFolderStandsTheSameTrainersOnTheSameSpotsInTheOrderOfTheirNames()
    {
        string folder = Folder();
        try
        {
            // Ten files written out of order, one of them damaged: eight stand, by name, the damaged one nobody
            foreach (string name in new[] { "Zed", "Amy", "Kim", "Bo", "Lu", "Max", "Ned", "Oz", "Pia", "Quin" })
                TrainerFiles.Write(folder, File(name));
            System.IO.File.WriteAllText(Path.Combine(folder, "Broken" + TrainerFile.Ending), "PPTRAINER 1\nnot a trainer");
            var first = FriendsHall.Stand(TrainerFiles.In(folder));
            var again = FriendsHall.Stand(TrainerFiles.In(folder));
            Assert.Equal(FriendsHall.Most, first.Count);
            Assert.Equal(new[] { "Amy", "Bo", "Kim", "Lu", "Max", "Ned", "Oz", "Pia" }, first.Select(f => f.File.Name));
            Assert.Equal(FriendsHall.Spots, first.Select(f => f.Spot));
            Assert.Equal(first.Select(f => (f.File.Checksum, f.Spot)), again.Select(f => (f.File.Checksum, f.Spot)));

            // A person of the hall: their look, their name, the hall's script, facing whoever comes up
            var person = FriendsHall.PersonOf(first[0].File, first[0].Spot);
            Assert.Equal("DAWN", person.NpcType);
            Assert.Equal("Amy", person.Name);
            Assert.Equal(FriendsHall.Script, person.Script);
            Assert.True(FriendsHall.IsFriend(person));
            Assert.Null(person.TrainerData);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void ARefusedFileStandsNobody()
    {
        string folder = Folder();
        try
        {
            var file = File("Amy");
            TrainerFiles.Write(folder, file);
            // One character of the team changed: the checksum no longer holds
            string path = Directory.GetFiles(folder).Single();
            System.IO.File.WriteAllText(path, System.IO.File.ReadAllText(path).Replace("Splash", "Splosh"));
            Assert.Empty(FriendsHall.Stand(TrainerFiles.In(folder)));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void ATrainerBeatenTodayStaysBeatenUntilTheDayMoves()
    {
        var saved = GameClock.FixedDate;
        try
        {
            GameClock.FixedDate = Day;
            var friend = File("Amy");
            var person = FriendsHall.PersonOf(friend, FriendsHall.Spots[0]);
            var host = new HeadlessScriptHost();
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Empoleon")!, 60));
            host.Friends[person] = friend;

            void Talk()
            {
                var runner = new ScriptRunner(ScriptLibrary.Default, host);
                runner.Start(ScriptLibrary.Default.Find(FriendsHall.Script, null)!, person);
                runner.RunToEnd();
            }

            // The first talk: their line, a battle on copies at 50 against their team, their last word
            Talk();
            var (team, foes, partner, doubles) = Assert.Single(host.FrontierBattles);
            Assert.All(team.Members, p => Assert.Equal(TrainerFile.FlatLevel, p.Level));
            Assert.NotSame(host.Party.Members[0], team.Members[0]);
            Assert.Equal(60, host.Party.Members[0].Level);
            Assert.Equal("Magikarp", Assert.Single(Assert.Single(foes).Party.Members).Species.Name);
            Assert.Null(partner);
            Assert.False(doubles);
            Assert.True(host.FriendsHall!.BeatenOn(friend.Checksum, Day));
            Assert.Equal(1, host.FriendsWon);
            Assert.Contains(host.Transcript, line => line.Speaker == "Amy" && line.Text == Text.Of("friends.line.0"));
            Assert.Contains(host.Transcript, line => line.Speaker == "Amy" && line.Text == Text.Of("friends.line.1"));

            // The same day: no battle, only their word that tomorrow will do
            Talk();
            Assert.Single(host.FrontierBattles);
            Assert.Equal(1, host.FriendsWon);

            // The next day they battle again
            GameClock.FixedDate = Day.AddDays(1);
            Talk();
            Assert.Equal(2, host.FrontierBattles.Count);

            // A save keeps only today's, and reads it back
            var kept = host.FriendsHall.ToSaved(Day.AddDays(1))!;
            Assert.Equal(friend.Checksum, Assert.Single(kept).Checksum);
            Assert.Null(host.FriendsHall.ToSaved(Day.AddDays(2)));
            Assert.True(FriendsHallState.From(kept).BeatenOn(friend.Checksum, Day.AddDays(1)));
        }
        finally { GameClock.FixedDate = saved; }
    }

    [Fact]
    public void ALostBattleBeatsNobody()
    {
        var friend = File("Amy");
        var person = FriendsHall.PersonOf(friend, FriendsHall.Spots[0]);
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 5));
        host.Friends[person] = friend;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FriendsHall.Script, null)!, person);
        runner.RunToEnd();
        Assert.Single(host.FrontierBattles);
        Assert.Equal(0, host.FriendsHall!.Count);
        Assert.Equal(0, host.FriendsWon);
    }

    // ------------------------------------------------------------------ in a game

    [Fact]
    public void InAGameAFriendStandsInTheHallBattlesAtFiftyAndLeavesTheTeamBagAndMoneyAsTheyWere()
    {
        var saved = GameClock.FixedDate;
        string folder = Folder();
        var paths = SavePaths.Current;
        try
        {
            GameClock.FixedDate = Day;
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var friend = File("Amy");
            TrainerFiles.Write(Path.Combine(folder, TrainerFile.FolderName), friend);

            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Empoleon")!, 60));
            game.State = GameState.Overworld;
            game.TrainersLookOnArrival = false;

            var hall = MapDatabase.Get(FriendsHall.Room);
            Assert.Equal(FriendsHall.Room, hall.Name);
            var spot = FriendsHall.Spots[0];
            game.Place(hall, spot.X, spot.Y + 1, Direction.Up);
            game.ArriveOnMap();
            var person = hall.GetNpcAt(spot.X, spot.Y);
            Assert.NotNull(person);
            Assert.True(FriendsHall.IsFriend(person!));
            // Come to again, the hall is laid out afresh: still one of them
            game.ArriveOnMap();
            Assert.Single(hall.NPCs.Where(FriendsHall.IsFriend));
            person = hall.GetNpcAt(spot.X, spot.Y)!;

            string Team() => string.Join("|", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level} {p.CurrentExp} {p.CurrentHP} {string.Join(",", p.Moves.Select(m => m.CurrentPP))}"));
            string before = Team();
            int money = game.Money;
            int items = game.Bag.AllItems.Sum(s => s.Quantity);

            game.Interact();
            Until(engine, () => game.State == GameState.Battle, "the friend's battle", press: true);
            var battle = game.Battle!;
            Assert.True(battle.IsFrontier);
            Assert.All(battle.PlayerParty.Members, p => Assert.Equal(TrainerFile.FlatLevel, p.Level));
            for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
            {
                if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(Math.Max(0, battle.PlayerPokemon.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status)));
                Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.Equal(BattleResult.PlayerVictory, battle.Result);
            Until(engine, () => game.State == GameState.Overworld && !engine.ScriptRunning && !game.Dialogue.IsActive, "the field again", press: true);

            Assert.Equal(before, Team());
            Assert.Equal(money, game.Money);
            Assert.Equal(items, game.Bag.AllItems.Sum(s => s.Quantity));
            Assert.Equal(1, game.Statistics.Get(Statistics.TrainerWon));
            var save = game.BuildSave();
            Assert.Equal(friend.Checksum, Assert.Single(save.FriendsBeaten!).Checksum);

            // Spoken to again the same day: no battle
            game.Interact();
            for (int f = 0; f < 600; f++) Run(engine, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            Assert.NotEqual(GameState.Battle, game.State);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            GameClock.FixedDate = saved;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(1f / 60f);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 60)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
