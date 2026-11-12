using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC7: the cups' stadium off the Battle Frontier annex's lobby (<see cref="Cups"/>, <see cref="LeadersRound"/>,
/// <see cref="SparringRoom"/>): each cup refusing what its rules forbid, eight battles to a cup's prize with rentals that
/// meet it, a region's round only with its Hall of Fame and cleared at 100, the copies leaving the team untouched, and
/// the sparring room's trainers of its type with a record per type.
/// </summary>
public class CupTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, new Random(level));

    private static Func<int> Draws(int seed)
    {
        var rng = new Random(seed);
        return () => rng.Next(0x10000);
    }

    [Fact]
    public void EveryCupRefusesWhatItsRulesForbid()
    {
        var crest = Cups.Get("crest")!;
        Assert.Equal(CupRefusal.None, Cups.WhyNot(new[] { Mon("Garchomp", 50), Mon("Lucario", 50), Mon("Togekiss", 55) }, crest));
        Assert.Equal(CupRefusal.Level, Cups.WhyNot(new[] { Mon("Garchomp", 60), Mon("Lucario", 50), Mon("Togekiss", 50) }, crest));
        Assert.Equal(CupRefusal.Total, Cups.WhyNot(new[] { Mon("Garchomp", 55), Mon("Lucario", 55), Mon("Togekiss", 55) }, crest));
        Assert.Equal(CupRefusal.SameSpecies, Cups.WhyNot(new[] { Mon("Garchomp", 50), Mon("Garchomp", 50), Mon("Togekiss", 50) }, crest));
        Assert.Equal(CupRefusal.Count, Cups.WhyNot(new[] { Mon("Garchomp", 50) }, crest));
        Assert.Equal(CupRefusal.Banned, Cups.WhyNot(new[] { Mon("Mewtwo", 50), Mon("Lucario", 50), Mon("Togekiss", 50) }, crest));

        var sprout = Cups.Get("sprout")!;
        Assert.Equal(CupRefusal.None, Cups.WhyNot(new[] { Mon("Bulbasaur", 5), Mon("Charmander", 5), Mon("Gible", 5) }, sprout));
        Assert.Equal(CupRefusal.Evolved, Cups.WhyNot(new[] { Mon("Ivysaur", 5), Mon("Charmander", 5), Mon("Gible", 5) }, sprout));
        Assert.Equal(CupRefusal.Evolved, Cups.WhyNot(new[] { Mon("Tauros", 5), Mon("Charmander", 5), Mon("Gible", 5) }, sprout));
        Assert.Equal(CupRefusal.Level, Cups.WhyNot(new[] { Mon("Bulbasaur", 6), Mon("Charmander", 5), Mon("Gible", 5) }, sprout));

        var pebble = Cups.Get("pebble")!;
        Assert.Equal(CupRefusal.TooTall, Cups.WhyNot(new[] { Mon("Onix", 25), Mon("Pikachu", 25), Mon("Eevee", 25) }, pebble));
        Assert.Equal(CupRefusal.TooHeavy, Cups.WhyNot(new[] { Mon("Graveler", 25), Mon("Pikachu", 25), Mon("Eevee", 25) }, pebble));
        Assert.Equal(CupRefusal.None, Cups.WhyNot(new[] { Mon("Pikachu", 25), Mon("Eevee", 25), Mon("Clefairy", 30) }, pebble));

        // The sparring room's type
        Assert.Equal(CupRefusal.Type, Cups.WhyNot(new[] { Mon("Charizard", 50), Mon("Pikachu", 50), Mon("Arcanine", 50) }, Cups.Get("summit")!, PokemonType.Fire));
    }

    [Fact]
    public void RentalsAndFoesMeetTheirCupAtItsFoesLevel()
    {
        foreach (var cup in Cups.All)
        {
            int level = Cups.FoeLevel(cup);
            Assert.InRange(level, cup.MinLevel, cup.MaxLevel);
            if (cup.Total > 0) Assert.True(level * Cups.Entered <= cup.Total, cup.Id);
            var rentals = Cups.Rentals(cup, Draws(cup.Id.Length));
            Assert.Equal(CupRefusal.None, Cups.WhyNot(rentals, cup));
            for (int battle = 1; battle <= Cups.Battles; battle++)
                Assert.Equal(CupRefusal.None, Cups.WhyNot(Cups.Foe(cup, battle, Cups.DrawsFor(77, battle)).Pokemon, cup));
        }
        // A battle drawn again for the same run is the same trainer
        var crest = Cups.Get("crest")!;
        Assert.Equal(Cups.Foe(crest, 3, Cups.DrawsFor(9, 3)).Name, Cups.Foe(crest, 3, Cups.DrawsFor(9, 3)).Name);
    }

    private static HeadlessScriptHost Host(Party party, StoryState? story = null)
    {
        var host = new HeadlessScriptHost(story ?? new StoryState(), party, new Inventory())
        {
            Map = Rooms.Value("BattleFrontierStadium"), MapNamed = Rooms.Value, Rng = new Random(5)
        };
        return host;
    }

    private static void Run(HeadlessScriptHost host, string script, string person)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, "BattleFrontierStadium")!, host.Map!.Everyone.Single(n => n.Key == person));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
    }

    [Fact]
    public void TheCupOfTheUnevolvedIsWonWithRentalsInEightBattlesAndItsPrizeComesOnce()
    {
        var party = new Party();
        party.Add(Mon("Garchomp", 80));
        var host = Host(party);
        // Enter, the Sprout Cup, rentals, then on after each of the first seven wins
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(1);
        for (int n = 0; n < 7; n++) host.Answers.Enqueue(0);
        Run(host, "Cups", "cups_clerk");
        Assert.Equal(Cups.Battles, host.FrontierBattles.Count);
        foreach (var (team, foes, _, doubles) in host.FrontierBattles)
        {
            Assert.False(doubles);
            Assert.Equal(3, team.Count);
            Assert.All(team.Members, p => Assert.Equal(5, p.Level));
            Assert.DoesNotContain(party.Members[0], team.Members);
            Assert.All(foes.Single().Party.Members, p => Assert.Equal(5, p.Level));
        }
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Lucky Egg")!));
        Assert.Equal(Cups.Get("sprout")!.Points, host.Frontier.BattlePoints);
        Assert.Equal(0, host.Story.Var(Cups.CupVariable));

        // Won again: the points again, the prize not
        var again = Host(party, host.Story);
        foreach (int a in new[] { 0, 1, 1, 0, 0, 0, 0, 0, 0, 0 }) again.Answers.Enqueue(a);
        Run(again, "Cups", "cups_clerk");
        Assert.Equal(0, again.Bag.GetQuantity(ItemDatabase.Get("Lucky Egg")!));
        // The team never changed
        Assert.Equal(80, party.Members[0].Level);
    }

    [Fact]
    public void ACupLostEndsTheRunAndATeamTheRulesRefuseIsTurnedAway()
    {
        var party = new Party();
        party.Add(Mon("Garchomp", 80));
        party.Add(Mon("Lucario", 80));
        party.Add(Mon("Togekiss", 80));
        var host = Host(party);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0); // the Crest Cup
        host.Answers.Enqueue(0); // my own
        host.Entering.Enqueue(new[] { 0, 1, 2 });
        Run(host, "Cups", "cups_clerk");
        Assert.Empty(host.FrontierBattles);
        Assert.Contains(host.Transcript, l => l.Text.Contains("don't meet the cup's rules", StringComparison.Ordinal));

        var lost = Host(party);
        lost.Fight = _ => BattleOutcome.Lost;
        foreach (int a in new[] { 0, 4, 0 }) lost.Answers.Enqueue(a); // the Summit Cup with my own
        lost.Entering.Enqueue(new[] { 0, 1, 2 });
        Run(lost, "Cups", "cups_clerk");
        Assert.Single(lost.FrontierBattles);
        Assert.All(lost.FrontierBattles[0].Team.Members, p => Assert.Equal(80, p.Level));
        Assert.Equal(0, lost.Story.Var(Cups.CupVariable));
    }

    [Fact]
    public void ARegionsRoundOpensWithItsHallOfFameAndIsClearedAtLevelOneHundred()
    {
        Assert.Contains("Sinnoh", LeadersRound.Regions);
        var order = LeadersRound.TrainersOf("Sinnoh");
        Assert.Equal(13, order.Count);
        Assert.Equal("champion_cynthia_rematch", order[^1].Trainer);
        Assert.StartsWith("elite_four_", order[8].Trainer);
        Assert.Equal(order.Count, order.Select(o => o.Trainer).Distinct().Count());

        var party = new Party();
        party.Add(Mon("Garchomp", 70));
        party.Add(Mon("Lucario", 62));
        party.Add(Mon("Togekiss", 66));
        var shut = Host(party);
        shut.Answers.Enqueue(0);
        Run(shut, "Round", "round_clerk");
        Assert.Empty(shut.FrontierBattles);

        var story = new StoryState();
        story.Set("SinnohHallOfFame");
        var host = Host(party, story);
        host.Answers.Enqueue(0);
        host.Entering.Enqueue(new[] { 0, 1, 2 });
        for (int n = 0; n < order.Count - 1; n++) host.Answers.Enqueue(0);
        Run(host, "Round", "round_clerk");
        Assert.Equal(order.Count, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b =>
        {
            Assert.All(b.Team.Members, p => Assert.Equal(100, p.Level));
            Assert.All(b.Foes.Single().Party.Members, p => Assert.Equal(100, p.Level));
            Assert.Equal(3, b.Foes.Single().Party.Count);
        });
        Assert.Equal(1, story.Var(LeadersRound.ClearedVariable("Sinnoh")));
        Assert.Equal(70, party.Members[0].Level);
    }

    [Fact]
    public void TheSparringRoomsTrainersAreOfItsTypeAndEachTypeKeepsItsOwnRecord()
    {
        Assert.Equal(18, SparringRoom.Ways.Length);
        var party = new Party();
        party.Add(Mon("Charizard", 60));
        party.Add(Mon("Arcanine", 60));
        party.Add(Mon("Infernape", 60));
        var records = new FrontierRecords();
        var room = SparringRoom.Room("fire");
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1, 2 }, party, Draws(4));
        for (int battle = 1; battle <= 12; battle++)
        {
            var foe = challenge.Opponent(records);
            Assert.Equal(3, foe.Pokemon.Count);
            Assert.All(foe.Pokemon, p => Assert.True(p.HasType(PokemonType.Fire), p.Species.Name));
            Assert.All(foe.Pokemon, p => Assert.Equal(50, p.Level));
            Assert.Equal(SparringRoom.PointsFor(battle), challenge.Won(records, false).Points);
        }
        Assert.Equal(12, records.Facility(SparringRoom.Place, "fire", false).Streak);
        Assert.Null(records.PeekFacility(SparringRoom.Place, "water", false));
        Assert.Equal(0, FacilityPlaces.TrophyWays(SparringRoom.Place));
    }
}
