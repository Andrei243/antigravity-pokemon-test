using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC3: the partners' tree's famous trainers (<see cref="PartnersTree"/>, <c>Data/tree-cast.json</c>): the cast
/// only after their region's Hall of Fame, three of their own team at 50 at every tenth battle of a Super room, a Champion
/// of the regions finished at its 50th, scouted once beaten and then a partner of the Multi room with two of their own
/// team clear of the player's species and items; and the hall's lines for each.
/// </summary>
public class PartnersTreeTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private static Party Team(params string[] species)
    {
        var party = new Party();
        foreach (string s in species) party.Add(new Pokemon(PokemonDatabase.Get(s)!, 60, new Random(party.Count + 1)));
        return party;
    }

    private static Func<int> Draws(int seed)
    {
        var rng = new Random(seed);
        return () => rng.Next(0x10000);
    }

    private static Func<string, bool> Finished(params string[] regions) => flag => regions.Any(r => flag == PartnersTree.HallOfFameFlag(r));

    [Fact]
    public void EveryMemberOfTheCastIsATrainerOfTheTablesWithALookAndARegionAndAtLeastThreeToBring()
    {
        Assert.NotEmpty(PartnersTree.Cast);
        Assert.Equal(PartnersTree.Cast.Count, PartnersTree.Cast.Select(c => c.Trainer).Distinct().Count());
        var regions = RegionDatabase.All.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var member in PartnersTree.Cast)
        {
            var record = PartnersTree.RecordOf(member) ?? throw new Xunit.Sdk.XunitException($"{member.Trainer} is no trainer");
            Assert.True(CharacterStyles.Default.Has(member.Look), $"no look {member.Look}");
            Assert.Contains(member.Region, regions);
            Assert.Contains(member.Role, new[] { "champion", "elite", "leader", "kahuna" });
            var three = PartnersTree.Foe(member).Pokemon;
            Assert.Equal(3, three.Count);
            Assert.All(three, p => Assert.Equal(FrontierRules.Level, p.Level));
            Assert.Equal(3, three.Select(p => p.Species.Name).Distinct().Count());
        }
        // A Champion for every region of the cast, so a hard room's 50th always has one once that region is finished
        foreach (string region in PartnersTree.Cast.Select(c => c.Region).Distinct())
            Assert.Contains(PartnersTree.Cast, c => c.Region == region && c.Role == "champion");
    }

    [Fact]
    public void TheCastComesOnlyOnceTheirRegionsHallOfFameIsSet()
    {
        Assert.Empty(PartnersTree.Eligible(_ => false));
        var alola = PartnersTree.Eligible(Finished("Alola"));
        Assert.NotEmpty(alola);
        Assert.All(alola, c => Assert.Equal("Alola", c.Region));
        var two = PartnersTree.Eligible(Finished("Alola", "Sinnoh"));
        Assert.Contains(two, c => c.Region == "Sinnoh");
        Assert.DoesNotContain(two, c => c.Region == "Kanto");
    }

    [Fact]
    public void AFamousTrainerComesAtEveryTenthBattleOfASuperRoomAndAChampionAtItsFiftieth()
    {
        Assert.True(PartnersTree.FamousComes(10, 50));
        Assert.True(PartnersTree.FamousComes(40, 50));
        Assert.False(PartnersTree.FamousComes(50, 50));
        Assert.False(PartnersTree.FamousComes(11, 50));

        var room = BattleTree.Room("single", true);
        var records = new FrontierRecords();
        var party = Team("Decidueye", "Incineroar", "Primarina");
        var eligible = PartnersTree.Eligible(Finished("Alola", "Kalos"));
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1, 2 }, party, Draws(31)).WithCast(eligible);
        for (int battle = 1; battle <= 50; battle++)
        {
            var foe = challenge.Opponent(records);
            int cast = challenge.CastFoe;
            if (battle % 10 == 0)
            {
                Assert.True(cast > 0, $"battle {battle} brings nobody of the cast");
                var member = PartnersTree.Cast[cast - 1];
                Assert.Contains(member, eligible);
                Assert.Equal(battle == 50 ? "champion" : member.Role, member.Role);
                if (battle != 50) Assert.NotEqual("champion", member.Role);
                Assert.Equal(member.Trainer, foe.Dress!.Id);
                Assert.Equal(3, foe.Pokemon.Count);
                // Their own Pokémon, from their own team
                var own = TrainerDatabase.PartyOf(PartnersTree.RecordOf(member)!).Members.Select(p => p.Species.Name).ToHashSet();
                Assert.All(foe.Pokemon, p => Assert.Contains(p.Species.Name, own));
                var trainer = challenge.OpponentAsTrainer(records);
                Assert.Equal(member.Trainer, trainer.Id);
            }
            else Assert.Equal(0, cast);
            var won = challenge.Won(records, false);
            Assert.Equal(battle == 50, won.Host);
            if (battle == 50) Assert.True(won.Trophy);
        }
        // Every famous trainer beaten is scouted for the Multi room (the list keeps the most recent ten): the 50th's Champion last
        var scouted = BattleTree.MultiRecord(records).Scouted!;
        Assert.InRange(scouted.Count, 2, BattleTree.MostScouted);
        Assert.Equal("champion", PartnersTree.Cast[PartnersTree.NumberOf(scouted[^1].Cast) - 1].Role);
        Assert.InRange(scouted.Count(s => s.Cast != null), 1, 2);
    }

    [Fact]
    public void TheNormalRoomsAndTheStreakHouseAreUntouchedByTheCast()
    {
        var eligible = PartnersTree.Eligible(_ => true);
        foreach (var room in new[] { BattleTree.Room("single", false), StreakHouse.Room("single", true) })
        {
            var a = new FrontierRecords();
            var b = new FrontierRecords();
            var with = StreakChallenge.Begin(a, room, new[] { 0, 1, 2 }, Team("Decidueye", "Incineroar", "Primarina"), Draws(7)).WithCast(eligible);
            var without = StreakChallenge.Begin(b, room, new[] { 0, 1, 2 }, Team("Decidueye", "Incineroar", "Primarina"), Draws(7));
            for (int battle = 1; battle <= 25; battle++)
            {
                Assert.Equal(without.Opponent(b).ShownName, with.Opponent(a).ShownName);
                Assert.Equal(0, with.CastFoe);
                with.Won(a, false);
                without.Won(b, false);
            }
        }
    }

    [Fact]
    public void WithNobodyOfTheCastASuperRoomGoesOnAsItDid()
    {
        var room = BattleTree.Room("double", true);
        var a = new FrontierRecords();
        var b = new FrontierRecords();
        var party = Team("Decidueye", "Incineroar", "Primarina", "Lycanroc");
        var with = StreakChallenge.Begin(a, room, new[] { 0, 1, 2, 3 }, party, Draws(5)).WithCast(Array.Empty<TreeCastRecord>());
        var without = StreakChallenge.Begin(b, room, new[] { 0, 1, 2, 3 }, party, Draws(5));
        for (int battle = 1; battle <= 12; battle++)
        {
            Assert.Equal(without.Opponent(b).ShownName, with.Opponent(a).ShownName);
            with.Won(a, false);
            without.Won(b, false);
        }
    }

    [Fact]
    public void AFamousPartnerBringsTwoOfTheirOwnTeamClearOfThePlayersSpeciesAndItems()
    {
        var cynthia = PartnersTree.Cast.Single(c => c.Trainer == "champion_cynthia_rematch");
        var own = TrainerDatabase.PartyOf(PartnersTree.RecordOf(cynthia)!).Members;
        var barred = own.OrderByDescending(p => p.Level).First();
        var items = own.Where(p => p.HeldItem != null).Select(p => p.HeldItem!.Name).Take(1).ToList();
        var partner = PartnersTree.Partner(cynthia, new[] { barred.Species.Name }, items);
        Assert.Equal(2, partner.Pokemon.Count);
        Assert.DoesNotContain(partner.Pokemon, p => p.Species.Name == barred.Species.Name);
        Assert.DoesNotContain(partner.Pokemon, p => p.HeldItem != null && items.Contains(p.HeldItem.Name));
        Assert.All(partner.Pokemon, p => Assert.Equal(FrontierRules.Level, p.Level));
        Assert.Equal(2, partner.Pokemon.Select(p => p.Species.Name).Distinct().Count());
    }

    [Fact]
    public void AFamousTrainerScoutedPartnersTheMultiRoomOnlyOnceBeaten()
    {
        var records = new FrontierRecords();
        Assert.Null(BattleTree.MultiRecord(records).Scouted);
        var diantha = PartnersTree.Cast.Single(c => c.Trainer == "kalos_diantha_rematch");
        BattleTree.ScoutCast(records, diantha);
        var multi = BattleTree.MultiRecord(records);
        Assert.Single(multi.Scouted!);
        multi.Partner = 1;
        var party = Team("Decidueye", "Incineroar");
        var challenge = StreakChallenge.Begin(records, BattleTree.Room("multi", false), new[] { 0, 1 }, party, Draws(3));
        Assert.Equal(PartnersTree.NumberOf("kalos_diantha_rematch"), challenge.CastPartner);
        Assert.Equal("kalos_diantha_rematch", challenge.Partner!.Dress!.Id);
        Assert.Equal(2, challenge.Partner.Pokemon.Count);
        Assert.DoesNotContain(challenge.Partner.Pokemon, p => p.Species.Name is "Decidueye" or "Incineroar");
        var trainer = challenge.PartnerAsTrainer()!;
        Assert.Equal(2, trainer.Party.Count);
        // Scouted again, she is kept once
        BattleTree.ScoutCast(records, diantha);
        Assert.Single(BattleTree.MultiRecord(records).Scouted!);
    }

    [Fact]
    public void AFamousPartnersMultiBattleIsFoughtThroughTheHallsScriptAndEveryLineIsTheirs()
    {
        var party = Team("Decidueye", "Incineroar");
        var story = new StoryState();
        story.Set("AlolaHallOfFame");
        var records = new FrontierRecords();
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Alola, Map = Rooms.Value("PoniGauntlet_BattleTreeHall"), MapNamed = Rooms.Value, Money = 50000, Rng = new Random(5) };
        BattleTree.ScoutCast(host.Frontier, PartnersTree.Cast.Single(c => c.Trainer == "alola_hala_rematch"));
        BattleTree.MultiRecord(host.Frontier).Partner = 1;
        host.Entering.Enqueue(new[] { 0, 1 });
        var library = ScriptLibrary.Default;
        var desk = new ScriptRunner(library, host);
        desk.Start(ScriptParser.Parse("PoniGauntlet_BattleTree", "script T\n  facility tree way multi\n  facility tree choose\n  facility tree begin\n  end\n").Single());
        desk.RunToEnd();
        Assert.Empty(host.Problems);
        var hall = new ScriptRunner(library, host);
        host.Answers.Enqueue(2);
        hall.Start(library.Find("OnEnter", "PoniGauntlet_BattleTreeHall")!);
        hall.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Transcript, l => l.Speaker == "Hala" && l.Text.Contains("drum", StringComparison.Ordinal));
        var (_, _, partner, doubles) = host.FrontierBattles[0];
        Assert.True(doubles);
        Assert.Equal("alola_hala_rematch", partner!.Id);
    }

    [Fact]
    public void EveryMemberOfTheCastHasALineInTheHallAndNoLineNamesASpecies()
    {
        var script = File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "scripts", "PoniGauntlet_BattleTreeHall.txt"));
        for (int n = 1; n <= PartnersTree.Cast.Count; n++)
        {
            Assert.Contains($"if var VAR_ALOLA_TREE_CAST == {n} goto Cast{n}\n", script.Replace("\r", ""));
            Assert.Contains($"label Cast{n}\n  speaker \"{PartnersTree.RecordOf(PartnersTree.Cast[n - 1])!.Name}\"", script.Replace("\r", ""));
        }
        var lines = script.Split('\n').Where(l => l.TrimStart().StartsWith("say ", StringComparison.Ordinal)).ToList();
        foreach (var species in PokemonDatabase.GetAll().Select(s => s.Name).Where(n => n.Length > 3))
            Assert.DoesNotContain(lines, l => System.Text.RegularExpressions.Regex.IsMatch(l, $@"\b{System.Text.RegularExpressions.Regex.Escape(species)}\b"));
    }
}
