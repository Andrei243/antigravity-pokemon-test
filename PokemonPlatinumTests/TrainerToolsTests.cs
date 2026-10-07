using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R12's trainer tools: the people who trade, the Hall of Fame's records, the Trainer Card's colour and
/// score, the Journal and the Vs. Seeker with its rematches. Each number is the original's, named in a comment.
/// </summary>
public class TrainerToolsTests
{
    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static readonly DateTime Day = new(2026, 10, 7);

    // ------------------------------------------------------------------ trades

    [Fact]
    public void ATradeGivesItsOwnPokemonAtTheLevelOfTheOneGiven()
    {
        // NPCTrade_CreateMon: Kazza the Abra, at the Machop's level, its IVs, personality, item and first trainer the table's
        var machop = Mon("Machop", 14);
        var party = PartyOf(Mon("Turtwig", 15), machop);
        var abra = NpcTrades.Trade(NpcTrades.Get("kazza")!, party, 1, Day)!;

        Assert.Same(abra, party.Members[1]);
        Assert.DoesNotContain(machop, party.Members);
        Assert.Equal(("Abra", "Kazza", 14), (abra.Species.Name, abra.Nickname, abra.Level));
        Assert.Equal((Nature.Quiet, "Synchronize", Gender.Male, 142u), (abra.Nature, abra.AbilityName, abra.Gender, abra.Personality));
        Assert.Equal((15, 15, 15, 20, 25, 25), (abra.IvHP, abra.IvAttack, abra.IvDefense, abra.IvSpeed, abra.IvSpAttack, abra.IvSpDefense));
        Assert.Equal("Oran Berry", abra.HeldItem?.Name);
        Assert.Equal(new TrainerMark("Hilary", 25643, PlayerLook.Girl), abra.OriginalTrainer);
        Assert.Equal((NpcTrade.MetBy, 14, Day), (abra.MetLocation, abra.MetLevel, abra.MetDate));
        Assert.Equal(abra.Species.BaseFriendship, abra.Friendship);
        Assert.Equal(abra.MaxHP, abra.CurrentHP);

        // Not the species asked for: nothing changes
        Assert.Null(NpcTrades.Trade(NpcTrades.Get("kazza")!, party, 0, Day));
        Assert.Equal("Turtwig", party.Members[0].Species.Name);

        // The Meister's Magikarp comes from a game in another language
        Assert.Equal("German", NpcTrades.Make(NpcTrades.Get("foppa")!, 20, Day).Language);
    }

