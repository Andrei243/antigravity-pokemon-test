using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Lockwork's story (plan 44 · SD3, SD4; docs/mechanics/rulings.md, "Locked hearts"): the five cells' thirty locked
/// Pokémon and the head's, who stands where and when, the Reclaimer from Looker, the spring's rite, every lock reclaimed
/// in its trainer's battle, and the machine stopped.
/// </summary>
[Collection("Shared clocks and dice")]
public class HeartsStoryTests
{
    private static readonly string[] Regions = { "sinnoh", "kanto", "johto", "hoenn", "unova" };

    private static readonly Dictionary<string, string[]> Locked = new()
    {
        ["sinnoh"] = new[] { "Gible", "Shinx", "Buizel", "Snover", "Stunky", "Riolu" },
        ["kanto"] = new[] { "Dratini", "Eevee", "Growlithe", "Magnemite", "Machop", "Lapras" },
        ["johto"] = new[] { "Larvitar", "Mareep", "Chinchou", "Heracross", "Teddiursa", "Sneasel" },
        ["hoenn"] = new[] { "Bagon", "Beldum", "Ralts", "Trapinch", "Absol", "Swablu" },
        ["unova"] = new[] { "Axew", "Deino", "Larvesta", "Rufflet", "Vullaby", "Joltik" },
    };

    private static List<string> CellIds(string region) =>
        Enumerable.Range(1, 4).Select(i => $"lockwork_{region}_agent_{i}").Append($"lockwork_{region}_admin").ToList();

    [Fact]
    public void EachCellLocksSixOfItsRegionsOwnSpeciesNoneLegendaryEachOnce()
    {
        var all = new List<string>();
        foreach (var region in Regions)
        {
            var species = CellIds(region).SelectMany(id => TrainerDatabase.PartyOf(TrainerDatabase.Get(id)!).Members)
                .Where(p => p.Lock != null).Select(p => p.Species.Name).ToList();
            Assert.Equal(Locked[region].OrderBy(s => s), species.OrderBy(s => s));
            Assert.All(species, s => Assert.False(PokemonDatabase.Get(s)!.Legendary || PokemonDatabase.Get(s)!.Mythical, s));
            all.AddRange(species);
        }
        Assert.Equal(30, all.Count);
        Assert.Equal(30, all.Distinct().Count());
        // The head's own, the last locked heart (the user's answer 41c)
        var head = TrainerDatabase.PartyOf(TrainerDatabase.Get("lockwork_head")!).Members.Single(p => p.Lock != null);
        Assert.Equal("Ampharos", head.Species.Name);
        // A double never has a locked member: a ball has one target
        Assert.All(Regions.SelectMany(CellIds).Append("lockwork_head"), id => Assert.False(TrainerDatabase.Get(id)!.DoubleBattle, id));
    }

