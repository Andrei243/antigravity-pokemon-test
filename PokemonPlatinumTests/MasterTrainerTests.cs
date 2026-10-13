using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B14, Let's Go's Master Trainers: a master for every species (<c>Data/master-trainers.json</c>, written by
/// <c>tools/DataImporter</c>'s <see cref="MasterTrainers"/>), battled one-on-one with one of the team of that species
/// and the bag closed to both sides (<see cref="CoreSetup.NoBattleItems"/>), chosen with <c>choosepokemon "Species"</c>
/// and fought with <c>master</c>; and the legality check (<see cref="Legality"/>) that keeps a move off a master that
/// its species can't learn. The Fight Area's scientist of plan 14 · B1 has his own record in <c>our-trainers.json</c>.
/// </summary>
public class MasterTrainerTests
{
    // ================================================================== the masters' file

    [Fact]
    public void EverySpeciesHasAMasterWithOnePokemonOfItsOwnAtSeventyFive()
    {
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        var masters = TrainerDatabase.Masters;
        Assert.Equal(1025, masters.Count);
        Assert.Equal(species.Select(s => TrainerDatabase.MasterId(s.Name)), masters.Select(m => m.Id));
        Assert.Equal(masters.Count, masters.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var (master, of) in masters.Zip(species))
        {
            Assert.Equal($"{of.Name} Master", master.Class);
            Assert.False(string.IsNullOrEmpty(master.Name), master.Id);
            Assert.Equal(0, master.PrizeMoney);
            Assert.Empty(master.Items);
            Assert.Equal(new[] { "Basic", "EvalAttack", "Expert" }, master.Ai);
            var pokemon = Assert.Single(master.Party);
            Assert.Equal((of.Name, TrainerDatabase.MasterLevel, 255, (string?)null, (string?)null),
                (pokemon.Species, pokemon.Level, pokemon.IvScale, pokemon.Item, pokemon.Form));
        }
        // The ids the scripts and the story know them by
        Assert.Equal(new[] { "master_bidoof", "master_mr_mime", "master_nidoran_f", "master_farfetchd", "master_type_null", "master_flabebe" },
            new[] { "Bidoof", "Mr. Mime", "Nidoran♀", "Farfetch'd", "Type: Null", "Flabébé" }.Select(TrainerDatabase.MasterId));
    }

    [Fact]
    public void AMastersMovesAreLearnableAndRunInFull()
    {
        int withMoves = 0;
        foreach (var master in TrainerDatabase.Masters)
        {
            var pokemon = master.Party[0];
            if (pokemon.Moves is not { Count: > 0 } moves) continue;
            withMoves++;
            Assert.True(moves.Count <= 4, master.Id);
            Assert.Equal(moves.Count, moves.Distinct().Count());
            var learnable = Legality.Learnable(PokemonDatabase.Get(pokemon.Species)!);
            foreach (string move in moves)
            {
                Assert.True(learnable.Contains(move), $"{master.Id} can't learn {move}");
                var data = MoveDatabase.Get(move);
                Assert.True(data.Kind == MoveKind.Standard && data.Support == MoveEffectSupport.Full, $"{master.Id}: {move} doesn't run in full");
            }
        }
        // About two thirds of the species have a set at the pinned commit (the importer's report counts them)
        Assert.InRange(withMoves, 600, 760);
    }

    [Fact]
    public void TheTableIsStillPlatinumsAndAMasterIsFoundById()
    {
        // All stays the original's table, so every test over it reads as it did; Get finds a master and our own
        Assert.DoesNotContain(TrainerDatabase.All, t => t.Id.StartsWith("master_", StringComparison.Ordinal));
        Assert.DoesNotContain(TrainerDatabase.All, t => TrainerDatabase.Ours.Any(o => o.Id == t.Id));
        var bidoof = TrainerDatabase.Get("master_bidoof");
        Assert.NotNull(bidoof);
        Assert.Equal("Bidoof Master", bidoof!.Class);
        Assert.Same(bidoof, TrainerDatabase.Masters.Single(m => m.Id == "master_bidoof"));
        Assert.Null(TrainerDatabase.Get("master_nobody"));

        // Built, the master's Pokémon is whole, holds nothing and has every IV
        var team = new Trainer { Id = bidoof.Id };
        TrainerDatabase.Fill(team, bidoof);
        var built = Assert.Single(team.Party.Members);
        Assert.Equal(("Bidoof", 75, 31, 31, (ItemData?)null), (built.Species.Name, built.Level, built.IvHP, built.IvSpeed, built.HeldItem));
        Assert.Equal(("Bidoof Master", 0), (team.TrainerClass, team.PrizeMoney));
        Assert.Equal(AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert, team.Ai);
    }

    [Fact]
    public void TheImporterDrawsTheSameMastersEveryTimeAndOnlyLearnableMoves()
    {
        // Showdown's files in miniature: a Bidoof set with a move Bidoof can't learn, a Starly set that fits whole
        string folder = Path.Combine(Path.GetTempPath(), "masters-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string file in MasterTrainers.SetFiles)
            {
                string path = Path.Combine(folder, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "{}");
            }
            File.WriteAllText(Path.Combine(folder, MasterTrainers.SetFiles[0]), """
                {
                  "bidoof": { "level": 90, "sets": [ { "role": "Fast Attacker", "movepool": ["Hyper Fang", "Psychic", "Quick Attack", "Swords Dance", "Hydro Pump"] } ] },
                  "starly": { "level": 90, "sets": [ { "role": "Wallbreaker", "movepool": ["Brave Bird", "Aerial Ace", "Quick Attack", "U-turn", "Close Combat"] } ] }
                }
                """);
            var species = new[] { "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Kricketot" }.Select(n => PokemonDatabase.Get(n)!).ToList();
            var moves = MoveDatabase.GetAll().ToList();
            var items = ItemDatabase.GetAll().ToList();

            var first = MasterTrainers.Build(folder, species, moves, items);
            var second = MasterTrainers.Build(folder, species, moves, items);
            Assert.Equal(GameDataFiles.Serialize(first.Masters), GameDataFiles.Serialize(second.Masters));
            Assert.Equal(species.OrderBy(s => s.DexNumber).Select(s => TrainerDatabase.MasterId(s.Name)), first.Masters.Select(m => m.Id));

            var starly = first.Masters.Single(m => m.Id == "master_starly").Party[0];
            // Close Combat is Staraptor's alone: the four Starly can learn are drawn
            Assert.Equal(new HashSet<string> { "Brave Bird", "Aerial Ace", "Quick Attack", "U-turn" }, starly.Moves!.ToHashSet());
            // Bidoof's set gives the two it can learn, and what its level taught it makes up the four
            var bidoof = first.Masters.Single(m => m.Id == "master_bidoof").Party[0];
            Assert.Equal(4, bidoof.Moves!.Count);
            Assert.DoesNotContain("Psychic", bidoof.Moves);
            Assert.DoesNotContain("Hydro Pump", bidoof.Moves);
            Assert.All(bidoof.Moves, m => Assert.Contains(m, Legality.Learnable(PokemonDatabase.Get("Bidoof")!)));
            // A species with no set knows what its level taught it
            Assert.Null(first.Masters.Single(m => m.Id == "master_kricketot").Party[0].Moves);
            Assert.Equal((4, 2, 1), (first.WithoutMoves, first.FromSets, first.MadeUp));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    // ================================================================== the legality check

    [Fact]
    public void TheLegalityCheckTakesTheLineTheMachinesAndTheTutors()
    {
        var staraptor = PokemonDatabase.Get("Staraptor")!;
        // Its own level-up moves, what Starly learnt before it evolved, a TM, and nothing it can't
        Assert.True(Legality.CanLearn(staraptor, "Close Combat"));
        Assert.True(Legality.CanLearn(staraptor, "Quick Attack"));
        Assert.True(Legality.CanLearn(staraptor, "U-turn"));
        Assert.False(Legality.CanLearn(staraptor, "Psychic"));
        Assert.False(Legality.CanLearn(PokemonDatabase.Get("Bidoof")!, "Hydro Pump"));
        // Grandma Wilma's Draco Meteor for a Dragon type, and a starter's strongest move fully grown
        Assert.True(Legality.CanLearn(PokemonDatabase.Get("Garchomp")!, "Draco Meteor"));
        Assert.True(Legality.CanLearn(PokemonDatabase.Get("Torterra")!, "Frenzy Plant"));
        Assert.False(Legality.CanLearn(PokemonDatabase.Get("Grotle")!, "Frenzy Plant"));

        var odd = Mon("Bidoof", 20, "Tackle", "Hydro Pump");
        Assert.Equal(new[] { "Hydro Pump" }, Legality.Unlearnable(odd));
        Assert.Empty(Legality.Unlearnable(Mon("Bidoof", 20, "Tackle")));
    }

    // ================================================================== the scientist of plan 14 · B1

    [Fact]
    public void TheFightAreasScientistHasATeamOfOurOwn()
    {
        var ours = Assert.Single(TrainerDatabase.Ours);
        Assert.Equal("scientist_ingram", ours.Id);
        Assert.Same(ours, TrainerDatabase.Get("scientist_ingram"));
        // A name that is nobody's in Platinum's table, and a team that could be had
        Assert.DoesNotContain(TrainerDatabase.All, t => t.Class == ours.Class && t.Name == ours.Name);
        foreach (var pokemon in TrainerDatabase.PartyOf(ours).Members)
        {
            Assert.Empty(Legality.Unlearnable(pokemon));
            Assert.All(pokemon.Moves, m => Assert.Equal(MoveEffectSupport.Full, MoveDatabase.Get(m.Name).Support));
        }
        // Platinum's prize for his class: the last Pokémon's level, four times over, by the class's rate (the table's Scientists')
        var shaun = TrainerDatabase.Get("scientist_shaun")!;
        Assert.Equal(shaun.PrizeMoney / shaun.Party[^1].Level * ours.Party[^1].Level, ours.PrizeMoney);
        // His script battles as him
        var script = ScriptLibrary.Default.Find("InverseBattle", "fight_area")!;
        Assert.Contains(script.Everything(), i => i.Op == Op.Battle && i.AsTrainer == "scientist_ingram" && i.Inverse);
    }

    // ================================================================== the bag closed, on the bare core

    private static BattleCore Master(Pokemon mine, Trainer master, BattleRandom? rolls = null, bool noItems = true, IBattleController? enemy = null)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { master }, Random = rolls ?? Calm(), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = enemy, PlayerName = "Lucas", NoBattleItems = noItems
        });
        core.Start();
        return core;
    }

    private static Trainer MasterOf(string species)
    {
        var record = TrainerDatabase.Get(TrainerDatabase.MasterId(species))!;
        var team = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(team, record);
        return team;
    }

    [Fact]
    public void NoItemIsUsedByEitherSideWithTheBagClosed()
    {
        var mine = Of(Gender.Male, "Bidoof", 30, "Tackle");
        mine.CurrentHP = 10;
        var foe = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", Items = new List<string> { "Potion" } };
        foe.Party.Add(Of(Gender.Male, "Bidoof", 30, "Tackle"));

        // Without the flag a hurt Pokémon may have a Potion; with it, nothing may be used, by either side
        var open = Master(mine, foe, noItems: false);
        Assert.Null(open.WhyNot(BattleChoice.UseItem(Mine, "Potion")));
        Assert.False(open.NoBattleItems);
        var closed = Master(Of(Gender.Male, "Bidoof", 30, "Tackle"), foe);
        closed.PlayerParty.Members[0].CurrentHP = 10;
        Assert.True(closed.NoBattleItems);
        Assert.Equal(BattleCore.NoItemsRefusal, closed.WhyNot(BattleChoice.UseItem(Mine, "Potion")));
        Assert.Equal(BattleCore.NoItemsRefusal, closed.WhyNot(BattleChoice.UseItem(Mine, "X Attack")));
        Assert.Equal(BattleCore.NoItemsRefusal, closed.WhyNot(BattleChoice.UseItem(Foe, "Potion")));
        Assert.Throws<ArgumentException>(() => closed.Submit(BattleChoice.UseItem(Mine, "Potion"), BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void ATrainerInTheRedKeepsItsPotionWithTheBagClosed()
    {
        // TrainerAI_ShouldUseItem would heal a Pokémon under a quarter of its HP (TrainerAiTests); not here
        Trainer Roark()
        {
            var t = new Trainer { Id = "roark", Name = "Roark", TrainerClass = "Leader", Ai = AiFlags.Basic, Items = new List<string> { "Potion" } };
            var geodude = Of(Gender.Male, "Geodude", 14, "Tackle");
            t.Party.Add(geodude);
            geodude.CurrentHP = geodude.MaxHP / 4 - 1;
            return t;
        }
        var open = Master(Of(Gender.Male, "Bidoof", 20, "Tackle"), Roark(), noItems: false, enemy: TrainerAi.Instance);
        Assert.Equal(ChoiceKind.Item, TrainerAi.Instance.ChooseAction(open, open.EnemySlots[0]).Kind);
        var closed = Master(Of(Gender.Male, "Bidoof", 20, "Tackle"), Roark(), enemy: TrainerAi.Instance);
        Assert.Equal(ChoiceKind.Fight, TrainerAi.Instance.ChooseAction(closed, closed.EnemySlots[0]).Kind);
    }

    [Fact]
    public void AMastersBattleIsPlayedToItsEndAndReplayed()
    {
        // The Bidoof Master's Bidoof against one Bidoof of the player's, one Pokémon a side, the master thinking
        // as the Elite Four do; played to its end on the battle's own seed, then again from its record alone
        Pokemon Ours() => Mon("Bidoof", 100, "Superpower", "Hyper Fang");
        var core = Master(Ours(), MasterOf("Bidoof"), new BattleRandom(20261013), enemy: TrainerAi.Instance);
        for (int turn = 0; turn < 60 && core.Result == BattleResult.None; turn++)
        {
            Assert.IsType<ActionRequest>(core.Request);
            core.Submit(BattleChoice.Fight(Mine, 0));
        }
        Assert.NotEqual(BattleResult.None, core.Result);
        Assert.Single(core.PlayerParty.Members);
        Assert.Single(core.Trainers[0].Party.Members);
        Assert.DoesNotContain(core.Log, e => e is Said s && s.Text.Contains("used a", StringComparison.Ordinal) && s.Text.Contains("Potion"));

        var again = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(Ours()), Trainers = new List<Trainer> { MasterOf("Bidoof") }, Random = new BattleRandom(core.Record!.Seed),
            Rules = Ruleset.Platinum, PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas", NoBattleItems = true
        });
        again.Replay(core.Record!);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Result, again.Result);
    }

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    // ================================================================== the battle's face

    [Fact]
    public void ABidoofBeatsTheBidoofMastersBidoofWithTheBagClosedAndKeepsWhatItGained()
    {
        // The battle as the game fights it: the team's own Bidoof alone on the player's side, the bag shut
        // Every IV whole, so it outruns the master's at 75 and its Superpower lands first; a point of EXP short of 91
        var chosen = Mon("Bidoof", 90, "Superpower", "Hyper Fang");
        chosen.IvHP = chosen.IvAttack = chosen.IvDefense = chosen.IvSpAttack = chosen.IvSpDefense = chosen.IvSpeed = 31;
        chosen.RecalculateStats();
        chosen.CurrentHP = chosen.MaxHP;
        chosen.CurrentExp = chosen.ExpForNextLevel - 1;
        var team = PartyOf(Mon("Starly", 30, "Tackle"), chosen);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = PartyOf(chosen), Inventory = bag, Pokedex = new Pokedex(), Trainers = { MasterOf("Bidoof") },
            Random = Steady(20261013), Rules = Ruleset.Platinum, NoBattleItems = true
        });
        Settle(battle);
        Assert.True(battle.BagClosed);

        // BAG says why and goes back to the commands; a Potion is refused all the same, and none is spent
        battle.SelectMainMenuOption(1);
        Assert.Equal(BattleCore.NoItemsRefusal, battle.CurrentMessage);
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        battle.UseBagItem("Potion");
        Settle(battle);
        Assert.Equal(3, bag.GetQuantity(ItemDatabase.Get("Potion")!));

        int level = chosen.Level;
        for (int turn = 0; turn < 30 && !battle.IsBattleOver; turn++) Turn(battle, 0);
        Assert.True(battle.IsBattleOver);
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        Assert.Equal(0, battle.PrizeMoney);
        // What the battle did to the team's own Pokémon stays with it: the EXP of a level 75 foe
        Assert.True(chosen.Level > level, $"{chosen.Level}");
        Assert.Same(chosen, team.Members[1]);
        Assert.Equal(30, team.Members[0].Level);
    }

    // ================================================================== the script's words

    private static (ScriptRunner Runner, HeadlessScriptHost Host, NPC Master) Ready(string code, params Pokemon[] team)
    {
        var library = ScriptLibrary.FromSources(("test", code), ("common", ""));
        var host = new HeadlessScriptHost();
        foreach (var p in team) host.Party.Add(p);
        var person = new NPC { Key = "master", Name = "Master", GridX = 3, GridY = 3 };
        var map = new Map(12, 12) { Name = "Room" };
        map.NPCs.Add(person);
        host.Map = map;
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), person);
        return (runner, host, person);
    }

    private const string Hall = """
        script Hall
          choosepokemon "Bidoof"
          if result == 255 goto Away
          say "{chosen} steps up."
          master self canlose
          if won goto Won
          say "Lost."
          end
        label Won
          say "{chosen} won."
          end
        label Away
          say "Another time."
          end
        """;

    [Fact]
    public void ChoosePokemonWithASpeciesAndMasterAreRead()
    {
        var code = ScriptParser.Parse("test", """
            script S
              choosepokemon "Bidoof"
              choosepokemon
              master self canlose
              master self
            """)[0].Everything().ToList();
        Assert.Equal(("Bidoof", ""), (code[0].Name, code[1].Name));
        Assert.Equal(new[] { Op.Master, Op.Master }, code.Skip(2).Select(i => i.Op));
        Assert.Equal((true, false), (code[2].Option, code[3].Option));
        // A species that isn't there is a problem of the library's
        var library = ScriptLibrary.FromSources(("test", "script S\n  choosepokemon \"Bidooof\"\n  end\n"), ("common", ""));
        Assert.Contains(library.Problems(), p => p.Contains("Bidooof"));
    }

    [Fact]
    public void BackingOutOfTheChoiceFightsNobody()
    {
        var (runner, host, _) = Ready(Hall, Mon("Bidoof", 20, "Tackle"));
        host.PokemonChoice = 255;
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains("open ChoosePokemon Bidoof", host.Log);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));
        Assert.Equal("Another time.", host.Transcript.Last().Text);
    }

    [Fact]
    public void APokemonOfAnotherSpeciesIsNoChoice()
    {
        // The game's screen greys a Starly; a host that hands its place back all the same has chosen nobody
        var (runner, host, _) = Ready(Hall, Mon("Starly", 20, "Tackle"), Mon("Bidoof", 20, "Tackle"));
        host.PokemonChoice = 0;
        runner.RunToEnd();
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));
        Assert.Equal("Another time.", host.Transcript.Last().Text);
        Assert.True(ScriptRunner.Fits(Mon("Bidoof", 5), "bidoof"));
        Assert.False(ScriptRunner.Fits(Mon("Starly", 5), "Bidoof"));
    }

    [Fact]
    public void AMasterBeatenIsHeldBeatenByTheStory()
    {
        var bidoof = Mon("Bidoof", 20, "Tackle");
        bidoof.Nickname = "Buck";
        var (runner, host, person) = Ready(Hall, Mon("Starly", 20, "Tackle"), bidoof);
        host.PokemonChoice = 1;
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains("battle master_bidoof alone Bidoof noitems Won", host.Log);
        Assert.True(host.Story.HasDefeated("master_bidoof"));
        Assert.Equal(new[] { "Buck steps up.", "Buck won." }, host.Transcript.Select(t => t.Text));
        // The person fought as the master: the class, one Pokémon at 75, and no prize
        Assert.Equal(("Bidoof Master", 0, 3000), (person.TrainerData!.TrainerClass, person.TrainerData.PrizeMoney, host.Money));
        Assert.Equal(75, Assert.Single(person.TrainerData.Party.Members).Level);
    }

    [Fact]
    public void AMasterLostToUnderCanLoseIsNotBeatenAndTheTeamIsHealed()
    {
        var bidoof = Mon("Bidoof", 20, "Tackle");
        var (runner, host, _) = Ready(Hall, bidoof);
        host.PokemonChoice = 0;
        host.Fight = _ => BattleOutcome.Lost;
        bidoof.CurrentHP = 0;
        runner.RunToEnd();
        // A fainted Pokémon is no choice: nobody is fought
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));

        bidoof.CurrentHP = 5;
        (runner, host, _) = Ready(Hall, bidoof);
        host.PokemonChoice = 0;
        host.Fight = _ => BattleOutcome.Lost;
        runner.RunToEnd();
        Assert.Contains("battle master_bidoof alone Bidoof noitems Lost", host.Log);
        Assert.False(host.Story.HasDefeated("master_bidoof"));
        Assert.Equal("Lost.", host.Transcript.Last().Text);
        Assert.Equal(bidoof.MaxHP, bidoof.CurrentHP);
    }

    [Fact]
    public void ThePartyScreenGreysWhatTheSpeciesRefuses()
    {
        var party = PartyOf(Mon("Starly", 20, "Tackle"), Mon("Bidoof", 20, "Tackle"));
        var screen = new PartyScreen();
        screen.OpenToChoose(p => ScriptRunner.Fits(p, "Bidoof"), "Only Bidoof can be chosen here.");
        Assert.True(screen.ChoosingSome);
        Assert.Equal(("NOT ABLE", (string?)null), (screen.ChooseTag(party.Members[0]), screen.ChooseTag(party.Members[1])));

        screen.SelectedIndex = 0;
        screen.Confirm(party);
        Assert.True(screen.IsActive);
        Assert.Equal("Only Bidoof can be chosen here.", screen.Message);
        Assert.Equal(PartyScreen.NoneChosen, screen.Chosen);

        screen.SelectedIndex = 1;
        screen.Confirm(party);
        Assert.False(screen.IsActive);
        Assert.Equal(1, screen.Chosen);

        // Opened with no species, every card may be chosen, as before
        screen.OpenToChoose();
        Assert.False(screen.ChoosingSome);
        Assert.Null(screen.ChooseTag(party.Members[0]));
    }
}
