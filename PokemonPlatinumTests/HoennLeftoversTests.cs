using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's leftovers of 2026-11-12 (plan 21; rulings, "The Brains of the Palace, the Arena and the Dome, the roulette's
/// service day and the Trainer Hill's clerk"): Emerald's three Brains that had none here, the right roulette table's
/// service day and the record of wins in a row, the Trainer Hill's clerk and the Hill's times mixed in from other saves.
/// </summary>
[Collection("MapDatabase")]
public class HoennLeftoversTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Party Three()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60, new Random(60)));
        party.Add(new Pokemon(PokemonDatabase.Get("Lucario")!, 55, new Random(55)));
        party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 52, new Random(52)));
        return party;
    }

    // ------------------------------------------------------------------ when the Brains come

    [Fact]
    public void TheBrainsComeWhenEmeraldsTableSays()
    {
        // sFrontierBrainStreakAppearances: the Palace {21, 42, 21}, the Arena {28, 56, 28}, the Dome {4, 9, 5}
        Assert.Equal(new[] { 0, 1, 0, 2, 0, 2, 2 }, new[] { 14, 21, 35, 42, 49, 63, 84 }.Select(EmeraldBrains.PalaceDue));
        Assert.Equal(new[] { 0, 1, 0, 2, 0, 2 }, new[] { 21, 28, 49, 56, 70, 84 }.Select(EmeraldBrains.ArenaDue));
        Assert.Equal(new[] { 0, 1, 0, 2, 0, 2, 2 }, new[] { 3, 4, 8, 9, 10, 14, 19 }.Select(EmeraldBrains.DomeDue));
    }

    [Theory]
    [InlineData(AnnexVenue.Palace, 14, "palace_maven_spenser", "Spenser")]
    [InlineData(AnnexVenue.Palace, 35, "palace_maven_spenser_gold", "Spenser")]
    [InlineData(AnnexVenue.Arena, 21, "arena_tycoon_greta", "Greta")]
    [InlineData(AnnexVenue.Arena, 49, "arena_tycoon_greta_gold", "Greta")]
    public void AVenuesBrainIsTheRoundsSeventhAndTheRestAreDrawnAsEver(AnnexVenue venue, int streak, string id, string name)
    {
        var records = new FrontierRecords();
        BattleVenueChallenge.RecordOf(records, venue).Streak = streak;
        var round = BattleVenueChallenge.Begin(records, venue, new[] { 0, 1, 2 }, Draws(7));
        Assert.True(round.BrainDue > 0);
        var brain = round.Foes[^1];
        Assert.Equal((id, name), (brain.Dress!.Id, brain.ShownName));
        Assert.Equal(3, brain.Pokemon.Count);
        Assert.All(brain.Pokemon, p => Assert.Equal(50, p.Level));

        // The same round one battle short of the Brain draws the same six before him (the seventh is drawn and set aside)
        var other = new FrontierRecords();
        BattleVenueChallenge.RecordOf(other, venue).Streak = streak;
        var venueWithout = venue == AnnexVenue.Palace ? AnnexVenue.Arena : AnnexVenue.Palace;
        BattleVenueChallenge.RecordOf(other, venueWithout).Streak = streak;
        var plain = BattleVenueChallenge.Begin(other, venueWithout, new[] { 0, 1, 2 }, Draws(7));
        Assert.Equal(0, plain.BrainDue);
        Assert.Equal(plain.Foes.Take(6).Select(f => f.Number), round.Foes.Take(6).Select(f => f.Number));
        Assert.Null(plain.Foes[^1].Dress);

        // The trainer the battle takes is the Brain, in their look
        for (int i = 0; i < 6; i++) round.Won(records);
        Assert.True(round.BrainNext);
        var trainer = round.OpponentAsTrainer();
        Assert.Equal((id, name), (trainer.Id, trainer.Name));
    }

    [Fact]
    public void TheDomeAceSitsInTheOtherHalfAndWinsEveryMatchButThePlayers()
    {
        var records = new FrontierRecords();
        records.Dome.Streak = 4;
        var dome = BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(1, dome.BrainDue);
        int ace = Enumerable.Range(0, BattleDomeChallenge.Entrants).Single(e => EmeraldBrains.IsBrain(dome.Field[e].Opponent));
        int half(int entrant) => dome.Bracket.Seats.IndexOf(entrant) / (BattleDomeChallenge.Entrants / 2);
        Assert.NotEqual(half(BattleDomeChallenge.Player), half(ace));
        Assert.Equal("Dome Ace Tucker", dome.Field[ace].Title);

        // Every other match of every round is fought headless: he is still in at the final
        var party = Three();
        for (int round = 0; round < 3; round++) Assert.False(dome.Won(records, party, Draws(100 + round)));
        Assert.Equal(ace, dome.Opponent is { } o ? dome.Field.ToList().IndexOf(o) : -1);
        Assert.Equal("dome_ace_tucker", dome.OpponentAsTrainer(party).Id);

        // A tournament with no Ace in it is drawn as before
        var plain = BattleDomeChallenge.Begin(new FrontierRecords(), new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(0, plain.BrainDue);
        Assert.DoesNotContain(plain.Field, e => EmeraldBrains.IsBrain(e.Opponent));
    }

    [Fact]
    public void ThePalacesHallPlaysSpensersBattleAtTheRoundsEnd()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Frontier.Palace.Streak = 14;
        host.Venue = BattleVenueChallenge.Begin(host.Frontier, AnnexVenue.Palace, new[] { 0, 1, 2 }, Draws(3));
        host.Map = Room("BattlePalace");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattlePalace")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Palace Maven himself"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("truest voice"));
        Assert.Equal(21, host.Frontier.Palace.Streak);
    }

    [Fact]
    public void TheDomesFinalAgainstTheAceIsAnnounced()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Frontier.Dome.Streak = 9;
        host.Dome = BattleDomeChallenge.Begin(host.Frontier, new[] { 0, 1, 2 }, Draws(5));
        host.Map = Room("BattleDome");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattleDome")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(4, host.FrontierBattles.Count);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Dome Ace himself"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("champion of the Battle Dome"));
    }

    [Fact]
    public void EveryBrainHasATeamALookAndASongOfAnAce()
    {
        foreach (var id in new[] { EmeraldBrains.SpenserId, EmeraldBrains.SpenserGoldId, EmeraldBrains.GretaId, EmeraldBrains.GretaGoldId,
                     EmeraldBrains.TuckerId, EmeraldBrains.TuckerGoldId })
        {
            var record = TrainerDatabase.Get(id);
            Assert.NotNull(record);
            Assert.Equal(3, record!.Party.Count);
        }
        foreach (var (cls, look) in new[] { ("Palace Maven", "spenser"), ("Arena Tycoon", "greta"), ("Dome Ace", "tucker") })
        {
            Assert.True(CharacterStyles.Default.Has(look), look);
            Assert.Equal(look, CharacterStyles.Default.InBattle(cls, "Trainer"));
        }
    }

    // ------------------------------------------------------------------ the roulette

    [Fact]
    public void TheRightTableOnItsServiceDayTakesSixAndPaysOnSix()
    {
        var wheel = new RouletteWheel(RouletteWheel.ServiceDayBet, 100, 12, false, false, new Random(1));
        Assert.True(wheel.RightTable);
        Assert.True(wheel.ServiceDay);
        Assert.Equal(6, wheel.MinBet);
        Assert.Equal(6, wheel.Board.MinBet);
        var plain = new RouletteWheel(3, 100, 12, false, false, new Random(1));
        Assert.False(plain.ServiceDay);
        Assert.Equal(3, plain.MinBet);
    }

    [Fact]
    public void TheBestRunOfWinsIsKeptAndToldOnlyWhenBeaten()
    {
        Assert.Equal(4, RouletteWheel.BestRun(4, null));
        Assert.Equal(4, RouletteWheel.BestRun(4, new RouletteWheel(1, 10, 12, false, false, new Random(1))));
        var host = new HeadlessScriptHost();
        host.Story.SetVar(RouletteWheel.BestRunVariable, 5);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("RouletteRecord", "mauville_city_game_corner")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Transcript);
    }

    [Fact]
    public void OnSaturdaysTheRightTableAsksForSix()
    {
        var host = new HeadlessScriptHost { Weekday = DayOfWeek.Saturday };
        host.Bag.AddItem(ItemDatabase.Get("Coin Case")!, 1);
        host.Pastimes.Coins.Set(50);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Roulette2", "mauville_city_game_corner")!, null);
        runner.RunToEnd();
        Assert.Contains(host.Asked, q => q.Question.Contains("6 Coins"));
        Assert.Contains(host.Log, l => l == "pastime wheel 6");
    }

    // ------------------------------------------------------------------ the Exchange Service Corner's dolls

    [Fact]
    public void TheIslandsDollClerksTradeBattlePointsForDecorations()
    {
        var host = new HeadlessScriptHost();
        host.Frontier.GivePoints(200);
        host.Answers.Enqueue(0);
        var clerk = new NPC { Name = "Clerk", Key = "teala_2" };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("DecorClerk2", "battle_frontier_exchange_service_corner")!, clerk);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(72, host.Frontier.BattlePoints);
        Assert.Contains(host.Decorations, d => d.Kind == DecorationDatabase.Get("Lapras Doll")!.Id);

        // Too few points: nothing is traded
        host.Answers.Enqueue(2);
        runner.Start(ScriptLibrary.Default.Find("DecorClerk2", "battle_frontier_exchange_service_corner")!, clerk);
        runner.RunToEnd();
        Assert.Equal(72, host.Frontier.BattlePoints);
        Assert.Contains(host.Transcript, l => l.Text.Contains("enough Battle Points"));
    }

    [Fact]
    public void TheLoungesTutorsTeachForBattlePointsOnceTheMoveIsLearned()
    {
        foreach (var (move, points, kin) in MoveTutors.FrontierLounge.Values.SelectMany(m => m))
        {
            Assert.NotNull(MoveDatabase.Get(move));
            Assert.NotNull(MoveDatabase.Get(kin));
            Assert.InRange(points, 16, 48);
        }
        var host = new HeadlessScriptHost();
        var chansey = new Pokemon(PokemonDatabase.Get("Chansey")!, 50, new Random(5));
        chansey.Moves.RemoveAll(m => m.Data.Name == "Soft-Boiled");
        while (chansey.Moves.Count > 2) chansey.Moves.RemoveAt(chansey.Moves.Count - 1);
        host.Party.Add(chansey);
        host.Frontier.GivePoints(20);
        var teachable = MoveTutors.FrontierTeachable(chansey, "left");
        int softBoiled = teachable.FindIndex(m => m.Move == "Soft-Boiled");
        Assert.True(softBoiled >= 0);
        host.Answers.Enqueue(0);          // yes
        host.Answers.Enqueue(softBoiled); // the move
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("LeftTutor", "battle_frontier_lounge_7")!, new NPC { Name = "Tutor", Key = "expert_f" });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.True(chansey.Knows("Soft-Boiled"));
        Assert.Equal(4, host.Frontier.BattlePoints);
    }

    [Fact]
    public void TheNatureGirlReadsAPokemonAsThePalaceDoes()
    {
        Assert.Equal(PalaceGroup.Attack, PalaceNatures.Leaning(Nature.Brave, low: false));
        var host = new HeadlessScriptHost();
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 20, new Random(3)) { Nature = Nature.Brave };
        host.Party.Add(p);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("NatureGirl", "battle_frontier_lounge_5")!, new NPC { Name = "Little Girl", Key = "little_girl" });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Transcript, l => l.Text.Contains("loves to attack"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("when it's hurt"));
    }

    // ------------------------------------------------------------------ the Trainer Hill

    [Fact]
    public void TheHillsClerkSellsEmeraldsTwoStocks()
    {
        Assert.Equal(11, MartDatabase.Specialties["trainer_hill"].Count);
        Assert.Equal(12, MartDatabase.Specialties["trainer_hill_champion"].Count);
        foreach (var item in MartDatabase.Specialties["trainer_hill"].Concat(MartDatabase.Specialties["trainer_hill_champion"]))
            Assert.NotNull(ItemDatabase.Get(item));
        var lobby = Room("TrainerHillLobby");
        var clerk = Assert.Single(lobby.NPCs, n => n.Key == "hill_clerk");
        Assert.True(lobby.IsSolid(clerk.GridX, clerk.GridY - 1));
        Assert.True(lobby.IsWalkable(clerk.GridX, clerk.GridY + 1));
    }

    [Fact]
    public void AnotherGamesHillTimesAreReadOutOnTheBoard()
    {
        var other = new SaveData { PlayerName = "Mira", TrainerId = 77, Started = new DateTime(2026, 2, 2) };
        other.StoryVariables[TrainerHill.Vars.Best(TrainerHillMode.Variety)] = 3600 * 13;
        var record = RecordMix.RecordOf(other, new DateTime(2026, 11, 12));
        Assert.Equal(new[] { 0, 3600 * 13, 0, 0 }, record.HillBest);
        Assert.Null(RecordMix.RecordOf(new SaveData { PlayerName = "Bo", TrainerId = 78 }, DateTime.Today).HillBest);

        var host = new HeadlessScriptHost();
        host.MixedRecords!.Records.Add(record);
        var gentleman = new NPC { Name = "Gentleman", Key = "hill_records" };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Records", "TrainerHillLobby")!, gentleman);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Mira, 13 min."));
    }
}