    [Fact]
    public void AScriptTradesThePokemonChosen()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script T
              choosepokemon
              if var RESULT == 255 end
              trade charap
              if var RESULT == 0 goto Refused
              setflag FLAG_TRADED_FOR_CHARAP_CHATOT
              end
            label Refused
              setflag FLAG_REFUSED
              end
            """));
        foreach (var (choice, traded) in new[] { (1, true), (0, false), (255, false) })
        {
            var host = new HeadlessScriptHost(party: PartyOf(Mon("Turtwig", 9), Mon("Buizel", 12))) { PokemonChoice = choice };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.All.Single());
            runner.RunToEnd();
            Assert.Equal(traded, host.Story.Has("FLAG_TRADED_FOR_CHARAP_CHATOT"));
            Assert.Equal(traded ? "Chatot" : "Buizel", host.Party.Members[1].Species.Name);
            Assert.Equal(choice == 0, host.Story.Has("FLAG_REFUSED"));
        }
    }

    // ------------------------------------------------------------------ EXP

    [Fact]
    public void APokemonFromAnotherGamesLanguageGainsMoreThanAnyTradedOne()
    {
        // BtlCmd_CalcExpGain: × 170 / 100 for another language, × 150 / 100 for another trainer's
        Assert.Equal(170, Formulas.ExpFor(100, false, false, traded: true, foreign: true));
        Assert.Equal(150, Formulas.ExpFor(100, false, false, traded: true));
    }

    // ------------------------------------------------------------------ Hall of Fame and the card

    [Fact]
    public void TheHallOfFameKeepsTheLastThirtyTeamsNewestFirst()
    {
        var hall = new HallOfFame();
        var party = PartyOf(Mon("Infernape", 50), Mon("Staraptor", 48));
        for (int i = 0; i < 32; i++) hall.Enter(party, _ => ("Lucas", 12345), Day.AddDays(i));

        Assert.Equal(32, hall.Total);
        Assert.Equal(HallOfFame.Kept, hall.Entries.Count);
        var (number, newest) = hall.NewestFirst().First();
        Assert.Equal((32, Day.AddDays(31)), (number, newest.Date));
        Assert.Equal(Day, hall.Debut);
        Assert.Equal(new[] { "Infernape", "Staraptor" }, newest.Team.Select(m => m.Species));
        Assert.Equal(("Lucas", 12345), (newest.Team[0].TrainerName, newest.Team[0].TrainerId));

        // The screen goes to older teams to the right of the newest, and on through a team's Pokémon
        var screen = new HallOfFameScreen();
        screen.Open();
        screen.Move(1, 0, hall);
        Assert.Equal(31, screen.Shown(hall)!.Value.Number);
        screen.Move(0, 1, hall);
        screen.Move(0, 1, hall);
        Assert.Equal((2, 0), (screen.Entry, screen.Member));
    }

    [Fact]
    public void TheCardsColourCountsWhatThePlayerHasDone()
    {
        // TrainerCase_CalculateTrainerCardLevel: a step for each of five things; the National Pokédex wants 482 caught
        Assert.Equal(0, TrainerCardRules.Level(false, 0, 0, false, 0));
        Assert.Equal(1, TrainerCardRules.Level(true, 481, 99, false, 49));
        Assert.Equal(2, TrainerCardRules.Level(true, 482, 0, false, 0));
        Assert.Equal(5, TrainerCardRules.Level(true, 493, 100, true, 50));
        Assert.Equal(TrainerCardRules.CardColour.NoPokedex, TrainerCardRules.Colour(false, 3));
        Assert.Equal(TrainerCardRules.CardColour.Normal, TrainerCardRules.Colour(true, 0));
        Assert.Equal(TrainerCardRules.CardColour.Black, TrainerCardRules.Colour(true, 5));

        // The eleven the goal does without
        Assert.Equal(482, TrainerCardRules.NationalCaught(Enumerable.Range(1, 493)));
        Assert.Equal(99_999_999, TrainerScore.Add(99_999_990, 35));
    }

    // ------------------------------------------------------------------ the Journal

    [Fact]
    public void TheJournalKeepsTenDaysAndFourLinesADay()
    {
        var journal = new Journal();
        for (int i = 0; i < 12; i++) journal.TakenUp(Day.AddDays(i), $"Town {i}");
        Assert.Equal(Journal.Pages, journal.All.Count);
        Assert.Equal("Town 11", journal.Today!.Place);

        // The same day goes on with the same page
        journal.TakenUp(Day.AddDays(11), "Elsewhere");
        Assert.Equal("Town 11", journal.Today.Place);

        // Four lines; the same line twice in a row once; the oldest goes
        foreach (var town in new[] { "Jubilife City", "Jubilife City", "Oreburgh City", "Floaroma Town", "Eterna City", "Hearthome City" })
            journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, town));
        Assert.Equal(new[] { "Oreburgh City", "Floaroma Town", "Eterna City", "Hearthome City" }, journal.Today.Events.Select(e => e.Subject));

        // A Gym too tough is forgotten once its Leader is beaten
        journal.Tell(new JournalEvent(JournalEventKind.GymWasTooTough, "Oreburgh"));
        journal.Tell(new JournalEvent(JournalEventKind.BeatGymLeader, "Oreburgh"));
        Assert.DoesNotContain(journal.Today.Events, e => e.Kind == JournalEventKind.GymWasTooTough);

        // Wild Pokémon knocked out are told of from the fifth in one place
        for (int i = 0; i < 4; i++) journal.Defeated("Bidoof", "Route 201");
        Assert.Null(journal.Today.Pokemon);
        journal.Defeated("Starly", "Route 201");
        Assert.Equal(new JournalPokemon(false, "Starly", "Route 201"), journal.Today.Pokemon);

        // It opens by itself on a game left two days or more
        Assert.False(journal.OpensOnContinue(Day.AddDays(12)));
        Assert.True(journal.OpensOnContinue(Day.AddDays(13)));
        Assert.Contains("Arrived in Hearthome City.", Journal.Lines(journal.Today));
        Assert.Contains("Battled lots of wild Pokémon on Route 201, Starly among them.", Journal.Lines(journal.Today));
    }

    [Theory]
    [InlineData("Route 203", "on Route 203")]
    [InlineData("Mt. Coronet", "on Mt. Coronet")]
    [InlineData("Iron Island", "on Iron Island")]
    [InlineData("Lake Verity", "at Lake Verity")]
    [InlineData("Valor Lakefront", "at Valor Lakefront")]
    [InlineData("Spear Pillar", "at Spear Pillar")]
    [InlineData("Valley Windworks", "at the Valley Windworks")]
    [InlineData("Great Marsh", "in the Great Marsh")]
    [InlineData("Jubilife City", "in Jubilife City")]
    [InlineData("Eterna Forest", "in Eterna Forest")]
    public void APlaceIsNamedAsASentenceSaysIt(string place, string said) => Assert.Equal(said, PlaceWords.In(place));

    [Fact]
    public void AVisitToAMartIsToldByWhatCameOfIt()
    {
        // shop_menu.c: bought and sold; bought plenty; sold plenty; bought; sold
        Assert.Equal(JournalEventKind.BusinessAtMart, GameEngine.ShopLine(1, 1));
        Assert.Equal(JournalEventKind.LotsOfShopping, GameEngine.ShopLine(2, 0));
        Assert.Equal(JournalEventKind.SoldALot, GameEngine.ShopLine(0, 3));
        Assert.Equal(JournalEventKind.ShoppedAtMart, GameEngine.ShopLine(1, 0));
        Assert.Equal(JournalEventKind.SoldALittle, GameEngine.ShopLine(0, 1));
        Assert.Null(GameEngine.ShopLine(0, 0));
    }

    [Fact]
    public void AnItemBallPickedUpIsALineOfTheJournal()
    {
        var map = new Map(5, 5) { Name = "Test" };
        var ball = new NPC { Name = "Potion", NpcType = NPC.ItemBallType, GridX = 2, GridY = 2, Item = "Potion", ItemCount = 1, HiddenBy = "FLAG_BALL" };
        map.NPCs.Add(ball);
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.ItemBall)!, ball);
        runner.RunToEnd();
        Assert.Contains(host.Journal.Today!.Events, e => e.Kind == JournalEventKind.ItemWasObtained && e.Subject == "Potion");
    }

    // ------------------------------------------------------------------ the Vs. Seeker

    private static (Map Map, NPC Beaten, NPC Fresh) Trainers(StoryState story)
    {
        var map = new Map(40, 40) { Name = "Sinnoh" };
        NPC Trainer(string id, int x, int y) => new()
        {
            Name = id, GridX = x, GridY = y, IsTrainer = true,
            TrainerData = new Trainer { Id = id, Name = id, TrainerClass = "Youngster" }
        };
        var beaten = Trainer("youngster_tristan", 20, 15);
        var fresh = Trainer("lass_natalie", 25, 20);
        map.NPCs.Add(beaten);
        map.NPCs.Add(fresh);
        // Out of range: eight tiles to the right
        map.NPCs.Add(Trainer("camper_zackary", 28, 20));
        story.Defeat("youngster_tristan");
        story.Defeat("camper_zackary");
        return (map, beaten, fresh);
    }

    /// <summary>A generator whose draws are the numbers given, in order.</summary>
    private sealed class Draws(params int[] values) : Random
    {
        private int next;
        public override int Next(int maxValue) => values[next++ % values.Length] % maxValue;
    }

    [Fact]
    public void TheVsSeekerNeedsAFullBatteryAndFindsTheTrainersBeatenWithinAScreen()
    {
        var story = new StoryState();
        var (map, beaten, fresh) = Trainers(story);

        // VsSeeker_UpdateStepCount: a step charges it while it is in the bag, to a hundred
        Assert.Equal(VsSeekerResult.NotCharged, VsSeeker.Use(map, 20, 20, true, story, new Draws(0), out _, out _));
        for (int i = 0; i < 99; i++) VsSeeker.Step(story, inBag: true);
        VsSeeker.Step(story, inBag: false);
        Assert.Equal(99, story.Var(VsSeeker.Battery));
        VsSeeker.Step(story, inBag: true);
        Assert.Equal(VsSeeker.FullBattery, story.Var(VsSeeker.Battery));

        // Not on a map that isn't the open one
        Assert.Equal(VsSeekerResult.NoTrainers, VsSeeker.Use(map, 20, 20, false, story, new Draws(0), out _, out _));

        // In range: seven tiles left, right and up, six down; a roll under 50 for each one beaten
        Assert.Equal(VsSeekerResult.Used, VsSeeker.Use(map, 20, 20, true, story, new Draws(10), out var ready, out var notYet));
        Assert.Equal(new[] { beaten }, ready);
        Assert.Equal(new[] { fresh }, notYet);
        Assert.True(beaten.ReadyForRematch);
        Assert.Equal(0, story.Var(VsSeeker.Battery));

        // A hundred steps and the rematches are over
        for (int i = 0; i < 99; i++) Assert.False(VsSeeker.Step(story, inBag: true));
        Assert.True(VsSeeker.Step(story, inBag: true));
        Assert.False(story.Has(VsSeeker.Used));
    }

    [Fact]
    public void ARematchBringsTheTeamOfTheLevelTheStoryHasReached()
    {
        // gVsSeekerRematchData: Camper Zackary's row is R_1, R_2, -, R_3
        var zackary = TrainerDatabase.Get("camper_zackary")!;
        Assert.Equal(new[] { "camper_zackary_rematch_1", "camper_zackary_rematch_2", null, "camper_zackary_rematch_3" }, zackary.Rematches);
        var story = new StoryState();
        story.Defeat("camper_zackary");
        // Level 1 not unlocked yet: his own team again
        Assert.Equal("camper_zackary", VsSeeker.RematchTeam(zackary, story));
        story.Set(VsSeeker.LevelFlag(1));
        Assert.Equal("camper_zackary_rematch_1", VsSeeker.RematchTeam(zackary, story));
        story.Defeat("camper_zackary_rematch_1");
        // Level 2 wanted but locked: back to the highest below it with a team
        Assert.Equal("camper_zackary_rematch_1", VsSeeker.RematchTeam(zackary, story));
        story.Set(VsSeeker.LevelFlag(2));
        story.Defeat("camper_zackary_rematch_2");
        // Level 3 has no team of its own: level 4 is the next, held back to 2 until it is unlocked
        Assert.Equal("camper_zackary_rematch_2", VsSeeker.RematchTeam(zackary, story));
        story.Set(VsSeeker.LevelFlag(4));
        Assert.Equal("camper_zackary_rematch_3", VsSeeker.RematchTeam(zackary, story));
        // Every one beaten: the last one, for ever
        story.Defeat("camper_zackary_rematch_3");
        Assert.Equal("camper_zackary_rematch_3", VsSeeker.RematchTeam(zackary, story));

        // A trainer with no team of its own for later levels battles with the same one
        Assert.Equal("youngster_logan", VsSeeker.RematchTeam(TrainerDatabase.Get("youngster_logan")!, story));
        Assert.Equal(240, TrainerDatabase.All.Count(t => t.Rematches != null));
    }

    [Fact]
    public void ATrainerWaitingForARematchBattlesWithTheRematchTeamOnce()
    {
        var story = new StoryState();
        story.Set(VsSeeker.LevelFlag(1));
        var (map, beaten, _) = Trainers(story);
        beaten.ReadyForRematch = true;
        var host = new HeadlessScriptHost(story, PartyOf(Mon("Torterra", 40))) { Map = map };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Trainer)!, beaten);
        runner.RunToEnd();

        Assert.Contains("battle youngster_tristan_rematch_1 Won", host.Log);
        Assert.False(beaten.ReadyForRematch);
        Assert.Equal("youngster_tristan", beaten.TrainerData!.Id);
    }
}
