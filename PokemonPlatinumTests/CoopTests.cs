using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// A friend in the partner's place (plan 13 · V20; rulings, "A friend in the partner's place"): a tag battle handed
/// <see cref="BattleSetup.FriendPartner"/> asks the partner's place from outside through the battle's own menus, the
/// friend's SWITCH lists the partner's team and their BAG is refused, the record keeps the friend's answers and replays,
/// and in a game the options' row asks first. With the row off, or the flag off, the partner thinks for itself as ever.
/// </summary>
[Collection("Shared clocks and dice")]
public class CoopTests
{
    private const uint Seed = 20261103;

    private static (Party Mine, Trainer Barry, List<Trainer> Grunts) Teams()
    {
        var party = new Party();
        party.Add(Mon("Luxray", 40, "Spark", "Bite"));
        party.Add(Mon("Bidoof", 10, "Tackle"));
        var barry = new Trainer { Id = "barry_coop", Name = "Barry", TrainerClass = "Pokémon Trainer", Ai = AiFlags.Basic };
        barry.Party.Add(Mon("Staraptor", 40, "Wing Attack", "Quick Attack"));
        barry.Party.Add(Mon("Snorlax", 40, "Body Slam"));
        var grunts = new List<Trainer>();
        foreach (var (name, species) in new[] { ("A", "Stunky"), ("B", "Glameow") })
        {
            var grunt = new Trainer { Id = "grunt_coop_" + name, Name = name, TrainerClass = "Galactic Grunt" };
            grunt.Party.Add(Mon(species, 30, "Scratch"));
            grunts.Add(grunt);
        }
        grunts[0].Party.Add(Mon("Zubat", 28, "Bite"));
        return (party, barry, grunts);
    }

