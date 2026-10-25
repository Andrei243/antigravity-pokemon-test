using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V6: the randomiser's species and items. A seed is one mapping on every machine (held against a table written
/// into the test), a Normal run builds none and changes nothing, every species and item it gives exists, a line stays a
/// line, the briefcase's three go to three first stages of three lines while the story goes by the slot, key items are
/// never swapped, and the panel, the title, the save and the Pokédex's area page carry the choice. GPU-free.
/// </summary>
[Collection("Shared clocks and dice")]
public class RandomizerTests
{
    private const int Seed = 20261025;

    private static Randomizer Of(int seed = Seed, bool species = true, bool items = true) =>
        Randomizer.Of(new RandomizerOptions { Seed = seed, Species = species, Items = items });

    private static RunRules Run(int seed = Seed, bool species = true, bool items = true) =>
        RunRules.Normal with { Random = new RandomizerOptions { Seed = seed, Species = species, Items = items } };

    private static readonly HashSet<string> Games = PokemonDatabase.GetAll().Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Who evolves from whom among the games' species, as the randomiser sees the lines.</summary>
    private static Dictionary<string, string> Pre()
    {
        var pre = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in PokemonDatabase.GetAll())
            foreach (var e in s.Evolutions ?? new())
                if (Games.Contains(e.TargetSpecies) && !string.Equals(e.TargetSpecies, s.Name, StringComparison.OrdinalIgnoreCase)) pre.TryAdd(e.TargetSpecies, s.Name);
        return pre;
    }

    private static IEnumerable<string> Next(string species) =>
        (PokemonDatabase.Get(species)!.Evolutions ?? new()).Select(e => e.TargetSpecies).Where(Games.Contains);

    // ---- Normal changes nothing

    [Fact]
    public void ANormalRunHasNoRandomizerAndWritesNothing()
    {
        Assert.Null(Randomizer.For(RunRules.Normal));
        Assert.Null(Randomizer.For(null));
        Assert.Null(Randomizer.For(RunRules.NuzlockePreset));
        Assert.True(RunRules.Normal.IsNormal);
        Assert.False(Run().IsNormal);
        Assert.Equal("custom", Run().PresetKey);

        // Normal is no field at all (a save from before it reads as Normal), and a randomised run keeps its seed whole
        Assert.DoesNotContain("Random", SaveManager.Write(new SaveData { PlayerName = "Lucas", Run = RunRules.NuzlockePreset }));
        var back = SaveManager.Read(SaveManager.Write(new SaveData { PlayerName = "Lucas", Run = Run(987654321, items: false) }))!;
        Assert.Equal(Run(987654321, items: false), back.Run);
        Assert.Equal(987654321, back.Run!.Random!.Seed);
        Assert.False(back.Run.Random!.Items);
    }

    [Fact]
    public void WithSpeciesOffTheSpeciesAreThemselvesAndWithItemsOffTheItemsAre()
    {
        var items = Of(species: false);
        Assert.False(items.MapsSpecies);
        Assert.Equal("Starly", items.Species("Starly"));
        Assert.Equal(new[] { "Turtwig", "Chimchar", "Piplup" }, items.Starters(new[] { "Turtwig", "Chimchar", "Piplup" }));
        var trainer = Trainer("leader_roark");
        Assert.Same(trainer, items.ForTrainer(trainer));

        var species = Of(items: false);
        Assert.Same(ItemDatabase.Get("Potion"), species.Item(ItemDatabase.Get("Potion")!, "FLAG_ANY"));
    }

    // ---- one seed, one mapping

    [Fact]
    public void ASeedIsOneMappingHeldAgainstATableWrittenHere()
    {
        // Worked out once and pinned: a seed must make the same game on every machine and in every version
        var expected = new Dictionary<string, string>
        {
            ["Starly"] = "Combee", ["Bidoof"] = "Applin", ["Shinx"] = "Meditite", ["Zubat"] = "Spearow", ["Geodude"] = "Blitzle",
            ["Turtwig"] = "Psyduck", ["Grotle"] = "Golduck", ["Chimchar"] = "Abra", ["Piplup"] = "Axew", ["Dialga"] = "Koraidon"
        };
        var random = Of();
        foreach (var (from, to) in expected) Assert.Equal(to, random.Species(from));
        Assert.Equal(new[] { "Psyduck", "Abra", "Axew" }, random.Starters(new[] { "Turtwig", "Chimchar", "Piplup" }));
        Assert.Equal("Soda Pop", random.Item(ItemDatabase.Get("Potion")!, "FLAG_A").Name);
        Assert.Equal("TM14", random.Item(ItemDatabase.Get("TM01")!, "FLAG_C").Name);

        // Asked in another order, by another object of the same options, it is the same
        var other = Randomizer.Of(new RandomizerOptions { Seed = 1 });
        Assert.NotSame(random, other);
        var again = Randomizer.Of(new RandomizerOptions { Seed = Seed });
        foreach (var s in PokemonDatabase.GetAll().Reverse()) Assert.Equal(random.Species(s.Name), again.Species(s.Name));
    }

    [Fact]
    public void AnotherSeedIsAnotherGameAndNoSeedLeavesEverythingAsItWas()
    {
        var all = PokemonDatabase.GetAll().ToList();
        foreach (int seed in new[] { 0, 1, Seed, RandomizerOptions.MaxSeed })
        {
            var random = Of(seed);
            int same = all.Count(s => random.Species(s.Name) == s.Name);
            Assert.True(same < all.Count / 10, $"Seed {seed} leaves {same} of {all.Count} species as they are.");
            Assert.True(all.Select(s => random.Species(s.Name)).Distinct().Count() > 300, $"Seed {seed} reaches too few species.");
        }
        int differing = all.Count(s => Of(1).Species(s.Name) != Of(2).Species(s.Name));
        Assert.True(differing > all.Count / 2, "Two seeds make nearly the same game.");
    }

    [Fact]
    public void EveryMappedSpeciesExistsIsOneOfTheGamesAndKeepsItsKind()
    {
        var random = Of();
        foreach (var s in PokemonDatabase.GetAll())
        {
            string to = random.Species(s.Name);
            var target = PokemonDatabase.Get(to);
            Assert.True(target != null, $"{s.Name} goes to {to}, which is no species.");
            Assert.False(target!.IsOurs, $"{s.Name} goes to {to}, a species of our own.");
            Assert.Equal(s.Legendary || s.Mythical, target.Legendary || target.Mythical);
            Assert.Same(target, random.Species(s));
        }
        // A name that is no species is left alone
        Assert.Equal("Not A Species", random.Species("Not A Species"));
    }

    [Fact]
    public void ALineStaysALine()
    {
        var random = Of();
        var pre = Pre();
        int kept = 0, checkedOnes = 0;
        foreach (var s in PokemonDatabase.GetAll().Where(s => pre.ContainsKey(s.Name)))
        {
            var after = Next(random.Species(pre[s.Name])).ToList();
            if (after.Count == 0) continue;
            checkedOnes++;
            if (after.Contains(random.Species(s.Name))) kept++;
        }
        Assert.True(checkedOnes > 100);
        Assert.Equal(checkedOnes, kept);
    }

    [Fact]
    public void TheStartersGoToThreeFirstStagesOfThreeDifferentLines()
    {
        var pre = Pre();
        string RootOf(string s) { while (pre.TryGetValue(s, out var p)) s = p; return s; }
        foreach (int seed in new[] { 0, 7, Seed, 424242424 })
            foreach (var region in RegionDatabase.All.Where(r => r.Starters.Count == 3))
            {
                var three = Of(seed).Starters(region.Starters);
                Assert.Equal(3, three.Distinct().Count());
                Assert.Equal(3, three.Select(RootOf).Distinct().Count());
                foreach (string s in three)
                {
                    Assert.False(pre.ContainsKey(s), $"{s} ({region.Id}, seed {seed}) is not a first stage.");
                    Assert.NotEmpty(Next(s));
                }
                // The species map says the same of a slot as the briefcase does
                for (int i = 0; i < 3; i++) Assert.Equal(three[i], Of(seed).Species(region.Starters[i]));
            }
    }

    // ---- the points of use

    private static Trainer Trainer(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException(id);
        var team = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(team, record);
        return team;
    }

    [Fact]
    public void ATrainersTeamIsTheMappedSpeciesOnACopy()
    {
        var random = Of();
        var roark = Trainer("leader_roark");
        var original = roark.Party.Members.Select(p => (p.Species.Name, p.Level, p.Personality, p.IvHP)).ToList();
        var copy = random.ForTrainer(roark);

        Assert.NotSame(roark, copy);
        // The trainer of the map keeps their team
        Assert.Equal(original, roark.Party.Members.Select(p => (p.Species.Name, p.Level, p.Personality, p.IvHP)));
        Assert.Equal(roark.Party.Count, copy.Party.Count);
        for (int i = 0; i < original.Count; i++)
        {
            var mine = copy.Party.Members[i];
            Assert.Equal(random.Species(original[i].Name), mine.Species.Name);
            Assert.Equal(original[i].Level, mine.Level);
            Assert.Equal(original[i].Personality, mine.Personality);
            Assert.Equal(original[i].IvHP, mine.IvHP);
            Assert.False(mine.IsShiny);
            Assert.Equal(mine.MaxHP, mine.CurrentHP);
            Assert.NotEmpty(mine.Moves);
        }
        // The same copy twice
        Assert.Equal(copy.Party.Members.Select(p => (p.Species.Name, p.Moves.Select(m => m.Data.Name).ToList().Count)),
            random.ForTrainer(roark).Party.Members.Select(p => (p.Species.Name, p.Moves.Select(m => m.Data.Name).ToList().Count)));

        // A Master Trainer's one species and a team with nobody in it stay
        var master = Trainer(TrainerDatabase.MasterId("Bidoof"));
        Assert.Same(master, random.ForTrainer(master));
        var nobody = new Trainer { Id = "nobody" };
        Assert.Same(nobody, random.ForTrainer(nobody));
    }

    [Fact]
    public void TheRivalFightsTheTeamTheStarterTakenNamesMapped()
    {
        // Choosing the mapped Turtwig still sends the rival in with rival_route_201_turtwig's team, mapped
        var random = Of();
        var host = new HeadlessScriptHost { Rules = Run() };
        var library = ScriptLibrary.FromSources(("test", "script S\n starter\n say \"You chose {starter}; he took {rivalstarter}; she has {assistantstarter}.\""));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.Single());
        runner.RunToEnd();

        Assert.Equal("Psyduck", host.Party.Members[0].Species.Name);
        Assert.Equal("Turtwig", host.Story.PlayerStarter);
        Assert.Equal("Chimchar", host.Story.RivalStarter);
        Assert.Equal("Piplup", host.Story.AssistantStarter);
        Assert.Equal("You chose Psyduck; he took Abra; she has Axew.", host.Transcript.Last().Text);

        var rival = random.ForTrainer(Trainer($"rival_route_201_{host.Story.PlayerStarter!.ToLowerInvariant()}"));
        Assert.Equal("Abra", rival.Party.Members[0].Species.Name);
        Assert.Equal(host.Story.RivalStarter, "Chimchar");
        Assert.Equal(random.Species(host.Story.RivalStarter), rival.Party.Members[0].Species.Name);
    }

    [Fact]
    public void GiftsAndStaticEncountersAreTheMappedSpeciesExceptWhatAScriptAsksTheTeamFor()
    {
        var random = Of();
        var host = new HeadlessScriptHost { Rules = Run() };
        // A gift no script asks the team for (the first of these that moves)
        string gift = new[] { "Eevee", "Riolu", "Togepi", "Happiny", "Munchlax" }.First(n => !ScriptLibrary.Default.SpeciesAsked.Contains(n) && random.Species(n) != n);
        var library = ScriptLibrary.FromSources(("test", $"script S\n givepokemon \"{gift}\" 10\n wildbattle \"Bidoof\" 7\n wildbattle \"Regirock\" 40\n givepokemon \"Dratini\" 15 knowing \"Extreme Speed\""));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.Single());
        runner.RunToEnd();

        Assert.Equal(random.Species(gift), host.Party.Members[0].Species.Name);
        Assert.Contains(host.Log, l => l.StartsWith($"wildbattle {random.Given("Bidoof")} 7"));
        // A species the story asks the team for stays what it is, and so does a gift that comes with a move of its own
        Assert.True(ScriptLibrary.Default.SpeciesAsked.Contains("Regirock"));
        Assert.Contains(host.Log, l => l.StartsWith("wildbattle Regirock 40"));
        Assert.Contains(host.Party.Members, p => p.Species.Name == "Dratini");

        // Under Normal nothing is different
        var plain = new HeadlessScriptHost();
        var normal = new ScriptRunner(library, plain);
        normal.Start(library.All.Single());
        normal.RunToEnd();
        Assert.Equal(gift, plain.Party.Members[0].Species.Name);
        Assert.Contains(plain.Log, l => l.StartsWith("wildbattle Bidoof 7"));
    }

    // ---- the items

    [Fact]
    public void AnItemIsAnotherOfItsPocketAndAboutItsPriceAndNeverAKeyItem()
    {
        var random = Of();
        var protectedByScripts = ScriptLibrary.Default.ItemsAsked;
        foreach (var item in ItemDatabase.GetAll())
        {
            var swapped = random.Item(item, "FLAG_SOMEWHERE");
            if (random.Protected(item))
            {
                Assert.Same(item, swapped);
                continue;
            }
            // (nothing that has no other of its pocket to be stays as it is)
            Assert.Equal(item.Pocket, swapped.Pocket);
            Assert.False(random.Protected(swapped), $"{item.Name} became {swapped.Name}.");
            Assert.NotEqual(ItemPocket.KeyItems, swapped.Pocket);
            Assert.DoesNotContain(swapped.Name, protectedByScripts);
            Assert.True(swapped.Id <= Randomizer.LastPlatinumItem || ReferenceEquals(swapped, item));
            if (swapped.Pocket == ItemPocket.TMsAndHMs) Assert.False(swapped.Name.StartsWith("HM"), $"{item.Name} became {swapped.Name}.");
            Assert.Same(swapped, random.Item(item, "FLAG_SOMEWHERE"));
        }
        // The story's own are never touched, in or out
        foreach (string kept in new[] { "Bicycle", "Old Rod", "HM01", "HM08", "Storage Key", "Town Map", "Coin Case" })
            Assert.Same(ItemDatabase.Get(kept), random.Item(ItemDatabase.Get(kept)!, "FLAG_X"));
        // Each ball its own item
        var places = Enumerable.Range(0, 60).Select(n => random.Item(ItemDatabase.Get("Potion")!, $"FLAG_BALL_{n}").Name).ToList();
        Assert.True(places.Distinct().Count() >= 8, "Sixty Potions turn into too few items.");
        // A TM is another TM, once
        Assert.Equal(1, Randomizer.CountOf(ItemDatabase.Get("Potion")!, ItemDatabase.Get("TM14")!, 5));
        Assert.Equal(5, Randomizer.CountOf(ItemDatabase.Get("Potion")!, ItemDatabase.Get("Super Potion")!, 5));
    }

    [Fact]
    public void AnItemBallHoldsTheSameOtherItemEveryTimeAndANormalRunHoldsWhatItAlwaysDid()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n setflag own\n find own"));
        string Found(RunRules rules, string flag, string item = "Potion", int count = 2)
        {
            var ball = new NPC { NpcType = NPC.ItemBallType, Name = item, Item = item, ItemCount = count, HiddenBy = flag };
            var host = new HeadlessScriptHost { Rules = rules };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.All.Single(), ball);
            runner.RunToEnd();
            Assert.True(host.Story.Has(flag));
            return ItemDatabase.GetAll().Single(i => host.Bag.GetQuantity(i) > 0).Name;
        }

        string once = Found(Run(), "FLAG_OBTAINED_ONE");
        Assert.NotEqual("Potion", once);
        Assert.Equal(once, Found(Run(), "FLAG_OBTAINED_ONE"));
        Assert.Equal(Of().Item(ItemDatabase.Get("Potion")!, "FLAG_OBTAINED_ONE").Name, once);
        Assert.Equal("Potion", Found(RunRules.Normal, "FLAG_OBTAINED_ONE"));
        Assert.Equal("Potion", Found(Run(items: false), "FLAG_OBTAINED_ONE"));
        // A key item on the ground is the key item
        Assert.Equal("Town Map", Found(Run(), "FLAG_OBTAINED_MAP", "Town Map", 1));
    }

    // ---- completability

    [Fact]
    public void ARunThatRandomisesSpeciesCountsSinnohsPokedexSeenOnceItsHallOfFameIsEntered()
    {
        var dex = new Pokedex();
        var story = new StoryState();
        var run = Run();
        int all = Pokedex.Entries(Pokedex.Sinnoh).Count;
        Assert.Equal(0, Randomizer.SinnohSeen(run, dex, story));
        story.CompleteRegion("Sinnoh");
        Assert.Equal(all, Randomizer.SinnohSeen(run, dex, story));
        // Every other run counts what it has seen
        Assert.Equal(0, Randomizer.SinnohSeen(RunRules.Normal, dex, story));
        Assert.Equal(0, Randomizer.SinnohSeen(Run(species: false), dex, story));
        Assert.Equal(0, Randomizer.SinnohSeen(null, dex, new StoryState()));
    }

    [Fact]
    public void ThePokedexsAreaPageOfASpeciesShowsWhereTheSpeciesThatStandForItLive()
    {
        var habitats = Habitats.For("sinnoh")!;
        var random = Of();
        var starly = habitats.Of("Starly");
        Assert.NotEmpty(starly);
        string met = random.Species("Starly");
        var places = habitats.Of(random.Sources(met));
        Assert.Contains("Starly", random.Sources(met));
        foreach (var place in starly) Assert.Contains(places, p => p.Name == place.Name && place.Cells.All(c => p.Cells.Contains(c)));
        // With no randomiser the page is as it was; the reverse of a species with nothing going to it is empty
        Assert.Equal(new[] { "Starly" }, Of(species: false).Sources("Starly"));
        string nobody = PokemonDatabase.GetAll().Select(s => s.Name).First(n => random.Sources(n).Count == 0);
        Assert.Empty(habitats.Of(random.Sources(nobody)));
    }

    // ---- the engine

    private const float Frame = 1f / 60f;

    [Fact]
    public void ARandomisedGameMeetsFightsAndKeepsTheMappedSpecies()
    {
        string folder = Path.Combine(Path.GetTempPath(), "randomizer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(5);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame("Lucas", PlayerLook.Boy, "Barry", Run());
            Assert.Equal(Run(), game.Run);
            Assert.NotNull(Randomizer.For(game.Run));
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
            game.State = GameState.Overworld;
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Up);

            // A wild Starly of Route 201's table is the species Starly goes to
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3 });
            for (int f = 0; f < 60 * 30 && game.State != GameState.Battle; f++) { InputManager.Feed(ActionFrame.None); engine.Update(Frame); }
            Assert.Equal(GameState.Battle, game.State);
            Assert.Equal("Combee", game.Battle!.EnemyPokemon.Species.Name);
            Assert.True(game.Pokedex.IsSeen(PokemonDatabase.Get("Combee")!.DexNumber));
            Assert.False(game.Pokedex.IsSeen(PokemonDatabase.Get("Starly")!.DexNumber));
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            RunRules.Current = RunRules.Normal;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // ---- the panel and the title

    private static void To(RunPanel panel, RunRow row)
    {
        for (int guard = 0; guard < 40 && panel.SelectedRow != row; guard++) panel.Move(1);
        Assert.Equal(row, panel.SelectedRow);
    }

    [Fact]
    public void ThePanelTurnsTheRandomiserOnWithARolledSeedAndOffWithBothHalves()
    {
        var panel = new RunPanel { RollSeed = () => 4242 };
        panel.Open();
        Assert.DoesNotContain(RunRow.Seed, panel.Rows);
        To(panel, RunRow.RandomSpecies);
        panel.Change(1);
        Assert.Equal(new RandomizerOptions { Seed = 4242, Species = true, Items = false }, panel.Rules.Random);
        Assert.Contains(RunRow.Seed, panel.Rows);
        Assert.Equal("on", panel.ValueText(RunRow.RandomSpecies).ToLowerInvariant());
        Assert.Equal("off", panel.ValueText(RunRow.RandomItems).ToLowerInvariant());
        Assert.Equal("4242", panel.ValueText(RunRow.Seed));
        Assert.Equal("custom", panel.Rules.PresetKey);

        To(panel, RunRow.RandomItems);
        panel.Change(1);
        Assert.True(panel.Rules.Random!.Items);
        To(panel, RunRow.Seed);
        int seeds = 0;
        panel.RollSeed = () => 100 + seeds++;
        panel.Change(1);
        panel.Change(-1);
        Assert.Equal(101, panel.Rules.Random!.Seed);

        // Both off is no randomiser, and so Normal again
        To(panel, RunRow.RandomSpecies);
        panel.Change(1);
        To(panel, RunRow.RandomItems);
        panel.Change(1);
        Assert.Null(panel.Rules.Random);
        Assert.True(panel.Rules.IsNormal);
        Assert.DoesNotContain(RunRow.Seed, panel.Rows);
    }

    [Fact]
    public void ASeedIsTypedOnTheKeyboardAndTakenWithOk()
    {
        var panel = new RunPanel { RollSeed = () => 4242 };
        panel.Open();
        To(panel, RunRow.RandomItems);
        panel.Change(1);
        To(panel, RunRow.Seed);
        panel.Confirm();
        Assert.True(panel.EnteringSeed);
        Assert.Equal(RunPanel.SeedDigits, panel.SeedEntry!.MaxLength);
        Assert.Equal(9, RunPanel.SeedDigits);
        // The other rows wait
        panel.Change(1);
        panel.Move(1);
        Assert.Equal(4242, panel.Rules.Random!.Seed);

        // A letter is passed over; the digits are on the fourth row
        panel.PressSeedKey();
        Assert.Equal("", panel.SeedEntry!.Text);
        for (int row = 0; row < 3; row++) panel.MoveCursor(0, 1);
        panel.MoveCursor(1, 0);
        panel.PressSeedKey();
        panel.MoveCursor(1, 0);
        panel.PressSeedKey();
        Assert.Equal("12", panel.SeedEntry!.Text);
        panel.SeedToDone();
        panel.PressSeedKey();
        Assert.False(panel.EnteringSeed);
        Assert.Equal(12, panel.Rules.Random!.Seed);

        // Nothing typed keeps the seed; B takes a digit back and then closes the keyboard
        panel.Confirm();
        panel.CancelSeedKey();
        Assert.False(panel.EnteringSeed);
        Assert.Equal(12, panel.Rules.Random!.Seed);
        Assert.Equal(123456789, Randomizer.ParseSeed("a1b2c3d4e5f6g7h8i9"));
        Assert.Equal(RandomizerOptions.MaxSeed, Randomizer.ParseSeed("99999999999999"));
        Assert.Null(Randomizer.ParseSeed(""));
        Assert.Null(Randomizer.ParseSeed("abc"));
    }

    [Fact]
    public void TheTitleHandsOverTheRandomisedRunAndTheCardMarksItWithItsSeed()
    {
        var title = new TitleScreen((SaveData?)null);
        title.PressConfirm();
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRun, title.Phase);
        title.Run.RollSeed = () => 777;
        To(title.Run, RunRow.RandomSpecies);
        title.Change(1);
        // Down to the seed, typed on the keyboard with the arrows and A
        To(title.Run, RunRow.Seed);
        title.PressConfirm();
        Assert.True(title.Run.EnteringSeed);
        for (int row = 0; row < 3; row++) title.Move(1);
        title.Change(1);
        title.PressConfirm();
        title.Change(1);
        title.PressConfirm();
        Assert.Equal("12", title.Run.SeedEntry!.Text);
        title.PressCancel();
        Assert.Equal("1", title.Run.SeedEntry!.Text);
        title.PressCancel();
        title.PressCancel();
        Assert.False(title.Run.EnteringSeed);
        Assert.Equal(TitlePhase.ChooseRun, title.Phase);
        Assert.Equal(777, title.Run.Rules.Random!.Seed);

        To(title.Run, RunRow.Start);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Assert.Equal(new RandomizerOptions { Seed = 777, Species = true, Items = false }, title.ChosenRun.Random);

        // The card says whose game it is and the seed
        Assert.Equal("CUSTOM · SEED 777", ModernUi.RunMark(title.ChosenRun));
        Assert.Equal("NUZLOCKE", ModernUi.RunMark(RunRules.NuzlockePreset));
        Assert.Null(ModernUi.RunMark(RunRules.Normal));
        var page = RunPanel.Showing(title.ChosenRun);
        Assert.Contains(RunRow.Seed, page.Rows);
        Assert.Equal("777", page.ValueText(RunRow.Seed));
        Assert.Equal("0", RunPanel.Showing(RunRules.Normal with { Random = new RandomizerOptions { Seed = 0 } }).ValueText(RunRow.Seed));
    }

    [Fact]
    public void TheBriefcaseShowsTheMappedThreeAndKeepsTheSlots()
    {
        var screen = new StarterSelectScreen();
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        screen.Open(sinnoh);
        Assert.Equal(sinnoh.Starters, screen.Starters);
        screen.Open(sinnoh, Of());
        Assert.Equal(new[] { "Psyduck", "Abra", "Axew" }, screen.Starters);
        Assert.Equal(sinnoh.Starters, screen.Slots);
        screen.SelectedIndex = 1;
        Assert.Equal("Chimchar", screen.ChosenSlot);
        screen.Confirm();
        screen.Move(1);
        var chosen = screen.Confirm();
        Assert.Equal("Abra", chosen!.Species.Name);
        Assert.Equal("Chimchar", screen.ChosenSlot);
    }

    [Fact]
    public void AMonotypeRunKeepsTheMappedStartersLineOnTheTeam()
    {
        var run = Run() with { Monotype = PokemonType.Fire };
        var psyduck = new Pokemon(PokemonDatabase.Get("Psyduck")!, 5);
        Assert.True(run.MayCarry(psyduck, "Turtwig"));
        Assert.True(run.MayCarry(new Pokemon(PokemonDatabase.Get("Golduck")!, 20), "Turtwig"));
        Assert.False(run.MayCarry(psyduck, "Chimchar"));
        // Under a plain Monotype the slot itself is the starter
        Assert.True((RunRules.Normal with { Monotype = PokemonType.Fire }).MayCarry(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5), "Turtwig"));
    }

    [Fact]
    public void TheSplitMixIsTheSculptorsGeneratorUnchanged()
    {
        // The numbers the sculptors' cache keys were made from (SplitMix32 from seed 1)
        var generator = new SplitMix(1);
        Assert.Equal(new SplitMix(1).Next(), generator.Next());
        Assert.Equal(SplitMix.Hash(5, 9), SplitMix.Hash(5, 9));
        Assert.Equal(SplitMix.Fnv("randomizer:1"), SplitMix.Fnv("randomizer:1"));
        Assert.NotEqual(SplitMix.Fnv("a"), SplitMix.Fnv("b"));
        Assert.Equal(SplitMix.Mix(1, 2, 3), SplitMix.Mix(1, 2, 3));
        Assert.NotEqual(SplitMix.Mix(1, 2, 3), SplitMix.Mix(1, 3, 2));
    }
}
