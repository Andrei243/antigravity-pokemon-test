using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V7: the randomiser's abilities, moves and types. The swap is made in memory for the game in progress and put
/// back whole (by itself and by <see cref="Ruleset.Use"/>), a run without them swaps nothing, a line keeps its types and
/// abilities together, a learnset keeps its levels and a move of the species' own type, and a trainer's copy keeps a move
/// of its type (Roark a Rock move). Runs alone: the swap changes the species everyone reads.
/// </summary>
[Collection("Shared clocks and dice")]
public class RandomizerTraitsTests
{
    private const int Seed = 20261031;

    private static RunRules Run(bool abilities = true, bool moves = true, bool types = true, bool species = false, bool items = false, int seed = Seed) =>
        RunRules.Normal with { Random = new RandomizerOptions { Seed = seed, Species = species, Items = items, Abilities = abilities, Moves = moves, Types = types } };

    private static Trainer Trainer(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException(id);
        var team = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(team, record);
        return team;
    }

    /// <summary>Everything a swap may change, as text, for every species and form of the games.</summary>
    private static List<string> Picture()
    {
        var all = new List<string>();
        foreach (var s in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
        {
            all.Add($"{s.Name}|{s.PrimaryType}|{s.SecondaryType}|{string.Join(",", s.Abilities)}|{string.Join(",", s.Learnset.Select(l => l.Level + ":" + l.MoveName))}");
            foreach (var f in s.Forms ?? new())
                all.Add($"{f.Name}|{string.Join(",", f.Types ?? new())}|{string.Join(",", f.Abilities ?? new())}|{string.Join(",", (f.Learnset ?? new()).Select(l => l.Level + ":" + l.MoveName))}");
        }
        return all;
    }

    private static void Swapped(RunRules run, Action check)
    {
        try
        {
            Randomizer.Use(run);
            check();
        }
        finally
        {
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    [Fact]
    public void ARunWithoutThemSwapsNothingAndTheOptionsAreKeptWithTheSave()
    {
        var before = Picture();
        Swapped(RunRules.Normal, () => Assert.Null(Randomizer.InForce));
        Swapped(RunRules.Normal with { Random = new RandomizerOptions { Seed = Seed } }, () => Assert.Null(Randomizer.InForce));
        Assert.Equal(before, Picture());

        // An older randomised save (V6) has none of them; a V7 one keeps them
        Assert.False(new RandomizerOptions().ChangesSpecies);
        var back = SaveManager.Read(SaveManager.Write(new SaveData { PlayerName = "Lucas", Run = Run(types: false) }))!;
        Assert.Equal(Run(types: false), back.Run);
        Assert.True(back.Run!.Random!.Abilities && back.Run.Random.Moves && !back.Run.Random.Types);
    }

    [Fact]
    public void TheSwapAndItsUndoLeaveTheDatabasesAsLoaded()
    {
        var before = Picture();
        List<string>? swapped = null;
        Swapped(Run(), () =>
        {
            Assert.Equal(Run().Random, Randomizer.InForce);
            swapped = Picture();
            int changed = before.Zip(swapped).Count(p => p.First != p.Second);
            Assert.True(changed > before.Count / 2, $"Only {changed} of {before.Count} species and forms changed.");
            // Used again with the same options: the same swap, from the data as loaded
            Randomizer.Use(Run());
            Assert.Equal(swapped, Picture());
        });
        Assert.Null(Randomizer.InForce);
        Assert.Equal(before, Picture());

        // Put back by itself too, and a second seed is another game
        Swapped(Run(seed: 1), () =>
        {
            Assert.NotEqual(swapped, Picture());
            Randomizer.PutBack();
            Assert.Equal(before, Picture());
        });
        Assert.Equal(before, Picture());
    }

    [Fact]
    public void ALineKeepsItsTypesAndAbilitiesTogetherAndAShapeKeepsItsOwn()
    {
        var turtwig = PokemonDatabase.Get("Turtwig")!;
        var arceus = PokemonDatabase.Get("Arceus")!;
        var shedinja = PokemonDatabase.Get("Shedinja")!;
        var (arceusType, arceusAbilities) = (arceus.PrimaryType, arceus.Abilities.ToList());
        Swapped(Run(moves: false), () =>
        {
            var random = Randomizer.Of(Run().Random! with { Moves = false });
            int line = Randomizer.LineOf("Turtwig");
            Assert.Equal(random.TypeIn(line, PokemonType.Grass), turtwig.PrimaryType);
            Assert.Equal(turtwig.PrimaryType, PokemonDatabase.Get("Grotle")!.PrimaryType);
            Assert.Equal(turtwig.PrimaryType, PokemonDatabase.Get("Torterra")!.PrimaryType);
            Assert.Equal(random.TypeIn(line, PokemonType.Ground), PokemonDatabase.Get("Torterra")!.SecondaryType);
            Assert.Equal(turtwig.Abilities, PokemonDatabase.Get("Grotle")!.Abilities);

            // Two types stay two, and nobody is given an ability that makes its own shape
            foreach (var s in PokemonDatabase.GetAll())
            {
                Assert.NotEqual(s.PrimaryType, s.SecondaryType);
                Assert.Equal(s.Abilities.Count, s.Abilities.Distinct().Count());
                foreach (string a in s.Abilities) Assert.NotNull(AbilityDatabase.Get(a));
            }
            Assert.DoesNotContain(PokemonDatabase.GetAll(), s => s.Name != "Shedinja" && s.Abilities.Contains("Wonder Guard"));

            // Arceus and Shedinja keep theirs
            Assert.Equal(arceusType, arceus.PrimaryType);
            Assert.Equal(arceusAbilities, arceus.Abilities);
            Assert.Contains("Wonder Guard", shedinja.Abilities);

            // A Pokémon made now has one of its species' new abilities and its types
            var made = new Pokemon(turtwig, 10, new Random(3));
            Assert.Contains(made.AbilityName, turtwig.Abilities);
            Assert.Equal(turtwig.PrimaryType, made.PrimaryType);
        });
    }

    [Fact]
    public void ALearnsetKeepsItsLevelsItsKindsAndAMoveOfTheSpeciesOwnType()
    {
        var before = PokemonDatabase.GetAll().ToDictionary(s => s.Name, s => (s.PrimaryType, s.SecondaryType, s.Learnset.ToList()));
        Swapped(Run(abilities: false), () =>
        {
            var random = Randomizer.Of(Run().Random! with { Abilities = false });
            int changedMoves = 0, total = 0;
            foreach (var s in PokemonDatabase.GetAll())
            {
                var (primary, secondary, was) = before[s.Name];
                Assert.Equal(was.Select(l => l.Level), s.Learnset.Select(l => l.Level));
                int line = Randomizer.LineOf(s.Name);
                // (a species whose ability makes its own shape keeps its types, and its moves their types)
                bool shaped = s.Abilities.Concat((s.Forms ?? new()).SelectMany(f => f.Abilities ?? new())).Any(Randomizer.ShapeAbilities.Contains);
                if (shaped) Assert.True(s.PrimaryType == primary && s.SecondaryType == secondary);
                for (int i = 0; i < was.Count; i++)
                {
                    if (!MoveDatabase.Has(was[i].MoveName)) continue;
                    var from = MoveDatabase.Get(was[i].MoveName).Under(Ruleset.Platinum);
                    var to = MoveDatabase.Get(s.Learnset[i].MoveName);
                    // Our own moves past level 100 are the line's climb, and stay
                    if (from.Id >= 3000) { Assert.Equal(from.Name, to.Name); continue; }
                    total++;
                    if (to.Name != from.Name) changedMoves++;
                    Assert.True(to.Id <= Randomizer.LastPlatinumMove || to.Type == PokemonType.Fairy, $"{s.Name} learns {to.Name}, a later move.");
                    Assert.Equal(from.Category == MoveCategory.Status, to.Under(Ruleset.Platinum).Category == MoveCategory.Status);
                    // A damaging move of the species' own type stays a move of that type, the line's shuffle moving it with the species
                    if (from.Category != MoveCategory.Status && (from.Type == primary || from.Type == secondary))
                        Assert.True(to.Under(Ruleset.Platinum).Type == (shaped ? from.Type : random.TypeIn(line, from.Type)),
                            $"{s.Name} learned {from.Name} ({from.Type}) and now learns {to.Name} ({to.Type}).");
                }
                // A move learned twice only where a type has run out of moves of its own
                Assert.True(s.Learnset.Count - s.Learnset.Select(l => l.MoveName).Distinct().Count() <= Math.Max(2, was.Count - was.Select(l => l.MoveName).Distinct().Count() + 2),
                    $"{s.Name} learns too many moves twice.");
            }
            Assert.True(changedMoves > total * 9 / 10, $"Only {changedMoves} of {total} moves changed.");
        });
    }

    [Fact]
    public void RoarksCopyKeepsARockMoveAndTheMapsTrainerKeepsHisTeam()
    {
        var random = Randomizer.Of(Run(abilities: false, types: false).Random!);
        var roark = Trainer("leader_roark");
        var original = roark.Party.Members.Select(p => p.Moves.Select(m => m.Data.Name).ToList()).ToList();
        var copy = random.ForTrainer(roark);

        Assert.NotSame(roark, copy);
        Assert.Equal(original, roark.Party.Members.Select(p => p.Moves.Select(m => m.Data.Name).ToList()));
        bool anyChanged = false;
        for (int i = 0; i < original.Count; i++)
        {
            var mine = copy.Party.Members[i];
            Assert.Equal(roark.Party.Members[i].Species.Name, mine.Species.Name);
            Assert.Equal(original[i].Count, mine.Moves.Count);
            var rocks = original[i].Count(m => MoveDatabase.Get(m).Type == PokemonType.Rock && MoveDatabase.Get(m).Category != MoveCategory.Status);
            Assert.Equal(rocks, mine.Moves.Count(m => m.Data.Type == PokemonType.Rock && m.Data.Category != MoveCategory.Status));
            Assert.All(mine.Moves, m => Assert.Equal(m.Data.MaxPP, m.CurrentPP));
            anyChanged |= !original[i].SequenceEqual(mine.Moves.Select(m => m.Data.Name));
        }
        Assert.True(anyChanged);
        Assert.Contains(copy.Party.Members, p => p.Moves.Any(m => m.Data.Type == PokemonType.Rock && m.Data.Category != MoveCategory.Status));
        // The same copy twice
        Assert.Equal(copy.Party.Members.SelectMany(p => p.Moves.Select(m => m.Data.Name)),
            random.ForTrainer(roark).Party.Members.SelectMany(p => p.Moves.Select(m => m.Data.Name)));
        // Without moves or abilities randomised the trainer is left as they are
        Assert.Same(roark, Randomizer.Of(Run(abilities: false, moves: false).Random!).ForTrainer(roark));
    }

    [Fact]
    public void ATrainersCopyTakesAnAbilityOfItsSpeciesSwappedAbilities()
    {
        Swapped(Run(moves: false, types: false), () =>
        {
            var random = Randomizer.Of(Run(moves: false, types: false).Random!);
            var copy = random.ForTrainer(Trainer("leader_roark"));
            Assert.All(copy.Party.Members, p => Assert.Contains(p.AbilityName, p.Abilities));
        });
    }

    [Fact]
    public void ThePanelTurnsEachOnAndTheRandomiserGoesWithTheLast()
    {
        var panel = new RunPanel { RollSeed = () => 77 };
        panel.Open();
        foreach (var row in new[] { RunRow.RandomAbilities, RunRow.RandomMoves, RunRow.RandomTypes })
        {
            for (int guard = 0; guard < 40 && panel.SelectedRow != row; guard++) panel.Move(1);
            Assert.Equal(row, panel.SelectedRow);
            panel.Change(1);
            Assert.Equal("on", panel.ValueText(row).ToLowerInvariant());
        }
        Assert.Equal(new RandomizerOptions { Seed = 77, Species = false, Items = false, Abilities = true, Moves = true, Types = true }, panel.Rules.Random);
        Assert.Contains(RunRow.Seed, panel.Rows);
        Assert.False(panel.Rules.IsNormal);
        foreach (var row in new[] { RunRow.RandomAbilities, RunRow.RandomMoves, RunRow.RandomTypes })
        {
            while (panel.SelectedRow != row) panel.Move(-1);
            panel.Change(-1);
        }
        Assert.Null(panel.Rules.Random);
        Assert.True(panel.Rules.IsNormal);
    }

    [Fact]
    public void ANewGameSwapsThemInAndANormalGameAfterItFindsTheDataAsLoaded()
    {
        var before = Picture();
        string folder = Path.Combine(Path.GetTempPath(), "randomizer-traits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(5);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame("Lucas", PlayerLook.Boy, "Barry", Run());
            Assert.Equal(Run().Random, Randomizer.InForce);
            Assert.NotEqual(before, Picture());

            engine.StartNewGame("Lucas", PlayerLook.Boy, "Barry", RunRules.Normal);
            Assert.Null(Randomizer.InForce);
            // (the game's own rules are the modern ones: the picture is taken again under Platinum's)
            Ruleset.Use(RulesPreset.Platinum);
            Assert.Equal(before, Picture());
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            RunRules.Current = RunRules.Normal;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