    private static BattleEngine TagBattle(bool friend, Inventory? bag = null)
    {
        var (party, barry, grunts) = Teams();
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag ?? new Inventory(), Pokedex = new Pokedex(), Trainers = grunts, Partner = barry,
            Format = BattleFormat.Double, Random = new BattleRandom(Seed), Rules = Ruleset.Platinum, PlayerName = "Lucas",
            FriendPartner = friend
        });
        Settle(battle);
        return battle;
    }

    /// <summary>Uses the first move, aimed at the first target the menu offers.</summary>
    private static void Fight(BattleEngine battle)
    {
        battle.SelectMove(0);
        if (battle.HUD.MenuState == BattleMenuState.SelectTarget) battle.SelectTarget(0);
    }

    [Fact]
    public void AFriendsTagBattleIsAnsweredForBothPlacesThroughTheMenusAndItsRecordReplays()
    {
        var battle = TagBattle(friend: true);
        Assert.True(battle.Core.FriendPartner);
        Assert.True(battle.HasFriendPartner);
        // Both places of the player's side are asked, the player's first
        var asked = Assert.IsType<ActionRequest>(battle.Core.Request).Places;
        Assert.Contains(new Place(BattleSide.Player, 0), asked);
        Assert.Contains(new Place(BattleSide.Player, 1), asked);
        Assert.False(battle.FriendChoosing);

        bool friendTurned = false, friendSwitched = false;
        for (int turn = 0; turn < 60 && !battle.IsBattleOver; turn++)
        {
            if (battle.IsChoosingReplacement)
            {
                var team = battle.SwitchParty;
                int next = Enumerable.Range(0, team.Count).First(i => !team.Members[i].IsFainted && battle.PlayerSlots.All(b => b.Pokemon != team.Members[i]));
                battle.SelectSwitch(next);
                Settle(battle);
                continue;
            }
            Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
            Assert.False(battle.FriendChoosing);
            Fight(battle);
            // The friend's turn follows the player's, on the partner's own Pokémon
            if (battle.HUD.MenuState == BattleMenuState.Main && !battle.IsBattleOver)
            {
                Assert.True(battle.FriendChoosing);
                Assert.Same(battle.PlayerSlots[1], battle.MenuBattler);
                friendTurned = true;
                // On the second turn the friend switches to the partner's Snorlax from the partner's own team
                var partnerTeam = battle.SwitchParty;
                int snorlax = partnerTeam.Members.FindIndex(p => p.Species.Name == "Snorlax");
                if (!friendSwitched && turn >= 1 && snorlax >= 0 && battle.PlayerSlots.All(b => b.Pokemon != partnerTeam.Members[snorlax]))
                {
                    battle.SelectMainMenuOption(2);
                    battle.SelectSwitch(snorlax);
                    friendSwitched = true;
                }
                else Fight(battle);
            }
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        Assert.True(friendTurned);
        Assert.True(friendSwitched);

        var record = battle.Core.Record!;
        Assert.True(record.FriendPartner);
        var partnerPlace = new Place(BattleSide.Player, 1);
        Assert.Contains(record.Answers.SelectMany(a => a), c => c.Who == partnerPlace && c.Kind == ChoiceKind.Switch);
        Assert.Contains(record.Answers.SelectMany(a => a), c => c.Who == partnerPlace && c.Kind == ChoiceKind.Fight);

        // Again with no screen, the same teams and answers: the same battle, line for line
        var (mine, barry, grunts) = Teams();
        var again = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, Trainers = grunts, Partner = barry, Format = BattleFormat.Double, Random = new BattleRandom(record.Seed),
            Rules = Ruleset.Platinum, PlayerName = "Lucas", PartnerController = null
        });
        again.Replay(record);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        Assert.Equal(battle.Result, again.Result);

        // And from the Vs. Recorder's file, which keeps that a friend chose
        var file = battle.RecordFile(new DateTime(2026, 11, 3))!;
        Assert.True(file.FriendPartner);
        var read = BattleRecordFile.Read(file.Serialize());
        Assert.Equal(ReplayRefusal.None, read.Refused);
        Assert.True(read.File!.FriendPartner);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(read.File.Replay()));

        // A friend's record fought with the partner's own mind is another battle, and refused
        var (m2, b2, g2) = Teams();
        var aiPartner = new BattleCore(new CoreSetup
        {
            PlayerParty = m2, Trainers = g2, Partner = b2, Format = BattleFormat.Double, Random = new BattleRandom(record.Seed),
            Rules = Ruleset.Platinum, PlayerName = "Lucas"
        });
        Assert.Throws<ArgumentException>(() => aiPartner.Replay(record));
    }

    [Fact]
    public void TheFriendsSwitchListsThePartnersTeamAndTheirBagIsRefused()
    {
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        var battle = TagBattle(friend: true, bag);
        Assert.Same(battle.PlayerParty, battle.SwitchParty);
        Fight(battle);
        Assert.True(battle.FriendChoosing);
        var partner = battle.PlayerSlots[1];
        Assert.NotNull(partner.Trainer);
        Assert.Same(partner.Roster, battle.SwitchParty);
        Assert.Equal(new[] { "Staraptor", "Snorlax" }, battle.SwitchParty.Members.Select(p => p.Species.Name));

        // BAG: a line of our own, and the menus stay at the commands
        battle.SelectMainMenuOption(1);
        Assert.NotEqual(BattleMenuState.SelectBagPocket, battle.HUD.MenuState);
        Settle(battle);
        Assert.True(battle.FriendChoosing);
        Assert.Equal(BattleCore.FriendBagRefusal, battle.Core.WhyNot(BattleChoice.UseItem(partner.Place, "Potion")));
        // The player's own place may still use the bag
        Assert.NotEqual(BattleCore.FriendBagRefusal, battle.Core.WhyNot(BattleChoice.UseItem(battle.PlayerSlots[0].Place, "Potion")));

        // POKéMON: the partner's team, never the player's
        battle.SelectMainMenuOption(2);
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        battle.SelectSwitch(1);
        Settle(battle);
        var switched = battle.Core.Record!.Answers.SelectMany(a => a).Single(c => c.Kind == ChoiceKind.Switch);
        Assert.Equal(partner.Place, switched.Who);
        Assert.Equal(1, switched.SwitchTo);
        Assert.Equal("Snorlax", battle.Core.PlayerSlots[1].Pokemon!.Species.Name);
        Assert.Equal("Luxray", battle.Core.PlayerSlots[0].Pokemon!.Species.Name);
    }

    [Fact]
    public void WithoutAFriendThePartnerThinksForItselfAndNothingChanges()
    {
        var battle = TagBattle(friend: false);
        Assert.False(battle.Core.FriendPartner);
        Assert.False(battle.HasFriendPartner);
        var asked = Assert.IsType<ActionRequest>(battle.Core.Request).Places;
        Assert.Equal(new[] { new Place(BattleSide.Player, 0) }, asked.Where(p => p.Side == BattleSide.Player));
        Fight(battle);
        Settle(battle);
        Assert.False(battle.FriendChoosing);
        Assert.False(battle.Core.Record!.FriendPartner);
        Assert.Null(battle.RecordFile(new DateTime(2026, 11, 3))!.FriendPartner);

        // A flag without a partner gives nobody a friend
        var party = new Party();
        party.Add(Mon("Luxray", 40, "Spark"));
        var alone = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { Mon("Bidoof", 5) },
            Random = new BattleRandom(Seed), Rules = Ruleset.Platinum, FriendPartner = true
        });
        Assert.False(alone.Core.FriendPartner);
    }

    [Fact]
    public void TheRowIsOffByDefaultAndStandsBeforeHelp()
    {
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(OptionRow.Coop, rows[Array.IndexOf(rows, OptionRow.Help) - 1]);
        var s = new GameSettings();
        Assert.False(s.Coop);
        OptionsScreen.Change(s, OptionRow.Coop, 1);
        Assert.True(s.Coop);
        var (label, value, help) = OptionsScreen.Describe(s, OptionRow.Coop);
        Assert.Equal(Text.Of("options.coop.label"), label);
        Assert.Equal(Text.Of("options.coop.help.on"), help);
        OptionsScreen.Change(s, OptionRow.Coop, -1);
        Assert.False(s.Coop);
        // Kept with the options and put back by RESET
        var read = System.Text.Json.JsonSerializer.Deserialize<GameSettings>(System.Text.Json.JsonSerializer.Serialize(new GameSettings { Coop = true }))!;
        Assert.True(read.Coop);
    }

    // ------------------------------------------------------------------ in a game

    [Fact]
    public void InAGameTheRowAsksBeforeABattleBesideAPartnerAndYesHandsTheFriendThePlace()
    {
        InAGame(coop: true, (engine, foe, barry) =>
        {
            var game = engine.Drive;
            game.StartTrainerBattle(foe, partner: barry);
            Assert.Equal(GameState.Dialogue, game.State);
            Assert.True(engine.AskingAboutAFriend);
            Assert.Equal(Text.Of("engine.coop.question", ("partner", "Barry")), game.Dialogue.CurrentLine);
            Until(engine, () => engine.Choice.IsOpen, "the answers");
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Assert.False(engine.AskingAboutAFriend);
            Until(engine, () => game.State == GameState.Battle, "the battle");
            Assert.True(game.Battle!.Core.FriendPartner);
        });
    }

    [Fact]
    public void InAGameNoLeavesThePartnerToThemselves()
    {
        InAGame(coop: true, (engine, foe, barry) =>
        {
            var game = engine.Drive;
            game.StartTrainerBattle(foe, partner: barry);
            Until(engine, () => engine.Choice.IsOpen, "the answers");
            engine.Choice.Move(1);
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Until(engine, () => game.State == GameState.Battle, "the battle");
            Assert.False(game.Battle!.Core.FriendPartner);
            Assert.NotNull(game.Battle.Core.Partner);
        });
    }

    [Fact]
    public void InAGameWithTheRowOffNothingIsAsked()
    {
        InAGame(coop: false, (engine, foe, barry) =>
        {
            var game = engine.Drive;
            game.StartTrainerBattle(foe, partner: barry);
            Assert.False(engine.AskingAboutAFriend);
            Assert.NotEqual(GameState.Dialogue, game.State);
            Until(engine, () => game.State == GameState.Battle, "the battle");
            Assert.False(game.Battle!.Core.FriendPartner);
            Assert.NotNull(game.Battle.Core.Partner);
        });
    }

    private const float Frame = 1f / 60f;

    private static void InAGame(bool coop, Action<GameEngine, NPC, Trainer> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "coop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true, Coop = coop });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Empoleon")!, 60));
            game.State = GameState.Overworld;
            game.TrainersLookOnArrival = false;

            var (_, barry, grunts) = Teams();
            var foe = new NPC { NpcType = "Youngster", TrainerData = grunts[0] };
            test(engine, foe, barry);
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

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, int most = 60 * 30)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
