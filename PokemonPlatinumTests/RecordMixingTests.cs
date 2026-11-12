using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Records mixed between one's own saves (plan 40 · GE10; rulings, "Records mixed between one's own saves"): another
/// save gives its trainer (a team the battle room could build), its Hoenn secret base and its interview; the game in play
/// and a damaged slot give nothing; the other saves are never written; the friends' hall stands the mixed trainers after
/// the folder's, Hoenn's spot leads into the friend's base with its owner in it, and the television tells the interview.
/// Two saves mixed at the desk each find the other's trainer and base. Shares the dice, the save folder and the clocks,
/// so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class RecordMixingTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Day = new(2026, 6, 1);

    private static readonly string[] Interview =
        { PhraseGroups.Key(PhraseGroup.Pokemon, "Pikachu"), PhraseGroups.Key(PhraseGroup.Types, "Fire") };

    private static SavedPokemonData Saved(string species, int level, params string[] moves) => new()
    {
        SpeciesName = species,
        Level = level,
        Moves = moves.Select(m => new SavedMoveData { MoveName = m, CurrentPP = 10 }).ToList()
    };

    private static SaveData Other(int trainerId, string name = "Mira") => new()
    {
        PlayerName = name,
        Look = PlayerLook.Girl,
        TrainerId = trainerId,
        Started = new DateTime(2026, 1, trainerId % 28 + 1),
        Party = { Saved("Pikachu", 30, "Thunder Shock", "Quick Attack") }
    };

    // ------------------------------------------------------------------ what another save gives

    [Fact]
    public void AnotherSavesTeamIsHeldToWhatTheBattleRoomBuilds()
    {
        var other = Other(41);
        other.Party.Clear();
        other.Party.Add(new SavedPokemonData { SpeciesName = "Togepi", IsEgg = true });
        // A level past 100 is held to 100; a move the species can't know is let go; an item nobody holds is let go
        var pikachu = Saved("Pikachu", 140, "Thunder Shock", "Hydro Pump");
        pikachu.HeldItem = "Poké Ball";
        other.Party.Add(pikachu);
        // A Pokémon whose moves are all wrong learns its own by level instead
        other.Party.Add(Saved("Bidoof", 20, "Psychic"));

        var record = RecordMix.RecordOf(other, Day);
        Assert.Equal(2, record.Team!.Count);
        var p = record.Team[0];
        Assert.Equal(("Pikachu", 100, null), (p.Species, p.Level, p.Item));
        Assert.Equal(new[] { "Thunder Shock" }, p.Moves);
        Assert.NotEmpty(record.Team[1].Moves);
        Assert.DoesNotContain("Psychic", record.Team[1].Moves);
        Assert.All(record.Team, b => Assert.Null(BuiltTeams.WhyNot(b)));

        // Its trainer stands as a friend's file of the game's own data, the same every time
        var file = RecordMix.TrainerOf(record)!;
        Assert.Null(TrainerFile.Refuse(file));
        Assert.Equal("Mira", file.Name);
        Assert.Equal(PlayerLook.Girl, file.Look);
        Assert.Equal(file.Checksum, RecordMix.TrainerOf(RecordMix.RecordOf(other, Day.AddDays(3)))!.Checksum);

        // A save with nothing that can battle gives no trainer, but still its record
        var eggs = Other(42);
        eggs.Party.Clear();
        eggs.Party.Add(new SavedPokemonData { SpeciesName = "Togepi", IsEgg = true });
        var bare = RecordMix.RecordOf(eggs, Day);
        Assert.Null(bare.Team);
        Assert.Null(RecordMix.TrainerOf(bare));
    }

    [Fact]
    public void AnotherSavesBaseAndInterviewAreKeptAsTheyStood()
    {
        var other = Other(43);
        other.StoryPhrases[StoryState.InterviewPhrase] = Interview.ToList();
        other.Decorations = new List<StoredDecoration>();
        SecretBases.Add(other.Decorations, DecorationDatabase.IdOf("Small Desk"));
        other.SecretBase = new SecretBaseSave { Id = 171, Map = "Hoenn", X = 10, Y = 11, OutX = 10, OutY = 12 };
        other.SecretBase.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 8 });

        var record = RecordMix.RecordOf(other, Day);
        Assert.Equal(Interview, record.Interview);
        var b = record.Base!;
        Assert.Equal((171, "Hoenn", 10, 11, 10, 12), (b.Id, b.Map, b.X, b.Y, b.OutX, b.OutY));
        var desk = Assert.Single(b.Standing);
        Assert.Equal((DecorationDatabase.IdOf("Small Desk"), 2, 8), (desk.Id, desk.X, desk.Y));

        // A phrase that isn't whole is no interview
        other.StoryPhrases[StoryState.InterviewPhrase] = new List<string> { Interview[0], "nothing:at-all" };
        Assert.Null(RecordMix.RecordOf(other, Day).Interview);
    }

    [Fact]
    public void MixingReplacesTheSameGamePassesTheOwnOverAndKeepsTheNewestFirst()
    {
        var state = new RecordMixState();
        var own = Other(50, "Self");
        Assert.Equal(0, RecordMix.Mix(state, new[] { own }, own.TrainerId, own.Started, Day));
        Assert.Empty(state.Records);

        Assert.Equal(2, RecordMix.Mix(state, new[] { Other(1, "Ann"), Other(2, "Bo") }, own.TrainerId, own.Started, Day));
        Assert.Equal(new[] { "Bo", "Ann" }, state.Records.Select(r => r.Name));

        // Ann's game mixed again: one record of it, now the newest
        var annLater = Other(1, "Ann");
        annLater.Party.Add(Saved("Bidoof", 9, "Tackle"));
        Assert.Equal(1, RecordMix.Mix(state, new[] { annLater }, own.TrainerId, own.Started, Day));
        Assert.Equal(new[] { "Ann", "Bo" }, state.Records.Select(r => r.Name));
        Assert.Equal(2, state.Records[0].Team!.Count);

        // At most eight are kept
        RecordMix.Mix(state, Enumerable.Range(10, 12).Select(n => Other(n, "T" + n)), own.TrainerId, own.Started, Day);
        Assert.Equal(RecordMix.Most, state.Records.Count);

        // And a save keeps them, reading them back as they were
        var saved = state.ToSave()!;
        var back = new RecordMixState();
        back.Load(saved);
        Assert.Equal(state.Records.Select(r => r.Key), back.Records.Select(r => r.Key));
        Assert.Null(new RecordMixState().ToSave());
    }

    [Fact]
    public void TheHallStandsTheMixedTrainersAfterTheFolders()
    {
        var state = new RecordMixState();
        RecordMix.Mix(state, new[] { Other(1, "Ann") }, 0, null, Day);
        var folder = new List<TrainerFiles.Entry>
        {
            new("Zed" + TrainerFile.Ending, TrainerFile.Of("Zed", PlayerLook.Boy, "Ace Trainer", 0, 1,
                new BuiltTeam { Name = "Zed", Members = { new BuiltPokemon { Species = "Magikarp", Moves = { "Splash" } } } }), null)
        };
        var standing = FriendsHall.Stand(folder.Concat(RecordMix.Entries(state)).ToList());
        Assert.Equal(new[] { "Zed", "Ann" }, standing.Select(s => s.File.Name));
        Assert.Equal(FriendsHall.Spots[1], standing[1].Spot);
    }

    // ------------------------------------------------------------------ the desk and the television

    private static HeadlessScriptHost Desk(params SaveData[] others)
    {
        var host = new HeadlessScriptHost();
        host.Others.AddRange(others);
        var attendant = new NPC { Name = "Attendant", Key = "teala_east" };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("RecordDesk", null)!, attendant);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheDeskMixesWithTheOtherSavesOrSaysThereAreNone()
    {
        var none = Desk();
        Assert.Empty(none.MixedRecords!.Records);
        Assert.Contains(none.Transcript, t => t.Text.Contains("no other adventure"));

        var two = Desk(Other(1, "Ann"), Other(2, "Bo"));
        Assert.Equal(2, two.MixedRecords!.Records.Count);
        Assert.Contains(two.Transcript, t => t.Text.Contains("Bo and Ann"));
    }

    [Fact]
    public void TheTelevisionTellsOfTheMixedInterviewsOnlyOnceThereAreSome()
    {
        var facts = new BroadcastFacts();
        Assert.DoesNotContain(Programme.MixedInterviews, Broadcasts.On(facts));

        var withOne = facts with { MixedInterviews = new[] { ("Ann", (IReadOnlyList<string>)Interview) } };
        Assert.Equal(Broadcasts.On(facts).Count + 1, Broadcasts.On(withOne).Count);
        var lines = Broadcasts.Lines(Programme.MixedInterviews, withOne);
        Assert.Contains(lines, l => l.Contains("Ann") && l.Contains(PhraseGroups.Shown(Interview)));
        Assert.Equal(Programme.MixedInterviews, Broadcasts.Named("mixed"));
    }

    // ------------------------------------------------------------------ two saves mixed in the game

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    // The tile from which a spot is faced, and the way to face: an open tile beside it
    private static (int X, int Y, Direction Facing) Before(Map map, int x, int y)
    {
        foreach (var (dx, dy, facing) in new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right) })
            if (map.InBounds(x + dx, y + dy) && map.IsWalkable(x + dx, y + dy) && map.GetNpcAt(x + dx, y + dy) == null) return (x + dx, y + dy, facing);
        throw new InvalidOperationException($"No open tile beside the spot at {x}, {y}");
    }

    private static void MakeGame(string name, int slot, int spotId)
    {
        var engine = Headless();
        var game = engine.Drive;
        engine.NewGameRegion = "Hoenn";
        engine.StartNewGame(name, PlayerLook.Girl);
        game.SaveSlot = slot;
        var lead = new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 30);
        game.Party.Add(lead);
        game.Story.SetPhrase(StoryState.InterviewPhrase, Interview);
        var hoenn = MapDatabase.Get("Hoenn");
        var spot = hoenn.SecretBaseSpots.First(s => s.Value == spotId).Key;
        var (ox, oy, _) = Before(hoenn, spot.X, spot.Y);
        game.SetSecretBase(new SecretBaseSave { Id = spotId, Map = "Hoenn", X = spot.X, Y = spot.Y, OutX = ox, OutY = oy });
        SecretBases.Add(game.Decorations, DecorationDatabase.IdOf("Small Desk"));
        game.SecretBase!.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 8 });
        game.Place(hoenn, ox, oy, Direction.Down);
        Assert.True(game.Save());
    }

    [Fact]
    public void TwoSavesMixedAtTheDeskEachFindTheOthersTrainerInTheHallAndBaseInHoenn()
    {
        string folder = Path.Combine(Path.GetTempPath(), "recordmix-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        var date = GameClock.FixedDate;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            GameClock.FixedDate = Day;
            Dice.Seed(40);
            var hoenn = MapDatabase.Get("Hoenn");
            // Two spots of two different rooms
            int first = 171;
            int second = hoenn.SecretBaseSpots.Values.First(id => id / 10 != first / 10);
            MakeGame("Ann", 1, first);
            MakeGame("Bo", 2, second);
            // A damaged third slot is passed over
            File.WriteAllText(SavePaths.Current.Slot(3), "{ not a save");

            byte[] Bytes(int slot) => File.ReadAllBytes(SavePaths.Current.Slot(slot));
            foreach (var (slot, otherName, otherSpot) in new[] { (1, "Bo", second), (2, "Ann", first) })
            {
                int otherSlot = 3 - slot;
                byte[] before = Bytes(otherSlot);
                var engine = Headless();
                engine.ContinueGame(slot);
                Until(engine, () => engine.Drive.State == GameState.Overworld, "the continued game");
                var game = engine.Drive;
                Assert.Single(game.OtherSaves());

                // The desk upstairs in Jubilife City's Pokémon Center
                game.Place(MapDatabase.Get(FriendsHall.Room), 3, 5, Direction.Up);
                Assert.True(engine.StartScript("common.RecordDesk", game.Map.NPCs.FirstOrDefault(n => n.Key == "teala_east")));
                Until(engine, () => !engine.ScriptRunning && game.State == GameState.Overworld, "the desk's talk", press: true);
                var mixed = Assert.Single(game.Records.Records);
                Assert.Equal(otherName, mixed.Name);
                Assert.Equal(before, Bytes(otherSlot));

                // The hall, come to again, stands the other's trainer
                game.ArriveOnMap();
                Assert.Contains(game.Map.NPCs, n => FriendsHall.IsFriend(n) && n.Name == otherName);

                // Hoenn: the other's spot leads into the other's base, furnished, its owner in it
                var spot = hoenn.SecretBaseSpots.First(s => s.Value == otherSpot).Key;
                var (x, y, facing) = Before(hoenn, spot.X, spot.Y);
                game.Place(hoenn, x, y, facing);
                game.TrainersLookOnArrival = false;
                game.Interact();
                string room = SecretBaseLayouts.Of(otherSpot)!.Map;
                Until(engine, () => game.Map.Name == room && game.State == GameState.Overworld && !engine.ScriptRunning, "the friend's base", press: true);
                Assert.Same(mixed, game.Records.Visiting);
                Assert.Contains(game.Map.Props, p => p.Type == PropType.Decoration && (p.X, p.Y) == (2, 8));
                Assert.Contains(game.Map.NPCs, n => FriendsHall.IsFriend(n) && n.Name == otherName);
                var door = game.Map.Warps.First();
                Assert.Equal(("Hoenn", mixed.Base!.OutX, mixed.Base.OutY), (door.TargetMap, door.TargetX, door.TargetY));

                // Out again: no friend's base is visited, and the player's own spot is still the player's
                game.Place(hoenn, x, y, facing);
                game.ArriveOnMap();
                Assert.Null(game.Records.Visiting);
                Assert.Equal(before, Bytes(otherSlot));
            }
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            GameClock.FixedDate = date;
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
