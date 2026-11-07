using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 36 · TR9: the later games' machines and tutors (docs/mechanics/rulings.md, "The later TMs and tutors"). Every move
/// a later game's machine teaches that no machine of Platinum's teaches is a machine of ours, TM93 on (ids from
/// <see cref="MoveTeaching.LaterFrom"/>, decision 7), sold on a training hall's machine shelf and taught by the modern rules
/// alone (<see cref="PokemonSpecies.LaterMachines"/>, <see cref="Ruleset.LaterMachines"/>); the later tutors' moves that
/// no machine teaches are a training hall's tutor's (<see cref="MoveTutors.TrainingHall"/>). The importer holds each
/// species' newest game's machines and tutors to them (<c>Importer.LaterGaps</c>, which it prints at every run); these
/// tests hold the data and the game to it: every move has a way, the machines teach and are used up as every TM is (plan
/// 39's decision 4), the compatibility both ways, Tera Blast, and a Dipplin taught Dragon Cheer evolving into Hydrapple.
/// </summary>
[Collection("Shared clocks and dice")]
public class LaterMachineTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new InvalidOperationException(name + " is not an item");

    private static ItemData MachineFor(string move) => ItemDatabase.GetAll().Single(i => MoveTeaching.IsLater(i) && i.TeachesMove == move);

    private static List<ItemData> Later => ItemDatabase.GetAll().Where(MoveTeaching.IsLater).OrderBy(i => i.Id).ToList();

    private static Pokemon Mon(string species, int level, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        if (moves.Length > 0)
        {
            p.Moves.Clear();
            foreach (string m in moves) p.Moves.Add(MoveDatabase.Create(m));
        }
        return p;
    }

    private const string Battle = "only a battle grants it", Cut = "learned in Generations 5 to 7 alone (by level or as an Egg move) and in no Pokémon's newest game, whose list the modern rules learn by",
        Event = "an event Pokémon's alone: no Pokémon learns it in the games", Partner = "Let's Go's partner Pikachu's or Eevee's own, from that game's tutor alone";

    /// <summary>
    /// The moves of the games that nothing teaches or learns, each with why: what a battle or a form's change grants, what
    /// no Pokémon learns in the games themselves, Let's Go's partners' own, the moves the newest games cut, and an Egg
    /// move of the newest games that a species of Platinum's doesn't take (its Egg moves are Platinum's).
    /// </summary>
    private static readonly Dictionary<string, string> NoWay = new()
    {
        ["Struggle"] = Battle + ": what a Pokémon uses when it has nothing else, never learned",
        ["Behemoth Blade"] = Battle + ": Zacian's Crowned form's Iron Head, which the change of form grants",
        ["Behemoth Bash"] = Battle + ": Zamazenta's Crowned form's Iron Head, which the change of form grants",
        ["Blazing Torque"] = Battle + ": a Starmobile's, Team Star's cars in Scarlet and Violet",
        ["Combat Torque"] = Battle + ": a Starmobile's, Team Star's cars in Scarlet and Violet",
        ["Magical Torque"] = Battle + ": a Starmobile's, Team Star's cars in Scarlet and Violet",
        ["Noxious Torque"] = Battle + ": a Starmobile's, Team Star's cars in Scarlet and Violet",
        ["Wicked Torque"] = Battle + ": a Starmobile's, Team Star's cars in Scarlet and Violet",
        ["Power Shift"] = "a form's alone: Legends: Arceus's tutor teaches it to Hisui's Braviary and Avalugg, and a form takes its species' lists",
        ["Celebrate"] = Event, ["Hold Hands"] = Event, ["Happy Hour"] = Event, ["Hold Back"] = Event,
        ["Baddy Bad"] = Partner, ["Bouncy Bubble"] = Partner, ["Buzzy Buzz"] = Partner, ["Floaty Fall"] = Partner,
        ["Freezy Frost"] = Partner, ["Glitzy Glow"] = Partner, ["Pika Papow"] = Partner, ["Sappy Seed"] = Partner,
        ["Sizzly Slide"] = Partner, ["Sparkly Swirl"] = Partner, ["Splishy Splash"] = Partner, ["Veevee Volley"] = Partner,
        ["Zippy Zap"] = Partner,
        ["Bestow"] = Cut, ["Chip Away"] = Cut, ["Heart Stamp"] = Cut, ["Steamroller"] = Cut, ["Spotlight"] = Cut,
        ["Powder"] = Cut, ["Ion Deluge"] = Cut,
        ["Chilly Reception"] = "Slowking's Egg move in Scarlet and Violet: a species of Platinum's takes Platinum's Egg moves, and the newest games' wait for a session of their own",
    };

    // ------------------------------------------------------------------ every move has a way

    [Fact]
    public void EveryMoveOfTheGamesHasAWayToBeTaughtOrLearnedButTheListed()
    {
        var sources = ItemSources.Everywhere;
        var machines = ItemDatabase.GetAll().Where(MoveTeaching.IsMachine).ToDictionary(i => i.Name);
        var shardTutors = MoveTutors.Places.SelectMany(MoveTutors.At).Select(m => m.Move).ToHashSet();
        var hallTutor = MoveTutors.At(MoveTutors.TrainingHall).Select(m => m.Move).ToHashSet();
        var ways = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in PokemonDatabase.Everyone())
        {
            // Level (by either rules, and a form's own list), an Egg, a machine that can be had, a tutor that stands somewhere
            foreach (var m in s.Learnset.Concat(s.Modern?.Learnset ?? new())) ways.Add(m.MoveName);
            foreach (var f in s.Forms ?? new())
                foreach (var m in (f.Learnset ?? new()).Concat(f.Modern?.Learnset ?? new())) ways.Add(m.MoveName);
            foreach (var m in s.EggMoves ?? new()) ways.Add(m);
            foreach (string name in (s.TmMoves ?? new()).Concat(s.LaterMachines ?? new()))
                if (machines.TryGetValue(name, out var machine) && sources.Of(name).Count > 0) ways.Add(machine.TeachesMove!);
            foreach (var m in s.TutorMoves ?? new()) if (shardTutors.Contains(m)) ways.Add(m);
            foreach (var m in s.LaterTutorMoves ?? new()) if (hallTutor.Contains(m)) ways.Add(m);
            if (s.PrimaryType == PokemonType.Dragon || s.SecondaryType == PokemonType.Dragon) ways.Add(MoveTutors.DracoMeteor);
            if (MoveTutors.UltimateMoveOf(s.Name) is { } ultimate) ways.Add(ultimate);
        }
        // Kanto's tutors of one move each teach whoever may learn the move's kin among Platinum's machines
        foreach (var (move, kin) in MoveTutors.Kanto)
            if (ways.Contains(kin)) ways.Add(move);

        var games = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard && m.Generation >= 1).Select(m => m.Name).ToList();
        var none = games.Where(m => !ways.Contains(m)).OrderBy(m => m, StringComparer.Ordinal).ToList();
        Assert.True(none.ToHashSet().SetEquals(NoWay.Keys),
            "no way to: " + string.Join(", ", none.Where(m => !NoWay.ContainsKey(m)))
            + "; listed, and has one now (take its line off): " + string.Join(", ", NoWay.Keys.Where(m => !none.Contains(m))));
        Assert.All(NoWay.Keys, m => Assert.Contains(m, games));
    }

    [Fact]
    public void EveryMachineASpeciesTakesIsOneThatCanBeHadAndEveryTutorMoveATutorsOwn()
    {
        var sources = ItemSources.Everywhere;
        var shardTutors = MoveTutors.Places.SelectMany(MoveTutors.At).Select(m => m.Move).ToHashSet();
        var hallTutor = MoveTutors.At(MoveTutors.TrainingHall).Select(m => m.Move).ToHashSet();
        var problems = new List<string>();
        foreach (var s in PokemonDatabase.Everyone())
        {
            foreach (string name in (s.TmMoves ?? new()).Concat(s.LaterMachines ?? new()))
            {
                var machine = ItemDatabase.Get(name);
                if (machine == null || !MoveTeaching.IsMachine(machine) || MoveDatabase.Get(machine.TeachesMove!)?.Name != machine.TeachesMove)
                    problems.Add($"{s.Name} takes {name}, which is no machine of a move of the data");
                // Sword and Shield's TRs wait for plan 33's Galar (TrainingToolsTests' Owed list)
                else if (sources.Of(name).Count == 0 && !name.StartsWith("TR", StringComparison.Ordinal)) problems.Add($"{s.Name} takes {name}, which nothing gives");
            }
            problems.AddRange((s.TutorMoves ?? new()).Where(m => !shardTutors.Contains(m)).Select(m => $"{s.Name}'s tutor move {m} is no shard tutor's"));
            problems.AddRange((s.LaterTutorMoves ?? new()).Where(m => !hallTutor.Contains(m)).Select(m => $"{s.Name}'s later tutor move {m} is not the hall's tutor's"));
        }
        Assert.Empty(problems);
        // Every later machine and every move of the hall's tutor is someone's
        var taken = PokemonDatabase.GetAll().SelectMany(s => s.LaterMachines ?? new()).ToHashSet();
        Assert.All(Later, m => Assert.Contains(m.Name, taken));
        var tutored = PokemonDatabase.GetAll().SelectMany(s => s.LaterTutorMoves ?? new()).ToHashSet();
        Assert.All(hallTutor, m => Assert.Contains(m, tutored));
    }

    // ------------------------------------------------------------------ the machines

    [Fact]
    public void TheLaterMachinesAreOursFromTM93InTheGamesOrder()
    {
        var later = Later;
        Assert.Equal(212, later.Count);
        for (int k = 0; k < later.Count; k++)
        {
            Assert.Equal(MoveTeaching.LaterFrom + k, later[k].Id);
            Assert.Equal($"TM{93 + k:D2}", later[k].Name);
        }
        Assert.True(later[^1].Id <= MoveTeaching.LaterTo);
        // Scarlet and Violet's first, from its TM001, Take Down, which no machine of Platinum's teaches; its last, Upper
        // Hand, before Sword and Shield's TM00, Mega Punch; Let's Go's last of all
        Assert.Equal(("TM93", "Take Down"), (later[0].Name, later[0].TeachesMove));
        Assert.Equal("Mega Punch", later[later.FindIndex(i => i.TeachesMove == "Upper Hand") + 1].TeachesMove);
        Assert.Equal("Mega Drain", later[^1].TeachesMove);
        // No move twice, and none a machine of Platinum's, Hoenn's HM09 or Johto's HM10 teaches
        var older = ItemDatabase.GetAll().Where(i => MoveTeaching.IsMachine(i) && !MoveTeaching.IsLater(i) && (i.Id < 1000 || i.Id >= 9000))
            .Select(i => i.TeachesMove).ToHashSet();
        Assert.Equal(later.Count, later.Select(i => i.TeachesMove).Distinct().Count());
        Assert.DoesNotContain(later, i => older.Contains(i.TeachesMove));
        // Tera Blast and Dragon Cheer among them
        Assert.Equal("TM200", MachineFor("Tera Blast").Name);
        Assert.Equal("TM249", MachineFor("Dragon Cheer").Name);
        // A machine of the TMs pocket, used from the bag, at a price of ours by the move's power
        Assert.All(later, i =>
        {
            Assert.Equal(ItemPocket.TMsAndHMs, i.Pocket);
            Assert.Equal("TmHm", i.FieldUse);
            Assert.False(i.CanUseInBattle);
            Assert.StartsWith($"Teaches {i.TeachesMove} to a compatible Pokémon, and is", i.Description);
            Assert.Contains(i.Price, new[] { 2000, 3000, 5500, 7500 });
        });
        Assert.Equal((3000, 2000, 7500, 5500), (MachineFor("Tera Blast").Price, MachineFor("Dragon Cheer").Price, MachineFor("Blast Burn").Price, MachineFor("Draco Meteor").Price));
    }

    [Fact]
    public void EveryLaterMachineIsUsedUpAsEveryTmIs()
    {
        // Plan 39's decision 4: a TM is used up in every game, as Scarlet and Violet's are, and made again at a PC with the
        // crafting kit; so is every later machine of ours, Sword and Shield's TMs and those of Generations 5 to 7 among them,
        // whose own games kept theirs. An HM, which can't be thrown away, is not
        Assert.True(MoveTeaching.IsUsedUp(MachineFor("Dragon Cheer")));
        Assert.True(MoveTeaching.IsUsedUp(MachineFor("Venom Drench")));
        Assert.True(MoveTeaching.IsUsedUp(MachineFor("Mega Punch")));
        Assert.True(MoveTeaching.IsUsedUp(MachineFor("Hone Claws")));
        Assert.True(MoveTeaching.IsUsedUp(Item("TM01")));
        Assert.True(MoveTeaching.IsUsedUp(Item("TR00")));
        Assert.False(MoveTeaching.IsUsedUp(Item("HM01")));
        Assert.False(MoveTeaching.IsUsedUp(Item("HM09")));
        Assert.All(Later, i => Assert.True(MoveTeaching.IsUsedUp(i), i.Name));
        Assert.All(Later, i => Assert.Contains("is used up", i.Description));
        // The TM maker makes each of them again (plan 39 · CR4)
        Assert.All(Later, i => Assert.NotNull(TmMaker.CostOf(i)));

        // Through the bag, by the modern rules: Dragon Cheer's machine teaches a Dipplin and is gone, and so is Mega Punch's
        // once it teaches a Machop
        Ruleset.Use(RulesPreset.Modern);
        try
        {
            var dipplin = Mon("Dipplin", 30, "Dragon Breath", "Astonish");
            var cheer = MachineFor("Dragon Cheer");
            var (inventory, notes) = Teach(dipplin, cheer);
            Assert.True(dipplin.Knows("Dragon Cheer"), string.Join(" | ", notes));
            Assert.Equal(0, inventory.GetQuantity(cheer));

            var machop = Mon("Machop", 30, "Low Kick");
            var punch = MachineFor("Mega Punch");
            var (kept, said) = Teach(machop, punch);
            Assert.True(machop.Knows("Mega Punch"), string.Join(" | ", said));
            Assert.Equal(0, kept.GetQuantity(punch));
        }
        finally { Ruleset.Use(RulesPreset.Platinum); }
    }

    /// <summary>The machine taught from the bag to a Pokémon with room for it (the bag's USE, its target chosen).</summary>
    private static (Inventory Inventory, List<string> Notes) Teach(Pokemon p, ItemData machine)
    {
        var inventory = new Inventory();
        inventory.AddItem(machine, 1);
        var party = new Party();
        party.Add(p);
        var bag = new BagScreen();
        bag.Open();
        var notes = new List<string>();
        bag.CurrentPocket = machine.Pocket;
        bag.SelectedIndex = inventory.GetPocketItems(machine.Pocket).FindIndex(s => s.Data == machine);
        bag.Confirm(inventory, party, notes.Add);
        while (bag.Actions![bag.ActionIndex] != BagAction.Use) bag.MoveCursor(1, 0);
        bag.Confirm(inventory, party, notes.Add);
        Assert.Equal("ABLE", bag.TagFor(p, new EvolutionContext { Party = party, Bag = inventory }));
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        return (inventory, notes);
    }

    [Fact]
    public void TheCompatibilityHoldsAKnownSpeciesBothWays()
    {
        // Scarlet and Violet teach Dipplin Dragon Cheer and Tera Blast by machine, Applin Tera Blast alone (PokeAPI's pokemon_moves)
        var cheer = MachineFor("Dragon Cheer");
        var tera = MachineFor("Tera Blast");
        Assert.True(MoveTeaching.CanLearn(Mon("Dipplin", 20), cheer, Ruleset.Modern));
        Assert.True(MoveTeaching.CanLearn(Mon("Dipplin", 20), tera, Ruleset.Modern));
        Assert.True(MoveTeaching.CanLearn(Mon("Applin", 20), tera, Ruleset.Modern));
        Assert.False(MoveTeaching.CanLearn(Mon("Applin", 20), cheer, Ruleset.Modern));
        // A species of Platinum's: Pikachu takes Volt Switch's machine and not Dragon Cheer's; and Platinum's own HM03,
        // Surf, which its later games teach it and Platinum's list doesn't, by the modern rules alone
        var pikachu = Mon("Pikachu", 20);
        Assert.True(MoveTeaching.CanLearn(pikachu, MachineFor("Volt Switch"), Ruleset.Modern));
        Assert.False(MoveTeaching.CanLearn(pikachu, cheer, Ruleset.Modern));
        Assert.DoesNotContain("HM03", pikachu.Species.TmMoves!);
        Assert.Contains("HM03", pikachu.Species.LaterMachines!);
        Assert.True(MoveTeaching.CanLearn(pikachu, Item("HM03"), Ruleset.Modern));
        Assert.False(MoveTeaching.CanLearn(pikachu, Item("HM03"), Ruleset.Platinum));
        // Never an Egg
        var egg = Mon("Dipplin", 1);
        egg.IsEgg = true;
        Assert.False(MoveTeaching.CanLearn(egg, cheer, Ruleset.Modern));
        // A species of ours takes its forebear's (Hydrapple's line past 100 keeps what Hydrapple takes)
        foreach (var ours in PokemonDatabase.Ours())
        {
            var from = SpeciesPastHundredForebear(ours);
            Assert.Equal(from.LaterMachines ?? new(), ours.LaterMachines ?? new());
            Assert.Equal(from.LaterTutorMoves ?? new(), ours.LaterTutorMoves ?? new());
        }
    }

    private static PokemonSpecies SpeciesPastHundredForebear(PokemonSpecies ours) => DataImporter.SpeciesPastHundred.ForebearOf(ours, PokemonDatabase.Everyone());

    [Fact]
    public void TeraBlastIsLearnableAndCanBeBoughtInSinnoh()
    {
        var tera = MachineFor("Tera Blast");
        Assert.True(ItemSources.In(RegionDatabase.Sinnoh).Has(tera.Name));
        // Every species Scarlet and Violet have (726 of the games' 1,025)
        var takers = PokemonDatabase.GetAll().Where(s => s.LaterMachines?.Contains(tera.Name) == true).ToList();
        Assert.True(takers.Count > 700, $"{takers.Count} species take {tera.Name}");
        Assert.True(Legality.CanLearn(PokemonDatabase.Get("Applin")!, "Tera Blast"));
        Assert.True(Legality.CanLearn(PokemonDatabase.Get("Garchomp")!, "Tera Blast"));
    }

    [Fact]
    public void ADipplinTaughtDragonCheerEvolvesIntoHydrapple()
    {
        var dipplin = Mon("Dipplin", 40, "Dragon Breath", "Astonish", "Recycle");
        var party = new Party();
        party.Add(dipplin);
        var context = new EvolutionContext { Party = party, Bag = new Inventory() };
        // Nothing teaches it Dragon Cheer by level, and without it a level brings nothing
        Assert.DoesNotContain(dipplin.Learnset, l => l.MoveName == "Dragon Cheer");
        Assert.Null(Evolution.Find(dipplin, EvolutionTrigger.LevelUp, context));

        Assert.True(MoveTeaching.CanLearn(dipplin, MachineFor("Dragon Cheer"), Ruleset.Modern));
        MoveTeaching.Learn(dipplin, "Dragon Cheer");
        var evolution = Evolution.Find(dipplin, EvolutionTrigger.LevelUp, context);
        Assert.NotNull(evolution);
        Assert.Equal("Hydrapple", evolution!.TargetSpecies);
        Evolution.Evolve(dipplin, evolution, context);
        Assert.Equal("Hydrapple", dipplin.Species.Name);
        Assert.True(dipplin.Knows("Dragon Cheer"));
    }

    [Fact]
    public void PlatinumsRulesTeachOnlyPlatinumsMachines()
    {
        var later = Later;
        foreach (var s in PokemonDatabase.Everyone())
        {
            // A species' own list never holds a later machine, and Platinum's rules teach it none of its later list
            Assert.DoesNotContain(s.TmMoves ?? new(), m => MoveTeaching.IsLater(Item(m)));
            foreach (var m in later) Assert.False(MoveTeaching.Takes(s, m, Ruleset.Platinum), $"{s.Name} takes {m.Name} by Platinum's rules");
            foreach (string m in s.LaterMachines ?? new())
                Assert.Equal(s.TmMoves?.Contains(m) == true, MoveTeaching.Takes(s, Item(m), Ruleset.Platinum));
        }
        Assert.False(Ruleset.Platinum.LaterMachines);
        Assert.True(Ruleset.Modern.LaterMachines);
    }

    // ------------------------------------------------------------------ the hall's tutor and shelf

    [Fact]
    public void TheHallsTutorTeachesTheLaterTutorsMovesThatNoMachineTeaches()
    {
        var hall = MoveTutors.At(MoveTutors.TrainingHall);
        Assert.Equal(27, hall.Count);
        Assert.True(MoveTutors.IsPlace(MoveTutors.TrainingHall));
        Assert.DoesNotContain(MoveTutors.TrainingHall, MoveTutors.Places);
        Assert.Equal(hall.Count, hall.Select(m => m.Move).Distinct().Count());
        Assert.All(hall, m => Assert.NotNull(MoveDatabase.Get(m.Move)));
        Assert.All(hall, m => Assert.Equal(8, m.Red + m.Blue + m.Yellow + m.Green));
        var machineMoves = ItemDatabase.GetAll().Where(MoveTeaching.IsMachine).Select(i => i.TeachesMove).ToHashSet();
        Assert.DoesNotContain(hall, m => machineMoves.Contains(m.Move));
        Assert.Equal(new TutorMove("Rising Voltage", 0, 2, 6, 0), hall.Single(m => m.Move == "Rising Voltage"));
        Assert.Equal("4 Red, 4 Green", hall.Single(m => m.Move == "Synthesis").Price);

        // Pikachu's later tutors' moves, in the tutor's order; one it knows is left out; an Egg learns none
        var pikachu = Mon("Pikachu", 30, "Thunder Shock");
        Assert.Equal(new[] { "Rising Voltage", "Covet", "Laser Focus", "Magnet Rise", "Signal Beam", "Volt Tackle" },
            MoveTutors.Teachable(pikachu, MoveTutors.TrainingHall).Select(m => m.Move));
        pikachu.Moves.Add(MoveDatabase.Create("Covet"));
        Assert.DoesNotContain("Covet", MoveTutors.Teachable(pikachu, MoveTutors.TrainingHall).Select(m => m.Move));
        pikachu.IsEgg = true;
        Assert.Empty(MoveTutors.Teachable(pikachu, MoveTutors.TrainingHall));
        // A shard tutor of Platinum's still teaches by Platinum's list alone: Porygon learns Magnet Rise from a later game's
        // tutor and not from Platinum's, so Snowpoint City's won't teach it and the hall's will
        var porygon = Mon("Porygon", 30, "Tackle");
        Assert.DoesNotContain("Magnet Rise", MoveTutors.Teachable(porygon, MoveTutors.Snowpoint).Select(m => m.Move));
        Assert.Contains("Magnet Rise", MoveTutors.Teachable(porygon, MoveTutors.TrainingHall).Select(m => m.Move));
    }

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static HeadlessScriptHost Champion(Map hall, params int[] answers)
    {
        var host = new HeadlessScriptHost { Map = hall };
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        return host;
    }

    private static HeadlessScriptHost Run(string script, Map hall, NPC who, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, hall.Name) ?? throw new InvalidOperationException($"no script {script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheHallsTutorTeachesForShardsAndTheClerkSellsTheMachines()
    {
        var hall = Room("SunyshoreTrainingHall");
        var tutor = hall.Everyone.Single(n => n.Script == "TrainingTutor");
        // Behind the counter, between the head coach and the clerk, and spoken to across it
        Assert.Equal((12, 3), (tutor.GridX, tutor.GridY));
        Assert.True(hall.IsCounter(12, 4));

        // Yes to him, the first of the team, Rising Voltage (2 Blue, 6 Yellow) into a free place, the Shards paid once learned
        var host = Champion(hall, 0, 0);
        host.PokemonChoice = 0;
        var pikachu = Mon("Pikachu", 30, "Thunder Shock");
        host.Party.Add(pikachu);
        host.Bag.AddItem(Item("Blue Shard"), 3);
        host.Bag.AddItem(Item("Yellow Shard"), 6);
        Run("TrainingTutor", hall, tutor, host);
        Assert.True(pikachu.Knows("Rising Voltage"), string.Join(" | ", host.Transcript.Select(t => t.Text)));
        Assert.Equal((1, 0), (host.Bag.GetQuantity(Item("Blue Shard")), host.Bag.GetQuantity(Item("Yellow Shard"))));

        // Too few Shards: nothing learned, nothing taken; before a Hall of Fame he only says so
        var poor = Champion(hall, 0, 0);
        poor.PokemonChoice = 0;
        var other = Mon("Pikachu", 30, "Thunder Shock");
        poor.Party.Add(other);
        poor.Bag.AddItem(Item("Yellow Shard"), 6);
        Run("TrainingTutor", hall, tutor, poor);
        Assert.False(other.Knows("Rising Voltage"));
        Assert.Equal(6, poor.Bag.GetQuantity(Item("Yellow Shard")));
        var early = Run("TrainingTutor", hall, tutor, new HeadlessScriptHost { Map = hall });
        Assert.Empty(early.Asked);
        Assert.Contains(early.Transcript, t => t.Text.Contains("only to Champions"));

        // The clerk's second shelf is the later machines, in their order, at their prices
        var clerk = hall.Everyone.Single(n => n.Script == "TrainingClerk");
        var shelf = Run("TrainingClerk", hall, clerk, Champion(hall, 1));
        Assert.Contains("open Shop training_hall_machines", shelf.Log);
        Assert.Equal(Later.Select(i => i.Name), MartDatabase.Stock("training_hall_machines", 0).Select(i => i.Name));
        var tools = Run("TrainingClerk", hall, clerk, Champion(hall, 0));
        Assert.Contains("open Shop training_hall", tools.Log);
        var none = Run("TrainingClerk", hall, clerk, Champion(hall, 2));
        Assert.DoesNotContain(none.Log, l => l.StartsWith("open Shop", StringComparison.Ordinal));
    }

    [Fact]
    public void TheTutorIsPlayedOnEveryWayThroughHim()
    {
        var walk = new ScriptWalk();
        var hall = Room("SunyshoreTrainingHall");
        var script = ScriptLibrary.Default.Find("TrainingTutor", hall.Name)!;
        var tutor = hall.Everyone.Single(n => n.Script == "TrainingTutor");
        foreach (bool champion in new[] { false, true })
        {
            var ways = walk.EveryWayThrough(script, hall, tutor, before: host =>
            {
                if (champion) host.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
                host.Bag.AddItem(Item("Blue Shard"), 8);
                host.Bag.AddItem(Item("Yellow Shard"), 8);
                host.Party.Add(Mon("Pikachu", 30, "Thunder Shock", "Quick Attack", "Thunder Wave", "Fly"));
            });
            Assert.NotEmpty(ways);
            Assert.All(ways, way => Assert.Empty(way.Host.Problems));
            if (!champion) Assert.All(ways, way => Assert.Empty(way.Host.Asked));
        }
    }
}