    [Fact]
    public void EveryCellsSixAreReclaimedInTheirTrainersBattlesWithTheReclaimer()
    {
        int reclaimed = 0;
        foreach (var id in Regions.SelectMany(CellIds).Append("lockwork_head"))
        {
            var record = TrainerDatabase.Get(id)!;
            var trainer = new Trainer { Id = id, Name = record.Name, TrainerClass = record.Class, PrizeMoney = record.PrizeMoney };
            foreach (var p in TrainerDatabase.PartyOf(record).Members) trainer.Party.Add(p);
            var party = new Party();
            party.Add(Of(Gender.Male, "Arceus", 100, "Judgment"));
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm().Force(RollKind.CatchShake, 0).Force(RollKind.CriticalCapture, 255),
                Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Lucas", Reclaimer = true
            });
            core.Start();
            for (int turn = 0; turn < 40 && core.Result == BattleResult.None; turn++)
            {
                if (core.Request is ReplacementRequest replace)
                {
                    core.Submit(BattleChoice.Switch(replace.Place, Enumerable.Range(0, trainer.Party.Count).First(i => core.CanSendIn(core.At(replace.Place), i))));
                    continue;
                }
                var asked = Assert.IsType<ActionRequest>(core.Request).Places;
                var choices = new List<BattleChoice>();
                if (asked.Contains(Mine))
                    choices.Add(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")) == null ? BattleChoice.UseItem(Mine, "Poké Ball") : BattleChoice.Fight(Mine, 0));
                if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, core.At(Foe).Pokemon!.Moves.IndexOf(core.UsableMoves(core.At(Foe)).First())));
                core.Submit(choices);
            }
            Assert.Equal(BattleResult.PlayerVictory, core.Result);
            reclaimed += core.Reclaimed.Count;
            Assert.All(core.Reclaimed, p => Assert.NotNull(p.Lock));
        }
        Assert.Equal(31, reclaimed);
    }

    [Fact]
    public void ACellsPeopleAndItsDoorWaitForItsRegionsHallOfFame()
    {
        MapDatabase.Initialize();
        foreach (var (map, flag, agents) in new[] { ("Sinnoh", "SinnohHallOfFame", 4), ("Kanto", "KantoHallOfFame", 4), ("Johto", "JohtoHallOfFame", 4),
                     ("Hoenn", "HoennHallOfFame", 4), ("CasteliaCity", "UnovaHallOfFame", 4) })
        {
            var people = MapDatabase.Get(map).Everyone.Where(n => n.Id.StartsWith("lockwork_agent_")).ToList();
            Assert.Equal(agents, people.Count);
            Assert.All(people, p => Assert.Equal(flag, p.ShownBy));
        }
        // The way down to each cell's admin is its doorman's (a script's warp), out with the cell; the room's way out lands
        // before him
        foreach (var (map, room, flag) in new[] { ("Sinnoh", "LockworkCellSinnoh", "SinnohHallOfFame"), ("Kanto", "LockworkCellKanto", "KantoHallOfFame"),
                     ("Johto", "LockworkCellJohto", "JohtoHallOfFame"), ("Hoenn", "LockworkCellHoenn", "HoennHallOfFame") })
        {
            var doorman = MapDatabase.Get(map).Everyone.Single(n => n.Id == "lockwork_doorman");
            Assert.Equal(flag, doorman.ShownBy);
            var back = MapDatabase.Get(room).Warps.Single();
            Assert.Equal((doorman.GridX, doorman.GridY + 1), (back.TargetX, back.TargetY));
            Assert.True(MapDatabase.Get(map).IsWalkable(back.TargetX, back.TargetY));
            Assert.Single(MapDatabase.Get(room).Everyone, n => n.Id == "lockwork_admin");
        }
        // The tower's door is there only from Unova's Hall of Fame, and so are its people
        Assert.Equal("UnovaHallOfFame", MapDatabase.Get("CasteliaCity").Warps.Single(w => w.TargetMap == "LockworkTower1F").From);
        Assert.Equal("UnovaHallOfFame", MapDatabase.Get("LockworkTower1F").Everyone.Single(n => n.Id == "lockwork_admin").ShownBy);
        // Looker's last scene stands clear of the tile the head is spoken to from
        var head = MapDatabase.Get("LockworkTower2F").Everyone.Single(n => n.Id == "lockwork_head");
        var looker = MapDatabase.Get("LockworkTower2F").Everyone.Single(n => n.Id == "lockwork_looker");
        Assert.True(System.Math.Abs(looker.GridX - head.GridX) + System.Math.Abs(looker.GridY - head.GridY) > 2);
        Assert.Single(MapDatabase.Get("Sinnoh").Everyone, n => n.Id == "lockwork_looker");
    }

    private static int Run(HeadlessScriptHost host, string file, string name, NPC? self = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, file)!, self ?? Person(file));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return runner.Result;
    }

    /// <summary>The person a place's script is about, as the place's own map has them.</summary>
    private static NPC? Person(string file)
    {
        MapDatabase.Initialize();
        return file switch
        {
            "survival_area" => MapDatabase.Get("Sinnoh").Everyone.Single(n => n.Id == "lockwork_looker"),
            "LockworkTower1F" => MapDatabase.Get("LockworkTower1F").Everyone.Single(n => n.Id == "lockwork_admin"),
            "LockworkTower2F" => MapDatabase.Get("LockworkTower2F").Everyone.Single(n => n.Id == "lockwork_head"),
            _ => null
        };
    }

    [Fact]
    public void TheCourierCarriesExactlyTheMissedSixAtATime()
    {
        var host = new HeadlessScriptHost();
        foreach (var id in new[] { "lock_sinnoh_gible", "lock_kanto_eevee", "lock_unova_axew" }) host.Hearts.Miss(id);
        MapDatabase.Initialize();
        var courier = MapDatabase.Get("LockworkTower1F").Everyone.Single(n => n.Id == "lockwork_courier");
        Assert.Equal(3, Run(host, "LockworkTower1F", "Courier", courier) is var _ ? CourierCount(courier) : 0);
        Assert.Equal(new[] { "Gible", "Eevee", "Axew" }, courier.TrainerData!.Party.Members.Select(p => p.Species.Name));
        Assert.All(courier.TrainerData.Party.Members, p => Assert.Equal(HeartLock.Full, p.Lock!.Gauge));
        // Seven missed: he carries six of them
        foreach (var id in new[] { "lock_johto_mareep", "lock_hoenn_ralts", "lock_kanto_lapras", "lock_hoenn_bagon" }) host.Hearts.Miss(id);
        Assert.Equal(6, HeartLock.CourierTeam(host.Hearts.Missed).Count);
        // A reclaim takes one off his list
        host.Hearts.Reclaim("lock_sinnoh_gible");
        Assert.DoesNotContain("lock_sinnoh_gible", host.Hearts.Missed);
    }

    private static int CourierCount(NPC courier) => courier.TrainerData?.Party.Count ?? 0;

    [Fact]
    public void LookerGivesTheReclaimerOnceAndTheSpringsRiteLowersTheTeamsLocks()
    {
        var host = new HeadlessScriptHost();
        var gible = TrainerDatabase.Build(new TrainerPokemonRecord { Species = "Gible", Level = 30, IvScale = 100, Personality = 1, Lock = "lock_sinnoh_gible" });
        host.Party.Add(gible);
        Run(host, "survival_area", "LookerReclaimer");
        Assert.True(host.Story.Has(ScriptRunner.ReclaimerFlag));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(HeartLock.ReclaimerItem)!));
        Run(host, "survival_area", "LookerReclaimer");
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(HeartLock.ReclaimerItem)!));

        Run(host, "sendoff_spring", "SpringRite");
        Assert.Equal(HeartLock.Full - HeartLock.Fall.Spring, gible.Lock!.Gauge);
        Run(host, "sendoff_spring", "SpringRite");
        Assert.Equal(HeartLock.Full - HeartLock.Fall.Spring, gible.Lock!.Gauge);
    }

    [Fact]
    public void TheHeadWaitsForTheAdminAndStopsTheMachine()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Of(Gender.Male, "Arceus", 100, "Judgment"));
        Run(host, "LockworkTower2F", "Head");
        Assert.False(host.Story.Has("FLAG_LOCKWORK_MACHINE_STOPPED"));
        Run(host, "LockworkTower1F", "Admin");
        Assert.True(host.Story.Has("FLAG_LOCKWORK_UNOVA_ADMIN_BEATEN"));
        Run(host, "LockworkTower2F", "Head");
        Assert.True(host.Story.Has("FLAG_LOCKWORK_MACHINE_STOPPED"));
    }
}
